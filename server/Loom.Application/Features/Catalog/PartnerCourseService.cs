using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Querying;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Common;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Catalog;

public sealed class PartnerCourseService(IAppDbContext db, ExchangeAccess access)
{
    private static readonly ListSpec<PartnerCourse, PartnerCourseResponse> List = ListSpec.For<PartnerCourse>()
        .SearchIn(c => c.Code, c => c.Name, c => c.NameHr)
        .SortBy("code", c => c.Code, isDefault: true)
        .SortBy("name", c => c.Name)
        .SortBy("nameHr", c => c.NameHr)
        .SortBy("semester", c => c.Semester)
        .SortBy("level", c => c.Level)
        .SortBy("ects", c => c.Ects)
        .Project(CatalogProjections.PartnerCourse);

    public async Task<PagedResponse<PartnerCourseResponse>> ListAsync(int institutionId, PartnerCourseListQuery query, CancellationToken ct)
    {
        var page = await db.PartnerCourses
            .AsNoTracking()
            .Where(c => c.InstitutionId == institutionId)
            .IncludeDeleted(query.IncludeDeleted)
            .WhereIf(query.Semester is not null, c => c.Semester == query.Semester)
            .WhereIf(query.Level is not null, c => c.Level == query.Level)
            .ToPageAsync(List, query, ct);

        var hasDeleted = await db.PartnerCourses.AnyAsync(c => c.InstitutionId == institutionId && c.IsDeleted, ct);
        return page with { HasDeleted = hasDeleted };
    }

    public async Task<ErrorOr<PartnerCourseResponse>> CreateAsync(int institutionId, PartnerCourseRequest request, CancellationToken ct)
    {
        if (!await db.Institutions.AnyAsync(i => i.Id == institutionId && i.Type == InstitutionType.Partner, ct))
            return CommonErrors.InstitutionNotFound;

        var course = new PartnerCourse { InstitutionId = institutionId };
        var applied = await ApplyAsync(course, request, ct);
        if (applied.IsError) return applied.Errors;

        db.PartnerCourses.Add(course);
        await db.SaveChangesAsync(ct);
        return course.ToResponse();
    }

    /// <summary>Courses of the exchange's partner institution, for whoever may work on the exchange (guests included).</summary>
    public async Task<ErrorOr<PagedResponse<PartnerCourseResponse>>> ListForExchangeAsync(Guid exchangeGuid, PartnerCourseListQuery query, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        return await ListAsync(context.Value.PartnerInstitutionId, query, ct);
    }

    /// <summary>Students (and guests) add missing courses while planning. The institution always comes from the exchange.</summary>
    public async Task<ErrorOr<PartnerCourseResponse>> CreateForExchangeAsync(Guid exchangeGuid, PartnerCourseRequest request, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        return await CreateAsync(context.Value.PartnerInstitutionId, request, ct);
    }

    public async Task<ErrorOr<PartnerCourseResponse>> UpdateAsync(int courseId, PartnerCourseRequest request, CancellationToken ct)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return CatalogErrors.CourseNotFound;

        var applied = await ApplyAsync(course, request, ct);
        if (applied.IsError) return applied.Errors;

        await db.SaveChangesAsync(ct);
        return course.ToResponse();
    }

    /// <summary>Soft-deletes courses that learning agreements or results use, removes the rest.</summary>
    public async Task<ErrorOr<Deleted>> DeleteAsync(int courseId, CancellationToken ct)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return CatalogErrors.CourseNotFound;

        if (await db.LearningAgreementEntries.AnyAsync(e => e.PartnerCourseId == courseId, ct)
            || await db.RecognitionEntries.AnyAsync(e => e.PartnerCourseId == courseId, ct))
            course.MarkDeleted();
        else
            db.PartnerCourses.Remove(course);

        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> RestoreAsync(int courseId, CancellationToken ct)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return CatalogErrors.CourseNotFound;

        course.Restore();
        await db.SaveChangesAsync(ct);
        return Result.Updated;
    }

    /// <summary>Where a course is used in learning agreements, grouped by programme, profile and home slot.</summary>
    public async Task<ErrorOr<PartnerCourseUsageResponse>> GetUsageAsync(int courseId, CancellationToken ct)
    {
        if (!await db.PartnerCourses.AnyAsync(c => c.Id == courseId, ct)) return CatalogErrors.CourseNotFound;

        var rows = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.PartnerCourseId == courseId && !e.IsDeleted)
            .Select(e => new
            {
                e.LearningAgreement.ExchangeId,
                e.LearningAgreement.Exchange.AcademicYear,
                ProgramName = e.LearningAgreement.Exchange.HomeProfile.Program.Name,
                ProfileName = e.LearningAgreement.Exchange.HomeProfile.Name,
                CourseIsvu = (int?)e.HomeSlot.Course!.IsvuCode,
                CourseName = e.HomeSlot.Course!.Name,
                GroupIsvu = e.HomeSlot.CourseGroup!.IsvuCode,
                GroupName = e.HomeSlot.CourseGroup!.Name,
                e.AwardedEcts,
            })
            .ToListAsync(ct);

        var groups = rows
            .GroupBy(r => new
            {
                r.ProgramName,
                r.ProfileName,
                IsvuCode = r.CourseIsvu ?? r.GroupIsvu,
                Name = r.CourseName ?? r.GroupName ?? string.Empty,
                IsCourseGroup = r.CourseName is null,
            })
            .Select(g => new PartnerCourseUsageGroup(
                g.Key.ProgramName,
                g.Key.ProfileName,
                g.Key.IsvuCode,
                g.Key.Name,
                g.Key.IsCourseGroup,
                g.Select(r => r.ExchangeId).Distinct().Count(),
                g.Sum(r => r.AwardedEcts ?? 0),
                g.Select(r => r.AcademicYear).Distinct().OrderDescending().ToList()))
            .OrderBy(g => g.ProgramName).ThenBy(g => g.ProfileName)
            .ToList();

        return new PartnerCourseUsageResponse(rows.Select(r => r.ExchangeId).Distinct().Count(), groups);
    }

    private async Task<ErrorOr<Success>> ApplyAsync(PartnerCourse course, PartnerCourseRequest request, CancellationToken ct)
    {
        var code = request.Code.NullIfBlank();
        var name = request.Name.NullIfBlank();
        if (code is null) return CatalogErrors.CourseCodeRequired;
        if (name is null) return CatalogErrors.CourseNameRequired;
        if (!Enum.TryParse<ExchangeSemester>(request.Semester, out var semester)) return CatalogErrors.InvalidSemester;
        if (!Enum.TryParse<StudyProgramLevel>(request.Level, out var level)) return CatalogErrors.InvalidLevel;

        var duplicate = await db.PartnerCourses.AnyAsync(
            c => c.InstitutionId == course.InstitutionId && c.Id != course.Id && c.Code == code, ct);
        if (duplicate) return CatalogErrors.DuplicateCode;

        course.Code = code;
        course.Name = name;
        course.NameHr = request.NameHr.NullIfBlank();
        course.Url = request.Url.NullIfBlank();
        course.Ects = request.Ects;
        course.LecturesH = request.LecturesH;
        course.AuditoryH = request.AuditoryH;
        course.LabH = request.LabH;
        course.Semester = semester;
        course.Level = level;
        return Result.Success;
    }
}

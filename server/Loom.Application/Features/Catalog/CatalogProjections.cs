using System.Linq.Expressions;
using Loom.Domain.Entities;

namespace Loom.Application.Features.Catalog;

/// <summary>Reusable SQL projections (and in-memory mappers for rows already loaded).</summary>
public static class CatalogProjections
{
    public static readonly Expression<Func<PartnerCourse, PartnerCourseResponse>> PartnerCourse = c => new PartnerCourseResponse(
        c.Id, c.Code, c.Name, c.NameHr, c.Url, c.Ects, c.LecturesH, c.AuditoryH, c.LabH,
        c.Semester, c.Level, c.IsDeleted);

    public static readonly Expression<Func<Institution, PartnerInstitutionAdminResponse>> PartnerInstitution = i => new PartnerInstitutionAdminResponse(
        i.Id, i.Name, i.NameHr, i.Country, i.City, i.ErasmusCode, i.PartnerCourses.Count, i.IsDeleted);

    public static readonly Expression<Func<HomeProfile, HomeProfileResponse>> HomeProfile = p => new HomeProfileResponse(p.Id, p.Name, p.NameEn);

    private static readonly Func<PartnerCourse, PartnerCourseResponse> PartnerCourseCompiled = PartnerCourse.Compile();
    private static readonly Func<HomeProfile, HomeProfileResponse> HomeProfileCompiled = HomeProfile.Compile();

    public static PartnerCourseResponse ToResponse(this PartnerCourse course) => PartnerCourseCompiled(course);
    public static HomeProfileResponse ToResponse(this HomeProfile profile) => HomeProfileCompiled(profile);

    /// <summary>"lectures/auditory/lab" hours, or null when none are known.</summary>
    public static string? Hours(this PartnerCourse course) =>
        course.LecturesH.HasValue || course.AuditoryH.HasValue || course.LabH.HasValue
            ? $"{course.LecturesH ?? 0}/{course.AuditoryH ?? 0}/{course.LabH ?? 0}"
            : null;
}

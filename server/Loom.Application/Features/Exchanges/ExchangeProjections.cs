using System.Linq.Expressions;
using Loom.Application.Features.Catalog;
using Loom.Domain.Entities;

namespace Loom.Application.Features.Exchanges;

public static class ExchangeProjections
{
    public static readonly Expression<Func<Exchange, ExchangeResponse>> Detail = e => new ExchangeResponse(
        e.Id,
        e.Guid,
        e.StudentId,
        e.Student.Name,
        e.Student.Jmbag,
        e.HomeProfile.Program.Institution.Name,
        e.HomeProfile.Program.Name,
        new HomeProfileResponse(e.HomeProfile.Id, e.HomeProfile.Name, e.HomeProfile.NameEn),
        e.PartnerInstitutionId,
        e.PartnerInstitution.Name,
        e.CoordinatorId,
        e.Coordinator != null ? e.Coordinator.Name : null,
        e.Student.Mentor,
        e.AcademicYear,
        e.SemesterType.ToString(),
        e.StudySemesters,
        e.CoordinatorMessage,
        e.EwpLink,
        e.Student.Email == "",
        e.CreatedAt,
        e.UpdatedAt);

    /// <summary>One row in a student's or coordinator's exchange list.</summary>
    public static readonly Expression<Func<Exchange, ExchangeSummaryResponse>> Summary = e => new ExchangeSummaryResponse(
        e.Id,
        e.Guid,
        e.StudentId,
        e.Student.Name,
        e.Student.Jmbag,
        e.PartnerInstitution.Name,
        e.HomeProfile.Program.Institution.Name,
        e.HomeProfile.Program.Name,
        e.HomeProfile.Name,
        e.AcademicYear,
        e.SemesterType.ToString(),
        e.LearningAgreement!.Status.ToString(),
        e.Recognition != null ? e.Recognition.Status.ToString() : null,
        e.EwpLink);
}

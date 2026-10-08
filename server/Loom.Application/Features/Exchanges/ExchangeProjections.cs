using System.Linq.Expressions;
using Loom.Application.DTOs.Exchange;
using Loom.Domain.Entities;

namespace Loom.Application.Features.Exchanges;

public static class ExchangeProjections
{
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

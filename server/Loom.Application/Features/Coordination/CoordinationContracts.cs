using Loom.Application.Common.Querying;

namespace Loom.Application.Features.Coordination;

public record CoordinatorOptionResponse(int Id, string Name);

public record StudentListQuery : ListQuery
{
    public string? AcademicYear { get; init; }
    /// <summary>Partner institution name.</summary>
    public string? PartnerInstitution { get; init; }
}

public record CoordinatorStudentResponse(
    int Id, string Name, string? Jmbag, string? InstitutionName, bool IsPlaceholder, int? InstitutionId, bool IsMyStudent);

public record PlaceholderStudentRequest(string Name, string Jmbag, int InstitutionId);

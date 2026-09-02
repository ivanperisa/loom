namespace Loom.Application.DTOs.Coordinator;

public record CoordinatorStudentFilterRequest
{
    public string? AcademicYear { get; init; }
    public string? PartnerInstitution { get; init; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(AcademicYear) && string.IsNullOrWhiteSpace(PartnerInstitution);
}

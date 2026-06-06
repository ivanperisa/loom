namespace Loom.Application.DTOs.Coordinator;

public record CoordinatorStudentResponse(
    int Id,
    string Name,
    string? Jmbag,
    string? InstitutionName,
    bool IsPlaceholder
);

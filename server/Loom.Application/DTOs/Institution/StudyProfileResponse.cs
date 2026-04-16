namespace Loom.Application.DTOs.Institution;

public record StudyProfileResponse(
    Guid Id,
    string Name,
    string? NameEn
);

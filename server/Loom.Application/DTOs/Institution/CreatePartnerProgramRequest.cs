namespace Loom.Application.DTOs.Institution;

public record CreatePartnerProgramRequest(
    string Name,
    string? NameEn,
    string Level
);

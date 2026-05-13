namespace Loom.Application.DTOs.Institution;

public record HomeProgramResponse(
    int Id,
    string Name,
    string? NameEn,
    string Level,
    int DurationSemesters,
    List<HomeProfileResponse> Profiles
);

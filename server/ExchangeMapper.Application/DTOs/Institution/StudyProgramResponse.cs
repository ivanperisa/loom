namespace ExchangeMapper.Application.DTOs.Institution;

public record StudyProgramResponse(
    Guid Id,
    string Name,
    string? NameEn,
    string Level,
    int DurationSemesters,
    List<StudyProfileResponse> Profiles
);

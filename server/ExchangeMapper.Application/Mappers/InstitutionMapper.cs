using ExchangeMapper.Application.DTOs.Course;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Mappers;

public static class InstitutionMapper
{
    public static InstitutionResponse ToResponse(this Institution institution) => new()
    {
        Id = institution.Id,
        Name = institution.Name,
        NameEn = institution.NameEn,
        Country = institution.Country,
        City = institution.City,
        ErasmusCode = institution.ErasmusCode
    };

    public static StudyProgramResponse ToResponse(this StudyProgram program) => new()
    {
        Id = program.Id,
        Name = program.Name,
        NameEn = program.NameEn,
        Level = program.Level.ToString(),
        DurationSemesters = program.DurationSemesters,
        IscedCode = program.IscedCode
    };

    public static StudyProfileResponse ToResponse(this StudyProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        NameEn = profile.NameEn,
        ExchangeSemesters = profile.ExchangeSemesters,
        ExchangeSemesterType = profile.ExchangeSemesterType,
        ExchangeSpots = profile.ExchangeSpots
    };

    public static CourseResponse ToResponse(this Course course) => new()
    {
        Id = course.Id,
        Code = course.Code,
        Name = course.Name,
        NameEn = course.NameEn,
        Ects = course.Ects,
        Type = course.Type.ToString(),
        Status = course.Status.ToString(),
        LecturesHours = course.LecturesHours,
        AuditoryHours = course.AuditoryHours,
        LabHours = course.LabHours
    };

    public static UserInstitutionResponse ToResponse(this UserInstitution ui) => new()
    {
        UserInstitutionId = ui.Id,
        HasActiveExchanges = ui.Exchanges.Count > 0,
        Institution = ui.Institution.ToResponse(),
        StudyProgram = ui.StudyProfile?.StudyProgram?.ToResponse(),
        StudyProfile = ui.StudyProfile?.ToResponse()
    };
}

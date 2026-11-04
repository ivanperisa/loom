namespace Loom.Application.Features.Completion;

/// <summary>Table 2: grades are per partner course (a course split over several slots has one grade).</summary>
public record SaveGradesRequest(List<CourseGradesRequest> Entries);

public record CourseGradesRequest(
    int PartnerCourseId,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate);

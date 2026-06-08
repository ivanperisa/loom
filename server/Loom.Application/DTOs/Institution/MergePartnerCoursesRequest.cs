namespace Loom.Application.DTOs.Institution;

public record MergePartnerCoursesRequest(
    int PrimaryCourseId,
    List<int> DuplicateCourseIds
);

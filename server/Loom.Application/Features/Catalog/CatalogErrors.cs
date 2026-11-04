using ErrorOr;

namespace Loom.Application.Features.Catalog;

public static class CatalogErrors
{
    public static Error InstitutionNameRequired => Error.Validation("INVALID_NAME", "Institution name is required.");
    public static Error CountryRequired => Error.Validation("INVALID_COUNTRY", "Country is required.");
    public static Error CourseNotFound => Error.NotFound("COURSE_NOT_FOUND", "Course not found.");
    public static Error CourseCodeRequired => Error.Validation("INVALID_CODE", "Course code is required.");
    public static Error CourseNameRequired => Error.Validation("INVALID_NAME", "Course name is required.");
    public static Error InvalidSemester => Error.Validation("INVALID_SEMESTER", "Invalid semester.");
    public static Error InvalidLevel => Error.Validation("INVALID_LEVEL", "Invalid study program level.");
    public static Error DuplicateCode => Error.Conflict("DUPLICATE_CODE", "A course with this code already exists for the institution.");
    public static Error InvalidMergeSet => Error.Validation("INVALID_MERGE_SET", "Invalid set of courses to merge.");
    public static Error MergeAcrossInstitutions => Error.Validation("INVALID_MERGE_SET", "Courses to merge must belong to the same institution.");
    public static Error PrimaryCourseNotFound => Error.NotFound("COURSE_NOT_FOUND", "Primary course not found.");
    public static Error MergeCourseNotFound => Error.NotFound("COURSE_NOT_FOUND", "One or more courses to merge were not found.");
    public static Error MergeConflict => Error.Conflict("MERGE_CONFLICT", "Some of these courses are mapped to the same slot in one exchange.");
}

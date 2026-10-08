using ErrorOr;

namespace Loom.Application.Features.Exchanges;

public static class ExchangeErrors
{
    public static Error AcademicYearRequired => Error.Validation("INVALID_ACADEMIC_YEAR", "Academic year is required.");
    public static Error InvalidSemesterType => Error.Validation("INVALID_SEMESTER_TYPE", "Invalid semester type.");
    public static Error InvalidStudySemesters => Error.Validation("INVALID_STUDY_SEMESTER", "Study semesters must be between 1 and 10.");
    public static Error OnlyCoordinatorsForOthers => Error.Forbidden("FORBIDDEN", "Only coordinators can create exchanges for other students.");
    public static Error NotYourStudent => Error.Forbidden("FORBIDDEN", "You are not the coordinator for this student.");
    public static Error TargetStudentNotFound => Error.NotFound("USER_NOT_FOUND", "Target student not found.");
    public static Error StudentNotFound => Error.NotFound("USER_NOT_FOUND", "Student not found.");
    public static Error HomeProfileNotFound => Error.NotFound("HOME_PROFILE_NOT_FOUND", "Home profile not found.");
    public static Error PartnerInstitutionNotFound => Error.NotFound("PARTNER_INSTITUTION_NOT_FOUND", "Partner institution not found.");
    public static Error SemesterHasEntries => Error.Conflict("LEARNING_AGREEMENT_HAS_ENTRIES",
        "Cannot change the semester: the learning agreement has courses mapped in a semester the new type does not cover. Remove those courses first.");
    public static Error NotDraft => Error.Conflict("NOT_DRAFT", "Only draft exchanges can be deleted.");
    public static Error OnlyCoordinatorMessage => Error.Forbidden("ACCESS_DENIED", "Only coordinators can update the message.");
    public static Error StudentRegistered => Error.Validation("STUDENT_REGISTERED", "This student signs in with an account.");
}

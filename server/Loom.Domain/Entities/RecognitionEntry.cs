using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class RecognitionEntry : EntityBase
{
    public int RecognitionId { get; set; }
    public Recognition Recognition { get; set; } = null!;

    public int LearningAgreementEntryId { get; set; }
    public LearningAgreementEntry LearningAgreementEntry { get; set; } = null!;

    public int? RecognizedAsCourseId { get; set; }
    public HomeCourse? RecognizedAsCourse { get; set; }

    public string? EnrollmentStatus { get; set; }
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public string? HrGrade { get; set; }
    public DateOnly? ExamDate { get; set; }
    public bool? IsRecognized { get; set; }
}

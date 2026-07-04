using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class MappingSchemeEntry : AuditableEntity
{
    public int ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;

    public int HomeSlotId { get; set; }
    public HomeSlot HomeSlot { get; set; } = null!;

    public int? PartnerCourseId { get; set; }
    public PartnerCourse? PartnerCourse { get; set; }

    public decimal? AwardedEcts { get; set; }

    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public string? HrGrade { get; set; }
    public DateOnly? ExamDate { get; set; }

    public bool? IsRecognized { get; set; }
    public int? RecognizedAsCourseId { get; set; }
    public HomeCourse? RecognizedAsCourse { get; set; }
}

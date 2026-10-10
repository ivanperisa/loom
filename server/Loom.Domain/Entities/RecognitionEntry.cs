using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

/// <summary>
/// Table 2: the result of one partner course on the exchange. One row per course, so a course split over several
/// slots still has one grade. Where it is recognised lives in <see cref="Placements"/> (the mapping scheme).
/// </summary>
public class RecognitionEntry : AuditableEntity
{
    public int RecognitionId { get; set; }
    public Recognition Recognition { get; set; } = null!;

    public int PartnerCourseId { get; set; }
    public PartnerCourse PartnerCourse { get; set; } = null!;

    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public string? HrGrade { get; set; }
    public DateOnly? ExamDate { get; set; }

    public ICollection<MappingSchemeEntry> Placements { get; set; } = [];
}

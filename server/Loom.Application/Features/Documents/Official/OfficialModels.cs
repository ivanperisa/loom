using Loom.Application.Features.Exchanges;
using Loom.Application.Features.Planning;

namespace Loom.Application.Features.Documents.Official;

/// <summary>Everything the official workbook shows, gathered once (see <see cref="OfficialDocumentService"/>).</summary>
public sealed record OfficialDocumentData(
    ExchangeResponse Exchange,
    List<HomeSlotResponse> Slots,
    LaSheet LearningAgreement,
    List<ResultLine> Agreed,
    ResultsSheet? Results,
    List<SignatureLine> Signatures);

/// <summary>The LA as of its latest approved version (or the draft, marked as such, before the first approval).</summary>
public sealed record LaSheet(int? VersionNo, Dictionary<int, GridSlot> Grid, List<ChangeLine> Changes);

/// <summary>Table 2 and the mapping scheme, present once final recognition started.</summary>
public sealed record ResultsSheet(List<ResultLine> Lines, Dictionary<int, GridSlot> Grid);

public sealed record GridSlot(string? Mode, List<GridLine> Lines);

public sealed record GridLine(string Code, string Name, string? NameHr, decimal Ects, bool Struck, string? Note);

public sealed record ChangeLine(string Amendment, bool Added, string Code, string Name, decimal? Ects, string SlotLabel);

/// <summary>A row of a recognition table (table 1 or table 2).</summary>
public sealed record ResultLine(
    int PartnerCourseId,
    string Code,
    string Name,
    string? NameHr,
    string? Hours,
    decimal CourseEcts,
    int? SlotCourseIsvu,
    string SlotCourseName,
    int? SlotGroupIsvu,
    string SlotGroupName,
    string SlotColor,
    int SlotSemester,
    decimal AwardedEcts,
    string? Status = null,
    string? OriginalGrade = null,
    string? EctsGrade = null,
    string? HrGrade = null,
    DateOnly? ExamDate = null);

public sealed record SignatureLine(string Document, int? VersionNo, string Status, string? ApprovedBy, DateTime? ApprovedAt);

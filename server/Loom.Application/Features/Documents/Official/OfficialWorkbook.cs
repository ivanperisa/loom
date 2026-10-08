using ClosedXML.Excel;

namespace Loom.Application.Features.Documents.Official;

/// <summary>
/// Renders the official xlsx: recognition results (table 2), agreed recognition (table 1), the learning agreement grid
/// with amendment marks and a changes table, the mapping scheme grid, and signatures. One layout, used by everyone.
/// </summary>
internal sealed class OfficialWorkbook(OfficialDocumentData data, OfficialText t)
{
    private const string Font = "Calibri";
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#D9D9D9");
    private static readonly XLColor NotPassedBg = XLColor.FromHtml("#FFCCCC");
    private static readonly XLColor CourseHeaderBg = XLColor.FromHtml("#FFFFCC");
    private static readonly XLColor GradeBg = XLColor.FromHtml("#DDD9C3");
    private static readonly XLColor Hairline = XLColor.FromHtml("#BFBFBF");
    private static readonly XLColor AtHomeOutline = XLColor.FromHtml("#4472C4");
    private static readonly XLColor Red = XLColor.FromHtml("#FF0000");
    private static readonly XLColor StruckRed = XLColor.FromHtml("#CC0000");

    public byte[] Build()
    {
        using var workbook = new XLWorkbook();
        if (data.Results is { } results)
            RecognitionSheet(workbook.AddWorksheet(t["sheetResults"]), results.Lines, t["sectionResults"], withGrades: true);
        RecognitionSheet(workbook.AddWorksheet(t["sheetAgreed"]), data.Agreed, t["sectionAgreed"], withGrades: false);
        LaSheet(workbook.AddWorksheet(t["sheetLa"]));
        if (data.Results is { } scheme)
            GridSheet(workbook.AddWorksheet(t["sheetScheme"]), t["schemeTitle"], scheme.Grid);
        InfoSheet(workbook.AddWorksheet(t["sheetInfo"]));
        foreach (var sheet in workbook.Worksheets)
        {
            // Printable as is: landscape, one page wide.
            sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
            sheet.PageSetup.FitToPages(1, 0);
            sheet.PageSetup.Margins.SetLeft(0.4).SetRight(0.4).SetTop(0.5).SetBottom(0.5);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ---------------------------------------------------------------- recognition tables

    private void RecognitionSheet(IXLWorksheet ws, List<ResultLine> lines, string sectionTitle, bool withGrades)
    {
        var ex = data.Exchange;
        void Info(int row, string label, string? value, bool red = false)
        {
            Cell(ws.Cell(row, 4), label, bold: true, halign: XLAlignmentHorizontalValues.Right, borders: false, color: red ? Red : null);
            Cell(ws.Cell(row, 5), value ?? "", borders: false);
        }
        Info(3, t["student"], ex.StudentName);
        Info(4, t["jmbag"], ex.StudentJmbag);
        Info(5, t["studyType"], t["studyTypeVal"]);
        Info(6, t["semester"], string.Join(", ", ex.StudySemesters.Order()));
        Info(7, t["university"], ex.PartnerInstitutionName, red: true);
        Cell(ws.Cell(8, 1), $"{t["profileLabel"]} {ex.HomeProfile.Name}", bold: true, size: 18, borders: false);
        Info(9, t["faculty"], "");
        Info(10, t["academicYear"], ex.AcademicYear);
        Info(11, t["exchSemester"], t[ex.SemesterType]);
        Info(12, t["mentor"], ex.Mentor);
        Cell(ws.Cell(14, 1), sectionTitle, italic: true, color: Red, borders: false);

        string[] headers = ["colPartnerCode", "colName", "colStatus", "colNameHr", "colHours", "colEcts", "colNo", "colRecognizedAs",
            "colSlotName", "colSlotCode", "colSlotCategory", "colSemester", "colAwarded"];
        string[] gradeHeaders = ["colOrigGrade", "colEctsGrade", "colHrGrade", "colDate"];
        var lastCol = withGrades ? 17 : 13;
        for (var i = 0; i < headers.Length; i++)
            Cell(ws.Cell(16, i + 1), t[headers[i]], bold: true, bg: CourseHeaderBg, wrap: true, halign: XLAlignmentHorizontalValues.Center);
        if (withGrades)
            for (var i = 0; i < gradeHeaders.Length; i++)
                Cell(ws.Cell(16, 14 + i), t[gradeHeaders[i]], bold: true, bg: GradeBg, wrap: true, halign: XLAlignmentHorizontalValues.Center);

        var row = 17;
        var categories = new List<(string Name, XLColor Color, decimal Ects)>();
        foreach (var course in lines.OrderBy(l => l.Name, StringComparer.CurrentCulture).GroupBy(l => l.PartnerCourseId))
        {
            var first = course.First();
            var notPassed = withGrades && course.Any(l => l.Status == "NotPassed");
            XLColor? plain = notPassed ? NotPassedBg : null;
            var partnerBg = notPassed ? NotPassedBg : XLColor.White;
            var gradeBg = notPassed ? NotPassedBg : GradeBg;
            var displayRows = GroupByElectiveGroup(course.ToList());
            var groupStart = row;

            for (var i = 0; i < displayRows.Count; i++)
            {
                var merged = displayRows[i];
                var entry = merged[0];
                var mergedEcts = merged.Sum(l => l.AwardedEcts);
                var slotBg = notPassed ? NotPassedBg : XLColor.FromHtml(entry.SlotColor);
                if (i == 0)
                {
                    Cell(ws.Cell(row, 1), first.Code, bg: partnerBg, bold: true, halign: XLAlignmentHorizontalValues.Center);
                    Cell(ws.Cell(row, 2), first.Name, bg: partnerBg, wrap: true);
                    Cell(ws.Cell(row, 3), withGrades && first.Status is { } status ? t[status] : "", bg: partnerBg);
                    Cell(ws.Cell(row, 4), first.NameHr, bg: partnerBg, wrap: true);
                    Cell(ws.Cell(row, 5), first.Hours, bg: partnerBg, halign: XLAlignmentHorizontalValues.Center);
                    Number(ws.Cell(row, 6), first.CourseEcts, bg: partnerBg, bold: true);
                    if (withGrades)
                    {
                        Cell(ws.Cell(row, 14), first.OriginalGrade, bg: gradeBg, halign: XLAlignmentHorizontalValues.Center);
                        Cell(ws.Cell(row, 15), first.EctsGrade, bg: gradeBg, halign: XLAlignmentHorizontalValues.Center);
                        Cell(ws.Cell(row, 16), first.HrGrade, bg: gradeBg, halign: XLAlignmentHorizontalValues.Center);
                        Cell(ws.Cell(row, 17), first.ExamDate?.ToString("dd/MM/yyyy"), bg: gradeBg);
                    }
                }
                else
                {
                    for (var c = 1; c <= 6; c++) Cell(ws.Cell(row, c), "", bg: partnerBg);
                    if (withGrades) for (var c = 14; c <= 17; c++) Cell(ws.Cell(row, c), "", bg: gradeBg);
                }

                Number(ws.Cell(row, 7), i + 1, bg: plain);
                Cell(ws.Cell(row, 8), entry.SlotCourseIsvu?.ToString(), bg: plain, halign: XLAlignmentHorizontalValues.Center);
                Cell(ws.Cell(row, 9), entry.SlotCourseName, bg: plain, wrap: true);
                Cell(ws.Cell(row, 10), entry.SlotGroupIsvu?.ToString(), bg: plain, halign: XLAlignmentHorizontalValues.Center);
                var category = string.IsNullOrEmpty(entry.SlotGroupName) ? t["mandatoryCourse"] : entry.SlotGroupName;
                Cell(ws.Cell(row, 11), category, bg: slotBg, wrap: true);
                Number(ws.Cell(row, 12), entry.SlotSemester, bg: plain);
                Number(ws.Cell(row, 13), mergedEcts, bg: slotBg, bold: true);

                if (!notPassed)
                {
                    var index = categories.FindIndex(c => c.Name == category);
                    if (index < 0) categories.Add((category, XLColor.FromHtml(entry.SlotColor), mergedEcts));
                    else categories[index] = categories[index] with { Ects = categories[index].Ects + mergedEcts };
                }
                row++;
            }

            if (displayRows.Count > 1)
            {
                int[] mergeColumns = withGrades ? [1, 2, 3, 4, 5, 6, 14, 15, 16, 17] : [1, 2, 3, 4, 5, 6];
                foreach (var c in mergeColumns) ws.Range(groupStart, c, row - 1, c).Merge();
            }
        }

        var total = lines.Where(l => !(withGrades && l.Status == "NotPassed")).Sum(l => l.AwardedEcts);
        Cell(ws.Cell(row, 1), t["total"], bold: true, bg: HeaderBg, halign: XLAlignmentHorizontalValues.Right);
        for (var c = 2; c <= lastCol; c++) Cell(ws.Cell(row, c), "", bg: HeaderBg);
        ws.Range(row, 1, row, 12).Merge();
        Number(ws.Cell(row, 13), total, bg: HeaderBg, bold: true);

        row += 2;
        var notesRow = row;
        Cell(ws.Cell(row, 2), t["notesTitle"], size: 8, bold: true, italic: true, color: Red, halign: XLAlignmentHorizontalValues.Right, borders: false);
        Cell(ws.Cell(row++, 3), t["notes1"], size: 8, italic: true, color: Red, borders: false);
        Cell(ws.Cell(row++, 3), t["notes2"], size: 8, italic: true, color: Red, borders: false);
        Cell(ws.Cell(row, 3), t["notes3"], size: 8, italic: true, color: Red, borders: false);

        var sumRow = notesRow;
        var sumCol = withGrades ? 14 : 12;
        foreach (var (name, color, ects) in categories)
        {
            Cell(ws.Cell(sumRow, sumCol), name, bg: color, size: 8, wrap: true);
            Number(ws.Cell(sumRow++, sumCol + 1), ects, bg: color, size: 8);
        }
        Cell(ws.Cell(sumRow, sumCol), t["total"], bg: HeaderBg, bold: true, size: 8);
        Number(ws.Cell(sumRow, sumCol + 1), total, bg: HeaderBg, bold: true, size: 8);

        double[] widths = [16, 49, 16, 59, 25, 6, 5, 12, 26, 10, 22, 8, 10, 14, 8, 8, 14];
        for (var c = 1; c <= lastCol; c++) ws.Column(c).Width = widths[c - 1];
        ws.Row(8).Height = 24;
        ws.Row(16).Height = 40;
    }

    /// <summary>Slots of one elective group (no own course) share a row; mandatory course slots get their own.</summary>
    private static List<List<ResultLine>> GroupByElectiveGroup(List<ResultLine> lines)
    {
        var rows = new List<List<ResultLine>>();
        var byGroup = new Dictionary<int, List<ResultLine>>();
        foreach (var line in lines)
        {
            if (line.SlotCourseIsvu is null && line.SlotGroupIsvu is int group)
            {
                if (byGroup.TryGetValue(group, out var existing)) { existing.Add(line); continue; }
                byGroup[group] = [line];
                rows.Add(byGroup[group]);
                continue;
            }
            rows.Add([line]);
        }
        return rows;
    }

    // ---------------------------------------------------------------- learning agreement

    private void LaSheet(IXLWorksheet ws)
    {
        var la = data.LearningAgreement;
        var title = la.VersionNo is int v
            ? t.Format("laTitleVersion", VersionStore.AmendmentLabel(v) is { } label ? $"{v} ({label})" : $"{v} ({t["original"]})")
            : t["laTitleDraft"];
        var row = GridSheet(ws, title, la.Grid);

        row += 2;
        Cell(ws.Cell(row, 1), t["changesTitle"], bold: true, size: 11, borders: false);
        row++;
        if (la.Changes.Count == 0)
        {
            Cell(ws.Cell(row, 1), t["noChanges"], italic: true, borders: false);
            ws.Range(row, 1, row, 8).Merge();
            return;
        }
        (string Key, int From, int To)[] columns =
            [("colAmendment", 1, 3), ("colChange", 4, 6), ("colPartnerCode", 7, 9), ("colName", 10, 19), ("colEcts", 20, 21), ("colHomeSlot", 22, 30)];
        foreach (var (key, from, to) in columns)
        {
            Cell(ws.Cell(row, from), t[key], bold: true, bg: HeaderBg, halign: XLAlignmentHorizontalValues.Center);
            ws.Range(row, from, row, to).Merge().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        foreach (var change in la.Changes)
        {
            row++;
            string?[] values = [change.Amendment, change.Added ? t["added"] : t["removed"], change.Code, change.Name, change.Ects?.ToString("0.#"), change.SlotLabel];
            for (var i = 0; i < columns.Length; i++)
            {
                Cell(ws.Cell(row, columns[i].From), values[i], color: change.Added ? null : StruckRed, wrap: true);
                ws.Range(row, columns[i].From, row, columns[i].To).Merge().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }
    }

    /// <summary>The 4 × 30 programme grid: each slot spans its ECTS, listing the courses placed in it. Returns the last row used.</summary>
    private int GridSheet(IXLWorksheet ws, string title, Dictionary<int, GridSlot> grid)
    {
        const int columns = 30;
        Cell(ws.Cell(1, 1), title, bold: true, size: 11, borders: false);
        Cell(ws.Cell(2, 1), data.Exchange.HomeProfile.Name, borders: false);
        Cell(ws.Cell(3, 1), t["semesterHeader"], bold: true, bg: HeaderBg, halign: XLAlignmentHorizontalValues.Center);
        for (var p = 1; p <= columns; p++) Number(ws.Cell(3, p + 1), p, bold: true, bg: HeaderBg, size: 8);

        var cursor = 4;
        foreach (var semester in data.Slots.Select(s => s.Semester).Distinct().Order())
        {
            var slots = data.Slots.Where(s => s.Semester == semester).OrderBy(s => s.SlotPosition).ToList();
            var maxLines = slots.Max(s => grid.GetValueOrDefault(s.Id)?.Lines.Count ?? 0);
            var rows = 2 + maxLines * 2;

            Number(ws.Cell(cursor, 1), semester, bold: true, bg: HeaderBg);
            ws.Range(cursor, 1, cursor + rows - 1, 1).Merge().Style.Fill.BackgroundColor = HeaderBg;

            foreach (var slot in slots)
            {
                var state = grid.GetValueOrDefault(slot.Id);
                var from = slot.SlotPosition + 1;
                var to = from + Math.Max(1, slot.Ects) - 1;
                var range = ws.Range(cursor, from, cursor + rows - 1, to);
                range.Style.Fill.BackgroundColor = XLColor.FromHtml(slot.Color);
                range.Style.Border.OutsideBorder = state?.Mode == "AtHome" ? XLBorderStyleValues.Medium : XLBorderStyleValues.Thin;
                range.Style.Border.OutsideBorderColor = state?.Mode == "AtHome" ? AtHomeOutline : Hairline;

                var code = (slot.CourseIsvuCode ?? slot.CourseGroupIsvuCode)?.ToString();
                var name = slot.CourseName ?? slot.CourseGroupName ?? "";
                SlotRow(ws, cursor, from, to, code ?? name, bold: code is not null);
                SlotRow(ws, cursor + 1, from, to, code is null ? "" : name);

                var lines = state?.Lines ?? [];
                for (var i = 0; i < maxLines; i++)
                {
                    var codeRow = cursor + 2 + i * 2;
                    if (i >= lines.Count)
                    {
                        SlotRow(ws, codeRow, from, to, "");
                        SlotRow(ws, codeRow + 1, from, to, "");
                        continue;
                    }
                    var line = lines[i];
                    var color = line.Struck ? StruckRed : null;
                    SlotRow(ws, codeRow, from, to, line.Note is null ? line.Code : $"{line.Code}  [{line.Note}]", bold: true, struck: line.Struck, color: color);
                    var details = string.Join("\n", new[] { line.Name, line.NameHr, $"{line.Ects:0.#} ECTS" }.Where(x => !string.IsNullOrEmpty(x)));
                    SlotRow(ws, codeRow + 1, from, to, details, struck: line.Struck, color: color);
                    ws.Range(codeRow + 1, from, codeRow + 1, to).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    ws.Range(codeRow + 1, from, codeRow + 1, to).Style.Border.BottomBorderColor = Hairline;
                }
            }

            ws.Row(cursor).Height = 14;
            ws.Row(cursor + 1).Height = 28;
            for (var i = 0; i < maxLines; i++)
            {
                ws.Row(cursor + 2 + i * 2).Height = 14;
                ws.Row(cursor + 3 + i * 2).Height = 46;
            }
            cursor += rows;
        }

        cursor++;
        Cell(ws.Cell(cursor, 2), "", bg: AtHomeOutline);
        Cell(ws.Cell(cursor, 3), t["atHome"], borders: false, size: 9);
        ws.Range(cursor, 3, cursor, 8).Merge();

        ws.Column(1).Width = 9;
        for (var c = 2; c <= columns + 1; c++) ws.Column(c).Width = 5.5;
        return cursor;
    }

    private static void SlotRow(IXLWorksheet ws, int row, int from, int to, string text, bool bold = false, bool struck = false, XLColor? color = null)
    {
        var cell = ws.Cell(row, from);
        cell.Value = text;
        var range = ws.Range(row, from, row, to);
        if (to > from) range.Merge();
        var style = range.Style;
        style.Font.FontName = Font;
        style.Font.FontSize = 9;
        style.Font.Bold = bold;
        style.Font.Strikethrough = struck;
        style.Font.FontColor = color ?? XLColor.Black;
        style.Alignment.WrapText = true;
        style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    }

    // ---------------------------------------------------------------- signatures

    private void InfoSheet(IXLWorksheet ws)
    {
        var ex = data.Exchange;
        var row = 1;
        void Pair(string label, string? value)
        {
            Cell(ws.Cell(row, 1), label, bold: true, borders: false);
            Cell(ws.Cell(row++, 2), value, borders: false);
        }
        Pair(t["student"], ex.StudentName);
        Pair(t["jmbag"], ex.StudentJmbag);
        Pair(t["university"], ex.PartnerInstitutionName);
        Pair(t["academicYear"], ex.AcademicYear);
        Pair(t["infoCoordinator"], ex.CoordinatorName);
        row++;

        string[] headers = ["infoDocument", "infoVersion", "infoStatus", "infoApprovedBy", "infoApprovedAt"];
        for (var i = 0; i < headers.Length; i++) Cell(ws.Cell(row, i + 1), t[headers[i]], bold: true, bg: HeaderBg);
        foreach (var line in data.Signatures)
        {
            row++;
            Cell(ws.Cell(row, 1), t[line.Document]);
            Cell(ws.Cell(row, 2), line.VersionNo is int v ? (VersionStore.AmendmentLabel(v) is { } label ? $"{v} ({label})" : v.ToString()) : "");
            Cell(ws.Cell(row, 3), t[line.Status]);
            Cell(ws.Cell(row, 4), line.ApprovedBy);
            Cell(ws.Cell(row, 5), line.ApprovedAt?.ToString("dd.MM.yyyy. HH:mm") + (line.ApprovedAt is null ? "" : " UTC"));
        }
        row += 2;
        Pair(t["infoGenerated"], DateTime.UtcNow.ToString("dd.MM.yyyy. HH:mm") + " UTC");

        ws.Column(1).Width = 34;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 26;
        ws.Column(5).Width = 22;
    }

    // ---------------------------------------------------------------- cell helpers

    private static void Cell(IXLCell cell, string? value, XLColor? bg = null, bool bold = false, double size = 9, bool wrap = false,
        XLAlignmentHorizontalValues halign = XLAlignmentHorizontalValues.Left, XLColor? color = null, bool borders = true, bool italic = false)
    {
        cell.Value = value ?? "";
        Style(cell, bg, bold, size, wrap, halign, color, borders, italic);
    }

    private static void Number(IXLCell cell, decimal value, XLColor? bg = null, bool bold = false, double size = 9)
    {
        cell.Value = value;
        Style(cell, bg, bold, size, false, XLAlignmentHorizontalValues.Center, null, true, false);
    }

    private static void Style(IXLCell cell, XLColor? bg, bool bold, double size, bool wrap, XLAlignmentHorizontalValues halign,
        XLColor? color, bool borders, bool italic)
    {
        var style = cell.Style;
        style.Font.FontName = Font;
        style.Font.FontSize = size;
        style.Font.Bold = bold;
        style.Font.Italic = italic;
        style.Font.FontColor = color ?? XLColor.Black;
        style.Alignment.WrapText = wrap;
        style.Alignment.Horizontal = halign;
        style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        if (bg is not null) style.Fill.BackgroundColor = bg;
        if (borders)
        {
            style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            style.Border.OutsideBorderColor = Hairline;
        }
    }
}

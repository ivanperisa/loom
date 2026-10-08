namespace Loom.Application.Features.Documents.Official;

/// <summary>Labels of the official workbook, Croatian (the official language) and English.</summary>
internal sealed class OfficialText(string lang)
{
    public bool IsEnglish { get; } = lang == "en";

    private static readonly Dictionary<string, (string Hr, string En)> Labels = new()
    {
        ["student"] = ("Student:", "Student:"),
        ["jmbag"] = ("JMBAG:", "JMBAG:"),
        ["studyType"] = ("Studij (prediplomski/diplomski):", "Study (undergraduate/graduate):"),
        ["studyTypeVal"] = ("diplomski", "graduate"),
        ["semester"] = ("Semestar:", "Semester:"),
        ["university"] = ("Sveučilište razmjene:", "Exchange university:"),
        ["faculty"] = ("Fakultet razmjene:", "Exchange faculty:"),
        ["academicYear"] = ("Ak. god. razmjene:", "Academic year:"),
        ["exchSemester"] = ("Semestar razmjene (zimski/ljetni):", "Exchange semester (winter/summer):"),
        ["mentor"] = ("Mentor:", "Mentor:"),
        ["Winter"] = ("zimski", "winter"),
        ["Summer"] = ("ljetni", "summer"),
        ["Both"] = ("zimski i ljetni", "winter and summer"),
        ["sectionResults"] = ("Predmeti koji se priznaju za druge predmete/obveze iz nastavnog programa", "Courses recognised towards programme obligations"),
        ["sectionAgreed"] = ("Dogovoreno priznavanje prema odobrenom ugovoru o učenju (ne mijenja se nakon početka završnog priznavanja)", "Agreed recognition from the approved learning agreement (frozen when final recognition starts)"),
        ["total"] = ("UKUPNO", "TOTAL"),
        ["profileLabel"] = ("Profil:", "Profile:"),
        ["notesTitle"] = ("NAPOMENE:", "NOTES:"),
        ["notes1"] = ("U Learning Agreement, tablica B stavlja KATEGORIJE PREDMETA, ne pojedine predmete!", "In the Learning Agreement, Table B lists COURSE CATEGORIES, not individual courses!"),
        ["notes2"] = ("Za jezgrene i obvezne predmete mora biti 1:1 zamjena te se mora u tablici mapiranja navesti ime predmeta za kojeg se priznaje!", "Core and mandatory courses require a 1:1 substitution: the course being substituted must be listed!"),
        ["notes3"] = ("Poveznice zamijeniti stvarnim poveznicama na strane/domaće kolegije", "Replace links with actual links to partner/home courses"),
        ["sheetResults"] = ("Priznavanje", "Recognition"),
        ["sheetAgreed"] = ("Dogovoreno priznavanje", "Agreed recognition"),
        ["sheetLa"] = ("Ugovor o učenju", "Learning agreement"),
        ["sheetScheme"] = ("Shema priznavanja", "Mapping scheme"),
        ["sheetInfo"] = ("Potpisi", "Signatures"),
        ["colPartnerCode"] = ("Šifra predmeta", "Course code"),
        ["colName"] = ("Naziv (engleski)", "Name (English)"),
        ["colStatus"] = ("Status predmeta", "Course status"),
        ["colNameHr"] = ("Naziv (hrvatski)", "Name (Croatian)"),
        ["colHours"] = ("Sati u obliku:\nPredavanja/Auditorne/\nlaboratorijske vježbe (P/A/L)", "Hours:\nLectures/Auditory/\nLaboratory (L/A/L)"),
        ["colEcts"] = ("ECTS", "ECTS"),
        ["colNo"] = ("Rbr.", "No."),
        ["colRecognizedAs"] = ("Priznaje se za predmet", "Recognised as"),
        ["colSlotName"] = ("Naziv", "Name"),
        ["colSlotCode"] = ("Izb. grupa", "Elective group"),
        ["colSlotCategory"] = ("Naziv izb. grupe", "Elective group name"),
        ["mandatoryCourse"] = ("Obavezan predmet", "Mandatory course"),
        ["colSemester"] = ("Semestar", "Semester"),
        ["colAwarded"] = ("Priznato ECTS-a", "Awarded ECTS"),
        ["colOrigGrade"] = ("Ocjena\noriginalna", "Original\ngrade"),
        ["colEctsGrade"] = ("Ocjena\nECTS\n(F-A)", "ECTS\ngrade\n(F-A)"),
        ["colHrGrade"] = ("Ocjena\nhrv.\n(1-5)", "Croatian\ngrade\n(1-5)"),
        ["colDate"] = ("Datum polaganja", "Exam date"),
        ["Passed"] = ("Položeno", "Passed"),
        ["NotPassed"] = ("Nepoloženo", "Not passed"),
        ["atHome"] = ("Položeno na FER-u", "Taken at the home institution"),
        ["semesterHeader"] = ("Semestar", "Semester"),
        ["laTitleVersion"] = ("Ugovor o učenju, odobrena verzija {0}", "Learning agreement, approved version {0}"),
        ["laTitleDraft"] = ("Ugovor o učenju — NACRT (još nije odobren)", "Learning agreement — DRAFT (not approved yet)"),
        ["original"] = ("izvorni ugovor", "original agreement"),
        ["removedIn"] = ("uklonjeno u {0}", "removed in {0}"),
        ["changesTitle"] = ("Izmjene ugovora o učenju", "Changes to the learning agreement"),
        ["colAmendment"] = ("Izmjena", "Amendment"),
        ["colChange"] = ("Promjena", "Change"),
        ["added"] = ("Dodano", "Added"),
        ["removed"] = ("Uklonjeno", "Removed"),
        ["colHomeSlot"] = ("Mjesto u programu", "Programme slot"),
        ["noChanges"] = ("Nema izmjena.", "No changes."),
        ["schemeTitle"] = ("Shema priznavanja (konačni raspored)", "Mapping scheme (final placement)"),
        ["infoDocument"] = ("Dokument", "Document"),
        ["infoVersion"] = ("Verzija", "Version"),
        ["infoStatus"] = ("Status", "Status"),
        ["infoApprovedBy"] = ("Odobrio/la", "Approved by"),
        ["infoApprovedAt"] = ("Datum odobrenja", "Approved on"),
        ["Approved"] = ("Odobreno", "Approved"),
        ["Draft"] = ("Nacrt", "Draft"),
        ["infoStarted"] = ("Završno priznavanje započeto", "Final recognition started"),
        ["infoGenerated"] = ("Izrađeno", "Generated"),
        ["infoCoordinator"] = ("Koordinator", "Coordinator"),
        ["la"] = ("Ugovor o učenju", "Learning agreement"),
        ["recognition"] = ("Priznavanje", "Recognition"),
        ["notStarted"] = ("nije započeto", "not started"),
    };

    public string this[string key] => Labels.TryGetValue(key, out var label) ? (IsEnglish ? label.En : label.Hr) : key;

    public string Format(string key, object value) => string.Format(this[key], value);

    /// <summary>Amendment n of the LA (version n+1): "I1" in Croatian (izmjena), "A1" in English. Null for the original.</summary>
    public string? Amendment(int? versionNo) => versionNo is > 1 ? $"{(IsEnglish ? "A" : "I")}{versionNo - 1}" : null;
}

namespace Loom.Infrastructure.Migrations;

/// <summary>
/// Data steps of the <see cref="DocumentVersions"/> migration. Plain SQL (no temp tables), so the idempotent deploy
/// script can run it inside its DO blocks.
/// </summary>
internal static class DocumentVersionsData
{
    /// <summary>
    /// exchange.snapshot → exchange.document_version. Approvals ("Auto") get numbers 1, 2, … per document in date order;
    /// "PreImport" snapshots become backups. LA payloads keep their JSON (same property names); their hash is computed
    /// exactly like the application does (sorted "slot|mode|course|ects" rows, SHA-256), so "nothing changed" works on
    /// old versions too. Recognition snapshots stored labels only: they come over as schema 0 (listed, no diff).
    /// </summary>
    public const string CopySnapshots = """
        INSERT INTO exchange.document_version (exchange_id, document, kind, version_no, schema_version, payload, content_hash, created_by_id, created_at)
        SELECT s.exchange_id,
               s.phase,
               CASE WHEN s.type = 'Auto' THEN 'Approved' ELSE 'Backup' END,
               CASE WHEN s.type = 'Auto'
                    THEN row_number() OVER (PARTITION BY s.exchange_id, s.phase, s.type ORDER BY s.created_at, s.id) END,
               CASE WHEN s.phase = 'LearningAgreement' THEN 1 ELSE 0 END,
               s.snapshot,
               md5(s.snapshot::text),
               s.changed_by_id,
               s.created_at
        FROM exchange.snapshot s;

        UPDATE exchange.document_version d
        SET content_hash = coalesce(
            (SELECT encode(sha256(convert_to(string_agg(r.line, E'\n' ORDER BY r.line COLLATE "C"), 'UTF8')), 'hex')
             FROM (SELECT concat_ws('|',
                              e->>'homeSlotId',
                              e->>'mode',
                              coalesce(e->>'partnerCourseId', ''),
                              coalesce(to_char((e->>'awardedEcts')::numeric, 'FM9999990.0'), '')) AS line
                   FROM jsonb_array_elements(d.payload->'entries') e) r),
            encode(sha256(''::bytea), 'hex'))
        WHERE d.document = 'LearningAgreement';
        """;

    // Each course's continuous runs of approved versions ("islands"): present in versions a..b.
    private const string Islands = """
        WITH presence AS (
            SELECT DISTINCT ON (d.exchange_id, d.version_no, (e->>'homeSlotId')::int, (e->>'partnerCourseId')::int)
                   d.exchange_id, d.version_no,
                   (e->>'homeSlotId')::int AS slot,
                   (e->>'partnerCourseId')::int AS course,
                   (e->>'awardedEcts')::numeric AS ects
            FROM exchange.document_version d, jsonb_array_elements(d.payload->'entries') e
            WHERE d.document = 'LearningAgreement' AND d.kind = 'Approved' AND e->>'partnerCourseId' IS NOT NULL
        ),
        latest AS (
            SELECT exchange_id, max(version_no) AS maxv
            FROM exchange.document_version
            WHERE document = 'LearningAgreement' AND kind = 'Approved'
            GROUP BY exchange_id
        ),
        islands AS (
            SELECT g.exchange_id, g.slot, g.course, min(g.version_no) AS a, max(g.version_no) AS b
            FROM (SELECT p.*, p.version_no - row_number() OVER (PARTITION BY p.exchange_id, p.slot, p.course ORDER BY p.version_no) AS grp
                  FROM presence p) g
            GROUP BY g.exchange_id, g.slot, g.course, g.grp
        )
        """;

    /// <summary>
    /// Amendment numbers used to be recomputed from all snapshots on every read; now they are stored per component.
    /// A live component that is in the latest approval gets the version its current run started in. Earlier runs, and
    /// components that are gone, come back as removed rows (removed in b+1, or pending removal if gone only in the draft),
    /// which is what the old read-time replay showed.
    /// </summary>
    public const string BackfillEntryVersions = Islands + """
        UPDATE exchange.learning_agreement_entry le
        SET added_in_version = i.a
        FROM islands i
        JOIN latest l ON l.exchange_id = i.exchange_id
        JOIN exchange.learning_agreement la ON la.exchange_id = i.exchange_id
        WHERE le.learning_agreement_id = la.id
          AND le.home_slot_id = i.slot
          AND le.partner_course_id = i.course
          AND i.b = l.maxv;
        """ + "\n" + Islands + """
        INSERT INTO exchange.learning_agreement_entry
            (learning_agreement_id, home_slot_id, mode, partner_course_id, awarded_ects, is_deleted, added_in_version, removed_in_version, created_at)
        SELECT la.id, i.slot, 'AtExchange', i.course, p.ects, true, i.a,
               CASE WHEN i.b = l.maxv THEN NULL ELSE i.b + 1 END,
               now()
        FROM islands i
        JOIN latest l ON l.exchange_id = i.exchange_id
        JOIN exchange.learning_agreement la ON la.exchange_id = i.exchange_id
        JOIN presence p ON p.exchange_id = i.exchange_id AND p.version_no = i.b AND p.slot = i.slot AND p.course = i.course
        WHERE NOT (i.b = l.maxv AND EXISTS (
                SELECT 1 FROM exchange.learning_agreement_entry le
                WHERE le.learning_agreement_id = la.id AND le.home_slot_id = i.slot AND le.partner_course_id = i.course))
          AND EXISTS (SELECT 1 FROM home.slot s WHERE s.id = i.slot)
          AND EXISTS (SELECT 1 FROM partner.course c WHERE c.id = i.course);
        """;

    /// <summary>
    /// Grades lived in recognition_entry until the first grade save copied the LA into mapping_scheme_entry. Where that
    /// copy never happened, make it now, with the grades. (Where it did, mapping_scheme_entry already has them.)
    /// </summary>
    public const string MoveRecognitionGrades = """
        INSERT INTO exchange.mapping_scheme_entry
            (exchange_id, home_slot_id, partner_course_id, awarded_ects, enrollment_status, original_grade, ects_grade, hr_grade,
             exam_date, is_recognized, recognized_as_course_id, created_at, updated_at)
        SELECT la.exchange_id, le.home_slot_id, le.partner_course_id, le.awarded_ects,
               CASE WHEN re.enrollment_status IN ('Passed', 'NotPassed') THEN re.enrollment_status END,
               re.original_grade, re.ects_grade, re.hr_grade, re.exam_date, re.is_recognized, re.recognized_as_course_id,
               now(), now()
        FROM exchange.learning_agreement_entry le
        JOIN exchange.learning_agreement la ON la.id = le.learning_agreement_id
        LEFT JOIN exchange.recognition_entry re ON re.learning_agreement_entry_id = le.id
        WHERE le.partner_course_id IS NOT NULL
          AND NOT le.is_deleted
          AND NOT EXISTS (SELECT 1 FROM exchange.mapping_scheme_entry m WHERE m.exchange_id = la.exchange_id)
          AND la.exchange_id IN (
              SELECT la2.exchange_id
              FROM exchange.recognition_entry r
              JOIN exchange.learning_agreement_entry e2 ON e2.id = r.learning_agreement_entry_id
              JOIN exchange.learning_agreement la2 ON la2.id = e2.learning_agreement_id
              WHERE r.enrollment_status IS NOT NULL OR r.original_grade IS NOT NULL OR r.ects_grade IS NOT NULL
                 OR r.hr_grade IS NOT NULL OR r.exam_date IS NOT NULL);
        """;

    /// <summary>Exchanges that already have results started final recognition implicitly (on the first grade save).</summary>
    public const string MarkStartedRecognitions = """
        UPDATE exchange.learning_agreement la
        SET concluded_at = s.started
        FROM (SELECT exchange_id, min(created_at) AS started FROM exchange.mapping_scheme_entry GROUP BY exchange_id) s
        WHERE la.exchange_id = s.exchange_id AND la.concluded_at IS NULL;

        INSERT INTO exchange.recognition (exchange_id, status, created_at, updated_at)
        SELECT la.exchange_id, 'Draft', now(), now()
        FROM exchange.learning_agreement la
        WHERE la.concluded_at IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM exchange.recognition r WHERE r.exchange_id = la.exchange_id);
        """;

    /// <summary>Submit/Reject were never used by the UI; any such document is a draft.</summary>
    public const string DropSubmittedAndRejected = """
        UPDATE exchange.learning_agreement SET status = 'Draft' WHERE status IN ('Submitted', 'Rejected');
        UPDATE exchange.recognition SET status = 'Draft' WHERE status IN ('Submitted', 'Rejected');
        """;
}

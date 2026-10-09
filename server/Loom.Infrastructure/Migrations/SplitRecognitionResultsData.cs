namespace Loom.Infrastructure.Migrations;

/// <summary>Data steps of the <see cref="SplitRecognitionResults"/> migration. Plain SQL, like <see cref="DocumentVersionsData"/>.</summary>
internal static class SplitRecognitionResultsData
{
    /// <summary>"Final recognition started" moves from the LA to the recognition, which is created where it is missing.</summary>
    public const string MoveStarted = """
        UPDATE exchange.recognition r
        SET started_at = la.concluded_at, started_by = la.concluded_by
        FROM exchange.learning_agreement la
        WHERE la.exchange_id = r.exchange_id AND la.concluded_at IS NOT NULL;

        INSERT INTO exchange.recognition (exchange_id, status, started_at, started_by, created_at, updated_at)
        SELECT la.exchange_id, 'Draft', la.concluded_at, la.concluded_by, now(), now()
        FROM exchange.learning_agreement la
        WHERE la.concluded_at IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM exchange.recognition r WHERE r.exchange_id = la.exchange_id);

        INSERT INTO exchange.recognition (exchange_id, status, started_at, created_at, updated_at)
        SELECT m.exchange_id, 'Draft', min(m.created_at), now(), now()
        FROM exchange.mapping_scheme_entry m
        WHERE NOT EXISTS (SELECT 1 FROM exchange.recognition r WHERE r.exchange_id = m.exchange_id)
        GROUP BY m.exchange_id;

        UPDATE exchange.recognition r
        SET started_at = s.started
        FROM (SELECT exchange_id, min(created_at) AS started FROM exchange.mapping_scheme_entry GROUP BY exchange_id) s
        WHERE r.exchange_id = s.exchange_id AND r.started_at IS NULL;
        """;

    /// <summary>
    /// One result per (exchange, course). Its rows carried copies of the same grade; where they differ, the most recently
    /// changed row that has a grade wins. Rows whose course was deleted (partner_course_id set to null) have no course to
    /// grade and are dropped.
    /// </summary>
    public const string CreateResults = """
        DELETE FROM exchange.mapping_scheme_entry WHERE partner_course_id IS NULL;

        INSERT INTO exchange.recognition_entry
            (recognition_id, partner_course_id, enrollment_status, original_grade, ects_grade, hr_grade, exam_date, created_at, updated_at)
        SELECT DISTINCT ON (m.exchange_id, m.partner_course_id)
               r.id, m.partner_course_id, m.enrollment_status, m.original_grade, m.ects_grade, m.hr_grade, m.exam_date,
               min(m.created_at) OVER w, max(m.updated_at) OVER w
        FROM exchange.mapping_scheme_entry m
        JOIN exchange.recognition r ON r.exchange_id = m.exchange_id
        WINDOW w AS (PARTITION BY m.exchange_id, m.partner_course_id)
        ORDER BY m.exchange_id, m.partner_course_id,
                 (m.enrollment_status IS NOT NULL OR m.original_grade IS NOT NULL OR m.ects_grade IS NOT NULL
                  OR m.hr_grade IS NOT NULL OR m.exam_date IS NOT NULL) DESC,
                 m.updated_at DESC, m.id DESC;
        """;

    /// <summary>The remaining rows become placements of their result; two placements of a course in one slot fold into one.</summary>
    public const string LinkPlacements = """
        UPDATE exchange.mapping_scheme_entry m
        SET recognition_entry_id = re.id
        FROM exchange.recognition_entry re
        JOIN exchange.recognition r ON r.id = re.recognition_id
        WHERE r.exchange_id = m.exchange_id AND re.partner_course_id = m.partner_course_id;

        UPDATE exchange.mapping_scheme_entry m
        SET awarded_ects = d.total
        FROM (SELECT min(id) AS keep, sum(coalesce(awarded_ects, 0)) AS total
              FROM exchange.mapping_scheme_entry
              GROUP BY recognition_entry_id, home_slot_id
              HAVING count(*) > 1) d
        WHERE m.id = d.keep;

        DELETE FROM exchange.mapping_scheme_entry m
        USING exchange.mapping_scheme_entry k
        WHERE k.recognition_entry_id = m.recognition_entry_id AND k.home_slot_id = m.home_slot_id AND k.id < m.id;

        UPDATE exchange.mapping_scheme_entry SET awarded_ects = 0 WHERE awarded_ects IS NULL OR awarded_ects < 0;
        """;

    /// <summary>Down: every placement gets its result's grades back, and the LA its "concluded" mark.</summary>
    public const string Restore = """
        UPDATE exchange.mapping_scheme_entry m
        SET exchange_id = r.exchange_id, partner_course_id = re.partner_course_id, enrollment_status = re.enrollment_status,
            original_grade = re.original_grade, ects_grade = re.ects_grade, hr_grade = re.hr_grade, exam_date = re.exam_date
        FROM exchange.recognition_entry re
        JOIN exchange.recognition r ON r.id = re.recognition_id
        WHERE re.id = m.recognition_entry_id;

        UPDATE exchange.learning_agreement la
        SET concluded_at = r.started_at, concluded_by = r.started_by
        FROM exchange.recognition r
        WHERE r.exchange_id = la.exchange_id;
        """;
}

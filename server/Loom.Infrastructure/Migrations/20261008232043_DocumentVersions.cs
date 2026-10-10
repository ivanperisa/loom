using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Loom.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 5 documents redesign: numbered document versions instead of free-form snapshots, amendment numbers stored
    /// per LA component, an explicit "start final recognition" step, one results dataset (mapping_scheme_entry), and
    /// only Draft/Approved statuses. Existing data is carried over; Down restores the schema but not the moved data.
    /// </summary>
    public partial class DocumentVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "added_in_version",
                schema: "exchange",
                table: "learning_agreement_entry",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "removed_in_version",
                schema: "exchange",
                table: "learning_agreement_entry",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "concluded_at",
                schema: "exchange",
                table: "learning_agreement",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "concluded_by",
                schema: "exchange",
                table: "learning_agreement",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_version",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: true),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_version_pkey", x => x.id);
                    table.CheckConstraint("document_version_document_check", "document IN ('LearningAgreement', 'Recognition')");
                    table.CheckConstraint("document_version_kind_check", "kind IN ('Approved', 'Backup')");
                    table.CheckConstraint("document_version_number_check", "(kind = 'Approved') = (version_no IS NOT NULL)");
                    table.ForeignKey(
                        name: "document_version_created_by_id_fkey",
                        column: x => x.created_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "document_version_exchange_id_fkey",
                        column: x => x.exchange_id,
                        principalSchema: "exchange",
                        principalTable: "exchange",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_learning_agreement_concluded_by",
                schema: "exchange",
                table: "learning_agreement",
                column: "concluded_by");

            migrationBuilder.CreateIndex(
                name: "document_version_number_key",
                schema: "exchange",
                table: "document_version",
                columns: new[] { "exchange_id", "document", "version_no" },
                unique: true,
                filter: "version_no IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_document_version_created_by_id",
                schema: "exchange",
                table: "document_version",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_version_exchange",
                schema: "exchange",
                table: "document_version",
                columns: new[] { "exchange_id", "document", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "learning_agreement_concluded_by_fkey",
                schema: "exchange",
                table: "learning_agreement",
                column: "concluded_by",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // ---- data: carry the old history and documents over (see DocumentVersionsData below)
            migrationBuilder.Sql(DocumentVersionsData.CopySnapshots);
            migrationBuilder.Sql(DocumentVersionsData.BackfillEntryVersions);
            migrationBuilder.Sql(DocumentVersionsData.MoveRecognitionGrades);
            migrationBuilder.Sql(DocumentVersionsData.MarkStartedRecognitions);
            migrationBuilder.Sql(DocumentVersionsData.DropSubmittedAndRejected);

            migrationBuilder.DropCheckConstraint(
                name: "recognition_status_check",
                schema: "exchange",
                table: "recognition");

            migrationBuilder.DropCheckConstraint(
                name: "learning_agreement_status_check",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.AddCheckConstraint(
                name: "recognition_status_check",
                schema: "exchange",
                table: "recognition",
                sql: "status IN ('Draft', 'Approved')");

            migrationBuilder.AddCheckConstraint(
                name: "learning_agreement_status_check",
                schema: "exchange",
                table: "learning_agreement",
                sql: "status IN ('Draft', 'Approved')");


            // ---- the old tables go last, after their data has moved
            migrationBuilder.DropTable(
                name: "recognition_entry",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "snapshot",
                schema: "exchange");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "learning_agreement_concluded_by_fkey",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.DropTable(
                name: "document_version",
                schema: "exchange");

            migrationBuilder.DropCheckConstraint(
                name: "recognition_status_check",
                schema: "exchange",
                table: "recognition");

            migrationBuilder.DropIndex(
                name: "idx_learning_agreement_concluded_by",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.DropCheckConstraint(
                name: "learning_agreement_status_check",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.DropColumn(
                name: "added_in_version",
                schema: "exchange",
                table: "learning_agreement_entry");

            migrationBuilder.DropColumn(
                name: "removed_in_version",
                schema: "exchange",
                table: "learning_agreement_entry");

            migrationBuilder.DropColumn(
                name: "concluded_at",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.DropColumn(
                name: "concluded_by",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.CreateTable(
                name: "recognition_entry",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    learning_agreement_entry_id = table.Column<int>(type: "integer", nullable: false),
                    recognition_id = table.Column<int>(type: "integer", nullable: false),
                    recognized_as_course_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ects_grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    enrollment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    exam_date = table.Column<DateOnly>(type: "date", nullable: true),
                    hr_grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_recognized = table.Column<bool>(type: "boolean", nullable: true),
                    original_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recognition_entry_pkey", x => x.id);
                    table.ForeignKey(
                        name: "recognition_entry_learning_agreement_entry_id_fkey",
                        column: x => x.learning_agreement_entry_id,
                        principalSchema: "exchange",
                        principalTable: "learning_agreement_entry",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recognition_entry_recognition_id_fkey",
                        column: x => x.recognition_id,
                        principalSchema: "exchange",
                        principalTable: "recognition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recognition_entry_recognized_as_course_id_fkey",
                        column: x => x.recognized_as_course_id,
                        principalSchema: "home",
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "snapshot",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    changed_by_id = table.Column<int>(type: "integer", nullable: false),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    phase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Auto")
                },
                constraints: table =>
                {
                    table.PrimaryKey("snapshot_pkey", x => x.id);
                    table.CheckConstraint("snapshot_phase_check", "phase IN ('LearningAgreement', 'Recognition')");
                    table.CheckConstraint("snapshot_type_check", "type IN ('Auto', 'PreImport')");
                    table.ForeignKey(
                        name: "snapshot_changed_by_id_fkey",
                        column: x => x.changed_by_id,
                        principalTable: "user",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "snapshot_exchange_id_fkey",
                        column: x => x.exchange_id,
                        principalSchema: "exchange",
                        principalTable: "exchange",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "recognition_status_check",
                schema: "exchange",
                table: "recognition",
                sql: "status IN ('Draft', 'Submitted', 'Approved', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "learning_agreement_status_check",
                schema: "exchange",
                table: "learning_agreement",
                sql: "status IN ('Draft', 'Submitted', 'Approved', 'Rejected')");

            migrationBuilder.CreateIndex(
                name: "idx_recognition_entry_recognition",
                schema: "exchange",
                table: "recognition_entry",
                column: "recognition_id");

            migrationBuilder.CreateIndex(
                name: "idx_recognition_entry_recognized_as_course_id",
                schema: "exchange",
                table: "recognition_entry",
                column: "recognized_as_course_id");

            migrationBuilder.CreateIndex(
                name: "recognition_entry_learning_agreement_entry_id_key",
                schema: "exchange",
                table: "recognition_entry",
                column: "learning_agreement_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_changed_by_id",
                schema: "exchange",
                table: "snapshot",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_created",
                schema: "exchange",
                table: "snapshot",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_exchange_created",
                schema: "exchange",
                table: "snapshot",
                columns: new[] { "exchange_id", "created_at" },
                descending: new[] { false, true });
        }
    }
}

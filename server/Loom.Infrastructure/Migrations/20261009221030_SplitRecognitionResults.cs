using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Loom.Infrastructure.Migrations
{
    /// <summary>
    /// Results get their own table: exchange.recognition_entry holds one result (status, grades) per partner course, and
    /// exchange.mapping_scheme_entry only places it (slot + ECTS). Before, every placement carried its own copy of the
    /// grade. "Final recognition started" moves from the LA to the recognition. Drops two columns nothing used
    /// (is_recognized, recognized_as_course_id).
    /// </summary>
    public partial class SplitRecognitionResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                schema: "exchange",
                table: "recognition",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "started_by",
                schema: "exchange",
                table: "recognition",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recognition_entry",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    recognition_id = table.Column<int>(type: "integer", nullable: false),
                    partner_course_id = table.Column<int>(type: "integer", nullable: false),
                    enrollment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    original_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ects_grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    hr_grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    exam_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("recognition_entry_pkey", x => x.id);
                    table.CheckConstraint("recognition_entry_enrollment_status_check", "enrollment_status IN ('Passed', 'NotPassed')");
                    table.ForeignKey(
                        name: "recognition_entry_partner_course_id_fkey",
                        column: x => x.partner_course_id,
                        principalSchema: "partner",
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "recognition_entry_recognition_id_fkey",
                        column: x => x.recognition_id,
                        principalSchema: "exchange",
                        principalTable: "recognition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "recognition_entry_id",
                schema: "exchange",
                table: "mapping_scheme_entry",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(SplitRecognitionResultsData.MoveStarted);
            migrationBuilder.Sql(SplitRecognitionResultsData.CreateResults);
            migrationBuilder.Sql(SplitRecognitionResultsData.LinkPlacements);

            migrationBuilder.AlterColumn<int>(
                name: "recognition_entry_id",
                schema: "exchange",
                table: "mapping_scheme_entry",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "awarded_ects",
                schema: "exchange",
                table: "mapping_scheme_entry",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,1)",
                oldPrecision: 4,
                oldScale: 1,
                oldNullable: true);

            // The old columns go (with their keys and indexes).
            migrationBuilder.DropForeignKey(name: "learning_agreement_concluded_by_fkey", schema: "exchange", table: "learning_agreement");
            migrationBuilder.DropForeignKey(name: "mapping_scheme_entry_exchange_id_fkey", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropForeignKey(name: "mapping_scheme_entry_partner_course_id_fkey", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropForeignKey(name: "mapping_scheme_entry_recognized_as_course_id_fkey", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropIndex(name: "idx_mapping_scheme_entry_exchange", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropIndex(name: "idx_mapping_scheme_entry_partner_course_id", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropIndex(name: "idx_mapping_scheme_entry_recognized_as_course_id", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropIndex(name: "idx_learning_agreement_concluded_by", schema: "exchange", table: "learning_agreement");
            migrationBuilder.DropCheckConstraint(name: "mapping_scheme_entry_enrollment_status_check", schema: "exchange", table: "mapping_scheme_entry");

            foreach (var column in new[] { "exchange_id", "partner_course_id", "enrollment_status", "original_grade", "ects_grade", "hr_grade", "exam_date", "is_recognized", "recognized_as_course_id" })
                migrationBuilder.DropColumn(name: column, schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropColumn(name: "concluded_at", schema: "exchange", table: "learning_agreement");
            migrationBuilder.DropColumn(name: "concluded_by", schema: "exchange", table: "learning_agreement");

            // The new keys and indexes.
            migrationBuilder.CreateIndex(
                name: "idx_recognition_started_by",
                schema: "exchange",
                table: "recognition",
                column: "started_by");

            migrationBuilder.CreateIndex(
                name: "idx_recognition_entry_partner_course_id",
                schema: "exchange",
                table: "recognition_entry",
                column: "partner_course_id");

            migrationBuilder.CreateIndex(
                name: "recognition_entry_course_key",
                schema: "exchange",
                table: "recognition_entry",
                columns: new[] { "recognition_id", "partner_course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "mapping_scheme_entry_slot_key",
                schema: "exchange",
                table: "mapping_scheme_entry",
                columns: new[] { "recognition_entry_id", "home_slot_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "mapping_scheme_entry_awarded_ects_check",
                schema: "exchange",
                table: "mapping_scheme_entry",
                sql: "awarded_ects >= 0");

            migrationBuilder.AddForeignKey(
                name: "mapping_scheme_entry_recognition_entry_id_fkey",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "recognition_entry_id",
                principalSchema: "exchange",
                principalTable: "recognition_entry",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "recognition_started_by_fkey",
                schema: "exchange",
                table: "recognition",
                column: "started_by",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "mapping_scheme_entry_recognition_entry_id_fkey", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropForeignKey(name: "recognition_started_by_fkey", schema: "exchange", table: "recognition");
            migrationBuilder.DropIndex(name: "idx_recognition_started_by", schema: "exchange", table: "recognition");
            migrationBuilder.DropIndex(name: "mapping_scheme_entry_slot_key", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropCheckConstraint(name: "mapping_scheme_entry_awarded_ects_check", schema: "exchange", table: "mapping_scheme_entry");

            migrationBuilder.AddColumn<int>(name: "exchange_id", schema: "exchange", table: "mapping_scheme_entry", type: "integer", nullable: true);
            migrationBuilder.AddColumn<int>(name: "partner_course_id", schema: "exchange", table: "mapping_scheme_entry", type: "integer", nullable: true);
            migrationBuilder.AddColumn<string>(name: "enrollment_status", schema: "exchange", table: "mapping_scheme_entry", type: "character varying(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<string>(name: "original_grade", schema: "exchange", table: "mapping_scheme_entry", type: "character varying(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ects_grade", schema: "exchange", table: "mapping_scheme_entry", type: "character varying(5)", maxLength: 5, nullable: true);
            migrationBuilder.AddColumn<string>(name: "hr_grade", schema: "exchange", table: "mapping_scheme_entry", type: "character varying(10)", maxLength: 10, nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "exam_date", schema: "exchange", table: "mapping_scheme_entry", type: "date", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "is_recognized", schema: "exchange", table: "mapping_scheme_entry", type: "boolean", nullable: true);
            migrationBuilder.AddColumn<int>(name: "recognized_as_course_id", schema: "exchange", table: "mapping_scheme_entry", type: "integer", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "concluded_at", schema: "exchange", table: "learning_agreement", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<int>(name: "concluded_by", schema: "exchange", table: "learning_agreement", type: "integer", nullable: true);

            migrationBuilder.Sql(SplitRecognitionResultsData.Restore);

            migrationBuilder.AlterColumn<int>(
                name: "exchange_id",
                schema: "exchange",
                table: "mapping_scheme_entry",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "awarded_ects",
                schema: "exchange",
                table: "mapping_scheme_entry",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,1)",
                oldPrecision: 4,
                oldScale: 1);

            migrationBuilder.DropColumn(name: "recognition_entry_id", schema: "exchange", table: "mapping_scheme_entry");
            migrationBuilder.DropTable(name: "recognition_entry", schema: "exchange");
            migrationBuilder.DropColumn(name: "started_at", schema: "exchange", table: "recognition");
            migrationBuilder.DropColumn(name: "started_by", schema: "exchange", table: "recognition");

            migrationBuilder.CreateIndex(name: "idx_mapping_scheme_entry_exchange", schema: "exchange", table: "mapping_scheme_entry", column: "exchange_id");
            migrationBuilder.CreateIndex(name: "idx_mapping_scheme_entry_partner_course_id", schema: "exchange", table: "mapping_scheme_entry", column: "partner_course_id");
            migrationBuilder.CreateIndex(name: "idx_mapping_scheme_entry_recognized_as_course_id", schema: "exchange", table: "mapping_scheme_entry", column: "recognized_as_course_id");
            migrationBuilder.CreateIndex(name: "idx_learning_agreement_concluded_by", schema: "exchange", table: "learning_agreement", column: "concluded_by");
            migrationBuilder.AddCheckConstraint(
                name: "mapping_scheme_entry_enrollment_status_check",
                schema: "exchange",
                table: "mapping_scheme_entry",
                sql: "enrollment_status IN ('Passed', 'NotPassed')");

            migrationBuilder.AddForeignKey(
                name: "learning_agreement_concluded_by_fkey",
                schema: "exchange",
                table: "learning_agreement",
                column: "concluded_by",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "mapping_scheme_entry_exchange_id_fkey",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "exchange_id",
                principalSchema: "exchange",
                principalTable: "exchange",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "mapping_scheme_entry_partner_course_id_fkey",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "partner_course_id",
                principalSchema: "partner",
                principalTable: "course",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "mapping_scheme_entry_recognized_as_course_id_fkey",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "recognized_as_course_id",
                principalSchema: "home",
                principalTable: "course",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

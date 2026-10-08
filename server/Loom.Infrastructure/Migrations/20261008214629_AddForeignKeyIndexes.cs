using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Redundant: each duplicates a UNIQUE constraint or a composite index on the same leading column.
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS exchange.idx_la_exchange;
                DROP INDEX IF EXISTS exchange.idx_recognition_exchange;
                DROP INDEX IF EXISTS exchange.idx_recognition_entry_la_entry;
                DROP INDEX IF EXISTS exchange.idx_snapshot_exchange;
                """);

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_changed_by_id",
                schema: "exchange",
                table: "snapshot",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "idx_slot_course_group_id",
                schema: "home",
                table: "slot",
                column: "course_group_id");

            migrationBuilder.CreateIndex(
                name: "idx_slot_course_id",
                schema: "home",
                table: "slot",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "idx_slot_slot_type_id",
                schema: "home",
                table: "slot",
                column: "slot_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_recognition_entry_recognized_as_course_id",
                schema: "exchange",
                table: "recognition_entry",
                column: "recognized_as_course_id");

            migrationBuilder.CreateIndex(
                name: "idx_recognition_approved_by",
                schema: "exchange",
                table: "recognition",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "idx_recognition_updated_by",
                schema: "exchange",
                table: "recognition",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_mapping_scheme_entry_partner_course_id",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "partner_course_id");

            migrationBuilder.CreateIndex(
                name: "idx_mapping_scheme_entry_recognized_as_course_id",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "recognized_as_course_id");

            migrationBuilder.CreateIndex(
                name: "idx_learning_agreement_approved_by",
                schema: "exchange",
                table: "learning_agreement",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "idx_learning_agreement_updated_by",
                schema: "exchange",
                table: "learning_agreement",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_coordinator_whitelist_institution_id",
                table: "coordinator_whitelist",
                column: "institution_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS idx_la_exchange ON exchange.learning_agreement (exchange_id);
                CREATE INDEX IF NOT EXISTS idx_recognition_exchange ON exchange.recognition (exchange_id);
                CREATE INDEX IF NOT EXISTS idx_recognition_entry_la_entry ON exchange.recognition_entry (learning_agreement_entry_id);
                CREATE INDEX IF NOT EXISTS idx_snapshot_exchange ON exchange.snapshot (exchange_id);
                """);

            migrationBuilder.DropIndex(
                name: "idx_snapshot_changed_by_id",
                schema: "exchange",
                table: "snapshot");

            migrationBuilder.DropIndex(
                name: "idx_slot_course_group_id",
                schema: "home",
                table: "slot");

            migrationBuilder.DropIndex(
                name: "idx_slot_course_id",
                schema: "home",
                table: "slot");

            migrationBuilder.DropIndex(
                name: "idx_slot_slot_type_id",
                schema: "home",
                table: "slot");

            migrationBuilder.DropIndex(
                name: "idx_recognition_entry_recognized_as_course_id",
                schema: "exchange",
                table: "recognition_entry");

            migrationBuilder.DropIndex(
                name: "idx_recognition_approved_by",
                schema: "exchange",
                table: "recognition");

            migrationBuilder.DropIndex(
                name: "idx_recognition_updated_by",
                schema: "exchange",
                table: "recognition");

            migrationBuilder.DropIndex(
                name: "idx_mapping_scheme_entry_partner_course_id",
                schema: "exchange",
                table: "mapping_scheme_entry");

            migrationBuilder.DropIndex(
                name: "idx_mapping_scheme_entry_recognized_as_course_id",
                schema: "exchange",
                table: "mapping_scheme_entry");

            migrationBuilder.DropIndex(
                name: "idx_learning_agreement_approved_by",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.DropIndex(
                name: "idx_learning_agreement_updated_by",
                schema: "exchange",
                table: "learning_agreement");

            migrationBuilder.DropIndex(
                name: "idx_coordinator_whitelist_institution_id",
                table: "coordinator_whitelist");
        }
    }
}

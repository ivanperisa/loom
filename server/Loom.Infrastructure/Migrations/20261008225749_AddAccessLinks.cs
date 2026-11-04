using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Loom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_link",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("access_link_pkey", x => x.id);
                    table.ForeignKey(
                        name: "access_link_created_by_id_fkey",
                        column: x => x.created_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "access_link_exchange_id_fkey",
                        column: x => x.exchange_id,
                        principalSchema: "exchange",
                        principalTable: "exchange",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_access_link_created_by_id",
                schema: "exchange",
                table: "access_link",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "idx_access_link_exchange_active",
                schema: "exchange",
                table: "access_link",
                column: "exchange_id",
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_access_link_token",
                schema: "exchange",
                table: "access_link",
                column: "token",
                unique: true);

            // Links already sent out were /access/{exchange guid}. Keep them working until the coordinator
            // regenerates the link, so students in the middle of planning are not locked out.
            migrationBuilder.Sql("""
                INSERT INTO exchange.access_link (exchange_id, token, created_by_id)
                SELECT e.id, e.guid::text, e.coordinator_id
                FROM exchange.exchange e
                JOIN "user" u ON u.id = e.student_id
                WHERE u.email = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_link",
                schema: "exchange");
        }
    }
}

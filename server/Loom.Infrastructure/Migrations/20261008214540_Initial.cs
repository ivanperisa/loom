using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Loom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "home");

            migrationBuilder.EnsureSchema(
                name: "partner");

            migrationBuilder.EnsureSchema(
                name: "exchange");

            migrationBuilder.CreateTable(
                name: "course",
                schema: "home",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    isvu_code = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_en = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("course_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "institution",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_hr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    erasmus_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    institution_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Partner"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("institution_pkey", x => x.id);
                    table.CheckConstraint("institution_institution_type_check", "institution_type IN ('Home', 'Partner')");
                });

            migrationBuilder.CreateTable(
                name: "slot_type",
                schema: "home",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("slot_type_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coordinator_whitelist",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    institution_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coordinator_whitelist_pkey", x => x.id);
                    table.ForeignKey(
                        name: "coordinator_whitelist_institution_id_fkey",
                        column: x => x.institution_id,
                        principalTable: "institution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "course",
                schema: "partner",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    institution_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_hr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ects = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    lectures_h = table.Column<int>(type: "integer", nullable: true),
                    auditory_h = table.Column<int>(type: "integer", nullable: true),
                    lab_h = table.Column<int>(type: "integer", nullable: true),
                    semester = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("course_pkey", x => x.id);
                    table.CheckConstraint("course_level_check", "level IN ('Graduate', 'Undergraduate', 'Postgraduate')");
                    table.CheckConstraint("course_semester_check", "semester IN ('Winter', 'Summer', 'Both')");
                    table.ForeignKey(
                        name: "course_institution_id_fkey",
                        column: x => x.institution_id,
                        principalTable: "institution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program",
                schema: "home",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    institution_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_en = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    duration_semesters = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("program_pkey", x => x.id);
                    table.CheckConstraint("program_level_check", "level IN ('Graduate', 'Undergraduate', 'Postgraduate')");
                    table.ForeignKey(
                        name: "program_institution_id_fkey",
                        column: x => x.institution_id,
                        principalTable: "institution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Student"),
                    is_onboarded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    jmbag = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    mentor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    institution_id = table.Column<int>(type: "integer", nullable: true),
                    coordinator_id = table.Column<int>(type: "integer", nullable: true),
                    coordinator_request_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_pkey", x => x.id);
                    table.CheckConstraint("user_coordinator_request_status_check", "coordinator_request_status IN ('Pending', 'Rejected')");
                    table.CheckConstraint("user_role_check", "role IN ('Student', 'Coordinator', 'Admin')");
                    table.ForeignKey(
                        name: "user_coordinator_id_fkey",
                        column: x => x.coordinator_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "user_institution_id_fkey",
                        column: x => x.institution_id,
                        principalTable: "institution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "course_group",
                schema: "home",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    slot_type_id = table.Column<int>(type: "integer", nullable: false),
                    isvu_code = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_en = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("course_group_pkey", x => x.id);
                    table.ForeignKey(
                        name: "course_group_slot_type_id_fkey",
                        column: x => x.slot_type_id,
                        principalSchema: "home",
                        principalTable: "slot_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "profile",
                schema: "home",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    program_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_en = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("profile_pkey", x => x.id);
                    table.ForeignKey(
                        name: "profile_program_id_fkey",
                        column: x => x.program_id,
                        principalSchema: "home",
                        principalTable: "program",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exchange",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    guid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    home_profile_id = table.Column<int>(type: "integer", nullable: false),
                    partner_institution_id = table.Column<int>(type: "integer", nullable: false),
                    coordinator_id = table.Column<int>(type: "integer", nullable: true),
                    academic_year = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    semester_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    study_semesters = table.Column<List<int>>(type: "integer[]", nullable: false),
                    coordinator_message = table.Column<string>(type: "text", nullable: true),
                    ewp_link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("exchange_pkey", x => x.id);
                    table.CheckConstraint("exchange_semester_type_check", "semester_type IN ('Winter', 'Summer', 'Both')");
                    table.ForeignKey(
                        name: "exchange_coordinator_id_fkey",
                        column: x => x.coordinator_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "exchange_home_profile_id_fkey",
                        column: x => x.home_profile_id,
                        principalSchema: "home",
                        principalTable: "profile",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "exchange_partner_institution_id_fkey",
                        column: x => x.partner_institution_id,
                        principalTable: "institution",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "exchange_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "slot",
                schema: "home",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    slot_position = table.Column<int>(type: "integer", nullable: false),
                    ects = table.Column<int>(type: "integer", nullable: false),
                    slot_type_id = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: true),
                    course_group_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("slot_pkey", x => x.id);
                    table.CheckConstraint("chk_slot_exactly_one_source", "(course_id IS NOT NULL AND course_group_id IS NULL) OR (course_id IS NULL AND course_group_id IS NOT NULL)");
                    table.CheckConstraint("slot_semester_check", "semester >= 1 AND semester <= 4");
                    table.CheckConstraint("slot_slot_position_check", "slot_position >= 1 AND slot_position <= 30");
                    table.ForeignKey(
                        name: "slot_course_group_id_fkey",
                        column: x => x.course_group_id,
                        principalSchema: "home",
                        principalTable: "course_group",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "slot_course_id_fkey",
                        column: x => x.course_id,
                        principalSchema: "home",
                        principalTable: "course",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "slot_profile_id_fkey",
                        column: x => x.profile_id,
                        principalSchema: "home",
                        principalTable: "profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "slot_slot_type_id_fkey",
                        column: x => x.slot_type_id,
                        principalSchema: "home",
                        principalTable: "slot_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "learning_agreement",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    message = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("learning_agreement_pkey", x => x.id);
                    table.CheckConstraint("learning_agreement_status_check", "status IN ('Draft', 'Submitted', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "learning_agreement_approved_by_fkey",
                        column: x => x.approved_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "learning_agreement_exchange_id_fkey",
                        column: x => x.exchange_id,
                        principalSchema: "exchange",
                        principalTable: "exchange",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "learning_agreement_updated_by_fkey",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "recognition",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    message = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("recognition_pkey", x => x.id);
                    table.CheckConstraint("recognition_status_check", "status IN ('Draft', 'Submitted', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "recognition_approved_by_fkey",
                        column: x => x.approved_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "recognition_exchange_id_fkey",
                        column: x => x.exchange_id,
                        principalSchema: "exchange",
                        principalTable: "exchange",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recognition_updated_by_fkey",
                        column: x => x.updated_by,
                        principalTable: "user",
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
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    changed_by_id = table.Column<int>(type: "integer", nullable: false),
                    phase = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Auto"),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
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

            migrationBuilder.CreateTable(
                name: "mapping_scheme_entry",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    home_slot_id = table.Column<int>(type: "integer", nullable: false),
                    partner_course_id = table.Column<int>(type: "integer", nullable: true),
                    awarded_ects = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    enrollment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    original_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ects_grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    hr_grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    exam_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_recognized = table.Column<bool>(type: "boolean", nullable: true),
                    recognized_as_course_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("mapping_scheme_entry_pkey", x => x.id);
                    table.CheckConstraint("mapping_scheme_entry_enrollment_status_check", "enrollment_status IN ('Passed', 'NotPassed')");
                    table.ForeignKey(
                        name: "mapping_scheme_entry_exchange_id_fkey",
                        column: x => x.exchange_id,
                        principalSchema: "exchange",
                        principalTable: "exchange",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "mapping_scheme_entry_home_slot_id_fkey",
                        column: x => x.home_slot_id,
                        principalSchema: "home",
                        principalTable: "slot",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "mapping_scheme_entry_partner_course_id_fkey",
                        column: x => x.partner_course_id,
                        principalSchema: "partner",
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "mapping_scheme_entry_recognized_as_course_id_fkey",
                        column: x => x.recognized_as_course_id,
                        principalSchema: "home",
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "learning_agreement_entry",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    learning_agreement_id = table.Column<int>(type: "integer", nullable: false),
                    home_slot_id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    partner_course_id = table.Column<int>(type: "integer", nullable: true),
                    awarded_ects = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("learning_agreement_entry_pkey", x => x.id);
                    table.CheckConstraint("learning_agreement_entry_mode_check", "mode IN ('AtHome', 'AtExchange', 'AfterExchange')");
                    table.ForeignKey(
                        name: "learning_agreement_entry_home_slot_id_fkey",
                        column: x => x.home_slot_id,
                        principalSchema: "home",
                        principalTable: "slot",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "learning_agreement_entry_learning_agreement_id_fkey",
                        column: x => x.learning_agreement_id,
                        principalSchema: "exchange",
                        principalTable: "learning_agreement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "learning_agreement_entry_partner_course_id_fkey",
                        column: x => x.partner_course_id,
                        principalSchema: "partner",
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "recognition_entry",
                schema: "exchange",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    recognition_id = table.Column<int>(type: "integer", nullable: false),
                    learning_agreement_entry_id = table.Column<int>(type: "integer", nullable: false),
                    recognized_as_course_id = table.Column<int>(type: "integer", nullable: true),
                    enrollment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    original_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ects_grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    hr_grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    exam_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_recognized = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
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

            migrationBuilder.CreateIndex(
                name: "coordinator_whitelist_email_key",
                table: "coordinator_whitelist",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_home_course_isvu",
                schema: "home",
                table: "course",
                column: "isvu_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_partner_course_institution_code",
                schema: "partner",
                table: "course",
                columns: new[] { "institution_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_home_course_group_slot_type",
                schema: "home",
                table: "course_group",
                column: "slot_type_id");

            migrationBuilder.CreateIndex(
                name: "exchange_guid_key",
                schema: "exchange",
                table: "exchange",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_exchange_coordinator",
                schema: "exchange",
                table: "exchange",
                column: "coordinator_id");

            migrationBuilder.CreateIndex(
                name: "idx_exchange_home_profile",
                schema: "exchange",
                table: "exchange",
                column: "home_profile_id");

            migrationBuilder.CreateIndex(
                name: "idx_exchange_partner_institution",
                schema: "exchange",
                table: "exchange",
                column: "partner_institution_id");

            migrationBuilder.CreateIndex(
                name: "idx_exchange_student",
                schema: "exchange",
                table: "exchange",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "idx_la_status",
                schema: "exchange",
                table: "learning_agreement",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "learning_agreement_exchange_id_key",
                schema: "exchange",
                table: "learning_agreement",
                column: "exchange_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_la_entry_la",
                schema: "exchange",
                table: "learning_agreement_entry",
                column: "learning_agreement_id");

            migrationBuilder.CreateIndex(
                name: "idx_la_entry_partner_course",
                schema: "exchange",
                table: "learning_agreement_entry",
                column: "partner_course_id");

            migrationBuilder.CreateIndex(
                name: "idx_la_entry_slot",
                schema: "exchange",
                table: "learning_agreement_entry",
                column: "home_slot_id");

            migrationBuilder.CreateIndex(
                name: "idx_mapping_scheme_entry_exchange",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "exchange_id");

            migrationBuilder.CreateIndex(
                name: "idx_mapping_scheme_entry_slot",
                schema: "exchange",
                table: "mapping_scheme_entry",
                column: "home_slot_id");

            migrationBuilder.CreateIndex(
                name: "idx_home_profile_program",
                schema: "home",
                table: "profile",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "idx_home_program_institution",
                schema: "home",
                table: "program",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "recognition_exchange_id_key",
                schema: "exchange",
                table: "recognition",
                column: "exchange_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_recognition_entry_recognition",
                schema: "exchange",
                table: "recognition_entry",
                column: "recognition_id");

            migrationBuilder.CreateIndex(
                name: "recognition_entry_learning_agreement_entry_id_key",
                schema: "exchange",
                table: "recognition_entry",
                column: "learning_agreement_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_home_slot_profile",
                schema: "home",
                table: "slot",
                column: "profile_id");

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

            migrationBuilder.CreateIndex(
                name: "idx_user_coordinator",
                table: "user",
                column: "coordinator_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_email",
                table: "user",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_user_institution",
                table: "user",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_jmbag_not_null",
                table: "user",
                column: "jmbag",
                unique: true,
                filter: "jmbag IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "user_external_id_key",
                table: "user",
                column: "external_id",
                unique: true);

            // The original hand-written schema used UNIQUE constraints (EF models them as unique indexes)
            // plus a few redundant indexes. Recreate both exactly, so a database built from migrations
            // matches the production baseline. The redundant indexes are dropped by a later migration.
            migrationBuilder.Sql("""
                ALTER TABLE exchange.exchange ADD CONSTRAINT exchange_guid_key UNIQUE USING INDEX exchange_guid_key;
                ALTER TABLE exchange.learning_agreement ADD CONSTRAINT learning_agreement_exchange_id_key UNIQUE USING INDEX learning_agreement_exchange_id_key;
                ALTER TABLE exchange.recognition ADD CONSTRAINT recognition_exchange_id_key UNIQUE USING INDEX recognition_exchange_id_key;
                ALTER TABLE exchange.recognition_entry ADD CONSTRAINT recognition_entry_learning_agreement_entry_id_key UNIQUE USING INDEX recognition_entry_learning_agreement_entry_id_key;
                ALTER TABLE public.coordinator_whitelist ADD CONSTRAINT coordinator_whitelist_email_key UNIQUE USING INDEX coordinator_whitelist_email_key;
                ALTER TABLE public."user" ADD CONSTRAINT user_external_id_key UNIQUE USING INDEX user_external_id_key;

                CREATE INDEX idx_la_exchange ON exchange.learning_agreement (exchange_id);
                CREATE INDEX idx_recognition_exchange ON exchange.recognition (exchange_id);
                CREATE INDEX idx_recognition_entry_la_entry ON exchange.recognition_entry (learning_agreement_entry_id);
                CREATE INDEX idx_snapshot_exchange ON exchange.snapshot (exchange_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coordinator_whitelist");

            migrationBuilder.DropTable(
                name: "mapping_scheme_entry",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "recognition_entry",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "snapshot",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "learning_agreement_entry",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "recognition",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "slot",
                schema: "home");

            migrationBuilder.DropTable(
                name: "learning_agreement",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "course",
                schema: "partner");

            migrationBuilder.DropTable(
                name: "course_group",
                schema: "home");

            migrationBuilder.DropTable(
                name: "course",
                schema: "home");

            migrationBuilder.DropTable(
                name: "exchange",
                schema: "exchange");

            migrationBuilder.DropTable(
                name: "slot_type",
                schema: "home");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "profile",
                schema: "home");

            migrationBuilder.DropTable(
                name: "program",
                schema: "home");

            migrationBuilder.DropTable(
                name: "institution");
        }
    }
}

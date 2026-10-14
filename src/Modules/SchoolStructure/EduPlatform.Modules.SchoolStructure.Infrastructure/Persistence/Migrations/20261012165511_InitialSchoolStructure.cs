using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchoolStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "school");

            migrationBuilder.CreateTable(
                name: "academic_years",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_classes",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade = table.Column<int>(type: "integer", nullable: false),
                    section = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    homeroom_teacher_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_classes", x => x.id);
                    table.ForeignKey(
                        name: "fk_school_classes_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalSchema: "school",
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_enrollments",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    withdrawn_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "fk_class_enrollments_school_classes_school_class_id",
                        column: x => x.school_class_id,
                        principalSchema: "school",
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "teaching_assignments",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekly_hours = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teaching_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_teaching_assignments_school_classes_school_class_id",
                        column: x => x.school_class_id,
                        principalSchema: "school",
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_teaching_assignments_subjects_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "school",
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_name",
                schema: "school",
                table: "academic_years",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_class_enrollments_school_class_id_student_user_id",
                schema: "school",
                table: "class_enrollments",
                columns: new[] { "school_class_id", "student_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_class_enrollments_student_user_id",
                schema: "school",
                table: "class_enrollments",
                column: "student_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_school_classes_academic_year_id_grade_section",
                schema: "school",
                table: "school_classes",
                columns: new[] { "academic_year_id", "grade", "section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subjects_code",
                schema: "school",
                table: "subjects",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_school_class_id",
                schema: "school",
                table: "teaching_assignments",
                column: "school_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_subject_id_school_class_id",
                schema: "school",
                table: "teaching_assignments",
                columns: new[] { "subject_id", "school_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teaching_assignments_teacher_user_id",
                schema: "school",
                table: "teaching_assignments",
                column: "teacher_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_enrollments",
                schema: "school");

            migrationBuilder.DropTable(
                name: "teaching_assignments",
                schema: "school");

            migrationBuilder.DropTable(
                name: "school_classes",
                schema: "school");

            migrationBuilder.DropTable(
                name: "subjects",
                schema: "school");

            migrationBuilder.DropTable(
                name: "academic_years",
                schema: "school");
        }
    }
}

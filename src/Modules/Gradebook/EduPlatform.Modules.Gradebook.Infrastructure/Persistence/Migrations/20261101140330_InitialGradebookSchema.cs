using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.Gradebook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGradebookSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gradebook");

            migrationBuilder.CreateTable(
                name: "absences",
                schema: "gradebook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teaching_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    period = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_absences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                schema: "gradebook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teaching_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    given_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grades", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_absences_student_user_id_teaching_assignment_id_date_period",
                schema: "gradebook",
                table: "absences",
                columns: new[] { "student_user_id", "teaching_assignment_id", "date", "period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grades_student_user_id_teaching_assignment_id",
                schema: "gradebook",
                table: "grades",
                columns: new[] { "student_user_id", "teaching_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_grades_teaching_assignment_id",
                schema: "gradebook",
                table: "grades",
                column: "teaching_assignment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "absences",
                schema: "gradebook");

            migrationBuilder.DropTable(
                name: "grades",
                schema: "gradebook");
        }
    }
}

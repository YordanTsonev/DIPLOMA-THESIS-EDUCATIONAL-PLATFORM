using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.Homework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialHomeworkSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "homework");

            migrationBuilder.CreateTable(
                name: "assignments",
                schema: "homework",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teaching_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    due_on = table.Column<DateOnly>(type: "date", nullable: false),
                    is_graded = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                schema: "homework",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_late = table.Column<bool>(type: "boolean", nullable: false),
                    mark = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    feedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_submissions_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalSchema: "homework",
                        principalTable: "assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignments_teaching_assignment_id",
                schema: "homework",
                table: "assignments",
                column: "teaching_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_assignment_id_student_user_id",
                schema: "homework",
                table: "submissions",
                columns: new[] { "assignment_id", "student_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submissions",
                schema: "homework");

            migrationBuilder.DropTable(
                name: "assignments",
                schema: "homework");
        }
    }
}

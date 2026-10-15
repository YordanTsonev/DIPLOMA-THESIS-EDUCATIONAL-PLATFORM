using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calendar_events",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    school_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period = table.Column<int>(type: "integer", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendar_events_school_classes_school_class_id",
                        column: x => x.school_class_id,
                        principalSchema: "school",
                        principalTable: "school_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_calendar_events_subjects_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "school",
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_school_class_id",
                schema: "school",
                table: "calendar_events",
                column: "school_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_starts_on_ends_on",
                schema: "school",
                table: "calendar_events",
                columns: new[] { "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_subject_id",
                schema: "school",
                table: "calendar_events",
                column: "subject_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_events",
                schema: "school");
        }
    }
}

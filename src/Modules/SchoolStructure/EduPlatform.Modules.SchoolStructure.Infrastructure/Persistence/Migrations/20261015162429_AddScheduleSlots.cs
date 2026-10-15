using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_slots",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    teaching_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    period = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ends_at = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    room = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_slots_teaching_assignments_teaching_assignment_id",
                        column: x => x.teaching_assignment_id,
                        principalSchema: "school",
                        principalTable: "teaching_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_schedule_slots_teaching_assignment_id_day_of_week_period",
                schema: "school",
                table: "schedule_slots",
                columns: new[] { "teaching_assignment_id", "day_of_week", "period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_slots",
                schema: "school");
        }
    }
}

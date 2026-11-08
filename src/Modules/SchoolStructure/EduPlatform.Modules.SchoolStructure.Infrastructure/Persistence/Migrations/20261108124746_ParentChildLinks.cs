using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParentChildLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parent_children",
                schema: "school",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parent_children", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_parent_children_parent_user_id_student_user_id",
                schema: "school",
                table: "parent_children",
                columns: new[] { "parent_user_id", "student_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parent_children_student_user_id",
                schema: "school",
                table: "parent_children",
                column: "student_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parent_children",
                schema: "school");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.Homework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HomeworkExamLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exam_id",
                schema: "homework",
                table: "assignments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "exam_id",
                schema: "homework",
                table: "assignments");
        }
    }
}

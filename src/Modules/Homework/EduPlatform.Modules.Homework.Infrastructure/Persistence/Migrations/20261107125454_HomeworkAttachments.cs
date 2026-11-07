using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPlatform.Modules.Homework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HomeworkAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_type",
                schema: "homework",
                table: "submissions",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "homework",
                table: "submissions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "homework",
                table: "submissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                schema: "homework",
                table: "submissions",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                schema: "homework",
                table: "assignments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "homework",
                table: "assignments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "homework",
                table: "assignments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                schema: "homework",
                table: "assignments",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_type",
                schema: "homework",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "homework",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "homework",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "storage_key",
                schema: "homework",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "content_type",
                schema: "homework",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "homework",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "homework",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "storage_key",
                schema: "homework",
                table: "assignments");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class updatefileposition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bucket_name",
                table: "document",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name_prefix",
                table: "document",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_name_directory",
                table: "document",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bucket_name",
                table: "document");

            migrationBuilder.DropColumn(
                name: "file_name_prefix",
                table: "document");

            migrationBuilder.DropColumn(
                name: "object_name_directory",
                table: "document");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addfilepreview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "file_preview_bucket_name",
                table: "document_file",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_preview_object_name",
                table: "document_file",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_preview_bucket_name",
                table: "document_file");

            migrationBuilder.DropColumn(
                name: "file_preview_object_name",
                table: "document_file");
        }
    }
}

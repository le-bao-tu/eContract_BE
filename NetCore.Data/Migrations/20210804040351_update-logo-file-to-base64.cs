using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class updatelogofiletobase64 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bucket_name",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "file_name",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "object_name",
                table: "organization_config");

            migrationBuilder.AddColumn<string>(
                name: "logo_file_base64",
                table: "organization_config",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo_file_base64",
                table: "organization_config");

            migrationBuilder.AddColumn<string>(
                name: "bucket_name",
                table: "organization_config",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                table: "organization_config",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_name",
                table: "organization_config",
                type: "text",
                nullable: true);
        }
    }
}

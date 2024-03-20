using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class add_user_sign_config : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "background_image_file_base64",
                table: "user_sign_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "more_info",
                table: "user_sign_config",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "background_image_file_base64",
                table: "user_sign_config");

            migrationBuilder.DropColumn(
                name: "more_info",
                table: "user_sign_config");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class updatefieldusersignconfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_base64",
                table: "user_sign_config");

            migrationBuilder.AddColumn<string>(
                name: "image_file_base64",
                table: "user_sign_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_file_base64",
                table: "user_sign_config",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sign_appearance_image",
                table: "user_sign_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sign_appearance_logo",
                table: "user_sign_config",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_file_base64",
                table: "user_sign_config");

            migrationBuilder.DropColumn(
                name: "logo_file_base64",
                table: "user_sign_config");

            migrationBuilder.DropColumn(
                name: "sign_appearance_image",
                table: "user_sign_config");

            migrationBuilder.DropColumn(
                name: "sign_appearance_logo",
                table: "user_sign_config");

            migrationBuilder.AddColumn<string>(
                name: "file_base64",
                table: "user_sign_config",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

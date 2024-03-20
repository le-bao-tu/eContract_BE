using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class update_sign_config : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "scale_image",
                table: "user_sign_config",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "scale_logo",
                table: "user_sign_config",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "scale_text",
                table: "user_sign_config",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scale_image",
                table: "user_sign_config");

            migrationBuilder.DropColumn(
                name: "scale_logo",
                table: "user_sign_config");

            migrationBuilder.DropColumn(
                name: "scale_text",
                table: "user_sign_config");
        }
    }
}

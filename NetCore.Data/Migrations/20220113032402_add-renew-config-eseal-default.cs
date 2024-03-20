using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addrenewconfigesealdefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_allow_renew",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_renew_times",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sign_info_default_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "renew_times",
                table: "document",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_allow_renew",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "max_renew_times",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "sign_info_default_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "renew_times",
                table: "document");
        }
    }
}

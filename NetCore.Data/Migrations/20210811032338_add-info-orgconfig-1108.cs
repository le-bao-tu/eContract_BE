using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addinfoorgconfig1108 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_approve_sign_dynamic_position",
                table: "organization_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_use_ui",
                table: "organization_config",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_approve_sign_dynamic_position",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "is_use_ui",
                table: "organization_config");
        }
    }
}

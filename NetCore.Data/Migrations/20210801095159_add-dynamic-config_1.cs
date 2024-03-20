using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class adddynamicconfig_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sms_type",
                table: "organization_config");

            migrationBuilder.AddColumn<int>(
                name: "sms_send_type",
                table: "organization_config",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sms_send_type",
                table: "organization_config");

            migrationBuilder.AddColumn<int>(
                name: "sms_type",
                table: "organization_config",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

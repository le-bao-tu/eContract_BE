using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addadssprofile1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "adss_profile_name",
                table: "workflow_user_sign",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "adss_profile_name",
                table: "workflow_user_sign",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}

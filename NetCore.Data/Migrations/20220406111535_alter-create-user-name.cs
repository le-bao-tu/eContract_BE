using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class altercreateusername : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "create_username",
                table: "document");

            migrationBuilder.AddColumn<string>(
                name: "created_username",
                table: "document",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_username",
                table: "document");

            migrationBuilder.AddColumn<string>(
                name: "create_username",
                table: "document",
                type: "text",
                nullable: true);
        }
    }
}

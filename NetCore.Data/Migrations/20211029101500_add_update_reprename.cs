using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class add_update_reprename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "repre_identity_number",
                table: "organization",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "repre_identity_number",
                table: "organization");
        }
    }
}

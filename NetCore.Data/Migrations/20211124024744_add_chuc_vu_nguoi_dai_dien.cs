using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class add_chuc_vu_nguoi_dai_dien : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "repre_position_line1",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_position_line2",
                table: "organization",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "repre_position_line1",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_position_line2",
                table: "organization");
        }
    }
}

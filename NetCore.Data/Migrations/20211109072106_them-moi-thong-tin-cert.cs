using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class themmoithongtincert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "csr",
                table: "user_hsm_account",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_key",
                table: "user_hsm_account",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "csr",
                table: "user_hsm_account");

            migrationBuilder.DropColumn(
                name: "public_key",
                table: "user_hsm_account");
        }
    }
}

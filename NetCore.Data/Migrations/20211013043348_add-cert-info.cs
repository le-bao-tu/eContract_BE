using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addcertinfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "valid_from",
                table: "user_hsm_account",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "valid_to",
                table: "user_hsm_account",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "user_hsm_account");

            migrationBuilder.DropColumn(
                name: "valid_to",
                table: "user_hsm_account");
        }
    }
}

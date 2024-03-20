using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class update_user_info : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "issue_by",
                table: "user",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "issue_date",
                table: "user",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "issue_by",
                table: "user");

            migrationBuilder.DropColumn(
                name: "issue_date",
                table: "user");
        }
    }
}

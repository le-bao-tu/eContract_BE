using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addclosdedocumentdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sign_close_after_day",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sign_close_at_date",
                table: "document",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sign_close_after_day",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "sign_close_at_date",
                table: "document");
        }
    }
}

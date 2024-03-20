using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class AddColumnIsUseEverifyDocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_use_everify",
                table: "document",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "document",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "verify_code",
                table: "document",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verify_date",
                table: "document",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_use_everify",
                table: "document");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "document");

            migrationBuilder.DropColumn(
                name: "verify_code",
                table: "document");

            migrationBuilder.DropColumn(
                name: "verify_date",
                table: "document");
        }
    }
}

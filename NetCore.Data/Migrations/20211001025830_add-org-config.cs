using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addorgconfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "eform_config",
                table: "user",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "eform_config",
                table: "organization_config",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "user_name_prefix",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "document",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_user_id",
                table: "document",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_document_user_user_id",
                table: "document",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_user_user_id",
                table: "document");

            migrationBuilder.DropIndex(
                name: "IX_document_user_id",
                table: "document");

            migrationBuilder.DropColumn(
                name: "eform_config",
                table: "user");

            migrationBuilder.DropColumn(
                name: "eform_config",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "user_name_prefix",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "document");
        }
    }
}

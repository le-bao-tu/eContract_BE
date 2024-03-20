using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addstatetodocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "document",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "state_id",
                table: "document",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_state_id",
                table: "document",
                column: "state_id");

            migrationBuilder.AddForeignKey(
                name: "FK_document_workflow_state_state_id",
                table: "document",
                column: "state_id",
                principalTable: "workflow_state",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_workflow_state_state_id",
                table: "document");

            migrationBuilder.DropIndex(
                name: "IX_document_state_id",
                table: "document");

            migrationBuilder.DropColumn(
                name: "state",
                table: "document");

            migrationBuilder.DropColumn(
                name: "state_id",
                table: "document");
        }
    }
}

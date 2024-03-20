using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class updatewf_3012 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "notify_config_user_sign_complete_id",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sign_org_confirm",
                table: "workflow",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "notify_config_document_complete_id",
                table: "workflow",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "adss_profile_sign_confirm",
                table: "organization_config",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_notify_config_user_sign_complete_id",
                table: "workflow_user_sign",
                column: "notify_config_user_sign_complete_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_notify_config_document_complete_id",
                table: "workflow",
                column: "notify_config_document_complete_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_notify_config_notify_config_document_complete_id",
                table: "workflow",
                column: "notify_config_document_complete_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_user_sign_notify_config_notify_config_user_sign_co~",
                table: "workflow_user_sign",
                column: "notify_config_user_sign_complete_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_notify_config_notify_config_document_complete_id",
                table: "workflow");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_user_sign_notify_config_notify_config_user_sign_co~",
                table: "workflow_user_sign");

            migrationBuilder.DropIndex(
                name: "IX_workflow_user_sign_notify_config_user_sign_complete_id",
                table: "workflow_user_sign");

            migrationBuilder.DropIndex(
                name: "IX_workflow_notify_config_document_complete_id",
                table: "workflow");

            migrationBuilder.DropColumn(
                name: "notify_config_user_sign_complete_id",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_sign_org_confirm",
                table: "workflow");

            migrationBuilder.DropColumn(
                name: "notify_config_document_complete_id",
                table: "workflow");

            migrationBuilder.DropColumn(
                name: "adss_profile_sign_confirm",
                table: "organization_config");
        }
    }
}

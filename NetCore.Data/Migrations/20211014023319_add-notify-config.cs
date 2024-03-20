using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addnotifyconfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_user_sign_workflow_step_expire_notify_workflow_ste~",
                table: "workflow_user_sign");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_user_sign_workflow_step_remind_notify_workflow_ste~",
                table: "workflow_user_sign");

            migrationBuilder.DropIndex(
                name: "IX_workflow_user_sign_workflow_step_expire_notify_id",
                table: "workflow_user_sign");

            migrationBuilder.DropIndex(
                name: "IX_workflow_user_sign_workflow_step_remind_notify_id",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "workflow_step_expire_notify_id",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "workflow_step_remind_notify_id",
                table: "workflow_user_sign");

            migrationBuilder.AddColumn<Guid>(
                name: "notify_config_expire_id",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "notify_config_remind_id",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notify_config",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    order = table.Column<int>(nullable: false),
                    status = table.Column<bool>(nullable: false),
                    description = table.Column<string>(nullable: true),
                    created_date = table.Column<DateTime>(nullable: true),
                    created_user_id = table.Column<Guid>(nullable: true),
                    modified_date = table.Column<DateTime>(nullable: true),
                    modified_user_id = table.Column<Guid>(nullable: true),
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    day_send_noti_before = table.Column<int>(nullable: false),
                    is_repeate = table.Column<bool>(nullable: false),
                    time_send_notify = table.Column<string>(nullable: true),
                    is_send_sms = table.Column<bool>(nullable: false),
                    sms_template = table.Column<string>(nullable: true),
                    is_send_email = table.Column<bool>(nullable: false),
                    email_title_template = table.Column<string>(nullable: true),
                    email_body_template = table.Column<string>(nullable: true),
                    is_send_notification = table.Column<bool>(nullable: false),
                    notification_title_template = table.Column<string>(nullable: true),
                    notification_body_template = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notify_config", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_notify_config_expire_id",
                table: "workflow_user_sign",
                column: "notify_config_expire_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_notify_config_remind_id",
                table: "workflow_user_sign",
                column: "notify_config_remind_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_user_sign_notify_config_notify_config_expire_id",
                table: "workflow_user_sign",
                column: "notify_config_expire_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_user_sign_notify_config_notify_config_remind_id",
                table: "workflow_user_sign",
                column: "notify_config_remind_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_user_sign_notify_config_notify_config_expire_id",
                table: "workflow_user_sign");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_user_sign_notify_config_notify_config_remind_id",
                table: "workflow_user_sign");

            migrationBuilder.DropTable(
                name: "notify_config");

            migrationBuilder.DropIndex(
                name: "IX_workflow_user_sign_notify_config_expire_id",
                table: "workflow_user_sign");

            migrationBuilder.DropIndex(
                name: "IX_workflow_user_sign_notify_config_remind_id",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "notify_config_expire_id",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "notify_config_remind_id",
                table: "workflow_user_sign");

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_step_expire_notify_id",
                table: "workflow_user_sign",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_step_remind_notify_id",
                table: "workflow_user_sign",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_workflow_step_expire_notify_id",
                table: "workflow_user_sign",
                column: "workflow_step_expire_notify_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_workflow_step_remind_notify_id",
                table: "workflow_user_sign",
                column: "workflow_step_remind_notify_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_user_sign_workflow_step_expire_notify_workflow_ste~",
                table: "workflow_user_sign",
                column: "workflow_step_expire_notify_id",
                principalTable: "workflow_step_expire_notify",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_user_sign_workflow_step_remind_notify_workflow_ste~",
                table: "workflow_user_sign",
                column: "workflow_step_remind_notify_id",
                principalTable: "workflow_step_remind_notify",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

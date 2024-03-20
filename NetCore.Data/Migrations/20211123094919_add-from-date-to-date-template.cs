using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addfromdatetodatetemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_expire~",
                table: "document_notify_schedule");

            migrationBuilder.DropForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_remind~",
                table: "document_notify_schedule");

            migrationBuilder.AddColumn<DateTime>(
                name: "from_date",
                table: "document_template",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "to_date",
                table: "document_template",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "notify_config_remind_id",
                table: "document_notify_schedule",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "notify_config_expire_id",
                table: "document_notify_schedule",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "is_send",
                table: "document_notify_schedule",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_expire~",
                table: "document_notify_schedule",
                column: "notify_config_expire_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_remind~",
                table: "document_notify_schedule",
                column: "notify_config_remind_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_expire~",
                table: "document_notify_schedule");

            migrationBuilder.DropForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_remind~",
                table: "document_notify_schedule");

            migrationBuilder.DropColumn(
                name: "from_date",
                table: "document_template");

            migrationBuilder.DropColumn(
                name: "to_date",
                table: "document_template");

            migrationBuilder.DropColumn(
                name: "is_send",
                table: "document_notify_schedule");

            migrationBuilder.AlterColumn<Guid>(
                name: "notify_config_remind_id",
                table: "document_notify_schedule",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "notify_config_expire_id",
                table: "document_notify_schedule",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_expire~",
                table: "document_notify_schedule",
                column: "notify_config_expire_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_document_notify_schedule_notify_config_notify_config_remind~",
                table: "document_notify_schedule",
                column: "notify_config_remind_id",
                principalTable: "notify_config",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

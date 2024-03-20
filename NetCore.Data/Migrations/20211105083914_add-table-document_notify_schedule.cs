using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addtabledocument_notify_schedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_notify_schedule",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    document_id = table.Column<Guid>(nullable: false),
                    document_code = table.Column<string>(nullable: true),
                    document_name = table.Column<string>(nullable: true),
                    user_id = table.Column<Guid>(nullable: false),
                    user_name = table.Column<string>(nullable: true),
                    notify_config_expire_id = table.Column<Guid>(nullable: false),
                    notify_config_remind_id = table.Column<Guid>(nullable: false),
                    sign_expire_at_date = table.Column<DateTime>(nullable: false),
                    sended_remind_at_date = table.Column<DateTime>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    created_date = table.Column<DateTime>(nullable: true),
                    modified_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_notify_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_notify_schedule_notify_config_notify_config_expire~",
                        column: x => x.notify_config_expire_id,
                        principalTable: "notify_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_notify_schedule_notify_config_notify_config_remind~",
                        column: x => x.notify_config_remind_id,
                        principalTable: "notify_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_notify_schedule_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_notify_schedule_notify_config_expire_id",
                table: "document_notify_schedule",
                column: "notify_config_expire_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_notify_schedule_notify_config_remind_id",
                table: "document_notify_schedule",
                column: "notify_config_remind_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_notify_schedule_user_id",
                table: "document_notify_schedule",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_notify_schedule");
        }
    }
}

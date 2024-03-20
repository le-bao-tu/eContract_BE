using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addwfstate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "state_id",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workflow_state",
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
                    name = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_state", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_state_id",
                table: "workflow_user_sign",
                column: "state_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_user_sign_workflow_state_state_id",
                table: "workflow_user_sign",
                column: "state_id",
                principalTable: "workflow_state",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_user_sign_workflow_state_state_id",
                table: "workflow_user_sign");

            migrationBuilder.DropTable(
                name: "workflow_state");

            migrationBuilder.DropIndex(
                name: "IX_workflow_user_sign_state_id",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "state_id",
                table: "workflow_user_sign");
        }
    }
}

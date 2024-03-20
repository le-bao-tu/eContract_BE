using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class adddocumenthistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_workflow_history",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    document_id = table.Column<Guid>(nullable: false),
                    document_status = table.Column<int>(nullable: false),
                    state = table.Column<string>(nullable: true),
                    reason_reject = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    list_document_file_json = table.Column<string>(nullable: true),
                    created_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_workflow_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_workflow_history_document_document_id",
                        column: x => x.document_id,
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_workflow_history_document_id",
                table: "document_workflow_history",
                column: "document_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_workflow_history");
        }
    }
}

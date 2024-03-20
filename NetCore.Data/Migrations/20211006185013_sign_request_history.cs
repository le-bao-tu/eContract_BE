using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class sign_request_history : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sign_request_history",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    document_id = table.Column<Guid>(nullable: false),
                    document_code = table.Column<string>(nullable: true),
                    user_id = table.Column<Guid>(nullable: false),
                    user_name = table.Column<string>(nullable: true),
                    consent = table.Column<string>(nullable: true),
                    created_date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sign_request_history", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sign_request_history");
        }
    }
}

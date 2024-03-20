using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addorg_type : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "organization_type_id",
                table: "organization",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_type",
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
                    table.PrimaryKey("PK_organization_type", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_organization_type_id",
                table: "organization",
                column: "organization_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_organization_organization_type_organization_type_id",
                table: "organization",
                column: "organization_type_id",
                principalTable: "organization_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organization_organization_type_organization_type_id",
                table: "organization");

            migrationBuilder.DropTable(
                name: "organization_type");

            migrationBuilder.DropIndex(
                name: "IX_organization_organization_type_id",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "organization_type_id",
                table: "organization");
        }
    }
}

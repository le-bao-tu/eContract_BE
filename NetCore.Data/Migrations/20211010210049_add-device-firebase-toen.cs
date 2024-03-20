using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class adddevicefirebasetoen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_store_idp",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "list_email_reception_json",
                table: "document",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "one_time_pass_code",
                table: "document",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pass_code_expire_date",
                table: "document",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_map_device",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    device_id = table.Column<string>(nullable: false),
                    device_name = table.Column<string>(nullable: true),
                    isIdentifierDevice = table.Column<bool>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_map_device", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_map_firebase_token",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    device_id = table.Column<string>(nullable: false),
                    firebase_token = table.Column<string>(nullable: true),
                    created_date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_map_firebase_token", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_map_device");

            migrationBuilder.DropTable(
                name: "user_map_firebase_token");

            migrationBuilder.DropColumn(
                name: "user_store_idp",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "list_email_reception_json",
                table: "document");

            migrationBuilder.DropColumn(
                name: "one_time_pass_code",
                table: "document");

            migrationBuilder.DropColumn(
                name: "pass_code_expire_date",
                table: "document");
        }
    }
}

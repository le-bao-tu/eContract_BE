using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addtablesendsms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sms_config_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "send_sms",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    source_addr = table.Column<string>(nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    message = table.Column<string>(nullable: true),
                    is_push = table.Column<bool>(nullable: false),
                    create_date = table.Column<DateTime>(nullable: false),
                    send_time = table.Column<DateTime>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    send_sms_response_json = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_send_sms", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "send_sms");

            migrationBuilder.DropColumn(
                name: "sms_config_json",
                table: "organization_config");
        }
    }
}

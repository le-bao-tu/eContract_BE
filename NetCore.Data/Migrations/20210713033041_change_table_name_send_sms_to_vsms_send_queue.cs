using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class change_table_name_send_sms_to_vsms_send_queue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "send_sms");

            migrationBuilder.CreateTable(
                name: "vsms_send_queue",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    source_addr = table.Column<string>(nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    message = table.Column<string>(nullable: true),
                    is_push = table.Column<bool>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: false),
                    sent_time = table.Column<DateTime>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    send_sms_response_json = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vsms_send_queue", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vsms_send_queue");

            migrationBuilder.CreateTable(
                name: "send_sms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_push = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    send_sms_response_json = table.Column<string>(type: "text", nullable: true),
                    send_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    source_addr = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_send_sms", x => x.id);
                });
        }
    }
}

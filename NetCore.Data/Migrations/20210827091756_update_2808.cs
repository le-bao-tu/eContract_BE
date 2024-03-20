using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class update_2808 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact");

            migrationBuilder.DropTable(
                name: "workflow_history");

            migrationBuilder.DropTable(
                name: "workflow_document");

            migrationBuilder.AddColumn<int>(
                name: "sign_expire_after_day",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state_name",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_data_bucket_name",
                table: "document_file_template",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_data_object_name",
                table: "document_file_template",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "file_type",
                table: "document_file_template",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "sign_expire_at_date",
                table: "document",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sign_expire_after_day",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "state_name",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "file_data_bucket_name",
                table: "document_file_template");

            migrationBuilder.DropColumn(
                name: "file_data_object_name",
                table: "document_file_template");

            migrationBuilder.DropColumn(
                name: "file_type",
                table: "document_file_template");

            migrationBuilder.DropColumn(
                name: "sign_expire_at_date",
                table: "document");

            migrationBuilder.CreateTable(
                name: "contact",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    modified_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", maxLength: 128, nullable: true),
                    organization_name = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    position_id = table.Column<Guid>(type: "uuid", maxLength: 128, nullable: true),
                    position_name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", maxLength: 128, nullable: true),
                    user_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    modified_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_document_workflow_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_apporve = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "text", nullable: true),
                    workflow_document_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_history_workflow_document_workflow_document_id",
                        column: x => x.workflow_document_id,
                        principalTable: "workflow_document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_document_workflow_id",
                table: "workflow_document",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_workflow_document_id",
                table: "workflow_history",
                column: "workflow_document_id");
        }
    }
}

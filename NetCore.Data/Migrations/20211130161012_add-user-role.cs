using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class adduserrole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_for_reject",
                table: "workflow_state",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "role",
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
                    code = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: false),
                    organization_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_map_document_organization",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    role_id = table.Column<Guid>(nullable: false),
                    organization_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_map_document_organization", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_map_document_organization_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_map_document_organization_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_map_document_type",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    role_id = table.Column<Guid>(nullable: false),
                    document_type_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_map_document_type", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_map_document_type_document_type_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "document_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_map_document_type_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_map_userinfo_organization",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    role_id = table.Column<Guid>(nullable: false),
                    organization_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_map_userinfo_organization", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_map_userinfo_organization_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_map_userinfo_organization_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_map_role",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    role_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_map_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_map_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_map_role_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_organization_id",
                table: "role",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_document_organization_organization_id",
                table: "role_map_document_organization",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_document_organization_role_id",
                table: "role_map_document_organization",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_document_type_document_type_id",
                table: "role_map_document_type",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_document_type_role_id",
                table: "role_map_document_type",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_userinfo_organization_organization_id",
                table: "role_map_userinfo_organization",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_userinfo_organization_role_id",
                table: "role_map_userinfo_organization",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_map_role_role_id",
                table: "user_map_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_map_role_user_id",
                table: "user_map_role",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_map_document_organization");

            migrationBuilder.DropTable(
                name: "role_map_document_type");

            migrationBuilder.DropTable(
                name: "role_map_userinfo_organization");

            migrationBuilder.DropTable(
                name: "user_map_role");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropColumn(
                name: "name_for_reject",
                table: "workflow_state");
        }
    }
}

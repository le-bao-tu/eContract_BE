using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class menuroleright : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "navigation",
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
                    i18n_name = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: false),
                    icon = table.Column<string>(nullable: false),
                    link = table.Column<string>(nullable: false),
                    hide_in_breadcrumb = table.Column<bool>(nullable: false),
                    parent_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "right",
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
                    groupname = table.Column<string>(name: "group-name", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_right", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "navigation_map_role",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    navigation_id = table.Column<Guid>(nullable: false),
                    role_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigation_map_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_navigation_map_role_navigation_navigation_id",
                        column: x => x.navigation_id,
                        principalTable: "navigation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_navigation_map_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_map_right",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    role_id = table.Column<Guid>(nullable: false),
                    right_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_map_right", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_map_right_right_right_id",
                        column: x => x.right_id,
                        principalTable: "right",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_map_right_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_navigation_map_role_navigation_id",
                table: "navigation_map_role",
                column: "navigation_id");

            migrationBuilder.CreateIndex(
                name: "IX_navigation_map_role_role_id",
                table: "navigation_map_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_right_right_id",
                table: "role_map_right",
                column: "right_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_map_right_role_id",
                table: "role_map_right",
                column: "role_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "navigation_map_role");

            migrationBuilder.DropTable(
                name: "role_map_right");

            migrationBuilder.DropTable(
                name: "navigation");

            migrationBuilder.DropTable(
                name: "right");
        }
    }
}

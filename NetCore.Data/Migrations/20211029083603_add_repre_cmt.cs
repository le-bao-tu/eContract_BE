using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class add_repre_cmt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "identity_back_bucket_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identity_back_object_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identity_front_bucket_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identity_front_object_name",
                table: "organization",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "identity_back_bucket_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "identity_back_object_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "identity_front_bucket_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "identity_front_object_name",
                table: "organization");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class addeformorg : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "confirm_digital_signature_document_type_code",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_certificate_document_type_code",
                table: "organization_config",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confirm_digital_signature_document_type_code",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "request_certificate_document_type_code",
                table: "organization_config");
        }
    }
}

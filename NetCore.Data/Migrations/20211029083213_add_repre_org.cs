using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class add_repre_org : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bussiness_license_bucket_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bussiness_license_object_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "repre_birthday",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_country_code",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "repre_country_id",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_country_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_current_address",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_email",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_full_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_issueby",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "repre_issue_date",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_permanent_address",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_phone_number",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_province_code",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "repre_province_id",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "repre_province_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "repre_sex",
                table: "organization",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bussiness_license_bucket_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "bussiness_license_object_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_birthday",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_country_code",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_country_id",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_country_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_current_address",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_email",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_full_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_issueby",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_issue_date",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_permanent_address",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_phone_number",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_province_code",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_province_id",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_province_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "repre_sex",
                table: "organization");
        }
    }
}

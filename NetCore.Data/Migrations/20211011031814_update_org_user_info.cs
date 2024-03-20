using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class update_org_user_info : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "certificate_base64",
                table: "user_hsm_account",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_dn",
                table: "user_hsm_account",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "district_id",
                table: "user",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "district_name",
                table: "user",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject_dn",
                table: "user",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_eform_info_json",
                table: "user",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zip_code",
                table: "user",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zip_code",
                table: "province",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "district_id",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "district_name",
                table: "organization",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zip_code",
                table: "organization",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "certificate_base64",
                table: "user_hsm_account");

            migrationBuilder.DropColumn(
                name: "subject_dn",
                table: "user_hsm_account");

            migrationBuilder.DropColumn(
                name: "district_id",
                table: "user");

            migrationBuilder.DropColumn(
                name: "district_name",
                table: "user");

            migrationBuilder.DropColumn(
                name: "subject_dn",
                table: "user");

            migrationBuilder.DropColumn(
                name: "user_eform_info_json",
                table: "user");

            migrationBuilder.DropColumn(
                name: "zip_code",
                table: "user");

            migrationBuilder.DropColumn(
                name: "zip_code",
                table: "province");

            migrationBuilder.DropColumn(
                name: "district_id",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "district_name",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "zip_code",
                table: "organization");
        }
    }
}

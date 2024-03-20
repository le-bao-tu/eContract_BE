using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class adddynamicconfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_email",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "user_phonenumber",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "user_position_name",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "default_request_headers_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "email_code",
                table: "organization_config");

            migrationBuilder.AddColumn<string>(
                name: "consent_sign_config_json",
                table: "workflow_user_sign",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_sign",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_send_mail_noti_result",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_send_mail_noti_sign",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_send_otp_noti_sign",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sign_certify",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sign_ltv",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sign_tsa",
                table: "workflow_user_sign",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_enable_smart_otp",
                table: "user",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "callback_authorization_url",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_request_callback_authorization_headers_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_request_callback_headers_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_request_sms_authorization_headers_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_request_sms_headers_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email_config_json",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_approve_certify",
                table: "organization_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_approve_ltv",
                table: "organization_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_approve_tsa",
                table: "organization_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_callback_authorization",
                table: "organization_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sms_authorization",
                table: "organization_config",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_document_type",
                table: "organization_config",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "organization_title",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sms_authorization_url",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sms_otp_template",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sms_type",
                table: "organization_config",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "sms_url",
                table: "organization_config",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "template_per_document_type",
                table: "organization_config",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consent_sign_config_json",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_auto_sign",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_send_mail_noti_result",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_send_mail_noti_sign",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_send_otp_noti_sign",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_sign_certify",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_sign_ltv",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_sign_tsa",
                table: "workflow_user_sign");

            migrationBuilder.DropColumn(
                name: "is_enable_smart_otp",
                table: "user");

            migrationBuilder.DropColumn(
                name: "callback_authorization_url",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "default_request_callback_authorization_headers_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "default_request_callback_headers_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "default_request_sms_authorization_headers_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "default_request_sms_headers_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "email_config_json",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "is_approve_certify",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "is_approve_ltv",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "is_approve_tsa",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "is_callback_authorization",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "is_sms_authorization",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "max_document_type",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "organization_title",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "sms_authorization_url",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "sms_otp_template",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "sms_type",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "sms_url",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "template_per_document_type",
                table: "organization_config");

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "workflow_user_sign",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_phonenumber",
                table: "workflow_user_sign",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_position_name",
                table: "workflow_user_sign",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_request_headers_json",
                table: "organization_config",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email_code",
                table: "organization_config",
                type: "text",
                nullable: true);
        }
    }
}

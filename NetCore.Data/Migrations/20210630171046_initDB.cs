using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCore.Data.Migrations
{
    public partial class initDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact",
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
                    application_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    email = table.Column<string>(maxLength: 128, nullable: true),
                    phone_number = table.Column<string>(maxLength: 128, nullable: true),
                    organization_id = table.Column<Guid>(maxLength: 128, nullable: true),
                    organization_name = table.Column<string>(nullable: true),
                    address = table.Column<string>(nullable: true),
                    position_id = table.Column<Guid>(maxLength: 128, nullable: true),
                    position_name = table.Column<string>(nullable: true),
                    user_id = table.Column<Guid>(maxLength: 128, nullable: true),
                    user_name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country",
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
                    code = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_sign_history",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    file_type = table.Column<int>(nullable: false),
                    old_file_bucket_name = table.Column<string>(nullable: true),
                    old_file_object_name = table.Column<string>(nullable: true),
                    old_file_name = table.Column<string>(nullable: true),
                    old_hash_file = table.Column<string>(nullable: true),
                    old_xml_file = table.Column<string>(nullable: true),
                    new_file_bucket_name = table.Column<string>(nullable: true),
                    new_file_object_name = table.Column<string>(nullable: true),
                    new_file_name = table.Column<string>(nullable: true),
                    new_hash_file = table.Column<string>(nullable: true),
                    new_xml_file = table.Column<string>(nullable: true),
                    document_id = table.Column<Guid>(nullable: false),
                    document_file_id = table.Column<Guid>(nullable: false),
                    description = table.Column<string>(nullable: true),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_sign_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_type",
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
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_account",
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
                    application_id = table.Column<Guid>(nullable: true),
                    identity_number = table.Column<long>(nullable: false),
                    code = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true),
                    from = table.Column<string>(nullable: true),
                    smtp = table.Column<string>(nullable: true),
                    port = table.Column<int>(nullable: false),
                    user = table.Column<string>(nullable: true),
                    send_type = table.Column<string>(nullable: true),
                    password = table.Column<string>(nullable: true),
                    ssl = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meta_data",
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
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    data_type = table.Column<int>(nullable: false),
                    is_require = table.Column<bool>(nullable: false),
                    list_data_json = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meta_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization",
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
                    application_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    parent_id = table.Column<Guid>(nullable: true),
                    tax_code = table.Column<string>(nullable: true),
                    identify_number = table.Column<string>(nullable: true),
                    issuer_by = table.Column<string>(nullable: true),
                    issuer_date = table.Column<DateTime>(nullable: true),
                    country_id = table.Column<Guid>(nullable: true),
                    country_name = table.Column<string>(nullable: true),
                    province_id = table.Column<Guid>(nullable: true),
                    province_name = table.Column<string>(nullable: true),
                    address = table.Column<string>(nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    email = table.Column<string>(nullable: true),
                    path = table.Column<string>(nullable: true),
                    is_deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_config",
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
                    organization_id = table.Column<Guid>(nullable: false),
                    email_code = table.Column<string>(nullable: false),
                    bucket_name = table.Column<string>(nullable: false),
                    object_name = table.Column<string>(nullable: false),
                    file_name = table.Column<string>(nullable: false),
                    callback_url = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "position",
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
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_appliation",
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
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_appliation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
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
                    user_id = table.Column<Guid>(nullable: false),
                    is_user = table.Column<bool>(nullable: false),
                    is_org_admin = table.Column<bool>(nullable: false),
                    is_system_admin = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow",
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
                    application_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    organization_id = table.Column<Guid>(maxLength: 128, nullable: true),
                    user_id = table.Column<Guid>(maxLength: 128, nullable: true),
                    is_deleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "province",
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
                    code = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true),
                    country_id = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_province", x => x.id);
                    table.ForeignKey(
                        name: "FK_province_country_country_id",
                        column: x => x.country_id,
                        principalTable: "country",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_batch",
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
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    document_type_id = table.Column<Guid>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    workflow_id = table.Column<Guid>(nullable: true),
                    workflow_contact_json = table.Column<string>(nullable: true),
                    number_of_email_per_week = table.Column<int>(nullable: false),
                    is_generateFile = table.Column<bool>(nullable: false),
                    list_meta_data_json = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_batch", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_batch_document_type_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "document_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_template",
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
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    document_type_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_template_document_type_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "document_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queue_send_email",
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
                    to_emails_json = table.Column<string>(nullable: true),
                    cc_emails_json = table.Column<string>(nullable: true),
                    bcc_emails_json = table.Column<string>(nullable: true),
                    title = table.Column<string>(nullable: true),
                    body = table.Column<string>(nullable: true),
                    base64_image = table.Column<string>(nullable: true),
                    is_sended = table.Column<bool>(nullable: false),
                    email_account_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queue_send_email", x => x.id);
                    table.ForeignKey(
                        name: "FK_queue_send_email_email_account_email_account_id",
                        column: x => x.email_account_id,
                        principalTable: "email_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user",
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
                    application_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: true),
                    name = table.Column<string>(maxLength: 128, nullable: true),
                    connect_id = table.Column<string>(maxLength: 256, nullable: true),
                    email = table.Column<string>(maxLength: 128, nullable: true),
                    phone_number = table.Column<string>(maxLength: 128, nullable: true),
                    user_name = table.Column<string>(maxLength: 128, nullable: false),
                    birthday = table.Column<DateTime>(nullable: true),
                    sex = table.Column<int>(nullable: true),
                    identity_type = table.Column<string>(nullable: true),
                    identity_number = table.Column<string>(nullable: true),
                    issuer_date = table.Column<DateTime>(nullable: true),
                    issuer_by = table.Column<string>(nullable: true),
                    country_id = table.Column<Guid>(nullable: true),
                    country_name = table.Column<string>(nullable: true),
                    province_id = table.Column<Guid>(nullable: true),
                    province_name = table.Column<string>(nullable: true),
                    position_name = table.Column<string>(nullable: true),
                    address = table.Column<string>(nullable: true),
                    password = table.Column<string>(nullable: true),
                    password_salt = table.Column<string>(nullable: true),
                    last_activity_date = table.Column<DateTime>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    is_lock = table.Column<bool>(nullable: false),
                    is_deleted = table.Column<bool>(nullable: false),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_document",
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
                    application_id = table.Column<Guid>(nullable: true),
                    workflow_id = table.Column<Guid>(nullable: false),
                    document_id = table.Column<Guid>(nullable: false),
                    state = table.Column<string>(nullable: true)
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
                name: "workflow_user_sign",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: true),
                    state = table.Column<string>(maxLength: 128, nullable: true),
                    workflow_id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: true),
                    user_name = table.Column<string>(nullable: true),
                    user_email = table.Column<string>(nullable: true),
                    user_phonenumber = table.Column<string>(nullable: true),
                    user_position_name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_user_sign", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_user_sign_workflow_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "district",
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
                    code = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true),
                    province_id = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_district", x => x.id);
                    table.ForeignKey(
                        name: "FK_district_province_province_id",
                        column: x => x.province_id,
                        principalTable: "province",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document",
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
                    application_id = table.Column<Guid>(nullable: true),
                    organization_id = table.Column<Guid>(nullable: true),
                    code = table.Column<string>(maxLength: 64, nullable: false),
                    name = table.Column<string>(maxLength: 128, nullable: false),
                    email = table.Column<string>(maxLength: 128, nullable: true),
                    phone_number = table.Column<string>(maxLength: 128, nullable: true),
                    full_name = table.Column<string>(maxLength: 128, nullable: true),
                    workflow_id = table.Column<Guid>(nullable: false),
                    document_type_id = table.Column<Guid>(nullable: true),
                    document_batch_id = table.Column<Guid>(nullable: true),
                    document_3rd_id = table.Column<string>(nullable: true),
                    document_status = table.Column<int>(nullable: false),
                    workflow_start_date = table.Column<DateTime>(nullable: true),
                    next_step_id = table.Column<Guid>(nullable: true),
                    next_step_user_id = table.Column<Guid>(nullable: true),
                    next_step_user_name = table.Column<string>(nullable: true),
                    next_step_user_email = table.Column<string>(nullable: true),
                    next_step_user_phone_number = table.Column<string>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    next_step_sign_type = table.Column<int>(nullable: false),
                    workflow_user_json = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_document_batch_document_batch_id",
                        column: x => x.document_batch_id,
                        principalTable: "document_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_document_type_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "document_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_file_template",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    file_bucket_name = table.Column<string>(nullable: true),
                    file_object_name = table.Column<string>(nullable: true),
                    file_name = table.Column<string>(nullable: true),
                    profile_name = table.Column<string>(nullable: true),
                    document_template_id = table.Column<Guid>(nullable: false),
                    meta_data_config_json = table.Column<string>(nullable: true),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_file_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_file_template_document_template_document_template_~",
                        column: x => x.document_template_id,
                        principalTable: "document_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_meta_data_config",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    document_template_id = table.Column<Guid>(nullable: false),
                    meta_data_id = table.Column<Guid>(nullable: false),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_meta_data_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_meta_data_config_document_template_document_templa~",
                        column: x => x.document_template_id,
                        principalTable: "document_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_meta_data_config_meta_data_meta_data_id",
                        column: x => x.meta_data_id,
                        principalTable: "meta_data",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sign_config",
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
                    application_id = table.Column<Guid>(nullable: true),
                    identity_number = table.Column<long>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    appearance_sign_type = table.Column<string>(nullable: false),
                    code = table.Column<string>(nullable: false),
                    list_sign_info_json = table.Column<string>(nullable: true),
                    file_base64 = table.Column<string>(nullable: false),
                    is_sign_default = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sign_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sign_config_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_history",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    workflow_document_id = table.Column<Guid>(nullable: false),
                    state = table.Column<string>(nullable: true),
                    is_apporve = table.Column<bool>(nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ward",
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
                    code = table.Column<string>(nullable: true),
                    name = table.Column<string>(nullable: true),
                    disctrict_id = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ward", x => x.id);
                    table.ForeignKey(
                        name: "FK_ward_district_disctrict_id",
                        column: x => x.disctrict_id,
                        principalTable: "district",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_file",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    file_bucket_name = table.Column<string>(nullable: true),
                    file_object_name = table.Column<string>(nullable: true),
                    file_name = table.Column<string>(nullable: true),
                    hash_file = table.Column<string>(nullable: true),
                    xml_file = table.Column<string>(nullable: true),
                    file_type = table.Column<int>(nullable: false),
                    document_id = table.Column<Guid>(nullable: false),
                    document_file_template_id = table.Column<Guid>(nullable: false),
                    profile_name = table.Column<string>(nullable: true),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_file_document_document_id",
                        column: x => x.document_id,
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_batch_file",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    file_bucket_name = table.Column<string>(nullable: true),
                    file_object_name = table.Column<string>(nullable: true),
                    file_name = table.Column<string>(nullable: true),
                    document_batch_id = table.Column<Guid>(nullable: false),
                    document_file_template_id = table.Column<Guid>(nullable: true),
                    order = table.Column<int>(nullable: false),
                    created_date = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_batch_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_batch_file_document_batch_document_batch_id",
                        column: x => x.document_batch_id,
                        principalTable: "document_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_batch_file_document_file_template_document_file_te~",
                        column: x => x.document_file_template_id,
                        principalTable: "document_file_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_district_province_id",
                table: "district",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_document_batch_id",
                table: "document",
                column: "document_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_document_type_id",
                table: "document",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_batch_document_type_id",
                table: "document_batch",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_batch_file_document_batch_id",
                table: "document_batch_file",
                column: "document_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_batch_file_document_file_template_id",
                table: "document_batch_file",
                column: "document_file_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_file_document_id",
                table: "document_file",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_file_template_document_template_id",
                table: "document_file_template",
                column: "document_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_meta_data_config_document_template_id",
                table: "document_meta_data_config",
                column: "document_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_meta_data_config_meta_data_id",
                table: "document_meta_data_config",
                column: "meta_data_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_template_document_type_id",
                table: "document_template",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_province_country_id",
                table: "province",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_queue_send_email_email_account_id",
                table: "queue_send_email",
                column: "email_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_organization_id",
                table: "user",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sign_config_user_id",
                table: "user_sign_config",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ward_disctrict_id",
                table: "ward",
                column: "disctrict_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_document_workflow_id",
                table: "workflow_document",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_workflow_document_id",
                table: "workflow_history",
                column: "workflow_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_user_sign_workflow_id",
                table: "workflow_user_sign",
                column: "workflow_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact");

            migrationBuilder.DropTable(
                name: "document_batch_file");

            migrationBuilder.DropTable(
                name: "document_file");

            migrationBuilder.DropTable(
                name: "document_meta_data_config");

            migrationBuilder.DropTable(
                name: "document_sign_history");

            migrationBuilder.DropTable(
                name: "organization_config");

            migrationBuilder.DropTable(
                name: "position");

            migrationBuilder.DropTable(
                name: "queue_send_email");

            migrationBuilder.DropTable(
                name: "system_appliation");

            migrationBuilder.DropTable(
                name: "user_role");

            migrationBuilder.DropTable(
                name: "user_sign_config");

            migrationBuilder.DropTable(
                name: "ward");

            migrationBuilder.DropTable(
                name: "workflow_history");

            migrationBuilder.DropTable(
                name: "workflow_user_sign");

            migrationBuilder.DropTable(
                name: "document_file_template");

            migrationBuilder.DropTable(
                name: "document");

            migrationBuilder.DropTable(
                name: "meta_data");

            migrationBuilder.DropTable(
                name: "email_account");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "district");

            migrationBuilder.DropTable(
                name: "workflow_document");

            migrationBuilder.DropTable(
                name: "document_template");

            migrationBuilder.DropTable(
                name: "document_batch");

            migrationBuilder.DropTable(
                name: "organization");

            migrationBuilder.DropTable(
                name: "province");

            migrationBuilder.DropTable(
                name: "workflow");

            migrationBuilder.DropTable(
                name: "document_type");

            migrationBuilder.DropTable(
                name: "country");
        }
    }
}

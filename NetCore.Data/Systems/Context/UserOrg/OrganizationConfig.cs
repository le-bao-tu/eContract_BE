using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("organization_config")]
    public class OrganizationConfig : BaseTableDefault
    {
        public OrganizationConfig()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("user_store_idp")]
        public string UserStoreIDP { get; set; }

        [Column("organization_title")]
        public string OrganizationTitle { get; set; }

        #region Logo đơn vị

        //[Column("bucket_name")]
        //public string BucketName { get; set; }

        //[Column("object_name")]
        //public string ObjectName { get; set; }

        //[Column("file_name")]
        //public string FileName { get; set; }

        [Column("logo_file_base64")]
        public string LogoFileBase64 { get; set; }

        #endregion

        [Column("consumer_key")]
        public string ConsumerKey { get; set; }

        #region Callback URL - Thông báo kết quả nhận được từ hệ thống
        [Column("notify_send_type")]
        public NotifySendType NotifySendType { get; set; }

        [Column("is_callback_authorization")]
        public bool IsCallbackAuthorization { get; set; } = false;

        [Column("callback_authorization_url")]
        public string CallbackAuthorizationUrl { get; set; }

        [Column("default_request_callback_authorization_headers_json")]
        public string DefaultRequestCallBackAuthorizationHeadersJson
        {
            get
            {
                return DefaultRequestCallBackAuthorizationHeaders == null ? null : JsonSerializer.Serialize(DefaultRequestCallBackAuthorizationHeaders);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    DefaultRequestCallBackAuthorizationHeaders = null;
                else
                    DefaultRequestCallBackAuthorizationHeaders = JsonSerializer.Deserialize<List<DefaultRequestHeader>>(value);
            }
        }

        [NotMapped]
        public List<DefaultRequestHeader> DefaultRequestCallBackAuthorizationHeaders { get; set; }

        [Column("callback_url")]
        public string CallbackUrl { get; set; }

        [Column("default_request_callback_headers_json")]
        public string DefaultRequestCallbackHeadersJson
        {
            get
            {
                return DefaultRequestCallbackHeaders == null ? null : JsonSerializer.Serialize(DefaultRequestCallbackHeaders);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    DefaultRequestCallbackHeaders = null;
                else
                    DefaultRequestCallbackHeaders = JsonSerializer.Deserialize<List<DefaultRequestHeader>>(value);
            }
        }

        [NotMapped]
        public List<DefaultRequestHeader> DefaultRequestCallbackHeaders { get; set; }
        #endregion

        #region Số loại hợp đồng giới hạn cấu hình trên hệ thống
        [Column("max_document_type")]
        public int MaxDocumentType { get; set; }

        [Column("template_per_document_type")]
        public int TemplatePerDocumentType { get; set; }
        #endregion

        #region SMS
        [Column("sms_send_type")]
        public SMSSendType SMSSendType { get; set; }

        [Column("sms_otp_template")]
        public string SMSOTPTemplate { get; set; }

        [Column("sms_config_json")]
        public string SMSConfigJson
        {

            get
            {
                return SMSConfig == null ? null : JsonSerializer.Serialize(SMSConfig);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    SMSConfig = null;
                else
                    SMSConfig = JsonSerializer.Deserialize<SMSConfig>(value);
            }
        }

        [NotMapped]
        public SMSConfig SMSConfig { get; set; }

        // Gọi SMS qua gateway khách hàng cung cấp
        [Column("is_sms_authorization")]
        public bool IsSMSAuthorization { get; set; } = false;

        [Column("sms_authorization_url")]
        public string SMSAuthorizationUrl { get; set; }

        [Column("default_request_sms_authorization_headers_json")]
        public string DefaultRequestSMSAuthorizationHeadersJson
        {
            get
            {
                return DefaultRequestSMSAuthorizationHeaders == null ? null : JsonSerializer.Serialize(DefaultRequestSMSAuthorizationHeaders);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    DefaultRequestSMSAuthorizationHeaders = null;
                else
                    DefaultRequestSMSAuthorizationHeaders = JsonSerializer.Deserialize<List<DefaultRequestHeader>>(value);
            }
        }

        [NotMapped]
        public List<DefaultRequestHeader> DefaultRequestSMSAuthorizationHeaders { get; set; }

        [Column("sms_url")]
        public string SMSUrl { get; set; }

        [Column("default_request_sms_headers_json")]
        public string DefaultRequestSMSHeadersJson
        {
            get
            {
                return DefaultRequestSMSHeaders == null ? null : JsonSerializer.Serialize(DefaultRequestSMSHeaders);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    DefaultRequestSMSHeaders = null;
                else
                    DefaultRequestSMSHeaders = JsonSerializer.Deserialize<List<DefaultRequestHeader>>(value);
            }
        }

        [NotMapped]
        public List<DefaultRequestHeader> DefaultRequestSMSHeaders { get; set; }

        #endregion

        #region Email
        [Column("email_config_json")]
        public string EmailConfigJson
        {
            get
            {
                return EmailConfig == null ? null : JsonSerializer.Serialize(EmailConfig);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    EmailConfig = null;
                else
                    EmailConfig = JsonSerializer.Deserialize<EmailConfig>(value);
            }
        }

        [NotMapped]
        public EmailConfig EmailConfig { get; set; }
        #endregion

        #region LTV, TSA, Certify
        [Column("is_approve_ltv")]
        public bool IsApproveLTV { get; set; }

        [Column("is_approve_tsa")]
        public bool IsApproveTSA { get; set; }

        [Column("is_approve_certify")]
        public bool IsApproveCertify { get; set; }
        #endregion

        #region Ký số, ký điện tử, ký HSM
        //TODO: Đang phân vân ko biết nên cho vào hay ko

        #endregion

        // Khách hàng có sử dụng UI eContract hay không?
        [Column("is_use_ui")]
        public bool IsUseUI { get; set; }

        // Cấu hình sử dụng ký mặc định là sử dụng CTS hay ký điện tử => để tạo eForm cho đúng
        [Column("eform_config")]
        public EFormConfigEnum EFormConfig { get; set; } = EFormConfigEnum.KY_DIEN_TU;

        [Column("user_name_prefix")]
        public string UserNamePrefix { get; set; }

        // Khách hàng có sử dụng chức năng cấu hình ký nâng cao hay không??? Nếu có thì bật cấu hình theo text lên cho khách hàng dùng
        [Column("is_approve_sign_dynamic_position")]
        public bool IsApproveSignDynamicPosition { get; set; }

        [Column("confirm_digital_signature_document_type_code")]
        public string ConfirmDigitalSignatureDocumentTypeCode { get; set; }

        [Column("request_certificate_document_type_code")]
        public string RequestCertificateDocumentTypeCode { get; set; }

        #region Thông tin khi ký xác nhận tổ chức
        [Column("adss_profile_sign_confirm")]
        public string ADSSProfileSignConfirm { get; set; }
        #endregion

        //#region Hmac OTP
        //[Column("hotp_senconds_valid")]
        //public int HOTPSecondsValid { get; set; }

        //[Column("hotp_wrong_times")]
        //public int HOTPWrongTimes { get; set; }

        //[Column("hotp_wrong_duration")]
        //public int HOTPWrongDuration { get; set; }
        //#endregion

        //Khách hàng có sử dụng ảnh preview hay không?
        [Column("use_image_preview")]
        public bool UseImagePreview { get; set; } = false;

        #region Thông tin chữ ký mặc định của đơn vị
        [Column("sign_info_default_json")]
        public string SignInfoDefaultJson
        {
            get
            {
                return SignInfoDefault == null ? null : JsonSerializer.Serialize(SignInfoDefault);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    SignInfoDefault = null;
                else
                    SignInfoDefault = JsonSerializer.Deserialize<SignInfoDefault>(value);
            }
        }

        [NotMapped]
        public SignInfoDefault SignInfoDefault { get; set; }
        #endregion
        
        [Column("is_use_everify")]
        public bool IsUseEverify { get; set; }
    }

    public class SignInfoDefault
    {
        public bool IsSignBy { get; set; }
        public bool IsOrganization { get; set; }
        public bool IsPosition { get; set; }
        public bool IsEmail { get; set; }
        public bool IsPhoneNumber { get; set; }
        public bool IsTimestemp { get; set; }
        public bool IsReason { get; set; }
        public bool IsLocation { get; set; }
        public bool IsContact { get; set; }
        public string MoreInfo { get; set; }
        public string BackgroundImageBase64 { get; set; }
    }

    public class DefaultRequestHeader
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class EmailConfig
    {
        public EmailType Type { get; set; } = EmailType.GMAIL;
        public string From { get; set; }
        public string SMTP { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string User { get; set; }
        public string SendType { get; set; } = "sync";
        public string Password { get; set; }
        public string SSL { get; set; } = "1";

        /*
            "from": "savis.econtract@gmail.com",
            "smtp": "smtp.gmail.com",
            "port": 587,
            "user": "Savis eContract",
            "sendtype": "sync",
            "password": "Savis@123",
            "ssl": "1"
         */
    }

    public enum EmailType
    {
        GMAIL = 1
    }

    public class SMSConfig
    {
        public SMSService Service { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Brandname { get; set; }
    }

    public enum SMSService
    {
        VSMS = 1,
    }

    public enum SMSSendType
    {
        VSMS_GATEWAY = 1,
        GHTK_GATEWAY = 2,
        SNF_GATEWAY = 3
    }

    public enum NotifySendType
    {
        MAVIN_GATEWAY = 1,
        GHTK_GATEWAY = 2,
        SNF_GATEWAY = 3
    }

    public enum EFormConfigEnum
    {
        KY_DIEN_TU = 1,
        KY_CTS_CA_NHAN = 2
    }
}

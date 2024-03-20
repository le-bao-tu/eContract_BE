using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class OrganizationConfigBaseModel
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public string OrganizationTitle { get; set; }

        //public string BucketName { get; set; }

        //public string ObjectName { get; set; }

        //public string FileName { get; set; }

        public string LogoFileBase64 { get; set; }

        public string ConsumerKey { get; set; }

        public string UserStoreIDP { get; set; }

        public NotifySendType NotifySendType { get; set; }

        public bool IsCallbackAuthorization { get; set; } = false;

        public string CallbackAuthorizationUrl { get; set; }

        public List<DefaultRequestHeader> DefaultRequestCallBackAuthorizationHeaders { get; set; }

        public string CallbackUrl { get; set; }

        public List<DefaultRequestHeader> DefaultRequestCallbackHeaders { get; set; }

        public int MaxDocumentType { get; set; }

        public int TemplatePerDocumentType { get; set; }

        public SMSConfig SMSConfig { get; set; }

        public EmailConfig EmailConfig { get; set; }

        public bool IsApproveLTV { get; set; }

        public bool IsApproveTSA { get; set; }

        public bool IsApproveCertify { get; set; }

        public EFormConfigEnum EFormConfig { get; set; }

        public string UserNamePrefix { get; set; }

        // Khách hàng có sử dụng UI eContract hay không?

        public bool IsUseUI { get; set; }

        // Khách hàng có sử dụng chức năng cấu hình ký nâng cao hay không??? Nếu có thì bật cấu hình theo text lên cho khách hàng dùng

        public bool IsApproveSignDynamicPosition { get; set; }

        public SMSSendType SMSSendType { get; set; }
        public string SMSOTPTemplate { get; set; }
        public bool IsSMSAuthorization { get; set; } = false;
        public string SMSAuthorizationUrl { get; set; }
        public List<DefaultRequestHeader> DefaultRequestSMSAuthorizationHeaders { get; set; }
        public string SMSUrl { get; set; }
        public List<DefaultRequestHeader> DefaultRequestSMSHeaders { get; set; }
        public string ADSSProfileSignConfirm { get; set; }

        public SignInfoDefault SignInfoDefault { get; set; }
        public bool UseImagePreview { get; set; } = false;
        public bool IsUseEverify { get; set; }
    }

    public class OrganizationConfigModel : OrganizationConfigBaseModel
    {
        [JsonIgnore]
        public Guid? CreatedUserId { get; set; }

        [JsonIgnore]
        public Guid? ModifiedUserId { get; set; }

        [JsonIgnore]
        public Guid? ApplicationId { get; set; }

        public void UpdateToEntity(OrganizationConfig entity)
        {
            //entity.Code = this.Code;
            entity.OrganizationTitle = this.OrganizationTitle;
            //entity.BucketName = this.BucketName;
            //entity.ObjectName = this.ObjectName;
            //entity.FileName = this.FileName;
            entity.LogoFileBase64 = this.LogoFileBase64;

            entity.ConsumerKey = this.ConsumerKey;
            entity.UserStoreIDP = this.UserStoreIDP;
            entity.CallbackUrl = this.CallbackUrl;

            entity.MaxDocumentType = this.MaxDocumentType;

            entity.TemplatePerDocumentType = this.TemplatePerDocumentType;

            entity.SMSConfig = this.SMSConfig;

            entity.EmailConfig = this.EmailConfig;

            entity.IsApproveLTV = this.IsApproveLTV;

            entity.IsApproveTSA = this.IsApproveTSA;

            entity.IsApproveCertify = this.IsApproveCertify;
            entity.EFormConfig = this.EFormConfig;
            entity.UserNamePrefix = this.UserNamePrefix;
            entity.IsUseUI = this.IsUseUI;
            entity.IsApproveSignDynamicPosition = this.IsApproveSignDynamicPosition;

            entity.NotifySendType = this.NotifySendType;

            entity.IsCallbackAuthorization = this.IsCallbackAuthorization;

            entity.CallbackAuthorizationUrl = this.CallbackAuthorizationUrl;

            entity.DefaultRequestCallBackAuthorizationHeaders = this.DefaultRequestCallBackAuthorizationHeaders;

            entity.DefaultRequestCallbackHeaders = this.DefaultRequestCallbackHeaders;

            entity.ADSSProfileSignConfirm = this.ADSSProfileSignConfirm;
            entity.UseImagePreview = this.UseImagePreview;

            entity.SMSSendType = this.SMSSendType;
            entity.SMSOTPTemplate = this.SMSOTPTemplate;
            entity.IsSMSAuthorization = this.IsSMSAuthorization;
            entity.SMSAuthorizationUrl = this.SMSAuthorizationUrl;
            entity.DefaultRequestSMSAuthorizationHeaders = this.DefaultRequestSMSAuthorizationHeaders;
            entity.SMSUrl = this.SMSUrl;
            entity.DefaultRequestSMSHeaders = this.DefaultRequestSMSHeaders;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.SignInfoDefault = this.SignInfoDefault;
            entity.IsUseEverify = this.IsUseEverify;
        }
    }
}
using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{

    public class NotifyConfigBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public int? DaySendNotiBefore { get; set; }
        public bool IsRepeat { get; set; }
        public string TimeSendNotify { get; set; }
        public bool IsSendSMS { get; set; }
        public string SMSTemplate { get; set; }
        public bool IsSendEmail { get; set; }
        public string EmailTitleTemplate { get; set; }
        public string EmailBodyTemplate { get; set; }
        public bool IsSendNotification { get; set; }
        public string NotificationTitleTemplate { get; set; }
        public string NotificationBodyTemplate { get; set; }
        public bool Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? OrganizationId { get; set; }
        public int NotifyType { get; set; }
    }

    public class NotifyConfigModel : NotifyConfigBaseModel
    {
        public int Order { get; set; } = 0;
    }

    public class NotifyConfigDetailModel : NotifyConfigModel
    {
        public Guid? CreatedUserId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Guid? ModifiedUserId { get; set; }
    }

    public class NotifyConfigCreateModel : NotifyConfigModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class NotifyConfigUpdateModel : NotifyConfigModel
    {
        public Guid ModifiedUserId { get; set; }
        public void UpdateToEntity(NotifyConfig entity)
        {
            entity.Code = this.Code;
            entity.Status = this.Status;
            entity.DaySendNotiBefore = this.DaySendNotiBefore;
            entity.IsRepeat = this.IsRepeat;
            entity.TimeSendNotify = this.TimeSendNotify;
            entity.IsSendSMS = this.IsSendSMS;
            entity.SMSTemplate = this.SMSTemplate;
            entity.IsSendEmail = this.IsSendEmail;
            entity.EmailTitleTemplate = this.EmailTitleTemplate;
            entity.EmailBodyTemplate = this.EmailBodyTemplate;
            entity.IsSendNotification = this.IsSendNotification;
            entity.NotificationTitleTemplate = this.NotificationTitleTemplate;
            entity.NotificationBodyTemplate = this.NotificationBodyTemplate;
            entity.Order = this.Order;
            entity.OrganizationId = this.OrganizationId;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.NotifyType = this.NotifyType;
        }
    }

    public class NotifyConfigQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public Guid? UserId { get; set; }
        public int? NotifyType { get; set; }
        public NotifyConfigQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class NotifyConfigSelectItemModel : SelectItemModel
    {
        public string DisplayName { get; set; }
        public Guid? OrganizationId { get; set; }
        public int NotifyType { get; set; }
    }
}

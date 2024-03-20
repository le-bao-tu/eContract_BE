using NetCore.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NetCore.WorkerService
{
    public class NotifyConfigModel
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public Guid? WorkflowStepId { get; set; }
        public Guid? NotifyConfigExpireId { get; set; }
        public Guid? NotifyConfigRemindId { get; set; }
        public DateTime SignExpireAtDate { get; set; }
        public DateTime? SendedRemindAtDate { get; set; }
        public string TimeSendNotify { get; set; }
        public string EmailTitleTemplate { get; set; }
        public string EmailBodyTemplate { get; set; }
        public bool IsSend { get; set; } = false;
        public string PhoneNumber { get; set; }
        public string OrganizationCode { get; set; }
        public Guid? OrganizationId { get; set; }
        public bool IsRepeat { get; set; }
        public int? DaySendNotiBefore { get; set; }
        public string NotificationTitleTemplate { get; set; }
        public string NotificationBodyTemplate { get; set; }
        public string SMSTemplate { get; set; }
        public bool? IsSendSMS { get; set; }
        public bool? IsSendEmail { get; set; }
        public bool? IsSendNotify { get; set; }
        public int NotifyType { get; set; }
        public List<int> UserReceiveNotiExpire { get; set; }

        public string UserReceiveNotiExpireJson
        {
            get
            {
                return UserReceiveNotiExpire == null ? null : JsonSerializer.Serialize(UserReceiveNotiExpire);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    UserReceiveNotiExpire = null;
                else
                    UserReceiveNotiExpire = JsonSerializer.Deserialize<List<int>>(value);
            }
        }
        public List<WorkFlowUserDocumentModel> WorkFlowUser { get; set; }

        public string WorkFlowUserJson
        {
            get
            {
                return WorkFlowUser == null ? null : JsonSerializer.Serialize(WorkFlowUser);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    WorkFlowUser = null;
                else
                    WorkFlowUser = JsonSerializer.Deserialize<List<WorkFlowUserDocumentModel>>(value);
            }
        }

    }
}

using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("workflow_step_remind_notify")]
    public class WorkflowStepRemindNotify : BaseTableWithOrganization
    {
        public WorkflowStepRemindNotify()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column("code")]
        public string Code { get; set; }

        [Column("day_send_noti_before")]
        public int DaySendNotiBefore { get; set; }

        [Column("is_repeate")]
        public bool IsRepeat { get; set; }

        [Column("time_send_notify")]
        public string TimeSendNotify { get; set; } // hh:mm

        [Column("is_send_sms")]
        public bool IsSendSMS { get; set; }

        [Column("sms_template")]
        public string SMSTemplate { get; set; }

        [Column("is_send_email")]
        public bool IsSendEmail { get; set; }

        [Column("email_title_template")]
        public string EmailTitleTemplate { get; set; }

        [Column("email_body_template")]
        public string EmailBodyTemplate { get; set; }

        [Column("is_send_notification")]
        public bool IsSendNotification { get; set; }

        [Column("notification_title_template")]
        public string NotificationTitleTemplate { get; set; }

        [Column("notification_body_template")]
        public string NotificationBodyTemplate { get; set; }
    }
}

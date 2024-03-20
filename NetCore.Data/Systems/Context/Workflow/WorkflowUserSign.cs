using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("workflow_user_sign")]
    public class WorkflowUserSign
    {
        public WorkflowUserSign()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("order")]
        public int Order { get; set; } = 0;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        /*
         Loại hình ký:
            1: ký chứng thực
            2: Ký phê duyệt
            3: Người review
         */
        [Column("type")]
        public SignType Type { get; set; }

        [StringLength(128)]
        [Column("name")]
        public string Name { get; set; }

        [ForeignKey("WorkflowState")]
        [Column("state_id")]
        public Guid? StateId { get; set; }

        public WorkflowState WorkflowState { get; set; }

        [StringLength(128)]
        [Column("state")]
        public string State { get; set; }

        [Column("state_name")]
        public string StateName { get; set; }

        [Column("sign_expire_after_day")]
        public int? SignExpireAfterDay { get; set; }

        [Column("sign_close_after_day")]
        public int? SignCloseAfterDay { get; set; }

        [ForeignKey("Workflow")]
        [Column("workflow_id")]
        public Guid WorkflowId { get; set; }

        public Workflow Workflow { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }

        [Column("is_sign_ltv")]
        public bool IsSignLTV { get; set; } = false;

        [Column("is_sign_tsa")]
        public bool IsSignTSA { get; set; } = false;

        [Column("is_sign_certify")]
        public bool IsSignCertify { get; set; } = false;

        #region Thông tin consent cần hiển thị
        [Column("consent_sign_config_json")]
        public string ConsentSignConfigJson
        {
            get
            {
                return ConsentSignConfig == null ? null : JsonSerializer.Serialize(ConsentSignConfig);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ConsentSignConfig = null;
                else
                    ConsentSignConfig = JsonSerializer.Deserialize<List<ConsentSignConfig>>(value);
            }
        }

        [NotMapped]
        public List<ConsentSignConfig> ConsentSignConfig { get; set; }
        #endregion

        //Quy trình chạy đến bước hiện tại
        //Có gửi OTP truy cập hợp đồng hay không => nhớ kiểm tra người dùng đã bật smart OTP chưa
        [Column("is_send_otp_noti_sign")]
        public bool IsSendOTPNotiSign { get; set; } = true;

        //Có nhận thông báo hợp đồng cần ký hay không?
        [Column("is_send_mail_noti_sign")]
        public bool IsSendMailNotiSign { get; set; } = true;

        //Có nhận thông báo hợp đồng đã hoàn thành ký hay không?
        [Column("is_send_mail_noti_result")]
        public bool IsSendMailNotiResult { get; set; } = true;

        // Có ký tự động hay là không? => nếu có cấu hình HSM và userpin + alias đã được lưu
        [Column("is_auto_sign")]
        public bool IsAutoSign { get; set; }

        // Profile ký adss
        [Column("adss_profile_name")]
        public string ADSSProfileName { get; set; }

        //Quy trình chạy xong bước hiện tại
        /// <summary>
        /// Có gửi thông báo cho hệ thống khách hàng hay không
        /// </summary>
        [Column("is_send_noti_signed_for_3rd_app")]
        public bool IsSendNotiSignedFor3rdApp { get; set; } = false;

        //#region Gửi thông báo
        //[ForeignKey("WorkflowStepExpireNotify")]
        //[Column("workflow_step_expire_notify_id")]
        //public Guid? WorkflowStepExpireNotifyId { get; set; }

        //public WorkflowStepExpireNotify WorkflowStepExpireNotify { get; set; }

        //[ForeignKey("WorkflowStepRemindNotify")]
        //[Column("workflow_step_remind_notify_id")]
        //public Guid? WorkflowStepRemindNotifyId { get; set; }

        //public WorkflowStepRemindNotify WorkflowStepRemindNotify { get; set; }
        //#endregion

        #region Gửi thông báo
        [ForeignKey("NotifyConfigExpire")]
        [Column("notify_config_expire_id")]
        public Guid? NotifyConfigExpireId { get; set; }

        public NotifyConfig NotifyConfigExpire { get; set; }

        [Column("user_receive_noti_expire_json")]
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


        [NotMapped]
        public List<int> UserReceiveNotiExpire { get; set; }

        [ForeignKey("NotifyConfigRemind")]
        [Column("notify_config_remind_id")]
        public Guid? NotifyConfigRemindId { get; set; }

        public NotifyConfig NotifyConfigRemind { get; set; }

        [ForeignKey("NotifyConfigUserSignComplete")]
        [Column("notify_config_user_sign_complete_id")]
        public Guid? NotifyConfigUserSignCompleteId { get; set; }

        public NotifyConfig NotifyConfigUserSignComplete { get; set; }
        #endregion

        #region Giới hạn renew HĐ
        [Column("is_allow_renew")]
        public bool IsAllowRenew { get; set; }

        [Column("max_renew_times")]
        public int? MaxRenewTimes { get; set; }
        #endregion

    }

    public class ConsentSignConfig
    {
        public bool IsDefaultCheck { get; set; }
        public string Content { get; set; }
    }

    public enum SignType
    {
        KY_CHUNG_THUC = 1,
        KY_PHE_DUYET = 2,
        REVIEW = 3,
        KY_SO = 4,
        KY_DIEN_TU = 5,
        KHONG_CAN_KY = 6
    }
}

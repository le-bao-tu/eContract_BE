using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("document_notify_schedule")]
    public class DocumentNotifySchedule
    {
        /*
        - Khi sinh/ký hợp đồng nếu thông tin quy trình có ngày hết hạn ký + thông báo nhắc nhở ký/hết hạn ký thì lưu thông tin vào đây
        - Khi ký hợp đồng mà bước sau có cần phải nhắc nhở ký thì cần phải xóa bản ghi cũ liên quan đến documentId và thêm bản ghi mới tương ứng với documentId đó
        
        //Khi gửi thông báo
        - Thời gian worker để chạy là 30p
        - Gửi thông báo hết hạn => Gửi xong xóa luôn bản ghi vì sau cũng ko dùng nữa
        - Gửi thông báo nhắc nhở => giờ gửi thông báo sẽ lấy trong bảng notify_config; sau khi gửi thông báo nhắc nhở sẽ cần cập nhật lại bản ghi liên quan đến SendedRemindAtDate = DateTime.Now
        - 1 ngày chỉ nhắc nhở 1 lần => dựa vào cái thời gian ngày kia thì không gửi nữa
         */

        public DocumentNotifySchedule()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("document_id")]
        public Guid DocumentId { get; set; }

        [Column("document_code")]
        public string DocumentCode { get; set; }

        [Column("document_name")]
        public string DocumentName { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public Guid UserId { get; set; }

        public User User { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }

        [Column("workflow_step_id")]
        public Guid? WorkflowStepId { get; set; }

        [ForeignKey("NotifyConfigExpire")]
        [Column("notify_config_expire_id")]
        public Guid? NotifyConfigExpireId { get; set; }

        public NotifyConfig NotifyConfigExpire { get; set; }

        [ForeignKey("NotifyConfigRemind")]
        [Column("notify_config_remind_id")]
        public Guid? NotifyConfigRemindId { get; set; }

        public NotifyConfig NotifyConfigRemind { get; set; }

        [Column("sign_expire_at_date")]
        public DateTime SignExpireAtDate { get; set; }

        [Column("sended_remind_at_date")]
        public DateTime? SendedRemindAtDate { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [Column("modified_date")]
        public DateTime? ModifiedDate { get; set; }

        [Column("is_send")]
        public bool IsSend { get; set; } = false;
    }
}

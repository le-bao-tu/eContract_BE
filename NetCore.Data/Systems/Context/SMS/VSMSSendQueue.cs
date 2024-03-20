using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("vsms_send_queue")]
    public class VSMSSendQueue
    {
        public VSMSSendQueue()
        {
        }
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        /// <summary>
        /// Branch
        /// </summary>
        [Column("source_addr")]
        public string SourceAddr { get; set; }
        /// <summary>
        /// Số điện thoại nhận tin nhắn
        /// </summary>
        [Column("phone_number")]
        public string PhoneNumber { get; set; }
        /// <summary>
        /// Nội dung tin nhắn
        /// </summary>
        [Column("message")]
        public string Message { get; set; }
        /// <summary>
        /// Trạng thái đẩy tin nhắn
        /// </summary>
        [Column("is_push")]
        public bool IsPush { get; set; } = false;
        /// <summary>
        /// Ngày tạo
        /// </summary>
        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        /// <summary>
        /// Thời gian gửi
        /// </summary>
        [Column("sent_time")]
        public DateTime? SentDate { get; set; } = null;
        /// <summary>
        /// Id của đơn vị
        /// </summary>
        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }
        /// <summary>
        /// Id của người nhận tin
        /// </summary>
        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }
        /// <summary>
        /// Order
        /// </summary>
        public int Order { get; set; } = 0;
        /// <summary>
        /// Kết quả gửi tin nhắn
        /// </summary>
        [Column("send_sms_response_json")]
        public string SendSMSResponseJson
        {
            get
            {
                return SendSMSResonse == null ? null : JsonSerializer.Serialize(SendSMSResonse);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    SendSMSResonse = null;
                else
                    SendSMSResonse = JsonSerializer.Deserialize<SendSMSResponse>(value);
            }
        }
        [NotMapped]
        public SendSMSResponse SendSMSResonse { get; set; }
    }
    public class SendSMSResponse
    {
        /// <summary>
        /// Số điện thoại nhận tin nhắn
        /// </summary>
        [Column("dest_addr")]
        [JsonPropertyName("dest_addr")]
        public string DestAddr { get; set; }
        /// <summary>
        /// Trạng thái gửi tin nhắn
        /// </summary>
        [Column("status")]
        [JsonPropertyName("status")]
        public int Status { get; set; }
        /// <summary>
        /// Id tin nhắn
        /// </summary>
        [Column("msgid")]
        [JsonPropertyName("msgid")]
        public int MsgId { get; set; }
        /// <summary>
        /// Mô tả trạng thái gửi tin nhắn
        /// </summary>
        [Column("decription")]
        [JsonPropertyName("decription")]
        public string Decription { get; set; }
    }
}

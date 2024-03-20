using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("queue_send_email")]
    public class QueueSendEmail : BaseTableDefault
    {
        public QueueSendEmail()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }


        /*
         List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, string base64Image
         */

        [NotMapped]
        public List<string> ToEmails { get; set; }

        [Column("to_emails_json")]
        public string ToEmailsJson
        {
            get
            {
                return ToEmails == null ? null : JsonSerializer.Serialize(ToEmails);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ToEmails = null;
                else
                    ToEmails = JsonSerializer.Deserialize<List<string>>(value);
            }
        }

        [NotMapped]
        public List<string> CCEmails { get; set; }

        [Column("cc_emails_json")]
        public string CCEmailsJson
        {
            get
            {
                return CCEmails == null ? null : JsonSerializer.Serialize(CCEmails);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    CCEmails = null;
                else
                    CCEmails = JsonSerializer.Deserialize<List<string>>(value);
            }
        }


        [NotMapped]
        public List<string> BccEmails { get; set; }

        [Column("bcc_emails_json")]
        public string BccEmailsJson
        {
            get
            {
                return BccEmails == null ? null : JsonSerializer.Serialize(BccEmails);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    BccEmails = null;
                else
                    BccEmails = JsonSerializer.Deserialize<List<string>>(value);
            }
        }

        [Column("title")]
        public string Title { get; set; }


        [Column("body")]
        public string Body { get; set; }

        [Column("base64_image")]
        public string Base64Image { get; set; }

        [Column("is_sended")]
        public bool IsSended { get; set; }
        public EmailAccount EmailAccount { get; set; }
        [Column("email_account_id")]
        public Guid EmailAccountId { get; set; }
    }
}

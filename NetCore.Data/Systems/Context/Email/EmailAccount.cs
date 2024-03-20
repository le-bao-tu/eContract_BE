using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("email_account")]
    public class EmailAccount: BaseTableWithIdentityNumber
    {
        public EmailAccount()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("from")]
        public string From { get; set; }

        [Column("smtp")]
        public string Smtp { get; set; }

        [Column("port")]
        public int Port { get; set; }

        [Column("user")]
        public string User { get; set; }

        [Column("send_type")]
        public string SendType { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("ssl")]
        public bool Ssl { get; set; }

        public List<QueueSendEmail> QueueSendEmails { get; set; }
    }
}

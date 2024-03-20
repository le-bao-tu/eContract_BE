using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("sign_request_history")]
    public class SignRequestHistory
    {
        public SignRequestHistory()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("document_id")]
        public Guid DocumentId { get; set; }

        [Column("document_code")]
        public string DocumentCode { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }

        [Column("consent")]
        public string Consent { get; set; }

        [Column("signature_base64")]
        public string SignatureBase64 { get; set; }

        [Column("logo_base64")]
        public string LogoBase64 { get; set; }

        [Column("hsm_account_id")]
        public Guid? HSMAccountId { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

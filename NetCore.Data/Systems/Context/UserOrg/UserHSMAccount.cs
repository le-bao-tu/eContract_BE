using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("user_hsm_account")]
    public class UserHSMAccount : BaseTableWithApplication
    {
        JsonSerializerOptions jso = new JsonSerializerOptions();

        public UserHSMAccount()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public Guid UserId { get; set; }

        public User User { get; set; }

        [Required]
        [Column("code")]
        public string Code { get; set; }

        [Column("subject_dn")]
        public string SubjectDN { get; set; }

        [Required]
        [Column("alias")]
        public string Alias { get; set; }

        [Column("user_pin")]
        public string UserPIN { get; set; }

        [Column("valid_from")]
        public DateTime? ValidFrom { get; set; }

        [Column("valid_to")]
        public DateTime? ValidTo { get; set; }

        [Column("certificate_base64")]
        public string CertificateBase64 { get; set; }

        [Column("chain_certificate_base64_json")]
        public string ChainCertificateBase64Json
        {
            get
            {
                return ChainCertificateBase64 == null ? null : JsonSerializer.Serialize(ChainCertificateBase64, jso);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ChainCertificateBase64 = null;
                else
                    ChainCertificateBase64 = JsonSerializer.Deserialize<List<string>>(value);
            }
        }

        [NotMapped]
        public List<string> ChainCertificateBase64 { get; set; }

        [Column("public_key")]
        public string PublicKey { get; set; }

        [Column("csr")]
        public string CSR { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("account_type")]
        public AccountType AccountType { get; set; } = AccountType.HSM;

        [Column("user_request_cert_json")]
        public string UserRequestCertJson
        {
            get
            {
                return UserRequestCert == null ? null : JsonSerializer.Serialize(UserRequestCert, jso);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    UserRequestCert = null;
                else
                    UserRequestCert = JsonSerializer.Deserialize<UserRequestCertModel>(value);
            }
        }

        [NotMapped]
        public UserRequestCertModel UserRequestCert { get; set; }
    }

    public class UserRequestCertModel
    {
        public string FullName { get; set; }
        public Guid? EFormId { get; set; }
        public bool IsConfirmEForm { get; set; } = false;
        public string FrontImageBucketName { get; set; }
        public string FrontImageObjectName { get; set; }
        public string BackImageBucketName { get; set; }
        public string BackImageObjectName { get; set; }
        public string FaceImageBucketName { get; set; }
        public string FaceImageObjectName { get; set; }
    }

    public enum AccountType
    {
        HSM = 1,
        ADSS = 2
    }
}

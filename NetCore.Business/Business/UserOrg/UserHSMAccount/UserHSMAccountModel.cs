using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class UserHSMAccountBaseModel
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Code { get; set; }

        public string Alias { get; set; }

        public DateTime? CreatedDate { get; set; }

        public bool IsDefault { get; set; }

        public bool Status { get; set; }
    }

    public class UserHSMAccountModel : UserHSMAccountBaseModel
    {
        public string SubjectDN { get; set; }

        public string UserPIN { get; set; }
        public bool HasUserPIN { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public string CertificateBase64 { get; set; }

        public AccountType AccountType { get; set; } = AccountType.HSM;
    }

    public class UserHSMAccountQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public Guid? UserId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";

        //asc - desc
        public string Ascending { get; set; } = "desc";

        public UserHSMAccountQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class UserHSMAccountCreateOrUpdateModel : UserHSMAccountModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
        public Guid? ModifiedUserId { get; set; }

        public void UpdateToEntity(UserHSMAccount entity)
        {
            entity.AccountType = AccountType;
            entity.Alias = Alias;
            // entity.UserPIN = UserPIN;
            entity.IsDefault = IsDefault;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.ValidFrom = this.ValidFrom;
            entity.ValidTo = this.ValidTo;
        }
    }

    public class RequestCertRAResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public RequestCertRADataModel Data { get; set; }
    }

    public class RequestCertRADataModel
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("subjectDn")]
        public string SubjectDn { get; set; }

        [JsonPropertyName("validFrom")]
        public string ValidFrom { get; set; }

        [JsonPropertyName("validTo")]
        public string ValidTo { get; set; }

        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        [JsonPropertyName("eform")]
        public string Eform { get; set; }

        [JsonPropertyName("eformId")]
        public string EformId { get; set; }
    }

    public class UserHSMAccountSelectItemModel : SelectItemModel
    {
        //public string Path { get; set; }
        //[JsonIgnore]
        public Guid UserId { get; set; }

        public bool IsHasUserPIN { get; set; }
        public bool IsDefault { get; set; }
        public string Alias { get; set; }

        [JsonIgnore]
        public string UserPIN { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public AccountType AccountType { get; set; } = AccountType.HSM;
    }

    public class CertificateInfoResponseModel
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonPropertyName("subjectName")]
        public string SubjectName { get; set; }

        [JsonPropertyName("issuerName")]
        public string IssuerName { get; set; }

        [JsonPropertyName("signatureAlgorithm")]
        public string SignatureAlgorithm { get; set; }

        [JsonPropertyName("notBefore")]
        public DateTime? NotBefore { get; set; }

        [JsonPropertyName("notAfter")]
        public DateTime? NotAfter { get; set; }

        [JsonPropertyName("thumbprint")]
        public string Thumbprint { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("issuer")]
        public string Issuer { get; set; }
    }

    public class CertKey
    {
        [JsonPropertyName("signatureAlgorithm")]
        public string SignatureAlgorithm { get; set; }

        [JsonPropertyName("keySize")]
        public string KeySize { get; set; }
    }
}
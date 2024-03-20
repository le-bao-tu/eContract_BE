using Microsoft.AspNetCore.Http;
using NetCore.Data;
using NetCore.DataLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class DocumentResponseModel
    {
        public Guid DocumentId { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string DocumentName { get; set; }
        public string DocumentCode { get; set; }
        public SignType SignType { get; set; }
        public List<DocumentFileResponseModel> ListDocumentFile { get; set; }
    }

    public class DocumentFileResponseModel
    {
        public Guid DocumentFileId { get; set; }
        public string Buckename { get; set; }
        public string ObjectName { get; set; }
        public string FileName { get; set; }
    }

    public class DocumentSignedResponseModel
    {
        public string Id { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeName { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public DocumentStatus DocumentWorkflowStatus { get; set; }
        //public DocumentStatus DocumentStatus { get; set; }
        public string FileUrl { get; set; }
        public List<DocumentSignedWorkFlowUser> WorkFlowUser { get; set; }
    }

    public class DocumentSignedWorkFlowUser
    {
        [JsonPropertyName("userConnectId")]
        public string UserConnectId { get; set; }

        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; }

        [JsonPropertyName("userEmail")]
        public string UserEmail { get; set; }

        [JsonPropertyName("userPhoneNumber")]
        public string UserPhoneNumber { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        //[JsonPropertyName("type")]
        //public SignType Type { get; set; }

        [JsonPropertyName("signAtDate")]
        public DateTime? SignAtDate { get; set; }
    }

    public class SignDocumentModel
    {
        public string DocumentCode { get; set; }
        public string OTP { get; set; }
        public string SignatureBase64 { get; set; }
    }

    public class SignDocumentMultileModel
    {
        public List<Guid> ListDocumentId { get; set; }
        public string OTP { get; set; }
        public string SignatureBase64 { get; set; }
    }

    public class SignDocumentMultileFor3rdModel
    {
        [JsonPropertyName("userConnectId")]
        public string UserConnectId { get; set; }

        [JsonPropertyName("listDocumentCode")]
        public List<string> ListDocumentCode { get; set; }

        [JsonPropertyName("otp")]
        public string OTP { get; set; }

        [JsonPropertyName("isFileUrlReturn")]
        public bool IsFileUrlReturn { get; set; } = false;

        [JsonPropertyName("signatureDetail")]
        public SignatureDetailFrom3rdmodel SignatureDetail { get; set; }

        [JsonPropertyName("location")]
        public LocationSign3rdModel Location { get; set; }

        [JsonPropertyName("deviceInfo")]
        public OpratingSystemMobileModel DeviceInfo { get; set; }

        [JsonPropertyName("signatureBase64")]
        public string SignatureBase64 { get; set; }

        [JsonPropertyName("ekycImageBase64")]
        public string EKYCImageBase64 { get; set; }

        [JsonIgnore]
        public MemoryStream EKYCMemoryStream { get; set; }
    }

    public class LocationSign3rdModel
    {
        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("geoLocation")]
        public string GeoLocation { get; set; }
    }

    public class SignatureDetailFrom3rdmodel
    {
        [JsonPropertyName("isShowUserName")]
        public bool IsShowUserName { get; set; }

        [JsonPropertyName("isShowPhoneNumber")]
        public bool IsShowPhoneNumber { get; set; }

        [JsonPropertyName("isShowEmail")]
        public bool IsShowEmail { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";
    }

    public class OpratingSystemMobileModel
    {
        [JsonPropertyName("appCodeName")]
        public string AppCodeName { get; set; }

        [JsonPropertyName("appName")]
        public string AppName { get; set; }

        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; }

        [JsonPropertyName("appType")]
        public string AppType { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }
    }

    public class SignDocumentUsbTokenModel
    {
        public string DocumentCode { get; set; }
        public string OTP { get; set; }
        public string FileBase64 { get; set; }
    }

    public class OTPByDocumentModel
    {
        public string DocumentCode { get; set; }
        public string OTP { get; set; }
    }

    public class SignDocumentUsbTokenDataModel
    {
        public Guid DocumentId { get; set; }
        public string FileBase64 { get; set; }
    }

    public class SignDocumentHSMModel
    {
        public string DocumentCode { get; set; }
        public string UserPin { get; set; }
        public string Base64Image { get; set; }
    }
    public class RejectDocumentModel
    {
        public Guid DocumentId { get; set; }
        public Guid UserId { get; set; }
        public string RejectReason { get; set; }
    }

    public class CoordinateFileModel
    {

        /// <summary>
        /// documentId
        /// </summary>
        [JsonPropertyName("documentId")]
        public Guid? DocumentId { get; set; }
        /// <summary>
        /// message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
        /// <summary>
        /// Lower left x
        /// </summary>
        [JsonPropertyName("llx")]
        public string Llx { get; set; }
        /// <summary>
        /// Lowwer left y
        /// </summary>
        [JsonPropertyName("lly")]
        public string Lly { get; set; }
        /// <summary>
        ///  Rectangle width
        /// </summary>
        [JsonPropertyName("urx")]
        public string Urx { get; set; }
        /// <summary>
        /// Rectangle height 
        /// </summary>
        [JsonPropertyName("ury")]
        public string Ury { get; set; }
        /// <summary>
        /// Sign At Page
        /// </summary>
        [JsonPropertyName("page")]
        public string Page { get; set; }
    }

    public class OTPUserRequestModel
    {
        public string UserConnectId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using NetCore.Data;
using NetCore.Shared;

namespace NetCore.Business
{
    #region Sign Hash Usbtoken
    public class HashFilesClientModel
    {
        public string Certificate { get; set; }
        public List<Guid> ListDocumentId { get; set; }
        public BaseSignAppearanceModel Appearance { get; set; }
    }
    public class HashFilesRequestModel
    {
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }
        [JsonPropertyName("requestList")]
        public List<HashFileModel> RequestList { get; set; }
    }
    public class HashFileModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("bucketName")]
        public string BucketName { get; set; }

        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; }

        [JsonPropertyName("bucketNameTemp")]
        public string BucketNameTemp { get; set; }

        [JsonPropertyName("objectNameTemp")]
        public string ObjectNameTemp { get; set; }

        [JsonPropertyName("appearances")]
        public List<SignAppearanceModel> Appearances { get; set; }
    }
    public class HashFilesResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }

        [JsonPropertyName("data")]
        public HashFileResponseDataModel Data { get; set; }
    }
    public class HashFileResponseDataModel
    {
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        [JsonPropertyName("resultList")]
        public List<HashFileResultModel> ResultList { get; set; }
    }
    public class HashFileResultModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("bucketNameTemp")]
        public string BucketNameTemp { get; set; }

        [JsonPropertyName("objectNameTemp")]
        public string ObjectNameTemp { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; }
    }
    public class AttachFilesRequestModel
    {
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        [JsonPropertyName("requestList")]
        public List<AttachFileModel> RequestList { get; set; }
    }
    public class AttachFileModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; }

        [JsonPropertyName("bucketNameTemp")]
        public string BucketNameTemp { get; set; }

        [JsonPropertyName("objectNameTemp")]
        public string ObjectNameTemp { get; set; }

        [JsonPropertyName("bucketNameSigned")]
        public string BucketNameSigned { get; set; }

        [JsonPropertyName("objectNameSigned")]
        public string ObjectNameSigned { get; set; }

        [JsonPropertyName("appearances")]
        public List<SignAppearanceAttachModel> Appearances { get; set; }
    }

    public class SignAppearanceAttachModel
    {
        [JsonPropertyName("ltv")]
        public float LTV { get; set; } = 1;
        [JsonPropertyName("tsa")]
        public float TSA { get; set; } = 1;
    }

    #endregion
    #region Electronic Sign
    public class ElectronicSignClientModel
    {
        public List<Guid> ListDocumentId { get; set; }
        public string OTP { get; set; }
        public bool IsFileUrlReturn { get; set; } = false;
        public SignAppearanceModel Appearance { get; set; }
        public Guid? UserSignConfigId { get; set; }
        public List<FileAttachmentModel> ListFileAttachment { get; set; }
    }
    public class ElectronicSignFileRequestModel
    {
        [JsonPropertyName("requestList")]
        public List<SignHashModel> RequestList { get; set; }
    }
    #endregion

    #region ADSS Sign
    public class SignADSSClientModel
    {
        public List<Guid> ListDocumentId { get; set; }
        //public string ProfileId { get; set; } = "adss:signing:profile:009"; //Signing Profile identifier(Cấu hình trên ADSS Server)
        //public string CertAlias { get; set; } = "samples_test_signing_certificate";
        //public string CertAlias { get; set; } = "Adss_OCS_Esign";
        //public string Password { get; set; } = "Admin@123";
        //public string SignedBy { get; set; }
        public Guid HsmAcountId { get; set; }
        public string UserPin { get; set; }
        public bool IsAutoSign { get; set; } = false;
        public string SigningReason { get; set; } = "Tôi đã đọc và đồng ý ký tài liệu.";
        public string SigningLocation { get; set; } = "";
        //public string ContactInfo { get; set; }
    }

    public class SignADSSFileModel
    {
        public Guid DocumentId { get; set; }
        public Guid DocumentFileId { get; set; }
        public string FileBucketName { get; set; }
        public string FileObjectName { get; set; }
        public string FileName { get; set; }
    }

    #endregion

    #region Sign HSM
    public class SignHSMClientModel
    {
        public List<Guid> ListDocumentId { get; set; }
        public Guid HSMAcountId { get; set; }
        public string UserPin { get; set; }
        public List<FileAttachmentModel> ListFileAttachment { get; set; }
        public BaseSignAppearanceModel Appearance { get; set; }
        public Guid? UserSignConfigId { get; set; }
    }

    public class FileAttachmentModel
    {
        [JsonPropertyName("bucketName")]
        public string BucketName { get; set; }
        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; }
    }

    public class SignHSMFileRequestModel
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }
        [JsonPropertyName("userPin")]
        public string UserPin { get; set; }
        [JsonPropertyName("requestList")]
        public List<SignHashModel> RequestList { get; set; }
    }
    #endregion

    #region Sign Hash model
    public class SignHashModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("bucketName")]
        public string BucketName { get; set; }
        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; }
        [JsonPropertyName("bucketNameSigned")]
        public string BucketNameSigned { get; set; }
        [JsonPropertyName("objectNameSigned")]
        public string ObjectNameSigned { get; set; }
        [JsonPropertyName("signatureAlgorithm")]
        public string SignatureAlgorithm { get; set; } = "SHA256withRSA";
        [JsonPropertyName("hashAlgorithm")]
        public string HashAlgorithm { get; set; } = "SHA-256";
        [JsonPropertyName("responseDataFormat")]
        public string ResponseDataFormat { get; set; } = "base64";
        [JsonPropertyName("dataFormat")]
        public string DataFormat { get; set; } = "hex";
        [JsonPropertyName("appearances")]
        public List<SignAppearanceModel> Appearances { get; set; }
        public string DocumentFileNamePrefix { get; set; }
        public string DocumentObjectNameDirectory { get; set; }
    }
    //Appearances
    public class BaseSignAppearanceModel
    {
        [JsonPropertyName("imageData")]
        public string ImageData { get; set; }
        [JsonPropertyName("logo")]
        public string Logo { get; set; }
        [JsonPropertyName("detail")]
        public string Detail { get; set; }
        [JsonPropertyName("scaleImage")]
        public float? ScaleImage { get; set; }
        [JsonPropertyName("scaleText")]
        public float? ScaleText { get; set; }
        [JsonPropertyName("scaleLogo")]
        public float? ScaleLogo { get; set; }
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "Tôi đã đọc, hiểu, đồng ý với nội dung của hợp đồng và thống nhất phương thức số/điện tử để ký hợp đồng.";
        [JsonPropertyName("ltv")]
        public float LTV { get; set; } = 1;
        [JsonPropertyName("tsa")]
        public float TSA { get; set; } = 1;
        [JsonPropertyName("certify")]
        public float Certify { get; set; } = 0;
    }

    public class SignAppearanceModel : BaseSignAppearanceModel
    {
        [JsonPropertyName("llx")]
        public float Llx { get; set; }
        [JsonPropertyName("lly")]
        public float Lly { get; set; }
        [JsonPropertyName("urx")]
        public float Urx { get; set; }
        [JsonPropertyName("ury")]
        public float Ury { get; set; }
        [JsonPropertyName("page")]
        public float Page { get; set; }
        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; } = true;
        [JsonPropertyName("signLocation")]
        public string SignLocation { get; set; } = "";
        [JsonPropertyName("contact")]
        public string Contact { get; set; } = "";
        [JsonPropertyName("phone")]
        public string Phone { get; set; }
        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        public string SignBy { get; set; }

        public SignAppearanceModel Copy()
        {
            return (SignAppearanceModel)this.MemberwiseClone();
        }
        public void SetCoordinate(SignCoordinateModel coordinate)
        {
            this.Llx = coordinate.Llx;
            this.Lly = coordinate.Lly;
            this.Urx = coordinate.Urx;
            this.Ury = coordinate.Ury;
            this.Page = coordinate.Page;
        }
    }

    public class SignCoordinateModel
    {
        [JsonPropertyName("llx")]
        public float Llx { get; set; }
        [JsonPropertyName("lly")]
        public float Lly { get; set; }
        [JsonPropertyName("urx")]
        public float Urx { get; set; }
        [JsonPropertyName("ury")]
        public float Ury { get; set; }
        [JsonPropertyName("page")]
        public float Page { get; set; }

        [JsonIgnore]
        public float Width { get; set; }
        [JsonIgnore]
        public float Height { get; set; }
    }
    #endregion

    #region SignFileResult
    public class SignFilesResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }
        [JsonPropertyName("data")]
        public SignFileResponseDataModel Data { get; set; }
    }
    public class SignFileResponseDataModel
    {
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }
        [JsonPropertyName("resultList")]
        public List<SignFileResultModel> ResultList { get; set; }
    }
    public class SignFileResultModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("bucketNameSigned")]
        public string BucketNameSigned { get; set; }
        [JsonPropertyName("objectNameSigned")]
        public string ObjectNameSigned { get; set; }
        [JsonPropertyName("data")]
        public string Data { get; set; }

    }
    public class DocumentSignedResult
    {
        public Guid DocumentId { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public List<DocumentFileSignedResult> ListFileSignedResult { get; set; }
    }
    public class DocumentFileSignedResult
    {
        public Guid DocumentFileId { get; set; }
        public string BucketNameSigned { get; set; }
        public string ObjectNameSigned { get; set; }
        public List<ImagePreview> ImagePreview { get; set; }
    }
    #endregion

    public class AutomaticSignFileResultModel
    {
        public string DocumentCode { get; set; }
        public bool IsSigned { get; set; }
        public string Message { get; set; }
    }

    public class SignEFormFrom3rdModel
    {
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public string DocumentCode { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class RequestSignDocumentFrom3rdModel
    {
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public string DocumentCode { get; set; }
        public string Reason { get; set; }
        public string ImageBase64 { get; set; }
        public string SignatureBase64 { get; set; }
        public Guid? CertificateId { get; set; }
        public string UserPIN { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class RequestSignDocumentFrom3rdFormDataModel
    {
        public IFormFile FaceVideo { get; set; }
        public IFormFile EKYC { get; set; }
        public IFormFile Signature { get; set; }
        public string DocumentCode { get; set; }
        public string LocationText { get; set; }
        public string DeviceInfoText { get; set; }
        public Guid? CertificateId { get; set; }
    }

    public class RequestSignDocumentFrom3rdResponseModel
    {
        public string JWT { get; set; }
        public Guid RequestId { get; set; }
        public Guid SadRequestId { get; set; }
    }

    #region Renew OTP
    public class RenewOTPReuquestFrom3rdModel
    {
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }
    public class RenewOTPResponseModel
    {
        public string OTP { get; set; }
        public int RemainingSeconds { get; set; }
        public DateTime ExpireAtUTCDate { get; set; }
    }
    #endregion

    #region SignRequest

    public class SignRequestModel
    {
        [JsonPropertyName("requestId")]
        public Guid SigningRequestId { get; set; }

        [JsonPropertyName("content")]
        public string Consent { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("firebaseToken")]
        public string FirebaseToken { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("orgCode")]
        public string OrgCode { get; set; }
    }

    public class SignRequestNoDataResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }
    }


    public class SignRequestResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public SignRequestDetailResponseModel Data { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }
    }

    public class SignRequestDetailResponseModel
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("sadRequestId")]
        public string SadRequestId { get; set; }

        [JsonPropertyName("jwt")]
        public string JWT { get; set; }
    }

    public class SignConfirmModel
    {
        public Guid RequestId { get; set; }
        public string OTP { get; set; }
        public string SAD { get; set; }
        public string SADRequestId { get; set; }
    }
    #endregion

    #region Demo luồng ký consent
    public class SADReqeustSignConfirmModel
    {
        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; }

        [JsonPropertyName("sadRequestId")]
        public Guid SadRequestId { get; set; }

        [JsonPropertyName("otp")]
        public string OTP { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

        [JsonPropertyName("organizationUnit")]
        public string OrganizationUnit { get; set; }

        [JsonPropertyName("identityNo")]
        public string IdentityNo { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("jwt")]
        public string JWT { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }
    }
    #endregion

    #region Đăng ký cấp CTS
    public class UserRequestKeyAndCSRModel
    {
        [JsonPropertyName("keyPrefix")]
        public string KeyPrefix { get; set; } = "eccontract-cert-request";

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("subjectDN")]
        public string SubjectDN { get; set; }

        [JsonPropertyName("keyLength")]
        public int KeyLength { get; set; } = 2048;

        [JsonPropertyName("pinCode")]
        public string PinCode { get; set; } = Utils.GenerateNewRandom();

        [JsonPropertyName("responseDataFormat")]
        public string ResponseDataFormat { get; set; } = "raw";

        //"keyPrefix": "vietcredit-staging",
        //"alias": "",
        //"subjectDN": "UID=CMT:123456789, CN = Nguyen Huu Thanh, T = Digital, OU = SAVIS, O = S, ST = Ha Noi, C = VN",
        //"keyLength": 2048,
        //"pinCode": "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92",
        //"responseDataFormat": "raw"
    }

    public class UserResponseKeyAndCSRModel
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public UserResponseKeyAndCSRDetailModel Data { get; set; }
    }

    public class UserResponseKeyAndCSRDetailModel
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; }

        [JsonPropertyName("csr")]
        public string CSR { get; set; }
    }

    //Yêu cầu cấp CTS
    public class CertificateRequestModel
    {
        [JsonPropertyName("userData")]
        public CertificateDataRequestModel UserData { get; set; }

        [JsonPropertyName("csr")]
        public string CSR { get; set; }
    }

    public class CertificateDataRequestModel
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("subjectDN")]
        public string SubjectDN { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("caName")]
        public string CAName { get; set; }

        [JsonPropertyName("endEntityProfileName")]
        public string EndEntityProfileName { get; set; }

        [JsonPropertyName("certificateProfileName")]
        public string CertificateProfileName { get; set; }

        [JsonPropertyName("validTime")]
        public string ValidTime { get; set; } = "24H";
    }

    //Yêu cầu cấp CTS Response Model
    public class CertificateReponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public CertificateDetailReponseModel Data { get; set; }
    }

    public class CertificateDetailReponseModel
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("subjectDn")]
        public string SubjectDN { get; set; }

        [JsonPropertyName("validFrom")]
        public string ValidFrom { get; set; }

        [JsonPropertyName("validTo")]
        public string ValidTo { get; set; }

        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }
    }
    #endregion

    #region Renew OTP from requestId
    public class RequestOTPFromRequestId
    {
        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; }
    }
    #endregion

    #region DotNet Sign Hash
    public class NetSignHSM
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("userPin")]
        public string UserPin { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("certificate")]
        public List<string> Certificate { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }

        [JsonPropertyName("requestList")]
        public List<NetSignRequest> RequestList { get; set; }

        [JsonPropertyName("userInfo")]
        public UserInfo UserInfo { get; set; }
    }

    public class NetSignTSA
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("requestList")]
        public List<NetSignRequest> RequestList { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }

        [JsonPropertyName("userInfo")]
        public UserInfo UserInfo { get; set; }
    }

    public class NetSignApprearance
    {
        [JsonPropertyName("llx")]
        public float Llx { get; set; }

        [JsonPropertyName("lly")]
        public float Lly { get; set; }

        [JsonPropertyName("urx")]
        public float Urx { get; set; }

        [JsonPropertyName("ury")]
        public float Ury { get; set; }

        [JsonPropertyName("page")]
        public float Page { get; set; }

        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; } = true;

        [JsonPropertyName("contact")]
        public string Contact { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("imageBase64")]
        public string ImageData { get; set; }

        [JsonPropertyName("tSA")]
        public bool TSA { get; set; }

        [JsonPropertyName("certify")]
        public bool Certify { get; set; }

        [JsonPropertyName("height")]
        public float Height { get; set; } = 0;

        [JsonPropertyName("width")]
        public float Width { get; set; } = 0;

        [JsonPropertyName("signBy")]
        public string SignBy { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

        [JsonPropertyName("position")]
        public string Position { get; set; }

        [JsonPropertyName("logoBase64")]
        public string Logo { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        [JsonPropertyName("moreInfo")]
        public string MoreInfo { get; set; }

        [JsonPropertyName("backgroundImageBase64")]
        public string BackgroundImageBase64 { get; set; }

        [JsonPropertyName("signatureGroup")]
        public SignatureGroup? SignatureGroup { get; set; }

        [JsonPropertyName("signatureType")]
        public SignatureType? SignatureType { get; set; } = Shared.SignatureType.CHU_KY_DAI_DIEN;

        public NetSignApprearance Copy()
        {
            return (NetSignApprearance)this.MemberwiseClone();
        }

        public void SetCoordinate(SignCoordinateModel coordinate)
        {
            this.Llx = coordinate.Llx;
            this.Lly = coordinate.Lly;
            this.Urx = coordinate.Urx;
            this.Ury = coordinate.Ury;
            this.Page = coordinate.Page;
            this.Width = coordinate.Width;
            this.Height = coordinate.Height;
        }

        //public NetSignApprearance CopySignAppearanceModelToNetAppearance(SignAppearanceModel appearanceModel)
        //{
        //    var netSignAppearance = new NetSignApprearance
        //    {
        //        Llx = appearanceModel.Llx,
        //        Lly = appearanceModel.Lly,
        //        Urx = appearanceModel.Urx,
        //        Ury = appearanceModel.Ury,
        //        Page = appearanceModel.Page,
        //        IsVisible = appearanceModel.IsVisible,
        //        Contact = appearanceModel.Contact,
        //        Phone = appearanceModel.Phone,
        //        Email = appearanceModel.Mail,
        //        Reason = appearanceModel.Reason,
        //        Location = appearanceModel.SignLocation,
        //        ImageData = appearanceModel.ImageData,
        //        Logo = appearanceModel.Logo,
        //        Detail = appearanceModel.Detail,
        //        SignBy = appearanceModel.SignBy
        //    };

        //    return netSignAppearance;
        //}
    }

    public class UserInfo
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public DateTime? Dob { get; set; }
        public string IdentityNumber { get; set; }
        public string IdentityType { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Country { get; set; }
        public string UserConnectId { get; set; }
        public int? Sex { get; set; }
        public string IssueName { get; set; }
        public DateTime? IssueDate { get; set; }
    }
    
    public class NetSignRequest
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("fileBase64")]
        public string FileBase64 { get; set; }

        [JsonPropertyName("documentCode")]
        public string DocumentCode { get; set; }

        [JsonPropertyName("appearances")]
        public NetSignApprearance Appearances { get; set; }
    }
    public class NetFileResponseModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("fileBase64")]
        public string FileBase64 { get; set; }

        #region Thông tin dùng cho tải file đã ký lên từ web-app
        [JsonPropertyName("fileBucketName")]
        public string FileBucketName { get; set; }

        [JsonPropertyName("fileObjectName")]
        public string FileObjectName { get; set; }
        #endregion
    }

    public class NetSignFileResponseDataModel
    {
        [JsonPropertyName("responseList")]
        public List<NetFileResponseModel> ResponseList { get; set; }
    }

    public class NetSignFileResult
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public NetSignFileResponseDataModel Data { get; set; }
    }

    public class NetHashFilesRequestModel
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("certificate")]
        public List<string> Certificate { get; set; }

        [JsonPropertyName("requestList")]
        public List<NetSignRequest> RequestList { get; set; }

        [JsonPropertyName("userSignConfigId")]
        public Guid? UserSignConfigId { get; set; }

        [JsonPropertyName("userInfo")]
        public UserInfo UserInfo { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }

        [JsonPropertyName("listFileAttachment")]
        public List<FileAttachmentModel> ListFileAttachment { get; set; }
    }

    public class NetRequestCertFromRAModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public NetRequestCertFromRADataModel Data { get; set; }
    }

    public class NetRequestCertFromRADataModel
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
        public Guid EformId { get; set; }
    }

    public class NetAttachFileModel
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("requestList")]
        public List<NetAttachFileRequestList> RequestList { get; set; }

        [JsonPropertyName("userInfo")]
        public UserInfo UserInfo { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }

        [JsonPropertyName("listFileAttachment")]
        public List<FileAttachmentModel> ListFileAttachment { get; set; }
    }

    public class NetAttachFileRequestList
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

    public class NetHashFileResponseModel
    {
        [JsonPropertyName("data")]
        public NetHashFileResponseDataModel Data { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class NetHashFileResponseDataModel
    {
        [JsonPropertyName("responseList")]
        public List<NetHashFileResponseListModel> ResponseList { get; set; }
    }

    public class NetHashFileResponseListModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("hashData")]
        public string HashData { get; set; }
    }

    public class RequestCertModel
    {
        [JsonPropertyName("caInfo")]
        public CertCAInfo CaInfo { get; set; }

        [JsonPropertyName("keyInfo")]
        public CertKeyInfo KeyInfo { get; set; }

        [JsonPropertyName("generalInfo")]
        public CertGeneralInfo GeneralInfo { get; set; }

        [JsonPropertyName("userInfo")]
        public CertUsreInfo UserInfo { get; set; }
    }

    public class CertCAInfo
    {
        [JsonPropertyName("caName")]
        public string CaName { get; set; }

        [JsonPropertyName("endEntityProfileName")]
        public string EndEntityProfileName { get; set; }

        [JsonPropertyName("certificateProfileName")]
        public string CertificateProfileName { get; set; }

        [JsonPropertyName("validTime")]
        public string ValidTime { get; set; }
    }

    public class CertKeyInfo
    {
        [JsonPropertyName("keyPrefix")]
        public string KeyPrefix { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("keyLength")]
        public int KeyLength { get; set; }

        [JsonPropertyName("pinCode")]
        public string PinCode { get; set; }
    }

    public class CertGeneralInfo
    {
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; }

        [JsonPropertyName("macAddress")]
        public string MacAddress { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }
    }

    public class CertUsreInfo
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("dob")]
        public string Dob { get; set; }

        [JsonPropertyName("identityNo")]
        public string IdentityNo { get; set; }

        [JsonPropertyName("issueDate")]
        public string IssueDate { get; set; }

        [JsonPropertyName("issuePlace")]
        public string IssuePlace { get; set; }

        [JsonPropertyName("permanentAddress")]
        public string PermanentAddress { get; set; }

        [JsonPropertyName("currentAddress")]
        public string CurrentAddress { get; set; }

        [JsonPropertyName("nation")]
        public string Nation { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

        [JsonPropertyName("organizationUnit")]
        public string OrganizationUnit { get; set; }

        [JsonPropertyName("documents")]
        public List<string> Documents { get; set; }
    }

    public class RequestCertResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public RequestCertResponseDataModel Data { get; set; }
    }

    public class RequestCertResponseDataModel
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
    #endregion

    #region Vkey Model
    public class VkeyResponseUserModel
    {
        /*
         {
            "code": 200,
            "message": "Thêm mới thành công",
            "traceId": "1e4d6a57-bbc2-4cc7-b0f8-7c7a789ed864",
            "data": {
                "userId": "385a8e319e1f4cbb827ffa3d6f3fff28",
                "tokenSerial": "t7D467C4C3",
                "apin": "GFgZX+3aAlNyanzwvmlFmxu9zKmCNvIVGQvcRJC6QGhW4HksWNFIt//hZwnGRkWtQ0FTsEtCQ5Z2zCrCjWsoDQb4FdO3SM1j55oJzSjf1oM="
            }
        }
         */
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public VkeyResponseUserDetailModel Data { get; set; }
    }

    public class VkeyResponseUserDetailModel
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("tokenSerial")]
        public string TokenSerial { get; set; }

        [JsonPropertyName("apin")]
        public string Apin { get; set; }
    }

    public class VkeyRequestAuthModel
    {
        /*
         {
            "messageId": "6648a8f5-a2df-4a6d-af12-25f4021a326b",
            "messageType": "AUTH",
            "ts": "nZ49D9F4F7",
            "payloadData": {
                "passType": "2",
                "notifyMsgFlag": "0",
                "notifyMsgData": {
                    "textToDisplay": "Authentication Request from Bank"
                },
                "msgFlag": "0",
                "msgData": {
                    "data": "eyJhcHBsaWNhdGlvbiI6Ik8zNjUiLCJ0aXRsZSI6IkZJRE8yIEF1dGhlbnRpY2F0aW9uIiwiZGlzcGxheU1lc3NhZ2UiOiJUaGlzIGlzIHRoZSBwdXNoIGNvbnRlbnQgZnJvbSBkYXRhYmFzZSIsInRpbWVvdXQiOjYwLCJ0cyI6Im5aNDlEOUY0RjcifQ=="
                }
            },
            "vseSign": "1",
            "customerId": "7837",
            "userId": "123",
            "deviceId": "123",
            "organization": "123",
            "organizationUnit": "456",
            "identityNo": "123456789",
            "email": "tienbnm258456@gmail.com",
            "callbackUrl": "http://10.0.20.7:30200/api/v1/contract/confirm-sign-document-from-esign",
            "username": "tiennk",
            "phoneNumber": "0987876543"
        }
         */
        [JsonPropertyName("messageId")]
        public string MessageId { get; set; }

        [JsonPropertyName("messageType")]
        public string MessageType { get; set; } = "AUTH";

        [JsonPropertyName("ts")]
        public string TS { get; set; } = "nZ49D9F4F7";

        [JsonPropertyName("payloadData")]
        public VkeyRequestAuthPayloadModel PayloadData { get; set; }

        [JsonPropertyName("vseSign")]
        public string VseSign { get; set; } = "1";

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = "7837";

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "123";

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("organization")]
        public string Organization { get; set; }

        [JsonPropertyName("organizationUnit")]
        public string OrganizationUnit { get; set; }

        [JsonPropertyName("identityNo")]
        public string IdentityNo { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }
    }

    public class VkeyRequestAuthPayloadModel
    {
        /*
         "passType": "2",
                "notifyMsgFlag": "0",
                "notifyMsgData": {
                    "textToDisplay": "Authentication Request from Bank"
                },
                "msgFlag": "0",
                "msgData": {
                    "data": "eyJhcHBsaWNhdGlvbiI6Ik8zNjUiLCJ0aXRsZSI6IkZJRE8yIEF1dGhlbnRpY2F0aW9uIiwiZGlzcGxheU1lc3NhZ2UiOiJUaGlzIGlzIHRoZSBwdXNoIGNvbnRlbnQgZnJvbSBkYXRhYmFzZSIsInRpbWVvdXQiOjYwLCJ0cyI6Im5aNDlEOUY0RjcifQ=="
                }
         */

        [JsonPropertyName("passType")]
        public string PassType { get; set; } = "2";

        [JsonPropertyName("notifyMsgFlag")]
        public string NotifyMsgFlag { get; set; } = "0";

        [JsonPropertyName("notifyMsgData")]
        public VkeyRequestAuthPayloadNotiDataModel NotifyMsgData { get; set; }

        [JsonPropertyName("msgFlag")]
        public string MsgFlag { get; set; } = "0";

        [JsonPropertyName("msgData")]
        public VkeyRequestAuthPayloadMsgDataModel MsgData { get; set; }
    }

    public class VkeyRequestAuthPayloadNotiDataModel
    {
        [JsonPropertyName("textToDisplay")]
        public string TextToDisplay { get; set; } = "Authentication Request from Bank";
    }
    public class VkeyRequestAuthPayloadMsgDataModel
    {
        [JsonPropertyName("data")]
        public string Data { get; set; } = "eyJhcHBsaWNhdGlvbiI6Ik8zNjUiLCJ0aXRsZSI6IkZJRE8yIEF1dGhlbnRpY2F0aW9uIiwiZGlzcGxheU1lc3NhZ2UiOiJUaGlzIGlzIHRoZSBwdXNoIGNvbnRlbnQgZnJvbSBkYXRhYmFzZSIsInRpbWVvdXQiOjYwLCJ0cyI6Im5aNDlEOUY0RjcifQ==";
    }
    #endregion

    #region Sign from single page
    public class NetAttachFromSinglePageModel
    {
        [JsonPropertyName("attachModel")]
        public NetAttachFileModel AttachModel { get; set; }

        [JsonPropertyName("requestInfoModel")]
        public RequestInfoModel RequestInfoModel { get; set; }
    }

    public class NetHashFromSinglePageModel
    {
        [JsonPropertyName("signModel")]
        public NetHashFilesRequestModel SignModel { get; set; }

        [JsonPropertyName("requestInfoModel")]
        public RequestInfoModel RequestInfoModel { get; set; }
    }

    public class RequestInfoModel
    {
        [JsonPropertyName("documentId")]
        public Guid DocumentId { get; set; }

        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("hashCode")]
        public string HashCode { get; set; }

        [JsonPropertyName("signType")]
        public int SignType { get; set; }
    }
    #endregion
    
    #region eVerify
    public class RequestSignNationHubRequestModel
    {
        public string TraceId { get; set; }               
        public List<NationHubRequestList> RequestList { get; set; }
    }

    public class NationHubRequestList
    {
        public string FileBase64 { get; set; }
        public Guid DocumentId { get; set; }
        public string DocumentCode { get; set; }
        public DocumentAction? DocumentAction { get; set; }
        public DateTime? StartDate { get; set; }
        public List<string> ReferenceDigest { get; set; }
        public ContractType? ContractType { get; set; }
    }

    public class EVerifyRequestModel
    {
        public List<EVerifyRequestDataModel> RequestList { get; set; }
    }
    public class EVerifyRequestDataModel
    {
        public string FileBase64 { get; set; }
        public Guid DocumentId { get; set; }
    }
    
    #endregion
}

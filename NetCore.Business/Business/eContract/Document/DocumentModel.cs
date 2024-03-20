using Microsoft.AspNetCore.Http;
using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class DocumentBaseModel
    {
        public Guid Id { get; set; }
        public string Document3rdId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        // [JsonIgnore]
        public Guid WorkflowId { get; set; }
        public Guid? DocumentBatchId { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; }
        public string DocumentBatchCode { get; set; }
        [JsonIgnore]
        public string DocumentBatchName { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public Guid? NextStepId { get; set; }
        public Guid? NextStepUserId { get; set; }
        public string NextStepUserName { get; set; }
        public string NextStepUserEmail { get; set; }
        public SignType NextStepSignType { get; set; }
        [JsonIgnore]
        public bool Status { get; set; } = true;
        public Guid? StateId { get; set; }
        public string State { get; set; }
        public string StateName { get; set; }
        public string StateNameForReject { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<DocumentFileModel> ListDocumentFile { get; set; }
        public bool IsSign { get; set; }
        public string LastReasonReject { get; set; }
        /// <summary>
        /// Thời hạn ký
        /// </summary>
        public DateTime? SignExpireAtDate { get; set; }

        /// <summary>
        /// Hết hạn ký
        /// True: Hết hạn
        /// False:Trong thời hạn
        /// </summary>
        public bool IsSignExpireAtDate { get; set; }
        [JsonIgnore]
        public string WorkFlowUserJson
        {
            get
            {
                return WorkFlowUser == null ? null : JsonSerializer.Serialize(WorkFlowUser);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    WorkFlowUser = null;
                else
                    WorkFlowUser = JsonSerializer.Deserialize<List<WorkFlowUserDocumentModel>>(value);
            }
        }

        public List<WorkFlowUserDocumentModel> WorkFlowUser { get; set; }

        public string UserFullName { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserIdentityNumber { get; set; }
        public string UserEmail { get; set; }
        public string OrganizationName { get; set; }

        public DateTime? SignCompleteAtDate { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string WorkflowCode { get; set; }
        public DateTime? WorkflowCreatedDate { get; set; }
        public bool IsCloseDocument { get; set; } = false;
        public DateTime? SignCloseAtDate { get; set; }
        public bool IsAllowRenew { get; set; }
        public string CreatedUserName { get; set; }
        
        [JsonIgnore]
        public string ExportDocumentDataJson { get; set; }

        public List<ExportDocumentData> ExportDocumentData
        {
            get
            {
                return !string.IsNullOrEmpty(ExportDocumentDataJson)
                    ? JsonSerializer.Deserialize<List<ExportDocumentData>>(ExportDocumentDataJson)
                    : null;
            }
        } 
    }

    public class DocumentModel : DocumentBaseModel
    {
        public List<DocumentFileModel> DocumentFile { get; set; }
        public string Description { get; set; }
        public int Order { get; set; } = 0;
        public string ADSSProfileName { get; set; }
        public int? SignExpireAfterDay { get; set; }
        public int? SignType { get; set; }
    }

    public class DocumentDetailModel : DocumentModel
    {
        public Guid? CreatedUserId { get; set; }

        //public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class DocumentUpdateModel : DocumentModel
    {
    }

    //public class DocumentCreateModel: DocumentModel
    //{

    //}
    public class DocumentQueryFilter
    {
        public string TextSearch { get; set; }
        public string ReferenceCode { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public int? Status { get; set; }
        public bool IsDeleted { get; set; }
        public bool? AssignMe { get; set; }
        public Guid? DocumentBatchId { get; set; }
        public Guid? StateId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";

        /// <summary>
        ///Hạn ký hợp đồng? 
        ///True: Hết hạn
        ///False: Trong thời hạn
        /// </summary>
        public bool IsSignExpireAtDate { get; set; }
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public DocumentQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }

        /// <summary>
        /// Sắp hết hạn ký hợp đồng
        /// True: Sắp hết hạn
        /// Fasle: Ngoài thời hạn được cấu hình
        /// </summary>
        public bool IsIncommingSignExpirationDate { get; set; }

        public string UserName { get; set; }

        public DateTime? SignStartDate { get; set; }

        public DateTime? SignEndDate { get; set; }

        public bool IsClosed { get; set; }
    }

    public class DocumentFileModel
    {
        [JsonIgnore]
        public Guid DocumentId { get; set; }
        public Guid Id { get; set; }
        public string FileBucketName { get; set; }
        public string FileObjectName { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FilePreviewUrl { get; set; }
        public string ProfileName { get; set; }
        public int Order { get; set; } = 0;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

    public class ResponeSendToWorkflowModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Result { get; set; }
        public string Message { get; set; }
    }

    public class ResponeGetCertInfoModel
    {
        public CertProfileModel Data { get; set; }
        public Code Code { get; set; }
        public string Message { get; set; }
        public int TotalTime { get; set; }
    }

    public class CertProfileModel
    {
        public string CertAlias { get; set; }
        public string CertUserPin { get; set; }
        public string CertSlotLabel { get; set; }
    }



    public class DocumentSendMailModel
    {
        public List<Guid> ListDocumentId { get; set; }
        public bool IsSendPrivateMail { get; set; }
        public bool IsSendMuiltipleMail { get; set; }
        public List<EmailModel> ListEmail { get; set; }
    }
    public class EmailModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class WorkflowDocumentProcessingModel
    {
        public List<Guid> ListDocumentId { get; set; }
        public string Base64Image { get; set; }
        public int? SignType { get; set; }   // 1. Ký chứng thực     2. Ký phê duyệt     3. Ký review  
        public string OrganizationId { get; set; }
    }

    public class WorkflowDocumentSignReponseModel
    {
        public Guid DocumentId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public SignFileModel SignFile { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class WorkflowDocumentSignFor3rdReponseModel
    {
        public string DocumentCode { get; set; }
        public string FileUrl { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }


    public class WorkflowDocumentSignEFormFor3rdReponseModel
    {
        public string DocumentCode { get; set; }
        public string FilePreviewUrl { get; set; }
        public string FileUrl { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    //Model tạo lập hợp đồng từ bên thứ 3
    public class DocumentCreatePDFManyModel
    {
        public List<UserConnectModel> ListUser { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? CustomOrganizationId { get; set; }
        public string OrganizationCode { get; set; }
        public string DocumentBatch3rdId { get; set; }
        public string WorkFlowCode { get; set; }
        public string DocumentTypeCode { get; set; }
        public bool IsPos { get; set; }
        public List<DocumentCreatePDFManyDetailModel> ListDocument { get; set; }
    }

    public class DocumentCreatePDFManyDetailModel
    {
        public string CustomerConnectId { get; set; }
        public string CreatedByUserConnectId { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string FileBase64 { get; set; }
        public string HashFile { get; set; }
        public List<string> WorkFlowUser { get; set; }
        public List<DocumentMetaData> MetaData { get; set; }
    }

    public class DocumentCreateMetaDataManyModel
    {
        public List<UserConnectModel> ListUser { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? CustomOrganizationId { get; set; }
        public string OrganizationCode { get; set; }
        public string DocumentBatch3rdId { get; set; }
        public string WorkFlowCode { get; set; }
        public string DocumentTypeCode { get; set; }
        public bool IsPos { get; set; }
        public List<DocumentCreateMetaDataManyDetailModel> ListDocument { get; set; }
    }

    public class DocumentCreateMetaDataManyDetailModel
    {
        public string CustomerConnectId { get; set; }
        public string CreatedByUserConnectId { get; set; }
        public string CreatedByUserName { get; set; }
        public List<ExportDocumentData> ExportDocumentData { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public List<MetaDataListForDocumentType> ListMetaData { get; set; }
        public List<string> WorkFlowUser { get; set; }
        public List<DocumentMetaData> MetaData { get; set; }
    }

    public class DocumentCreateManyResponseTempModel
    {
        [JsonIgnore]
        public Document Document { get; set; }
        [JsonIgnore]
        public Guid DocumentId { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public Guid? NextStepUserId { get; set; }
        public string NextStepUserName { get; set; }
        public string NextStepUserEmail { get; set; }
        public string NextStepUserPhoneNumber { get; set; }
        public string NextStepUserFullName { get; set; }
        public string OneTimePassCode { get; set; }
        public string State { get; set; }
        public string StateName { get; set; }
        public DateTime? SignExpireAtDate { get; set; }
    }

    public class DocumentCreateNotIncludeFileModel
    {
        public string CustomerConnectId { get; set; }
        public string WorkFlowCode { get; set; }
        public string DocumentTypeCode { get; set; }
        public string DocumentCode { get; set; }
        public string HashFile { get; set; }
        public bool IsPos { get; set; }
        public List<string> WorkFlowUser { get; set; }
    }

    public class CustomerInfoModel
    {
        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhoneNumber { get; set; }
    }

    public class WorkFlowInfoModel
    {
        [JsonPropertyName("identity")]
        public Guid? Identity { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    public class CreatDocument3rdResponseModel
    {
        public string DocumentBatch3rdId { get; set; }
        public string DocumentBatchCode { get; set; }
        public List<ResponseAccessLinkModel> ListDocument { get; set; }
    }

    public class ResponseAccessLinkModel
    {
        public string Url { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentCode { get; set; }
        public string State { get; set; }
        public string StateName { get; set; }
        [JsonIgnore]
        public DateTime? SignExpireAtDate { get; set; }
        [JsonPropertyName("signExpireAtDate")]
        public string SignExpireAtDateFormat
        {
            get
            {
                if (SignExpireAtDate.HasValue)
                {
                    return SignExpireAtDate.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff");
                }
                return null;
            }
        }
    }

    //public class WorkFlowDetailModel
    //{
    //    [JsonPropertyName("id")]
    //    public Guid Id { get; set; }
    //    [JsonPropertyName("code")]
    //    public string Code { get; set; }
    //    [JsonPropertyName("name")]
    //    public string Name { get; set; }
    //    [JsonPropertyName("organizationId")]
    //    public Guid OrganizationId { get; set; }
    //    [JsonPropertyName("createdDate")]
    //    public DateTime CreatedDate { get; set; }
    //    [JsonPropertyName("listUser")]
    //    public List<WorkFlowUserDocumentModel> ListUser { get; set; }
    //}
    //public class WorkFlowResponseModel
    //{
    //    [JsonPropertyName("data")]
    //    public WorkFlowDetailModel Data { get; set; }
    //    [JsonPropertyName("message")]
    //    public string Message { get; set; }
    //    [JsonPropertyName("code")]
    //    public Code Code { get; set; }
    //}

    public class OrgAndUserConnectInfoResponseModel
    {
        [JsonPropertyName("data")]
        public OrgAndUserConnectInfo Data { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("code")]
        public Code Code { get; set; }
    }

    public class OrgAndUserConnectInfoRequestModel
    {
        public Guid OrganizationId { get; set; }
        public Guid? CustomOrganizationId { get; set; }
        public List<string> ListUserConnectId { get; set; }
    }


    public class OrgAndUserConnectInfo
    {
        public OrganizationForServiceModel OrganizationInfo { get; set; }
        public List<UserConnectInfoModel> ListUserConnectInfo { get; set; }
    }


    //TODO: Delete here
    public class UserConnectInfoDeleteModel
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        [JsonPropertyName("userConnectId")]
        public string UserConnectId { get; set; }
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; }
        [JsonPropertyName("userEmail")]
        public string UserEmail { get; set; }
        [JsonPropertyName("userPhoneNumber")]
        public string UserPhoneNumber { get; set; }
    }

    //TODO: Delete here
    public class OrganizationForServiceDeleteModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    public class DocumentApproveRejectFrom3rdModel
    {
        public string DocumentCode { get; set; }
        public string Reason { get; set; }
    }

    public class DocumentRequestByUserConnectIdModel
    {
        public string UserConnectId { get; set; }
    }
    public class DocumentRequestByUserConnectIdResonseModel
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeName { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public string State { get; set; }
        public DateTime? CreadtedDate { get; set; }
        public string DocumentStateName { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
    }

    public class CreateEFormFrom3rdModel
    {
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class GetLatestDocumentUserFrom3rdModel
    {
        public string DocumentTypeCode { get; set; }
        public List<string> ListDocumentTypeCode { get; set; }
        public List<string> ListWorkFlowCode { get; set; }
        public string WorkFlowCode { get; set; }
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class GetDoumentDetailByCodeFrom3rdModel
    {
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public string DocumentCode { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class DocumentInfoFrom3rdResponseModel
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeName { get; set; }
        public string IdentiNumber { get; set; }
        public string UserFullName { get; set; }
        public string DocumentName { get; set; }
        public string DocumentCode { get; set; }
        public string FilePreviewUrl { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public string DocumentStatusName { get; set; }
        public string State { get; set; }
        public string StateName { get; set; }
        public string LastReasonReject { get; set; }
        [JsonIgnore]
        public DateTime? SignExpireAtDate { get; set; }
        [JsonPropertyName("signExpireAtDate")]
        public string SignExpireAtDateFormat
        {
            get
            {
                if (SignExpireAtDate.HasValue)
                {
                    return SignExpireAtDate.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff");
                }
                return null;
            }
        }
        public List<string> ListImagePreview { get; set; }
        public List<DocumentMetaData> ListMetaData { get; set; }
    }

    public class CreateEFormFrom3rdResponseModel
    {
        public string EFormType { get; set; }
        public string DocumentCode { get; set; }
        public string FilePreviewUrl { get; set; }
        public string IdentifierDevice { get; set; }
        public List<string> ListImagePreview { get; set; }
        public string Consent { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
    }

    public class RequestUrlDownloadDocumentFrom3rdModel
    {
        public Guid? UserId { get; set; }
        public string UserConnectId { get; set; }
        public string DocumentCode { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class UrlDownloadDocumentFrom3rdResponseModel
    {
        public string Fileurl { get; set; }
    }

    public class Document3rdDetailResponseModel
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeName { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public string State { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public string DocumentStatusName { get; set; }
        public string FileUrl { get; set; }
        public List<DocumentSignedWorkFlowUser> WorkFlowUser { get; set; }
        public List<DocumentMetaData> MetaData { get; set; }
    }

    public class DocumentUpdateSignExpireAtDateModel
    {
        public Guid? DocumentId { get; set; }
        public DateTime? SignExpireAtDate { get; set; }
        public Guid? NotifyConfigId { get; set; }
        public string LastReasonReject { get; set; }
        public List<Guid> DocumentIds { get; set; }
        public List<Guid?> UserIds { get; set; }
    }

    public class GetUserInWorkflowInListDocumentIdModel
    {
        public List<Guid> DocumentIds { get; set; }
    }

    public class DocumentUpdateStatusModel
    {
        public List<Guid> DocumentIds { get; set; }
        public string LastReasonReject { get; set; }
        public Guid? NotifyConfigId { get; set; }
    }

    public class DocumentRejectModel
    {
        // list document id
        public List<Guid> ListId { get; set; }
        public string LastReasonReject { get; set; }
        public Guid? NotifyConfigId { get; set; }
    }

    public class DocumentSendNotify 
    {
        public Guid Id { get; set; }
        public Guid? NotifyConfigId { get; set; }
    }

    public class DocumentByListUserModel
    {
        public Guid UserId { get; set; }
        public List<DocumentByListUserInfoModel> Documents { get; set; }
    }

    public class DocumentByListUserInfoModel
    {
        public Guid DocumentId { get; set; }
        public string DocumentCode { get; set; }
        public Guid UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class FileTemplateStreamModel
    {
        public Guid Id { get; set; }
        public MemoryStream FileTemplateStream { get; set; }
    }

    #region Web app
    public class GetDocumentFromWebAppModel
    {
        public string DocumentCode { get; set; }
        public string Email { get; set; }
        public string PassCode { get; set; }
    }
    public class GetDocumentFromWebAppResponseModel
    {
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
        public string FilePreviewUrl { get; set; }
        public string HashCode { get; set; }
        public CreateEFormFrom3rdResponseModel EFormData { get; set; }
    }

    public class SignDocumentFromWebAppModel
    {
        public Guid DocumentId { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string SignatureBase64 { get; set; }
        public string OTP { get; set; }
        /// <summary>
        /// Loại hình ký: 1: Ký, 2: Ký và gửi email
        /// </summary>
        public int SignType { get; set; }
        public string HashCode { get; set; }
    }   
    
    public class UploadSignedDocumentFromWebAppModel
    {
        public string FileBucketName { get; set; }
        public string FileObjectName { get; set; }
        public string HashCode { get; set; }
    }

    public class RejectDocumentFromWebAppModel
    {
        public Guid DocumentId { get; set; }
        public Guid UserId { get; set; }
        public string Reason { get; set; }
        public string HashCode { get; set; }
    }

    public class SignDocumentFromWebAppResponseModel
    {
        public string FileUrl { get; set; }
    }

    public class ConfirmEformFromWebAppModel
    {
        public Guid UserId { get; set; }
        public string HashCode { get; set; }
    }

    public class ResendOTPSignDocumentFromWebAppModel
    {
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
        public string HashCode { get; set; }
    }
    #endregion

    #region Mobile App
    public class DocumentQueryFilterMobileApp
    {
        public DocumentQueryFilterMobileApp()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }

        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public Guid CurrentUserId { get; set; }
        public string UserConnectId { get; set; }
        public string DocumentTypeCode { get; set; }
        public bool? IsSign { get; set; }
    }

    public class DocumentBaseModelMobileApp
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeName { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public string State { get; set; }
        public bool IsSign { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? SignExpireAtDate { get; set; }
    }
    #endregion

    public class EVerifyDocumentRequest
    {
        public List<EVerifyDocumentData> EVerifyDocuments { get; set; }
        public string TraceId { get; set; }
    }

    public class EVerifyDocumentData
    {
        public Guid DocumentId { get; set; }
        public string VerificationCode { get; set; }
    }
}
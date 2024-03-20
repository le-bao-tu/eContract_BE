using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("document")]
    public class Document : BaseTableWithOrganization
    {
        public Document()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [StringLength(128)]
        [Column("name")]
        public string Name { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        public User User { get; set; }

        [StringLength(128)]
        [Column("email")]
        public string Email { get; set; }

        [StringLength(128)]
        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        [StringLength(128)]
        [Column("full_name")]
        public string FullName { get; set; }

        [Column("workflow_id")]
        public Guid WorkflowId { get; set; }

        [ForeignKey("DocumentType")]
        [Column("document_type_id")]
        public Guid? DocumentTypeId { get; set; }

        public DocumentType DocumentType { get; set; }

        [ForeignKey("DocumentBatch")]
        [Column("document_batch_id")]
        public Guid? DocumentBatchId { get; set; }

        public DocumentBatch DocumentBatch { get; set; }

        [Column("document_3rd_id")]
        public string Document3rdId { get; set; }

        [Column("document_status")]
        public DocumentStatus DocumentStatus { get; set; }

        [Column("workflow_start_date")]
        public DateTime? WorkflowStartDate { get; set; }

        [Column("next_step_id")]
        public Guid? NextStepId { get; set; }

        [Column("next_step_user_id")]
        public Guid? NextStepUserId { get; set; }

        [Column("next_step_user_name")]
        public string NextStepUserName { get; set; }

        [Column("next_step_user_email")]
        public string NextStepUserEmail { get; set; }

        [Column("next_step_user_phone_number")]
        public string NextStepUserPhoneNumber { get; set; }

        [Column("sign_complete_at_date")]
        public DateTime? SignCompleteAtDate { get; set; }

        [Column("sign_expire_at_date")]
        public DateTime? SignExpireAtDate { get; set; }

        [Column("sign_close_at_date")]
        public DateTime? SignCloseAtDate { get; set; }

        [Column("request_sign_at_date")]
        public DateTime? RequestSignAtDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("WorkflowState")]
        [Column("state_id")]
        public Guid? StateId { get; set; }

        public WorkflowState WorkflowState { get; set; }

        [StringLength(128)]
        [Column("state")]
        public string State { get; set; }

        /*
        Loại hình ký:
           1: ký chứng thực
           2: Ký phê duyệt
           3: Người review
           4: Người nhận CC/CN sau ký
        */
        [Column("next_step_sign_type")]
        public SignType NextStepSignType { get; set; }

        //Workflow
        [Column("workflow_user_json")]
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

        [NotMapped]
        public List<WorkFlowUserDocumentModel> WorkFlowUser { get; set; }

        [Column("meta_data_json")]
        public string MetaDataJson
        {
            get
            {
                return MetaData == null ? null : JsonSerializer.Serialize(MetaData);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    MetaData = null;
                else
                    MetaData = JsonSerializer.Deserialize<List<DocumentMetaData>>(value);
            }
        }

        [NotMapped]
        public List<DocumentMetaData> MetaData { get; set; }

        [Column("one_time_pass_code")]
        public string OneTimePassCode { get; set; }

        [Column("pass_code_expire_date")]
        public DateTime? PassCodeExpireDate { get; set; }

        [Column("list_email_reception_json")]
        public string ListEmailReceptionJson
        {
            get
            {
                return EmailsReception == null ? null : JsonSerializer.Serialize(EmailsReception);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    EmailsReception = null;
                else
                    EmailsReception = JsonSerializer.Deserialize<List<string>>(value);
            }
        }

        [NotMapped]
        public List<string> EmailsReception { get; set; }

        [Column("last_reason_reject")]
        public string LastReasonReject { get; set; }

        [Column("bucket_name")]
        public string BucketName { get; set; }

        [Column("object_name_directory")]
        public string ObjectNameDirectory { get; set; }

        [Column("file_name_prefix")]
        public string FileNamePrefix { get; set; }

        // Mỗi lần thay đổi trạng thái quy trình (ký + duyệt) => cập nhật RenewTimes (số lần renew của HĐ về 0)
        [Column("renew_times")]
        public int RenewTimes { get; set; }

        [Column("created_username")]
        public string CreatedUserName{ get; set; }

        [Column("export_document_data")]
        public string ExportDocumentDataJson
        {
            get
            {
                return ExportDocumentData == null ? null : JsonSerializer.Serialize(ExportDocumentData);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    ExportDocumentData = null;
                else
                    ExportDocumentData = JsonSerializer.Deserialize<List<ExportDocumentData>>(value);
            }
        }
        
        [NotMapped]
        public List<ExportDocumentData> ExportDocumentData { get; set; }
        
        // Ngày hiệu lực hợp đồng
        public DateTime? StartDate { get; set; }
        
        [Column("document_sign_info")]
        public string DocumentSignInfoJson {
            get
            {
                return DocumentSignInfo == null ? null : JsonSerializer.Serialize(DocumentSignInfo);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    DocumentSignInfo = null;
                else
                    DocumentSignInfo = JsonSerializer.Deserialize<DocumentSignInfo>(value);
            } 
        }
        
        [NotMapped]
        public DocumentSignInfo DocumentSignInfo { get; set; }
        
        [Column("verify_code")]
        public string VerifyCode { get; set; }
        
        [Column("is_verified")]
        public bool IsVerified { get; set; }
        
        [Column("verify_date")]
        public DateTime? VerifyDate { get; set; }
        
        [Column("is_use_everify")]
        public bool IsUseEverify { get; set; }
    }

    public class DocumentSignInfo
    {
        [JsonPropertyName("sign_tsa")]
        public int SignTSA { get; set; } = 0;
        
        [JsonPropertyName("sign_usb_token")]
        public int SignUSBToken { get; set; } = 0;
        
        [JsonPropertyName("sign_hsm")]
        public int SignHSM { get; set; } = 0;
        
        [JsonPropertyName("sign_adss")]
        public int SignADSS { get; set; } = 0;
        
        [JsonPropertyName("advance_ekyc")]
        public bool AdvanceEKyc { get; set; } = false;
    }

    public class ExportDocumentData
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class DocumentMetaData
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public enum DocumentStatus
    {
        DRAFT = 0,
        PROCESSING = 1,
        CANCEL = 2,
        FINISH = 3,
        ERROR = 500,
    }

    /*
        Loại hình ký:
        1: ký chứng thực
        2: Ký phê duyệt
        3: Người review
        4: Người nhận CC/CN sau ký
    */
    //public enum SignType
    //{
    //    KY_CHUNG_THUC = 1,
    //    KY_PHE_DUYET = 2,
    //    REVIEW = 3,
    //    KY_SO = 4,
    //    KY_DIEN_TU = 5
    //}
}

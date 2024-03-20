using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("document_batch")]
    public class DocumentBatch : BaseTableWithOrganization
    {
        public DocumentBatch()
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

        [Column("document_batch_3rd_id")]
        public string DocumentBatch3rdId { get; set; }

        [ForeignKey("DocumentType")]
        [Column("document_type_id")]
        public Guid? DocumentTypeId { get; set; }

        public DocumentType DocumentType { get; set; }

        /*
         Cấu hình thêm mới:
            1: Tải lên file để ký
            2: Tạo hợp đồng bằng cách nhập dữ liệu từ file excel vào mẫu đã được cấu hình
         */
        [Column("type")]
        public int Type { get; set; }

        [Column("workflow_id")]
        public Guid? WorkflowId { get; set; }

        //Workflow
        [Column("workflow_contact_json")]
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
                    WorkFlowUser = JsonSerializer.Deserialize<List<WorkFlowUserDataModel>>(value);
            }
        }

        [NotMapped]
        public List<WorkFlowUserDataModel> WorkFlowUser { get; set; }

        [Column("number_of_email_per_week")]
        public int NumberOfEmailPerWeek { get; set; }

        [Column("is_generateFile")]
        public bool IsGenerateFile { get; set; } = false;

        [Column("list_meta_data_json")]
        public string ListMetaDataJson
        {
            get
            {
                return ListMetaData == null ? null : JsonSerializer.Serialize(ListMetaData);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ListMetaData = null;
                else
                    ListMetaData = JsonSerializer.Deserialize<List<MetaDataDocumentFileModel>>(value);
            }
        }

        [NotMapped]
        public List<MetaDataDocumentFileModel> ListMetaData { get; set; }

        [Column("identity_number", Order = 99)]
        public long IdentityNumber { get; set; }
    }

    public class WorkFlowUserDataModel
    {
        public Guid Id { get; set; }

        public SignType Type { get; set; }

        public string Name { get; set; }

        public Guid? UserId { get; set; }

        public string UserName { get; set; }

        public string UserFullName { get; set; }

        public string UserConnectId { get; set; }

        public string UserEmail { get; set; }

        public string UserPhoneNumber { get; set; }

        public Guid? StateId { get; set; }

        public string State { get; set; }

        public string StateName { get; set; }
    }

    public class WorkFlowUserDocumentModel : WorkFlowUserDataModel
    {
        public DateTime? SignAtDate { get; set; }
        public int? SignExpireAfterDay { get; set; }

        public string RejectReason { get; set; }

        public DateTime? RejectAtDate { get; set; }
        public int? SignCloseAfterDay { get; set; }

        public List<AttachDocument> ListAttachDocument { get; set; }
    }

    public class AttachDocument
    { 
        public string BucketName { get; set; }
        public string ObjectName { get; set; }
    }

    public class MetaDataDocumentFileModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public List<MetaDataDocumentModel> MetaData { get; set; }
    }
}

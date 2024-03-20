using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("document_workflow_history")]
    public class DocumentWorkflowHistory
    {
        public DocumentWorkflowHistory()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("Document")]
        [Column("document_id")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; }

        [Column("document_status")]
        public DocumentStatus DocumentStatus { get; set; }

        [Column("state")]
        public string State { get; set; }

        [Column("reason_reject")]
        public string ReasonReject { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("list_document_file_json")]
        public string ListDocumentFileJson
        {
            get
            {
                return ListDocumentFile == null ? null : JsonSerializer.Serialize(ListDocumentFile);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ListDocumentFile = null;
                else
                    ListDocumentFile = JsonSerializer.Deserialize<List<DocumentFileWorkflowHistory>>(value);
            }
        }

        [NotMapped]
        public List<DocumentFileWorkflowHistory> ListDocumentFile { get; set; }

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

    public class DocumentFileWorkflowHistory
    {
        [JsonPropertyName("document_file_id")]
        public Guid DocumentFileId { get; set; }

        [JsonPropertyName("bucket_name")]
        public string BucketName { get; set; }

        [JsonPropertyName("file_object_name")]
        public string ObjectName { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("hash_sha256")]
        public string HashSHA256 { get; set; }
    }
}

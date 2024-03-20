using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("document_batch_file")]
    public class DocumentBatchFile
    {
        public DocumentBatchFile()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("file_bucket_name")]
        public string FileBucketName { get; set; }

        [Column("file_object_name")]
        public string FileObjectName { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }

        [ForeignKey("DocumentBatch")]
        [Column("document_batch_id")]
        public Guid DocumentBatchId { get; set; }

        public DocumentBatch DocumentBatch { get; set; }

        [ForeignKey("DocumentFileTemplate")]
        [Column("document_file_template_id")]
        public Guid? DocumentFileTemplateId { get; set; }

        public DocumentFileTemplate DocumentFileTemplate { get; set; }

        [Column("order")]
        public int Order { get; set; } = 0;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

    public class MetaDataDocumentModel
    {
        public string Value { get; set; }
        public Guid MetaDataId { get; set; }
        public string MetaDataCode { get; set; }
        public string MetaDataName { get; set; }
    }
}

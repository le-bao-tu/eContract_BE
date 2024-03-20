using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    //Bảng này sau sẽ bỏ
    [Table("document_sign_history")]
    public class DocumentSignHistory
    {
        public DocumentSignHistory()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("file_type")]
        public FILE_TYPE FileType { get; set; }

        [Column("old_file_bucket_name")]
        public string OldFileBucketName { get; set; }

        [Column("old_file_object_name")]
        public string OldFileObjectName { get; set; }

        [Column("old_file_name")]
        public string OldFileName { get; set; }

        [Column("old_hash_file")]
        public string OldHashFile { get; set; }

        [Column("old_xml_file")]
        public string OldXMLFile { get; set; }

        [Column("new_file_bucket_name")]
        public string NewFileBucketName { get; set; }

        [Column("new_file_object_name")]
        public string NewFileObjectName { get; set; }

        [Column("new_file_name")]
        public string NewFileName { get; set; }

        [Column("new_hash_file")]
        public string NewHashFile { get; set; }

        [Column("new_xml_file")]
        public string NewXMLFile { get; set; }

        [Column("document_id")]
        public Guid DocumentId { get; set; }

        [Column("document_file_id")]
        public Guid DocumentFileId { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("order")]
        public int Order { get; set; } = 0;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }
}

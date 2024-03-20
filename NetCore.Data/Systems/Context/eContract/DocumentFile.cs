using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("document_file")]
    public class DocumentFile
    {
        public DocumentFile()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("file_bucket_name")]
        public string FileBucketName { get; set; }

        [Column("file_object_name")]
        public string FileObjectName { get; set; }

        [Column("file_preview_bucket_name")]
        public string FilePreviewBucketName { get; set; }

        [Column("file_preview_object_name")]
        public string FilePreviewObjectName { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }

        [Column("hash_file")]
        public string HashFile { get; set; }

        [Column("xml_file")]
        public string XMLFile { get; set; }

        /*
         Loại file:
            1. PDF
            2. HASH
            3. XML
         */
        [Column("file_type")]
        public FILE_TYPE FileType { get; set; }

        [ForeignKey("Document")]
        [Column("document_id")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; }

        [Column("document_file_template_id")]
        public Guid DocumentFileTemplateId { get; set; }

        [Column("profile_name")]
        public string ProfileName { get; set; }

        [Column("order")]
        public int Order { get; set; } = 0;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [Column("image_preview_json")]
        public string ImagePreviewJson
        {
            get
            {
                return ImagePreview == null ? null : JsonSerializer.Serialize(ImagePreview);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ImagePreview = null;
                else
                    ImagePreview = JsonSerializer.Deserialize<List<ImagePreview>>(value);
            }
        }

        [NotMapped]
        public List<ImagePreview> ImagePreview { get; set; }
    }

    public class ImagePreview
    {
        public string BucketName { get; set; }
        public string ObjectName { get; set; }
    }

    public enum FILE_TYPE
    {
        PDF = 1,
        HASH = 2,
        XML = 3
    }
}

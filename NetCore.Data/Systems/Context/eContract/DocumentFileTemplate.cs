using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("document_file_template")]
    public class DocumentFileTemplate
    {
        public DocumentFileTemplate()
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

        [Column("file_data_bucket_name")]
        public string FileDataBucketName { get; set; }

        [Column("file_data_object_name")]
        public string FileDataObjectName { get; set; }

        [Column("file_type")]
        public TemplateFileType FileType { get; set; } = TemplateFileType.PDF;

        [Column("profile_name")]
        public string ProfileName { get; set; }

        [ForeignKey("DocumentTemplate")]
        [Column("document_template_id")]
        public Guid DocumentTemplateId { get; set; }

        public DocumentTemplate DocumentTemplate { get; set; }

        [Column("meta_data_config_json")]
        public string MetaDataConfigJson
        {
            get
            {
                return MetaDataConfig == null ? null : JsonSerializer.Serialize(MetaDataConfig);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    MetaDataConfig = null;
                else
                    MetaDataConfig = JsonSerializer.Deserialize<List<MetaDataConfig>>(value);
            }
        }

        [NotMapped]
        public List<MetaDataConfig> MetaDataConfig { get; set; }

        [Column("sign_position_config_json")]
        public string SignPositionConfigJson
        {
            get
            {
                return SignPositionConfig == null ? null : JsonSerializer.Serialize(SignPositionConfig);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    SignPositionConfig = null;
                else
                    SignPositionConfig = JsonSerializer.Deserialize<List<MetaDataConfig>>(value);
            }
        }

        [NotMapped]
        public List<MetaDataConfig> SignPositionConfig { get; set; }

        [Column("order")]
        public int Order { get; set; } = 0;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

    public class MetaDataConfig
    {
        public Guid MetaDataId { get; set; }
        public string MetaDataCode { get; set; }
        public string FixCode { get; set; }
        public SignType SignType { get; set; }
        public string Type { get; set; }
        public int Page { get; set; }
        public bool IsShow { get; set; }
        public string Value { get; set; }
        public string TextAlign { get; set; }
        public string TextDecoration { get; set; }
        public string Font { get; set; }
        public string FontStyle { get; set; }
        public int FontSize { get; set; }
        public string FontWeight { get; set; }
        public string Color { get; set; }
        public decimal LLX { get; set; }
        public decimal LLY { get; set; }
        public decimal Height { get; set; }
        public decimal Width { get; set; }
        public int BorderWidthOfPage { get; set; }
        public decimal PageHeight { get; set; }
        public decimal PageWidth { get; set; }

        //Tọa độ động => nếu tìm thấy thì sẽ lấy theo tọa độ này
        public bool IsDynamicPosition { get; set; }
        public string TextAnchor { get; set; }
        public int FromPage { get; set; }
        public int TextFindedPosition { get; set; } = 1;
        public decimal DynamicFromAnchorLLX { get; set; }
        public decimal DynamicFromAnchorLLY { get; set; }
    }

    public enum TemplateFileType
    {
        PDF = 1,
        DOCX = 2
    }
}

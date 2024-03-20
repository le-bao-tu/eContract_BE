using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("document_template")]
    public class DocumentTemplate : BaseTableWithOrganization
    {
        public DocumentTemplate()
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

        [ForeignKey("DocumentType")]
        [Column("document_type_id")]
        public Guid DocumentTypeId { get; set; }

        public DocumentType DocumentType { get; set; }

        [Column("from_date")]
        public DateTime? FromDate { get; set; }

        [Column("to_date")]
        public DateTime? ToDate { get; set; }

        [Column("group_code")]
        public string GroupCode { get; set; }

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
                    MetaDataConfig = JsonSerializer.Deserialize<List<DocumentTemplateMeteDataConfig>>(value);
            }
        }

        [NotMapped]
        public List<DocumentTemplateMeteDataConfig> MetaDataConfig { get; set; }
    }

    public class DocumentTemplateMeteDataConfig
    {
        public Guid MetaDataId { get; set; }
        public string MetaDataCode { get; set; }
        public string MetaDataName { get; set; }
    }
}

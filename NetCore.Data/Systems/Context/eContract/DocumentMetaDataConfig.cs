using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("document_meta_data_config")]
    public class DocumentMetaDataConfig
    {
        public DocumentMetaDataConfig()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("DocumentTemplate")]
        [Column("document_template_id")]
        public Guid DocumentTemplateId { get; set; }

        public DocumentTemplate DocumentTemplate { get; set; }

        [ForeignKey("MetaData")]
        [Column("meta_data_id")]
        public Guid MetaDataId { get; set; }

        public MetaData MetaData { get; set; }

        [Column("order")]
        public int Order { get; set; } = 0;

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }
}

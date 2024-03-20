using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("role_map_document_type")]
    public class RoleMapDocumentType
    {
        public RoleMapDocumentType()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("Role")]
        [Required]
        [Column("role_id")]
        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        [ForeignKey("DocumentType")]
        [Required]
        [Column("document_type_id")]
        public Guid DocumentTypeId { get; set; }

        public DocumentType DocumentType { get; set; }
    }
}

using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("role_map_document_organization")]
    public class RoleMapDocumentOfOrganization
    {
        public RoleMapDocumentOfOrganization()
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

        [ForeignKey("Organization")]
        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        public Organization Organization { get; set; }
    }
}

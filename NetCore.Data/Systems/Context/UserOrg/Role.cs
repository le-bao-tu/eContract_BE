using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("role")]
    public class Role : BaseTableDefault
    {
        public Role()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [ForeignKey("Organization")]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        public Organization Organization { get; set; }
    }
}

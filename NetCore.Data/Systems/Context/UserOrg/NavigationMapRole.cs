using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("navigation_map_role")]
    public class NavigationMapRole
    {
        public NavigationMapRole()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("Navigation")]
        [Required]
        [Column("navigation_id")]
        public Guid NavigationId { get; set; }

        public Navigation Navigation { get; set; }

        [ForeignKey("Role")]
        [Required]
        [Column("role_id")]
        public Guid RoleId { get; set; }
        public Role Role { get; set; }
    }
}

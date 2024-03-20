using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("role_map_right")]
    public class RoleMapRight
    {
        public RoleMapRight()
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

        [ForeignKey("Right")]
        [Required]
        [Column("right_id")]
        public Guid RightId { get; set; }

        public Right Right { get; set; }
    }
}

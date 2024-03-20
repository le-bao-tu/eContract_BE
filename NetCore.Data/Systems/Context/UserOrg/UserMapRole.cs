using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("user_map_role")]
    public class UserMapRole
    {
        public UserMapRole()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [ForeignKey("User")]
        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        public User User { get; set; }

        [ForeignKey("Role")]
        [Required]
        [Column("role_id")]
        public Guid RoleId { get; set; }
        public Role Role { get; set; }
    }
}

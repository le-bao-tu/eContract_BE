using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("user_role")]
    public class UserRole : BaseTableDefault
    {
        public UserRole()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }


        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("is_user")]
        public bool IsUser { get; set; }

        [Required]
        [Column("is_org_admin")]
        public bool IsOrgAdmin { get; set; }

        [Required]
        [Column("is_system_admin")]
        public bool IsSystemAdmin { get; set; }
    }
}

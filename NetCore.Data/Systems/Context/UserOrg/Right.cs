using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("right")]
    public class Right : BaseTableDefault
    {
        public Right()
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

        [Required]
        [Column("group-name")]
        public string GroupName { get; set; }
    }
}

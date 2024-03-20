using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("system_appliation")]
    public class SystemApplication: BaseTableDefault
    {
        public SystemApplication()
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
    }
}

using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("district")]
    public class District : BaseTableDefault
    {
        public District()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [ForeignKey("Province")]
        [Column("province_id")]
        public Guid? ProvinceId { get; set; }

        public Province Province { get; set; }
    }
}

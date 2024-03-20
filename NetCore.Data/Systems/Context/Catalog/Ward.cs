using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("ward")]
    public class Ward : BaseTableDefault
    {
        public Ward()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [ForeignKey("District")]
        [Column("disctrict_id")]
        public Guid? DistrictId { get; set; }

        public District District { get; set; }
    }
}

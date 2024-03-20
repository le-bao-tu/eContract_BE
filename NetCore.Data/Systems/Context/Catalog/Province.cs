using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("province")]
    public class Province : BaseTableDefault
    {
        public Province()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("zip_code")]
        public string ZipCode { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [ForeignKey("Country")]
        [Column("country_id")]
        public Guid? CountryId { get; set; }

        public Country Country { get; set; }
    }
}

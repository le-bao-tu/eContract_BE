using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("navigation")]
    public class Navigation : BaseTableDefault
    {
        public Navigation()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [Column("i18n_name")]
        public string I18nName { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        [Column("link")]
        public string Link { get; set; }

        [Column("hide_in_breadcrumb")]
        public bool HideInBreadcrumb { get; set; }

        [Column("parent_id")]
        public Guid? ParentId { get; set; }
    }
}

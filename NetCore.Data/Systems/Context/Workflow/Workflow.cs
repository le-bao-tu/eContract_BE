using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("workflow")]
    public class Workflow : BaseTableWithApplication
    {
        public Workflow()
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

        [StringLength(128)]
        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [StringLength(128)]
        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("is_sign_org_confirm")]
        public bool IsSignOrgConfirm { get; set; } = false;

        [Column("is_sign_certify")]
        public bool IsSignCertify { get; set; } = false;

        #region Gửi thông báo
        //Hợp đồng hoàn thành ký
        [ForeignKey("NotifyConfigDocumentComplete")]
        [Column("notify_config_document_complete_id")]
        public Guid? NotifyConfigDocumentCompleteId { get; set; }

        public NotifyConfig NotifyConfigDocumentComplete { get; set; }
        #endregion
        
        [Column("is_use_everify")]
        public bool IsUseEverify { get; set; } = false;
    }
}

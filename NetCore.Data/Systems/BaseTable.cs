using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using NetCore.Shared;

namespace NetCore.Data
{
    public class BaseTableDefault
    {
        [Column("order", Order = 100)]
        public int Order { get; set; } = 0;

        [Column("status", Order = 101)]
        public bool Status { get; set; } = true;

        [Column("description", Order = 102)]
        public string Description { get; set; }

        [Column("created_date", Order = 103)]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [Column("created_user_id", Order = 104)]
        public Guid? CreatedUserId { get; set; } = UserConstants.AdministratorId;

        [Column("modified_date", Order = 105)]
        public DateTime? ModifiedDate { get; set; }

        [Column("modified_user_id", Order = 106)]
        public Guid? ModifiedUserId { get; set; }
    }

    public class BaseTableWithApplication: BaseTableDefault
    {

        [Column("application_id", Order = 107)]
        public Guid? ApplicationId { get; set; } = AppConstants.RootAppId;
    }

    public class BaseTableWithOrganization : BaseTableWithApplication
    {
        [Column("organization_id", Order = 108)]
        public Guid? OrganizationId { get; set; }
    }
    public class BaseTableWithIdentityNumber : BaseTableWithApplication
    {

        [Column("identity_number", Order = 99)]
        public long IdentityNumber { get; set; }
    }
}

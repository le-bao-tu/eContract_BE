using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class UserRoleBaseModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsUser { get; set; }
        public bool IsOrgAdmin { get; set; }
        public bool IsSystemAdmin { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class UserRoleModel : UserRoleBaseModel
    {
        public int Order { get; set; } = 0;
        public bool IsLock { get; set; } = false;

    }
    public class UserRoleCreateOrUpdateModel : UserRoleModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
        public Guid? ModifiedUserId { get; set; }

        public void UpdateToEntity(UserRole entity)
        {
            entity.IsOrgAdmin = IsOrgAdmin;
            entity.IsSystemAdmin = IsSystemAdmin;
            entity.IsUser = IsUser;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }
}
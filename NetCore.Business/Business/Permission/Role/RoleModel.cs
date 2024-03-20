using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class RoleBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; } = true;
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class RoleModel : RoleBaseModel
    {
        public int Order { get; set; } = 0;
    }

    public class RoleCreateModel : RoleModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class RoleUpdateModel : RoleModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Role entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class UpdateDataPermissionModel
    {
        public Guid Id { get; set; }
        public List<Guid> ListDocumentTypeId { get; set; }
        public List<Guid> ListDocumentOfOrganizationId { get; set; }
        public List<Guid> ListUserInfoOfOrganizationId { get; set; }
        public Guid ModifiedUserId { get; set; }
    }
    public class GetDataPermissionModel
    {
        public Guid Id { get; set; }
    }
    public class ResultGetDataPermissionModel
    {
        public List<Guid> ListDocumentTypeId { get; set; }
        public List<Guid> ListDocumentOfOrganizationId { get; set; }
        public List<Guid> ListUserInfoOfOrganizationId { get; set; }
    }

    public class RoleQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid OrganizationId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public RoleQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class RoleSelectItemModel : SelectItemModel { 
        public Guid OrganizationId { get; set; }
    }

    public class UpdateRightByRoleModel
    {
        public Guid RoleId { get; set; }
        public List<Guid> RightIds { get; set; }
    }

    public class GetListRightIdByRoleModel
    {
        public Guid RoleId { get; set; }
    }

    public class ResultGetListRightIdByRoleModel
    {
        public List<Guid> RightIds { get; set; }
    }

    public class UpdateNavigationByRoleModel
    {
        public Guid RoleId { get; set; }
        public List<Guid> NavigationIds { get; set; }
    }

    public class GetListNavigationByRoleModel
    {
        public Guid RoleId { get; set; }
    }

    public class ResultGetListNavigationByRoleModel
    {
        public List<Guid> NavigationIds { get; set; }
    }
}

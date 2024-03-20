using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class NavigationBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string I18nName { get; set; }
        public string Icon { get; set; }
        public string Link { get; set; }
        public bool HideInBreadcrumb { get; set; } = false;
        public Guid? ParentId { get; set; }        
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public string Description { get; set; }
    }

    public class NavigationModel : NavigationBaseModel
    {
        public int Order { get; set; } = 0;
        public List<Guid> RoleIds { get; set; }
    }

    public class NavigationCreateModel : NavigationModel
    {
        public List<Guid> RoleIds { get; set; }
        public Guid? CreatedUserId { get; set; }
    }

    public class NavigationUpdateModel : NavigationModel
    {
        public Guid ModifiedUserId { get; set; }
        public List<Guid> RoleIds { get; set; }

        public void UpdateToEntity(Navigation entity)
        {
            entity.Name = this.Name;
            entity.I18nName = this.I18nName;
            entity.Link = this.Link;
            entity.Icon = this.Icon;
            entity.HideInBreadcrumb = this.HideInBreadcrumb;
            entity.ParentId = this.ParentId;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.ModifiedDate = DateTime.Now;
        }
    }

    public class NavigationQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public NavigationQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class NavigationSelectItemModel : SelectItemModel
    {    
        public Guid? ParentId { get; set; }
        public string I18nName { get; set; }
        public string Icon { get; set; }
        public bool Status { get; set; }
        public string Link { get; set; }
        public bool HideInBreadcrumb { get; set; }
        public string Description { get; set; }
        public List<Guid> ListRoleId { get; set; }
        public List<string> ListRoleCode { get; set; }
    }
}

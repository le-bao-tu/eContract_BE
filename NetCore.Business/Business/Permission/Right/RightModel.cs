using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class RightBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }      
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public string Description { get; set; }
    }

    public class RightModel : RightBaseModel
    {
        public int Order { get; set; } = 0;
        //public List<Guid> RoleIds { get; set; }
    }

    public class RightCreateModel : RightModel
    {
        //public List<Guid> RoleIds { get; set; }
        public Guid? CreatedUserId { get; set; }
    }

    public class RightUpdateModel : RightModel
    {
        public Guid ModifiedUserId { get; set; }
        //public List<Guid> RoleIds { get; set; }

        public void UpdateToEntity(Right entity)
        {
            entity.Name = this.Name;
            entity.GroupName = this.GroupName;
            entity.Status = this.Status;
            entity.Order = this.Order;
        }
    }

    public class RightQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public RightQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class RightSelectItemModel : SelectItemModel
    {
        public string GroupName { get; set; }
    }
}

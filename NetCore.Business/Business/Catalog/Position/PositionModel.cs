using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class PositionBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public string ShortName { get; set; }

        /*
            1: Phòng ban
            2: Đơn vị
         */
        public int Type { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string LogoUrl { get; set; }
        public string Path { get; set; }
        public bool Status { get; set; } = true;
        public Guid? OrganizationId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class PositionModel : PositionBaseModel
    {
        public int Order { get; set; } = 0;

        public string Description { get; set; }
    }

    public class PositionDetailModel : PositionModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class PositionCreateModel : PositionModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class PositionUpdateModel : PositionModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Position entity)
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

    public class PositionQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public PositionQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class PositionSelectItemModel: SelectItemModel
    {
        public Guid? OrganizationId { get; set; }
    }
}
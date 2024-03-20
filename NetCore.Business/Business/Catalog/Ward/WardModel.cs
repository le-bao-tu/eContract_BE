using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class WardBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? DistrictId { get; set; }
        public string DistrictName { get; set; }
        public Guid? ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
    }

    public class WardModel : WardBaseModel
    {
        public int Order { get; set; } = 0;

        public string Description { get; set; }
    }

    public class WardDetailModel : WardModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class WardCreateModel : WardModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class WardUpdateModel : WardModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Ward entity)
        {
            //entity.Code = this.Code;
            entity.DistrictId = this.DistrictId;
            entity.Name = this.Name;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class WardQueryFilter
    {
        public Guid? ProvinceId { get; set; }
        public Guid? DistrictId { get; set; }
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public WardQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class WardSelectItemModel : SelectItemModel { }
}
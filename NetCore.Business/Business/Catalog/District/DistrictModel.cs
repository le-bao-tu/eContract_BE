using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class DistrictBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public bool Status { get; set; } = true;
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class DistrictModel : DistrictBaseModel
    {
        public int Order { get; set; } = 0;

    }

    public class DistrictDetailModel : DistrictModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class DistrictCreateModel : DistrictModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class DistrictUpdateModel : DistrictModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(District entity)
        {
            //entity.Code = this.Code;
            entity.ProvinceId = this.ProvinceId;
            entity.Name = this.Name;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class DistrictQueryFilter
    {
        public Guid? ProvinceId { get; set; }
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public DistrictQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class DistrictSelectItemModel : SelectItemModel
    {
        public Guid? ProvinceId { get; set; }
    }
}
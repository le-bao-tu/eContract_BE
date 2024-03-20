using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class ProvinceBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid? CountryId { get; set; }
        public string CountryName { get; set; }
        public bool Status { get; set; } = true;
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class ProvinceModel : ProvinceBaseModel
    {
        public int Order { get; set; } = 0;
    }

    public class ProvinceDetailModel : ProvinceModel
    {
        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class ProvinceCreateModel : ProvinceModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class ProvinceUpdateModel : ProvinceModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Province entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.CountryId = this.CountryId;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class ProvinceQueryFilter
    {
        public Guid? CountryId { get; set; }
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public ProvinceQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class ProvinceSelectItemModel : SelectItemModel
    {
        public string ZipCode { get; set; }
    }
}
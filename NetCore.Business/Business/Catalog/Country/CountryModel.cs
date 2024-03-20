using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class CountryBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; } = true;
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class CountryModel : CountryBaseModel
    {
        public int Order { get; set; } = 0;

    }

    public class CountryDetailModel : CountryModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class CountryCreateModel : CountryModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class CountryUpdateModel : CountryModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Country entity)
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

    public class CountryQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public CountryQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class CountrySelectItemModel : SelectItemModel { }
}
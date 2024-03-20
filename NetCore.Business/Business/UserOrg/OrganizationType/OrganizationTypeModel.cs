using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class OrganizationTypeBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; } = true;
        public string Description { get; set; }
        public Guid? OrganizationId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class OrganizationTypeModel : OrganizationTypeBaseModel
    {
        public int Order { get; set; } = 0;
    }

    public class OrganizationTypeDetailModel : OrganizationTypeModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class OrganizationTypeCreateModel : OrganizationTypeModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class OrganizationTypeUpdateModel : OrganizationTypeModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(NetCore.Data.OrganizationType entity)
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

    public class OrganizationTypeQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public OrganizationTypeQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class OrganizationTypeSelectItemModel : SelectItemModel
    {
        public Guid? OrganizationId { get; set; }
    }

    public class OrganizationTypeSelectItemFor3rdModel
    {
        public string OrganizationTypeCode { get; set; }
        public string OrganizationTypeName { get; set; }
    }

    public class MetaDataListForOrganizationType
    {
        public string MetaDataCode { get; set; }
        public string MetaDataName { get; set; }
        public string MetaDataValue { get; set; }
    }
}

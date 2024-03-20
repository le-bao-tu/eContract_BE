using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class MetaDataBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public MetaDataType DataType { get; set; } = MetaDataType.TEXT;
        public bool IsRequire { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public class MetaDataModel : MetaDataBaseModel
    {
        public List<MetaDataList> ListData { get; set; }
        public int Order { get; set; } = 0;
    }

    public class MetaDataDetailModel : MetaDataModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class MetaDataCreateModel : MetaDataModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class MetaDataUpdateModel : MetaDataModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(MetaData entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.DataType = this.DataType;
            entity.IsRequire = this.IsRequire;
            entity.ListData = this.ListData;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class MetaDataFileValue
    {
        public Guid MetaDataId { get; set; }
        public string MetaDataValue { get; set; }
        public string MetaDataCode { get; set; }
        public string MetaDataName { get; set; }
        public int Page { get; set; }
        public string TextAlign { get; set; }
        public string TextDecoration { get; set; }
        public string Font { get; set; }
        public string FontStyle { get; set; }
        public int FontSize { get; set; }
        public string FontWeight { get; set; }
        public string Color { get; set; }
        public decimal LLX { get; set; }
        public decimal LLY { get; set; }
        public decimal PageHeight { get; set; }
        public decimal PageWidth { get; set; }
        public decimal Height { get; set; }
        public decimal Width { get; set; }
        public int BorderWidthOfPage { get; set; }
    }

    public class MetaDataQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public MetaDataQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class MetaDataSelectItemModel : SelectItemModel
    {
        public bool Status { get; set; }
        public Guid? OrganizationId { get; set; }
    }
    public class CreateManyMetaDataResult
    {
        public Guid? Id { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }

}
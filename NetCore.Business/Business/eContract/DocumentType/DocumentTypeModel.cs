using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class DocumentTypeBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; } = true;
        public Guid? OrganizationId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class DocumentTypeModel : DocumentTypeBaseModel
    {
        public int Order { get; set; } = 0;

    }

    public class DocumentTypeDetailModel : DocumentTypeModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class DocumentTypeCreateModel : DocumentTypeModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class DocumentTypeUpdateModel : DocumentTypeModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(DocumentType entity)
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

    public class DocumentTypeQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public DocumentTypeQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class DocumentTypeSelectItemModel : SelectItemModel
    {
        public Guid? OrganizationId { get; set; }
        public bool Status { get; set; }
    }

    public class DocumentTypeSelectItemFor3rdModel
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeName { get; set; }
    }

    public class MetaDataListForDocumentType
    {
        public string MetaDataCode { get; set; }
        public string MetaDataName { get; set; } = "";
        public string MetaDataValue { get; set; } = "";
    }
}
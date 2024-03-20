using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class DocumentTemplateBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public Guid? OrganizationId { get; set; }

        public List<Guid> ListDocumentTemplateFile { get; set; }
        public bool? CheckSignConfig { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string GroupCode { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsContainParent { get; set; } = false;
    }

    public class DocumentTemplateModel : DocumentTemplateBaseModel
    {
        public List<DocumentFileTemplateModel> DocumentFileTemplate { get; set; }
        public List<DocumentMetaDataConfigModel> DocumentMetaDataConfig { get; set; }
        public int Order { get; set; } = 0;
    }

    public class DocumentTemplateDetailModel : DocumentTemplateModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class DocumentTemplateCreateModel : DocumentTemplateModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class DocumentTemplateDuplicateModel
    {
        public Guid Id { get; set; }
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public class DocumentTemplateUpdateModel : DocumentTemplateModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(DocumentTemplate entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.DocumentTypeId = this.DocumentTypeId;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.FromDate = this.FromDate;
            entity.ToDate = this.ToDate;
            entity.GroupCode = this.GroupCode;
        }
    }

    public class DocumentTemplateQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public DocumentTemplateQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class DocumentFileTemplateModel
    {
        public Guid Id { get; set; }
        public string FileBucketName { get; set; }
        public string FileObjectName { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileDataUrl { get; set; }
        public string FileDataBucketName { get; set; }
        public string FileDataObjectName { get; set; }
        public TemplateFileType FileType { get; set; } = TemplateFileType.PDF;
        public string ProfileName { get; set; }
        public List<MetaDataConfig> MetaDataConfig { get; set; }
    }

    public class DocumentMetaDataConfigModel
    {
        public Guid MetaDataId { get; set; }
        public string MetaDataCode { get; set; }
        public string MetaDataName { get; set; }
        //public MetaDataConfig MetaDataConfig { get; set; }
        public int Order { get; set; }
        public MetaDataType DataType { get; set; }
        public bool IsRequire { get; set; } = false;
        public List<MetaDataList> ListData { get; set; }
    }

    public class DocumentTemplateSelectItemModel : SelectItemModel
    {
        public Guid? OrganizationId { get; set; }
    }

    public class DocumentByGroupCodeModel
    {
        public string GroupCode { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
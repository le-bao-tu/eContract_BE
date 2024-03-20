using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class DocumentBatchBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public class DocumentBatchModel : DocumentBatchBaseModel
    {
        public List<WorkFlowUserDataModel> WorkFlowUser { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; }
        public int Type { get; set; }
        public Guid? WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public int NumberOfEmailPerWeek { get; set; }
        public int Order { get; set; } = 0;
        public List<MetaDataDocumentFileModel> ListMetaData { get; set; }
        public List<DocumentBatchFileModel> ListFile { get; set; }
    }

    public class DocumentBatchDetailModel : DocumentBatchModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class DocumentBatchCreateModel : DocumentBatchModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class DocumentBatchUpdateModel : DocumentBatchModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(DocumentBatch entity)
        {
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class DocumentBatchQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public DocumentBatchQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class DocumentBatchFileModel
    {
        public Guid Id { get; set; }
        public string FileBucketName { get; set; }
        public string FileObjectName { get; set; }
        public string FileName { get; set; }
        public Guid? DocumentFileTemplateId { get; set; }
        //public int Order { get; set; } = 0;
        //public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

    public class DocumentBatchGenerateFileModel
    {
        public Guid Id { get; set; }
        public Guid? CreatedUserId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ApplicationId { get; set; }
        //public List<DocumentBatchFileItemModel> ListDocument { get; set; }
    }

    public class DocumentBatchFileItemModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public List<DocumentBatchFileModel> ListFile { get; set; }
    }
}
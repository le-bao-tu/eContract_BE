using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{

    public class WorkflowStateBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameForReject { get; set; }
        public Guid? OrganizationId { get; set; }
        public bool Status { get; set; } = true;
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }

    }

    public class WorkflowStateModel : WorkflowStateBaseModel
    {
        public int Order { get; set; } = 0;
    }

    public class WorkflowStateDetailModel : WorkflowStateModel
    {
        public Guid? CreatedUserId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Guid? ModifiedUserId { get; set; }
    }

    public class WorkflowStateCreateModel : WorkflowStateModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class WorkflowStateUpdateModel : WorkflowStateModel
    {
        public Guid ModifiedUserId { get; set; }
        public void UpdateToEntity(WorkflowState entity)
        {
            entity.Name = this.Name;
            entity.NameForReject = this.NameForReject;
            entity.Status = this.Status;
            entity.Order = this.Order;
            //entity.OrganizationId = this.OrganizationId;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class WorkflowStateQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public Guid? UserId { get; set; }
        public WorkflowStateQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class WorkflowStateSelectItemModel : SelectItemModel
    {
        public string NameForReject { get; set; }
        public string DisplayName { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}

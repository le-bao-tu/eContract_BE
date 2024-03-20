using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class SystemApplicationBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Order { get; set; } = 0;
        public string Description { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
    }

    public class SystemApplicationModel : SystemApplicationBaseModel
    {
    }

    public class SystemApplicationDetailModel : SystemApplicationModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class SystemApplicationCreateModel : SystemApplicationModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class SystemApplicationUpdateModel : SystemApplicationModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(SystemApplication entity)
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

    public class SystemApplicationQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public SystemApplicationQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }
}
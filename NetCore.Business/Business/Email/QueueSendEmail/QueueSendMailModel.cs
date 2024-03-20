using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class QueueSendEmailBaseModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Base64Image { get; set; }
        //public bool IsSended { get; set; }
        //public Guid EmailAccountId { get; set; }
        public string EmailCode { get; set; }
        public bool Status { get; set; } = true;
        public List<string> BccEmails { get; set; }
        public List<string> CCEmails { get; set; }
        public List<string> ToEmails { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class QueueSendMailModel : QueueSendEmailBaseModel
    {
        public int Order { get; set; } = 0;

    }

    public class QueueSendEmailDetailModel : QueueSendMailModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class QueueSendEmailCreateModel : QueueSendMailModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class QueueSendEmailUpdateModel : QueueSendMailModel
    {
        public Guid ModifiedUserId { get; set; }

        //public void UpdateToEntity(QueueSendEmail entity)
        //{
        //    //entity.Code = this.Code;
        //    entity.Title = Title;
        //    entity.Body = Body;
        //    entity.Base64Image = Base64Image;
        //    entity.IsSended = IsSended;
        //    entity.EmailAccountId = EmailAccountId;
        //    entity.Status = this.Status;
        //    entity.Order = this.Order;
        //    entity.Description = this.Description;
        //    entity.ModifiedDate = DateTime.Now;
        //    entity.ModifiedUserId = this.ModifiedUserId;
        //}
    }

    public class QueueSendEmailQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public QueueSendEmailQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class QueueSendEmailSelectItemModel : SelectItemModel { }
}
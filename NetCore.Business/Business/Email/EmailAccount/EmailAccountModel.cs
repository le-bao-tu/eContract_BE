using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class EmailAccountBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string From { get; set; }
        public string Smtp { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string SendType { get; set; }
        public string Password { get; set; }
        public bool Ssl { get; set; }
        public bool Status { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class EmailAccountModel : EmailAccountBaseModel
    {
        public int Order { get; set; } = 0;

    }

    public class EmailAccountDetailModel : EmailAccountModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class EmailAccountCreateModel : EmailAccountModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class EmailAccountUpdateModel : EmailAccountModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(EmailAccount entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.Status = this.Status;
            entity.From = From;
            entity.Smtp = Smtp;
            entity.Port = Port;
            entity.User = User;
            entity.SendType = SendType;
            entity.Password = Password;
            entity.Ssl = Ssl;
            entity.Order = this.Order;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }
    public class EmailAccountQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public EmailAccountQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class EmailAccountSelectItemModel : SelectItemModel {
    }
}
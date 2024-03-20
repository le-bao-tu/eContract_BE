using NetCore.DataLog;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class SystemLogModel
    {
        public string TraceId { get; set; }
        public string ActionCode { get; set; }
        public string ActionName { get; set; }
        public string IP { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ParentId { get; set; }
        public string Device { get; set; }
        public DataLog.OperatingSystem OperatingSystem { get; set; }
        public Location Location { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string ApplicationId { get; set; }

        //Lưu dữ liệu tạm để khi bổ sung cho trường  hợp: ký điện tử => gọi ký tự động bên HSM => nó phải khác action code
        public string TempActionCode { get; set; }
        public string TempActionName { get; set; }

        // Lưu dữ liệu tạm để khi gọi sang hàm khác để biết mình đang tương tác với object nào => lưu log
        public string TempObjectCode { get; set; }
        public string TempObjectId { get; set; }
        public string TempDescription { get; set; }

        public List<ActionDetail> ListAction { get; set; }
    }

    public class ActionDetail
    {
        public ActionDetail()
        {
        }
        public ActionDetail(string description)
        {
            this.Description = description;
        }
        public ActionDetail(string objectCode, string objectId, string description)
        {
            this.ObjectCode = objectCode;
            this.ObjectId = objectId;
            this.Description = description;
        }
        public ActionDetail(string objectCode, string objectId, string description, string metaData)
        {
            this.ObjectCode = objectCode;
            this.ObjectId = objectId;
            this.Description = description;
            this.MetaData = metaData;
        }
        public string ActionCode { get; set; }
        public string ActionName { get; set; }
        public string SubActionCode { get; set; }
        public string SubActionName { get; set; }
        public string ObjectCode { get; set; }
        public string ObjectId { get; set; }
        public string Description { get; set; }
        public string MetaData { get; set; }
        public string UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class SystemLogQueryFilter
    {
        public string TextSearch { get; set; }
        public string TradeId { get; set; }
        public string Device { get; set; }
        public string ActionCode { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public string ApplicationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SystemLogQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class ActionCodeForComboboxModel
    {
        public string ActionCode { get; set; }
        public string ActionName { get; set; }
    }
}

using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class DocumentStatusModel
    {
        public int Draft { get; set; }
        public int WaitMeSign { get; set; }
        public int Processing { get; set; }
        public int Completed { get; set; }
    }

    public class OrgReportFilterModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Guid OrganizationID { get; set; }
    }

    public class DashboardModel
    {
        // tổng số hợp đồng
        public int TotalDocument { get; set; }

        // hợp đồng chờ ký
        public int WaitMeSign { get; set; }

        // hợp đồng sắp hết hạn
        public int IncommingExpired { get; set; }

        // hợp đồng đã hủy
        public int Draft { get; set; }

        // hợp đồng đã ký
        public int Completed { get; set; }

        // hơp đồng lỗi
        public int Error { get; set; }

        // bảng thống kê số lượng hợp đồng đã ký
        public List<DocumentDashboardTableSignCompleted> ListSignCompleted { get; set; }

        // bảng thống kê số lượng hợp đồng sắp hết hạn
        public List<DocumentDashboardTableSignIncommingExpired> ListSignIncommingExpired { get; set; }

        // số lượt ký LTV
        public long SignLTV { get; set; }

        // số lượt ký TSA
        public long SignTSA { get; set; }

        // số lượt ký thường
        public long SignNormal { get; set; }

        // số lượt ký điện tử an toàn
        public long SignDTAT { get; set; }

        public long SignTSA_ESEAL { get; set; }

        public long SignDIG_NORMAL { get; set; }

        public int Expired { get; set; }

        public string OrganizationName { get; set; }
    }

    public class DocumentDashboardTableSignCompleted
    {
        // thời gian
        public string DateTimeLabel { get; set; }

        // tỷ lệ cùng kỳ
        public float CompletedRate { get; set; }

        // số hơp đồng
        public int DocumentCount { get; set; }
    }

    public class DocumentDashboardTableSignIncommingExpired
    {
        // thời gian
        public string DateTimeLabel { get; set; }

        // số hợp đồng
        public int DocumentCount { get; set; }

        // tỷ lệ cùng kỳ
        public float IncommingExpiredRate { get; set; }
    }

    public class DashboardRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
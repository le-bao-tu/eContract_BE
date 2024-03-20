using NetCore.Shared;
using System;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface trang chủ
    /// </summary>
    public interface IDashboardHandler
    {
        /// <summary>
        /// Lấy danh sách số lượng hợp đồng theo trạng thái
        /// </summary>
        /// <param name="userId">Id người dùng</param>
        /// <returns>Danh sách số lượng hợp đồng theo trạng thái</returns>
        Task<Response> GetNumberDocumentStatus(Guid userId, Guid organizationId);

        Task<Response> GetDashboardInfo(DashboardRequest requestModel, Guid userId, Guid organizationId, SystemLogModel systemLog);
    }
}

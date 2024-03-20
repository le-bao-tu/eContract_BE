using NetCore.DataLog;
using NetCore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface nhật ký hệ thống
    /// </summary>
    public interface ISystemLogHandler
    {
        /// <summary>
        /// Thêm mới nhật ký hệ thống
        /// </summary>
        /// <param name="model">Model thêm mới nhật ký hệ thống</param>
        /// <returns>Id nhật ký hệ thống</returns>
        Task<Response> Create(SystemLog model);

        /// <summary>
        /// Thêm mới nhật ký hệ thống theo danh sách
        /// </summary>
        /// <param name="model">Model thêm mới nhật ký hệ thống</param>
        /// <returns></returns>
        Task<Response> Create(SystemLogModel model);

        /// <summary>
        /// Cập nhật nhật ký hệ thống
        /// </summary>
        /// <param name="model">Model cập nhật nhật ký hệ thống</param>
        /// <returns>Id nhật ký hệ thống</returns>
        Task<Response> Update(SystemLog model);

        /// <summary>
        /// Xóa nhật ký hệ thống
        /// </summary>
        /// <param name="listId">Danh sách Id nhật ký hệ thống</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<string> listId);

        /// <summary>
        /// Lấy danh sách nhật ký hệ thống theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách nhật ký hệ thống</returns>
        Task<Response> Filter(SystemLogQueryFilter filter);

        /// <summary>
        /// Lấy danh sách nhật ký hợp đồng
        /// </summary>
        /// <param name="documentId">Id hợp đồng</param>
        /// <returns>Danh sách nhật ký hệ thống</returns>
        Task<Response> FilterByDocument(string documentId);

        /// <summary>
        /// Lấy nhật ký hệ thống theo Id
        /// </summary>
        /// <param name="id">Id nhật ký hệ thống</param>
        /// <returns>Thông tin nhật ký hệ thống</returns>
        Task<Response> GetById(string id);

        /// <summary>
        /// Lấy dữ liêu thống kê liên quan đến hợp đồng theo đơn vị
        /// </summary>
        /// <param name="orgID"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        Task<Response> GetReportDocumentByOrgID(OrgReportFilterModel filter);

        /// <summary>
        /// Lấy All dữ liệu Action Code
        /// </summary>
        /// <returns>Danh sách Action Code và Action Name</returns>
        Task<Response> GetActionCodeForCombobox();
    }
}

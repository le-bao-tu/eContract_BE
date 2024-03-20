using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý cấu hình mẫu chữ ký cho từng người dùng
    /// </summary>
    public interface IUserHSMAccountHandler
    {
        /// <summary>
        /// Thêm mới cấu hình mẫu chữ ký cho từng người dùng
        /// </summary>
        /// <param name="model">Model thêm mới cấu hình mẫu chữ ký cho từng người dùng</param>
        /// <returns>Id cấu hình mẫu chữ ký cho từng người dùng</returns>
        Task<Response> Create(UserHSMAccountCreateOrUpdateModel model, SystemLogModel systemLog);

        Task<Response> CreateFromService(UserHSMAccountCreateOrUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật cấu hình mẫu chữ ký cho từng người dùng
        /// </summary>
        /// <param name="model">Model cập nhật cấu hình mẫu chữ ký cho từng người dùng</param>
        /// <returns>Id cấu hình mẫu chữ ký cho từng người dùng</returns>
        Task<Response> Update(UserHSMAccountCreateOrUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        ///
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> UpdateStatus(Guid userHSMAccountId, SystemLogModel systemLog);

        /// <summary>
        /// Xóa cấu hình mẫu chữ ký cho từng người dùng
        /// </summary>
        /// <param name="listId">Danh sách Id cấu hình mẫu chữ ký cho từng người dùng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách cấu hình mẫu chữ ký cho từng người dùng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách cấu hình mẫu chữ ký cho từng người dùng</returns>
        Task<Response> Filter(UserHSMAccountQueryFilter filter);

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Response> GetListHSM(Guid userId, UserHSMAccountQueryFilter filter);

        /// <summary>
        /// Lấy danh sách mẫu chữ ký cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách mẫu chữ ký cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, Guid? userId = null);

        /// <summary>
        /// Lấy danh sách mẫu chữ ký cho combobox với HSM valid và ADSS
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách mẫu chữ ký cho combobox</returns>
        Task<Response> GetListComboboxHSMValid(int count = 0, Guid? userId = null);

        Task<List<UserHSMAccountSelectItemModel>> GetListData();

        /// <summary>
        /// Đọc thông tin từ CTS
        /// </summary>
        /// <param name="certificateBase64"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetInfoCertificate(Guid userHSMAccountId, SystemLogModel systemLog);

        /// <summary>
        /// Tải xuống CTS
        /// </summary>
        /// <param name="userHSMAccountId"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<MemoryStream> DownloadCertificate(Guid userHSMAccountId, SystemLogModel systemLog);
    }
}
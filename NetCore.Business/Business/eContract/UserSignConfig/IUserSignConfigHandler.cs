using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý cấu hình mẫu chữ ký cho từng người dùng
    /// </summary>
    public interface IUserSignConfigHandler
    {
        /// <summary>
        /// Thêm mới cấu hình mẫu chữ ký cho từng người dùng
        /// </summary>
        /// <param name="model">Model thêm mới cấu hình mẫu chữ ký cho từng người dùng</param>
        /// <returns>Id cấu hình mẫu chữ ký cho từng người dùng</returns>
        Task<Response> Create(UserSignConfigCreateOrUpdateModel model, SystemLogModel systemLog);


        /// <summary>
        /// Cập nhật cấu hình mẫu chữ ký cho từng người dùng
        /// </summary>
        /// <param name="model">Model cập nhật cấu hình mẫu chữ ký cho từng người dùng</param>
        /// <returns>Id cấu hình mẫu chữ ký cho từng người dùng</returns>
        Task<Response> Update(UserSignConfigCreateOrUpdateModel model, SystemLogModel systemLog);

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
        Task<Response> Filter(UserSignConfigQueryFilter filter);
        /// <summary>
        /// Lấy danh sách mẫu chữ ký cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách mẫu chữ ký cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, Guid? userId = null);

        #region Kết nối từ bên thứ 3
        //Thêm mới cấu hình ký từ bên thứ 3
        Task<Response> CreateFrom3rd(UserSign3rdModel model, SystemLogModel systemLog);

        Task<Response> UpdateFrom3rd(UserSignUpdate3rdModel model, SystemLogModel systemLog);

        //Lấy danh sách cấu hình ký theo người dùng => chỉ lấy image
        Task<Response> GetSignConfigUser3rd(string userConnectId, SystemLogModel systemLog);

        //Xóa cấu hình ký của người dùng từ bên thứ 3
        Task<Response> DeleteFrom3rd(Guid id, SystemLogModel systemLog);
        #endregion

        Task<UserSignConfigModel> GetUserSignConfigForSign(Guid userId);

        Task<UserSignConfigModel> GetById(Guid userId);
    }
}

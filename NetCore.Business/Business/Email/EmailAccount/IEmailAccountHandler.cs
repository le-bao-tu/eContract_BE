using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý tài khoản email
    /// </summary>
    public interface IEmailAccountHandler
    {
        /// <summary>
        /// Thêm mới tài khoản email
        /// </summary>
        /// <param name="model">Model thêm mới tài khoản email</param>
        /// <returns>Id tài khoản email</returns>
        Task<Response> Create(EmailAccountCreateModel model);

        /// <summary>
        /// Thêm mới tài khoản email theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin tài khoản email</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<EmailAccountCreateModel> list);

        /// <summary>
        /// Cập nhật tài khoản email
        /// </summary>
        /// <param name="model">Model cập nhật tài khoản email</param>
        /// <returns>Id tài khoản email</returns>
        Task<Response> Update(EmailAccountUpdateModel model);

        /// <summary>
        /// Xóa tài khoản email
        /// </summary>
        /// <param name="listId">Danh sách Id tài khoản email</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId);

        /// <summary>
        /// Lấy danh sách tài khoản email theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách tài khoản email</returns>
        Task<Response> Filter(EmailAccountQueryFilter filter);


        /// <summary>
        /// Lấy tài khoản email theo Id
        /// </summary>
        /// <param name="id">Id tài khoản email</param>
        /// <returns>Thông tin tài khoản email</returns>
        Task<Response> GetById(Guid id);

        /// <summary>
        /// Lấy danh sách tài khoản email cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách tài khoản email cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, string textSearch = "");
    }
}

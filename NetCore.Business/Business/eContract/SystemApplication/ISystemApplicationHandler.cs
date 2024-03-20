using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý ứng dụng hệ thống
    /// </summary>
    public interface ISystemApplicationHandler
    {
        /// <summary>
        /// Thêm mới ứng dụng
        /// </summary>
        /// <param name="model">Model thêm mới ứng dụng</param>
        /// <returns>Id ứng dụng</returns>
        Task<Response> Create(SystemApplicationCreateModel model);

        /// <summary>
        /// Thêm mới ứng dụng theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin ứng dụng</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<SystemApplicationCreateModel> list);

        /// <summary>
        /// Cập nhật ứng dụng
        /// </summary>
        /// <param name="model">Model cập nhật ứng dụng</param>
        /// <returns>Id ứng dụng</returns>
        Task<Response> Update(SystemApplicationUpdateModel model);

        /// <summary>
        /// Xóa ứng dụng
        /// </summary>
        /// <param name="listId">Danh sách Id ứng dụng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId);

        /// <summary>
        /// Lấy danh sách ứng dụng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách ứng dụng</returns>
        Task<Response> Filter(SystemApplicationQueryFilter filter);

        /// <summary>
        /// Lấy ứng dụng theo Id
        /// </summary>
        /// <param name="id">Id ứng dụng</param>
        /// <returns>Thông tin ứng dụng</returns>
        Task<Response> GetById(Guid id);

        /// <summary>
        /// Lấy danh sách ứng dụng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách ứng dụng cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, string textSearch = "");
    }
}

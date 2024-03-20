using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý chức vụ
    /// </summary>
    public interface IPositionHandler
    {
        /// <summary>
        /// Thêm mới chức vụ
        /// </summary>
        /// <param name="model">Model thêm mới chức vụ</param>
        /// <returns>Id chức vụ</returns>
        Task<Response> Create(PositionCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới chức vụ theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin chức vụ</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<PositionCreateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật chức vụ
        /// </summary>
        /// <param name="model">Model cập nhật chức vụ</param>
        /// <returns>Id chức vụ</returns>
        Task<Response> Update(PositionUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa chức vụ
        /// </summary>
        /// <param name="listId">Danh sách Id chức vụ</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách chức vụ theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách chức vụ</returns>
        Task<Response> Filter(PositionQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy chức vụ theo Id
        /// </summary>
        /// <param name="id">Id chức vụ</param>
        /// <returns>Thông tin chức vụ</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách chức vụ cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách chức vụ cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null);
    }
}

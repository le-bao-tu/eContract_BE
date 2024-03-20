using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý phường xã
    /// </summary>
    public interface IWardHandler
    {
        /// <summary>
        /// Thêm mới phường xã
        /// </summary>
        /// <param name="model">Model thêm mới phường xã</param>
        /// <returns>Id phường xã</returns>
        Task<Response> Create(WardCreateModel model, SystemLogModel systemModel);

        /// <summary>
        /// Thêm mới phường xã theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin phường xã</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<WardCreateModel> list, SystemLogModel systemModel);

        /// <summary>
        /// Cập nhật phường xã
        /// </summary>
        /// <param name="model">Model cập nhật phường xã</param>
        /// <returns>Id phường xã</returns>
        Task<Response> Update(WardUpdateModel model, SystemLogModel systemModel);

        /// <summary>
        /// Xóa phường xã
        /// </summary>
        /// <param name="listId">Danh sách Id phường xã</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemModel);

        /// <summary>
        /// Lấy danh sách phường xã theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách phường xã</returns>
        Task<Response> Filter(WardQueryFilter filter, SystemLogModel systemModel);

        /// <summary>
        /// Lấy phường xã theo Id
        /// </summary>
        /// <param name="id">Id phường xã</param>
        /// <returns>Thông tin phường xã</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemModel);

        /// <summary>
        /// Lấy danh sách phường xã cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách phường xã cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemModel, int count = 0, string textSearch = "");
    }
}

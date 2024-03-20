using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý quận huyện
    /// </summary>
    public interface IDistrictHandler
    {
        /// <summary>
        /// Thêm mới quận huyện
        /// </summary>
        /// <param name="model">Model thêm mới quận huyện</param>
        /// <returns>Id quận huyện</returns>
        Task<Response> Create(DistrictCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới quận huyện theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin quận huyện</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<DistrictCreateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật quận huyện
        /// </summary>
        /// <param name="model">Model cập nhật quận huyện</param>
        /// <returns>Id quận huyện</returns>
        Task<Response> Update(DistrictUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa quận huyện
        /// </summary>
        /// <param name="listId">Danh sách Id quận huyện</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quận huyện theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách quận huyện</returns>
        Task<Response> Filter(DistrictQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy quận huyện theo Id
        /// </summary>
        /// <param name="id">Id quận huyện</param>
        /// <returns>Thông tin quận huyện</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quận huyện cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách quận huyện cho combobox</returns>
        Task<Response> GetListCombobox(Guid? provinceId, SystemLogModel systemLog, int count = 0, string textSearch = "");
    }
}

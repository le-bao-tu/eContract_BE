using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý tỉnh thành
    /// </summary>
    public interface IProvinceHandler
    {
        /// <summary>
        /// Thêm mới tỉnh thành
        /// </summary>
        /// <param name="model">Model thêm mới tỉnh thành</param>
        /// <returns>Id tỉnh thành</returns>
        Task<Response> Create(ProvinceCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới tỉnh thành theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin tỉnh thành</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<ProvinceCreateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật tỉnh thành
        /// </summary>
        /// <param name="model">Model cập nhật tỉnh thành</param>
        /// <returns>Id tỉnh thành</returns>
        Task<Response> Update(ProvinceUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa tỉnh thành
        /// </summary>
        /// <param name="listId">Danh sách Id tỉnh thành</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách tỉnh thành theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách tỉnh thành</returns>
        Task<Response> Filter(ProvinceQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy tỉnh thành theo Id
        /// </summary>
        /// <param name="id">Id tỉnh thành</param>
        /// <returns>Thông tin tỉnh thành</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách tỉnh thành cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách tỉnh thành cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, Guid? districtId, int count = 0, string textSearch = "");
    }
}

using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý quốc gia
    /// </summary>
    public interface ICountryHandler
    {
        /// <summary>
        /// Thêm mới quốc gia
        /// </summary>
        /// <param name="model">Model thêm mới quốc gia</param>
        /// <returns>Id quốc gia</returns>
        Task<Response> Create(CountryCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới quốc gia theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin quốc gia</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<CountryCreateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật quốc gia
        /// </summary>
        /// <param name="model">Model cập nhật quốc gia</param>
        /// <returns>Id quốc gia</returns>
        Task<Response> Update(CountryUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa quốc gia
        /// </summary>
        /// <param name="listId">Danh sách Id quốc gia</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quốc gia theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách quốc gia</returns>
        Task<Response> Filter(CountryQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy quốc gia theo Id
        /// </summary>
        /// <param name="id">Id quốc gia</param>
        /// <returns>Thông tin quốc gia</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quốc gia cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách quốc gia cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "");
    }
}

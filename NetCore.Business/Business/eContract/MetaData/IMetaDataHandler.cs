using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý thông tin trong hợp đồng
    /// </summary>
    public interface IMetaDataHandler
    {
        /// <summary>
        /// Thêm mới thông tin trong hợp đồng
        /// </summary>
        /// <param name="model">Model thêm mới thông tin trong hợp đồng</param>
        /// <returns>Id thông tin trong hợp đồng</returns>
        Task<Response> Create(MetaDataCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới thông tin trong hợp đồng theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin thông tin trong hợp đồng</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<MetaDataCreateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật thông tin trong hợp đồng
        /// </summary>
        /// <param name="model">Model cập nhật thông tin trong hợp đồng</param>
        /// <returns>Id thông tin trong hợp đồng</returns>
        Task<Response> Update(MetaDataUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa thông tin trong hợp đồng
        /// </summary>
        /// <param name="listId">Danh sách Id thông tin trong hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách thông tin trong hợp đồng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách thông tin trong hợp đồng</returns>
        Task<Response> Filter(MetaDataQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy thông tin trong hợp đồng theo Id
        /// </summary>
        /// <param name="id">Id thông tin trong hợp đồng</param>
        /// <returns>Thông tin thông tin trong hợp đồng</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách thông tin trong hợp đồng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách thông tin trong hợp đồng cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null);

        /// <summary>
        /// Lấy danh sách meta data từ cache
        /// </summary>
        /// <returns></returns>
        Task<List<MetaDataSelectItemModel>> GetListFromCache();
    }
}

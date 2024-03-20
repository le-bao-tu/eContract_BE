using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý loại hợp đồng
    /// </summary>
    public interface IDocumentTypeHandler
    {
        /// <summary>
        /// Thêm mới loại hợp đồng
        /// </summary>
        /// <param name="model">Model thêm mới loại hợp đồng</param>
        /// <returns>Id loại hợp đồng</returns>
        Task<Response> Create(DocumentTypeCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới loại hợp đồng theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin loại hợp đồng</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<DocumentTypeCreateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật loại hợp đồng
        /// </summary>
        /// <param name="model">Model cập nhật loại hợp đồng</param>
        /// <returns>Id loại hợp đồng</returns>
        Task<Response> Update(DocumentTypeUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa loại hợp đồng
        /// </summary>
        /// <param name="listId">Danh sách Id loại hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách loại hợp đồng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách loại hợp đồng</returns>
        Task<Response> Filter(DocumentTypeQueryFilter filter);

        /// <summary>
        /// Lấy loại hợp đồng theo Id
        /// </summary>
        /// <param name="id">Id loại hợp đồng</param>
        /// <returns>Thông tin loại hợp đồng</returns>
        Task<Response> GetById(Guid id);

        /// <summary>
        /// Lấy loại hợp đồng theo Id
        /// </summary>
        /// <param name="id">Id loại hợp đồng</param>
        /// <returns>Thông tin loại hợp đồng</returns>
        Task<DocumentTypeModel> GetDetailById(Guid id);

        /// <summary>
        /// Lấy danh sách loại hợp đồng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách loại hợp đồng cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, string textSearch = "", Guid? orgId = null);
        Task<Response> GetAllListCombobox(int count = 0, string textSearch = "", Guid? orgId = null);

        /// <summary>
        /// Lấy danh sách loại hợp đồng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách loại hợp đồng cho combobox</returns>
        Task<Response> GetListComboboxFor3rd(int count = 0, string textSearch = "", Guid? orgId = null);

        /// <summary>
        /// Lấy danh sách Meta Data theo loại hợp đồng cho đơn vị thứ 3
        /// </summary>
        /// <param name="documenTypeCode">Mã loại hợp đồng</param>
        /// <param name="orgId">Id đơn vị</param>
        /// <returns>Danh sách meta data</returns>
        Task<Response> GetListMetaDataByDocumentType(string documenTypeCode, Guid? orgId = null);

        Task<Response> GetListComboboxAllStatus(int count = 0, string textSearch = "", Guid? orgId = null);
    }
}

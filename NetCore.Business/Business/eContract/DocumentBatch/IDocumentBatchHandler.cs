using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý lô hợp đồng
    /// </summary>
    public interface IDocumentBatchHandler
    {
        /// <summary>
        /// Thêm mới lô hợp đồng
        /// </summary>
        /// <param name="model">Model thêm mới lô hợp đồng</param>
        /// <returns>Id lô hợp đồng</returns>
        Task<Response> Create(DocumentBatchCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật lô hợp đồng
        /// </summary>
        /// <param name="model">Model cập nhật lô hợp đồng</param>
        /// <returns>Id lô hợp đồng</returns>
        Task<Response> Update(DocumentBatchUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa lô hợp đồng
        /// </summary>
        /// <param name="listId">Danh sách Id lô hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách lô hợp đồng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách lô hợp đồng</returns>
        Task<Response> Filter(DocumentBatchQueryFilter filter);

        /// <summary>
        /// Lấy lô hợp đồng theo Id
        /// </summary>
        /// <param name="id">Id lô hợp đồng</param>
        /// <returns>Thông tin lô hợp đồng</returns>
        Task<Response> GetById(Guid id);

        /// <summary>
        /// Lấy danh sách lô hợp đồng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách lô hợp đồng cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, string textSearch = "");

        /// <summary>
        /// Sinh danh sách hợp đồng
        /// </summary>
        /// <param name="model">Model tạo hợp đồng</param>
        /// <returns>Trạng thái thành công/thất bại</returns>
        Task<Response> GenerateListDocument_v2(DocumentBatchGenerateFileModel model, SystemLogModel systemLog);
    }
}

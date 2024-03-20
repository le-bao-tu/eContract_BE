using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý biểu mẫu hợp đồng
    /// </summary>
    public interface IDocumentTemplateHandler
    {
        /// <summary>
        /// Thêm mới biểu mẫu hợp đồng
        /// </summary>
        /// <param name="model">Model thêm mới biểu mẫu hợp đồng</param>
        /// <returns>Id biểu mẫu hợp đồng</returns>
        Task<Response> Create(DocumentTemplateCreateModel model, SystemLogModel systemLog);
        Task<Response> Duplicate(DocumentTemplateDuplicateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật biểu mẫu hợp đồng
        /// </summary>
        /// <param name="model">Model cập nhật biểu mẫu hợp đồng</param>
        /// <returns>Id biểu mẫu hợp đồng</returns>
        Task<Response> Update(DocumentTemplateUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật cấu hình biểu mẫu hợp đồng
        /// </summary>
        /// <param name="list">Danh sách file cấu hình biểu mẫu</param>
        /// <returns>Trạng thái thành công/thất bại</returns>
        Task<Response> UpdateMetaData(List<DocumentFileTemplateModel> list, SystemLogModel systemLog);

        /// <summary>
        /// Xóa biểu mẫu hợp đồng
        /// </summary>
        /// <param name="listId">Danh sách Id biểu mẫu hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách biểu mẫu hợp đồng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách biểu mẫu hợp đồng</returns>
        Task<Response> Filter(DocumentTemplateQueryFilter filter);

        /// <summary>
        /// Lấy biểu mẫu hợp đồng theo Id
        /// </summary>
        /// <param name="id">Id biểu mẫu hợp đồng</param>
        /// <returns>Thông tin biểu mẫu hợp đồng</returns>
        Task<Response> GetById(Guid id);

        /// <summary>
        /// Lấy danh sách biểu mẫu hợp đồng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách biểu mẫu hợp đồng cho combobox</returns>
        Task<Response> GetListCombobox(int count = 0, string textSearch = "", Guid? orgId = null);

        /// <summary>
        /// Lấy danh sách biểu mẫu hợp đồng theo loại hợp đồng
        /// </summary>
        /// <param name="id">Id loại hợp đồng</param>
        /// <returns>Danh sách biểu mẫu hợp đồng</returns>
        Task<Response> GetListTemplateByTypeId(Guid id);

        /// <summary>
        /// Lấy danh sách biểu mẫu theo group code
        /// </summary>
        /// <param name="groupCode"></param>
        /// <returns>Danh sách biểu mẫu</returns>
        Task<Response> GetListDocumentTemplateByGroupCode(DocumentByGroupCodeModel model);

        Task<List<DocumentTemplateModel>> CaculateActiveDocumentTemplateByGroupCode(string groupCode);
    }
}

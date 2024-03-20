using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý quy trình
    /// </summary>
    public interface IWorkflowHandler
    {
        /// <summary>
        /// Thêm mới quy trình
        /// </summary>
        /// <param name="model">Model thêm mới quy trình</param>
        /// <returns>Id quy trình</returns>
        Task<Response> Create(WorkflowCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật quy trình
        /// </summary>
        /// <param name="model">Model cập nhật quy trình</param>
        /// <returns>Id quy trình</returns>
        Task<Response> Update(WorkflowUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa quy trình
        /// </summary>
        /// <param name="listId">Danh sách Id quy trình</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quy trình theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách quy trình</returns>
        Task<Response> Filter(WorkflowQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy quy trình theo Id
        /// </summary>
        /// <param name="id">Id quy trình</param>
        /// <returns>Thông tin quy trình</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy quy trình theo mã
        /// </summary>
        /// <param name="code">Mã quy trình</param>
        /// <returns>Thông tin quy trình</returns>
        Task<Response> GetByCode(string code, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quy trình cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách quy trình cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? userId = null, Guid? organizationId = null);

        /// <summary>
        /// Lấy thông tin chi tiết quy trình
        /// </summary>
        /// <param name="wfId">Id quy trình</param>
        /// <param name="stepId">Step Id (stepId == null => tài liệu đã hoàn thành quy trình)</param>
        /// <returns></returns>
        Task<WorkflowUserStepDetailModel> GetDetailStepById(SystemLogModel systemLog, Guid wfId, Guid? stepId);

        Task<List<WorkflowUserStepDetailModel>> GetDetailWFById(Guid wfId, SystemLogModel systemLog);
        Task<WorkflowModel> GetWFInfoById(Guid id, SystemLogModel systemLog);
    }
}

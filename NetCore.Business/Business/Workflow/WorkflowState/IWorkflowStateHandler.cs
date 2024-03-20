using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface danh mục trạng thái hợp đồng
    /// </summary>
    public interface IWorkflowStateHandler
    {
        #region:CRUD
        /// <summary>
        /// Tạo mới trạng thái hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<Response> Create(WorkflowStateCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhập trạng thái hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<Response> Update(WorkflowStateUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa trạng thái hợp đồng
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<Response> Delete(List<Guid> ids, SystemLogModel systemLog);
        #endregion

        #region:GET
        /// <summary>
        /// Tìm kiếm theo điều kiện lọc
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Response> Filter(WorkflowStateQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy thông tin theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách trạng thái hợp đồng
        /// </summary>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, Guid? orgID);
        #endregion
    }
}

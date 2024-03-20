using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.API.Controller.Workflow
{
    /// <summary>
    ///  Module danh sách trạng thái hợp đồng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/workflow-state")]
    [ApiExplorerSettings(GroupName = "Workflow - 02. Workflow-state (Trạng thái hợp đồng)")]
    public class WorkflowStateController : ApiControllerBase
    {
        private IWorkflowStateHandler _workflowStateHandler { get; set; }
        public WorkflowStateController(IWorkflowStateHandler workflowStateHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _workflowStateHandler = workflowStateHandler;
        }

        /// <summary>
        /// Tạo mới trạng thái hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] WorkflowStateCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WORKFLOW_STATE_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_WORKFLOW_STATE_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _workflowStateHandler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhập trạng thái hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] WorkflowStateUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WORKFLOW_STATE_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_WORKFLOW_STATE_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _workflowStateHandler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Xóa trạng thái hợp đồng
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> ids)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WORKFLOW_STATE_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_WORKFLOW_STATE_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _workflowStateHandler.Delete(ids, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ResponseObject<WorkflowModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _workflowStateHandler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Tìm kiếm theo điều kiện lọc
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [Authorize]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] WorkflowStateQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                var result = await _workflowStateHandler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        [HttpGet, Route("for-combobox")]
        //[Authorize]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _workflowStateHandler.GetListCombobox(u.SystemLog, u.OrganizationId);

                return result;
            });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module quy trình
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/workflow")]
    [ApiExplorerSettings(GroupName = "Workflow - 01. Workflow (Quy trình)")]
    public class WorkflowController : ApiControllerBase
    {
        private readonly IWorkflowHandler _handler;
        public WorkflowController(IWorkflowHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới quy trình
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }
        /// </remarks>
        /// <param name="model">Thông tin quy trình</param>
        /// <returns>Id quy trình</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] WorkflowCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WORKFLOW_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_WORKFLOW_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                if (!model.OrganizationId.HasValue)
                {
                    model.OrganizationId = u.OrganizationId;
                }
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới quy trình theo danh sách
        ///// </summary>
        ///// <remarks>
        ///// Sample request:
        /////
        /////     [
        /////         {
        /////             "code": "Code",
        /////             "name": "Name",
        /////             "status": true,
        /////             "description": "Description",
        /////             "order": 1
        /////         }   
        /////     ]
        ///// </remarks>
        ///// <param name="list">Danh sách thông tin quy trình</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<WorkflowCreateModel> list)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        foreach (var item in list)
        //        {
        //            item.CreatedUserId = u.UserId;
        //            item.ApplicationId = u.ApplicationId;
        //        }
        //        var result = await _handler.CreateMany(list);
        //        return result;
        //    });
        //}
        /// <summary>
        /// Cập nhật quy trình
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }   
        /// </remarks>
        /// <param name="model">Thông tin quy trình cần cập nhật</param>
        /// <returns>Id quy trình đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] WorkflowUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WORKFLOW_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_WORKFLOW_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin quy trình theo id
        /// </summary> 
        /// <param name="id">Id quy trình</param>
        /// <returns>Thông tin chi tiết quy trình</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<WorkflowModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy thông tin quy trình theo mã
        ///// </summary> 
        ///// <param name="code">Mã quy trình</param>
        ///// <returns>Thông tin chi tiết quy trình</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpGet, Route("detail-by-code")]
        //[ProducesResponseType(typeof(ResponseObject<WorkflowModel>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetByCode(string code)
        //{
        //    //return await ExecuteFunction(async (RequestUser u) =>
        //    //{
        //    var result = await _handler.GetByCode(code);

        //    return Helper.TransformData(result);
        //    //});
        //}

        /// <summary>
        /// Lấy danh sách quy trình theo điều kiện lọc
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "textSearch": "",
        ///         "pageSize": 20,
        ///         "pageNumber": 1
        ///     }
        /// </remarks>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách quy trình</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] WorkflowQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                //filter.OrganizationId = filter.OrganizationId.HasValue ? filter.OrganizationId.Value : u.OrganizationId;
                filter.CurrentUserId = u.UserId;
                filter.CurrentOrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Lấy tất cả danh sách quy trình
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách quy trình</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                WorkflowQueryFilter filter = new WorkflowQueryFilter()
                {
                    TextSearch = ts,
                    PageNumber = null,
                    PageSize = null
                };
                filter.CurrentUserId = u.UserId;
                filter.CurrentOrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Xóa quy trình
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id quy trình</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WORKFLOW_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_WORKFLOW_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách quy trình cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <param name="orgId">Id đơn vị</param>
        /// <param name="userId">Id người dùng</param>
        /// <returns>Danh sách quy trình</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "", Guid? orgId = null, Guid? userId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                orgId = u.OrganizationId;
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts, userId, orgId);

                return result;
            });
        }
    }
}

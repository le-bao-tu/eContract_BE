using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.API.Controller.UserOrg
{
    /// <summary>
    /// Module quản lý loại đơn vị
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/organization-type")]
    [ApiExplorerSettings(GroupName = "User&Org - 01. Organization-Type (Loại đơn vị)")]
    public class OrganizationTypeController : ApiControllerBase
    {
        private readonly IOrganizationTypeHandler _organizationTypeHandler;

        public OrganizationTypeController(IOrganizationTypeHandler organizationTypeHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _organizationTypeHandler = organizationTypeHandler;
        }

        /// <summary>
        /// Tạo mới loại đơn vị
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] OrganizationTypeCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORGANIZATION_TYPE_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORGANIZATION_TYPE_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _organizationTypeHandler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhập loại đơn vị
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] OrganizationTypeUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORGANIZATION_TYPE_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORGANIZATION_TYPE_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _organizationTypeHandler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Xóa loại đơn vị
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        [HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORGANIZATION_TYPE_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORGANIZATION_TYPE_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _organizationTypeHandler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lọc dữ liệu theo điều kiện
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentTypeBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] OrganizationTypeQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                var result = await _organizationTypeHandler.Filter(filter);

                return result;
            });
        }

        /// <summary>
        /// Lấy dữ liệu theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<DocumentTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _organizationTypeHandler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách loại đơn vị
        /// </summary>
        /// <param name="count"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        [HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _organizationTypeHandler.GetListCombobox(count, ts, u.OrganizationId);

                return result;
            });
        }
    }
}

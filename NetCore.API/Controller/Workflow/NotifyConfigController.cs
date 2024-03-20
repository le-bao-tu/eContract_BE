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
    /// Module gủi thông báo
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/notify-config")]
    [ApiExplorerSettings(GroupName = "Workflow - 03.Notify Config (Gửi thông báo)")]
    public class NotifyConfigController : ApiControllerBase
    {
        private readonly INotifyConfigHandler _handler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        public NotifyConfigController(INotifyConfigHandler handler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _orgConfigHandler = orgConfigHandler;
        }

        /// <summary>
        /// Tạo mới thông báo
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] NotifyConfigCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_NOTIFYCONFIG_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_NOTIFYCONFIG_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhập thông báo
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] NotifyConfigUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_NOTIFYCONFIG_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_NOTIFYCONFIG_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.OrganizationId = u.OrganizationId;
                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy dữ liệu theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<NotifyConfigModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lọc theo điều kiện
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<NotifyConfigBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] NotifyConfigQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter);

                return result;
            });
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_NOTIFYCONFIG_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_NOTIFYCONFIG_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, u.OrganizationId);

                return result;
            });
        }

        [Authorize, HttpGet, Route("for-combobox-by-type")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxByType(int type)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxByType(type, u.SystemLog, u.OrganizationId);

                return result;
            });
        }
    }
}

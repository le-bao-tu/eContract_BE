using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API.Controller.Permission
{  
    /// <summary>
    /// Module Menu
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/navigation")]
    [ApiExplorerSettings(GroupName = "Permission - 02. Menu")]
    public class NavigationController : ApiControllerBase
    {
        private readonly INavigationHandler _handler;

        public NavigationController(INavigationHandler navigationHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = navigationHandler;
        }

        /// <summary>
        /// Thêm mới Menu
        /// </summary>
        /// <param name="model">Thông tin Menu</param>
        /// <returns>Id Menu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] NavigationCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_NAVIGATION_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_NAVIGATION_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật Menu
        /// </summary> 
        /// <param name="model">Thông tin Menu cần cập nhật</param>
        /// <returns>Id Menu đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] NavigationUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_NAVIGATION_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_NAVIGATION_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin Menu theo id
        /// </summary> 
        /// <param name="id">Id Menu</param>
        /// <returns>Thông tin chi tiết Menu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<NavigationModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách Menu theo điều kiện lọc
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
        /// <returns>Danh sách Menu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<NavigationBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] NavigationQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Xóa Menu
        /// </summary> 
        /// <param name="listId">Danh sách Id Menu</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_NAVIGATION_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_NAVIGATION_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách Menu cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách Menu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<NavigationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách Menu cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách Menu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all-for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<NavigationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts, 2);

                return result;
            });
        }
    }
}

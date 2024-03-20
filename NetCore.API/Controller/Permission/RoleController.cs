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
    /// Module nhóm người dùng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/role")]
    [ApiExplorerSettings(GroupName = "Permission - 01. Role (Nhóm người dùng)")]
    public class RoleController : ApiControllerBase
    {
        private readonly IRoleHandler _handler;
        public RoleController(IRoleHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới nhóm người dùng
        /// </summary>
        /// <param name="model">Thông tin nhóm người dùng</param>
        /// <returns>Id nhóm người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] RoleCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ROLE_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ROLE_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] RoleUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ROLE_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ROLE_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật phân quyền dữ liệu cho nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("update-data-permission")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDataPermission([FromBody] UpdateDataPermissionModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ROLE_DATA_PERMISSION_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ROLE_DATA_PERMISSION_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.UpdateDataPermission(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách phân quyền dữ liệu cho nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-data-permission")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDataPermission([FromBody] GetDataPermissionModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetDataPermission(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin nhóm người dùng theo id
        /// </summary> 
        /// <param name="id">Id nhóm người dùng</param>
        /// <returns>Thông tin chi tiết nhóm người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<RoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách nhóm người dùng theo điều kiện lọc
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
        /// <returns>Danh sách nhóm người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<RoleBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] RoleQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Xóa nhóm người dùng
        /// </summary> 
        /// <param name="listId">Danh sách Id nhóm người dùng</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ROLE_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_ROLE_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách nhóm người dùng cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách nhóm người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<RoleSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách phân quyền chức năng cho nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-list-right-by-role")]
        [ProducesResponseType(typeof(ResponseObject<ResultGetListRightIdByRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListRightIdByRole([FromBody] GetListRightIdByRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListRightIdByRole(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật phần quyền chức năng theo nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("update-right-by-role")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateRightByRole([FromBody] UpdateRightByRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.UpdateRightByRole(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách Menu cho nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-list-navigation-by-role")]
        [ProducesResponseType(typeof(ResponseObject<ResultGetListNavigationByRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListNavigationIdByRole([FromBody] GetListNavigationByRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListNavigationByRole(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật Menu theo nhóm người dùng
        /// </summary> 
        /// <param name="model">Thông tin nhóm người dùng cần cập nhật</param>
        /// <returns>Id nhóm người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("update-navigation-by-role")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateNavigationByRole([FromBody] UpdateNavigationByRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.UpdateNavigationByRole(model, u.SystemLog);

                return result;
            });
        }
    }
}

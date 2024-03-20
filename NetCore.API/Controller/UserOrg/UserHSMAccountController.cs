using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module tài khoản hsm người dùng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/user-hsm-account")]
    [ApiExplorerSettings(GroupName = "User&Org - 04. UserHSMAccount (tài khoản hsm người dùng)")]
    public class UserHSMAccountController : ApiControllerBase
    {
        private readonly IUserHSMAccountHandler _handler;

        public UserHSMAccountController(IUserHSMAccountHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới tài khoản hsm người dùng từ service
        /// </summary>
        /// <param name="model">Thông tin tài khoản hsm người dùng</param>
        /// <returns>Id tài khoản hsm người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("create-from-service")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateFromService([FromBody] UserHSMAccountCreateOrUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HSM_ACCOUNT_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_HSM_ACCOUNT_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.UserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.CreateFromService(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Thêm mới tài khoản hsm người dùng
        /// </summary>
        /// <param name="model">Thông tin tài khoản hsm người dùng</param>
        /// <returns>Id tài khoản hsm người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] UserHSMAccountCreateOrUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HSM_ACCOUNT_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_HSM_ACCOUNT_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật tài khoản hsm người dùng
        /// </summary>
        /// <param name="model">Thông tin tài khoản hsm người dùng cần cập nhật</param>
        /// <returns>Id tài khoản hsm người dùng đã cập nhật thành công</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] UserHSMAccountCreateOrUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HSM_ACCOUNT_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_HSM_ACCOUNT_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// update trạng thái chứng thư số
        /// </summary>
        /// <param name="userHSMAccountId"></param>
        /// <param name=""></param>
        /// <returns></returns>

        [Authorize, HttpGet, Route("update-status")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(Guid userHSMAccountId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HSM_ACCOUNT_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_HSM_ACCOUNT_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.UpdateStatus(userHSMAccountId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách tài khoản hsm người dùng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách tài khoản hsm người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<UserHSMAccountBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] UserHSMAccountQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                /*var userid = u.UserId;*/
                /*filter.UserId = u.UserId;*/
                var result = await _handler.Filter(filter);
                return result;
            });
        }

        /// <summary>
        /// lấy ra danh sách chứng thư số theo Account đăng nhập
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("get-hsm-account")]
        [ProducesResponseType(typeof(ResponseObject<List<UserHSMAccountBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListHSM([FromBody] UserHSMAccountQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                /*var userid = u.UserId;*/
                var result = await _handler.GetListHSM(u.UserId, filter);
                return result;
            });
        }

        /// <summary>
        /// Xóa tài khoản hsm người dùng
        /// </summary>
        /// <param name="listId">Danh sách Id tài khoản hsm người dùng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HSM_ACCOUNT_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_HSM_ACCOUNT_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách tài khoản hsm người dùng cho combobox
        /// </summary>
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="userId">Id người dùng</param>
        /// <returns>Danh sách tài khoản hsm người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<UserHSMAccountSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, Guid? userId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(count, userId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách tài khoản hsm valid người dùng cho combobox
        /// </summary>
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="userId">Id người dùng</param>
        /// <returns>Danh sách tài khoản hsm người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox-hsm-valid")]
        [ProducesResponseType(typeof(ResponseObject<List<UserHSMAccountSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxHSMValid(int count = 0, Guid? userId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxHSMValid(count, userId);

                return result;
            });
        }

        [Authorize, HttpGet, Route("get-info-cert")]
        public async Task<IActionResult> GetInfoCertificate(Guid userHSMAccountId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _handler.GetInfoCertificate(userHSMAccountId, u.SystemLog);
            });
        }

        [HttpPost, Route("download-certificate")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadCertificate(Guid userHSMAccountId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var memoryStream = await _handler.DownloadCertificate(userHSMAccountId, u.SystemLog);
                return File(memoryStream, MediaTypeNames.Application.Octet, "certificate.cer");
            });
        }
    }
}
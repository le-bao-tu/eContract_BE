using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module cấu hình chữ ký người dùng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/user-sign-config")]
    [ApiExplorerSettings(GroupName = "User&Org - 04. UserSignConfig (cấu hình chữ ký người dùng)")]
    public class UserSignConfigController : ApiControllerBase
    {
        private readonly IUserSignConfigHandler _handler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        public UserSignConfigController(IUserSignConfigHandler handler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _orgConfigHandler = orgConfigHandler;
        }

        /// <summary>
        /// Thêm mới cấu hình chữ ký người dùng
        /// </summary>
        /// <param name="model">Thông tin cấu hình chữ ký người dùng</param>
        /// <returns>Id cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] UserSignConfigCreateOrUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_USER_SIGN_CONFIG_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_USER_SIGN_CONFIG_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật cấu hình chữ ký người dùng
        /// </summary> 
        /// <param name="model">Thông tin cấu hình chữ ký người dùng cần cập nhật</param>
        /// <returns>Id cấu hình chữ ký người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] UserSignConfigCreateOrUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_USER_SIGN_CONFIG_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_USER_SIGN_CONFIG_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Lấy danh sách cấu hình chữ ký người dùng theo điều kiện lọc
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
        /// <returns>Danh sách cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSignConfigBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] UserSignConfigQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy tất cả danh sách cấu hình chữ ký người dùng
        ///// </summary> 
        ///// <param name="ts">Từ khóa tìm kiếm</param>
        ///// <returns>Danh sách cấu hình chữ ký người dùng</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpGet, Route("all")]
        //[ProducesResponseType(typeof(ResponseObject<List<UserSignConfigBaseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetAll(string ts = null)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        UserSignConfigQueryFilter filter = new UserSignConfigQueryFilter()
        //        {
        //            TextSearch = ts,
        //            PageNumber = null,
        //            PageSize = null
        //        };
        //        var result = await _handler.Filter(filter);

        //        return result;
        //    });
        //}

        /// <summary>
        /// Xóa cấu hình chữ ký người dùng
        /// </summary> 
        /// <param name="listId">Danh sách Id cấu hình chữ ký người dùng</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_USER_SIGN_CONFIG_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_USER_SIGN_CONFIG_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách cấu hình chữ ký người dùng cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="userId">Id người dùng</param>
        /// <returns>Danh sách cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSignConfigBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, Guid? userId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(count, userId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách cấu hình chữ ký người dùng cho combobox 
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="userId">Id người dùng</param>
        /// <returns>Danh sách cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("for-combobox-sign")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSignConfigBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxSign(int count = 0, Guid? userId = null)
        {
            var result = await _handler.GetListCombobox(count, userId);

            return Helper.TransformData(result);
        }

        #region API kết nối từ đơn vị thứ 3
        /// <summary>
        /// Thêm mới cấu hình chữ ký người dùng từ đơn vị thứ 3
        /// </summary>
        /// <param name="model">Thông tin cấu hình chữ ký người dùng</param>
        /// <returns>Id cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("create-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateFrom3rd([FromBody] UserSign3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_USERSIGN_CONFIG_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_USERSIGN_CONFIG_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request); if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }
                #endregion

                var result = await _handler.CreateFrom3rd(model, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Cập nhật cấu hình chữ ký người dùng từ đơn vị thứ 3
        /// </summary>
        /// <param name="model">Thông tin cấu hình chữ ký người dùng</param>
        /// <returns>Id cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPut, Route("update-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateFrom3rd([FromBody] UserSignUpdate3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_USERSIGN_CONFIG_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_USERSIGN_CONFIG_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request); if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }
                #endregion

                var result = await _handler.UpdateFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Xóa cấu hình chữ ký người dùng từ đơn vị thứ 3
        /// </summary> 
        /// <param name="model">Id cấu hình chữ ký người dùng</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpDelete, Route("delete-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteFrom3rd([FromBody] UserSignDelete3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DELETE_USERSIGN_CONFIG_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_DELETE_USERSIGN_CONFIG_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request); if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }
                #endregion

                var result = await _handler.DeleteFrom3rd(model.Id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách cấu hình chữ ký người dùng cho đơn vị thứ 3
        /// </summary> 
        /// <param name="model">Id kết nối người dùng</param>
        /// <returns>Danh sách cấu hình chữ ký người dùng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("get-by-user-connect-id")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSignConfigBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxSign(UserSignFilter3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DELETE_USERSIGN_CONFIG_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_DELETE_USERSIGN_CONFIG_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request); if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }
                #endregion

                var result = await _handler.GetSignConfigUser3rd(model.UserConnectId, u.SystemLog);

                return result;
            });
        }
        #endregion

    }
}

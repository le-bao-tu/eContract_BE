using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API.Controller.eContract
{
    /// <summary>
    /// Module thiết bị
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/device")]
    [ApiExplorerSettings(GroupName = "User - 01. Device")]
    public class DeviceController : ApiControllerBase
    {
        private readonly ISystemLogHandler _logHandler;
        private readonly IUserHandler _userHandler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;

        public DeviceController(IUserHandler userHandler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _userHandler = userHandler;
            _logHandler = logHandler;
            _orgConfigHandler = orgConfigHandler;
        }

        #region Kết nối ứng dụng từ bên thứ 3

        /// <summary>
        /// Cập nhật thiết bị mặc định từ bên thứ 3
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("update-identifier-device-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddDevice3rd(DeviceAddRequestModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, DataLog.Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_IDENTIFIER_DEVICE);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_IDENTIFIER_DEVICE;
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

                var result = await _userHandler.AddDevice(model, u.SystemLog);

                return result;
            });
        }

        #endregion

        /// <summary>
        /// Cập nhật thiết bị mặc định
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("update-identifier-device")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddDevice(DeviceAddRequestModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_IDENTIFIER_DEVICE);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_IDENTIFIER_DEVICE;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                var result = await _userHandler.AddDevice(model, u.SystemLog);
                return result;
            });
        }

        /// <summary>
        /// Cập nhật firebase token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("add-firebase-token")]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddOrUpdateFirebaseToken(FirebaseRequestModel model)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ADD_FIREBASE_TOKEN);
                u.SystemLog.ActionName = LogConstants.ACTION_ADD_FIREBASE_TOKEN;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                var result = _userHandler.AddOrUpdateFirebaseToken(model, u.SystemLog);
                return result;
            });
        }

        /// <summary>
        /// Cập nhật firebase token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost, Route("add-firebase-token-3rd")]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddOrUpdateFirebaseToken3rd(FirebaseRequestModel3rd model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ADD_FIREBASE_TOKEN);
                u.SystemLog.ActionName = LogConstants.ACTION_ADD_FIREBASE_TOKEN;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

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

                var result = await _userHandler.AddOrUpdateFirebaseToken3rd(model, u.SystemLog);
                return result;
            });
        }

        /// <summary>
        /// Cập nhật firebase token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("delete-firebase-token")]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteFirebaseToken(FirebaseRequestModel model)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DELETE_FIREBASE_TOKEN);
                u.SystemLog.ActionName = LogConstants.ACTION_DELETE_FIREBASE_TOKEN;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                var result = _userHandler.DeleteFirebaseToken(model, u.SystemLog);
                return result;
            });
        }
    }
}

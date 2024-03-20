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
    /// Module người dùng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/user")]
    [ApiExplorerSettings(GroupName = "User&Org - 02. User (Người dùng)")]
    public class UserController : ApiControllerBase
    {
        private readonly IUserHandler _handler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;

        public UserController(IUserHandler handler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _orgConfigHandler = orgConfigHandler;
        }

        #region 3rd Service

        /// <summary>
        /// Thêm mới hoặc cập nhật người dùng
        /// </summary>
        /// <param name="listUser">Danh sách thông tin người dùng</param>
        /// <returns>Id người dùng, Connect Id</returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("create-or-update")]
        [ProducesResponseType(typeof(ResponseObject<List<UserConnectResonseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOrUpdate([FromBody] List<UserConnectModel> listUser)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_USER_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_USER_3RD;
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

                //if (u.OrganizationId == null || u.OrganizationId == Guid.Empty)
                //{
                //    return new ResponseError(Code.Forbidden, "X-OrganizationId không hợp lệ");
                //}

                #endregion Lấy thông tin đơn vị

                UpdateOrCreateUserModel model = new UpdateOrCreateUserModel()
                {
                    OrganizationId = u.OrganizationId,
                    ListUser = listUser
                };

                // u.SystemLog.ObjectCode = CacheConstants.USER;

                var result = await _handler.CreateOrUpdate(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin người dùng cho service bên thứ 3
        /// </summary>
        /// <param name="userConnectId">Model chứa thông tin người dùng</param>
        /// <returns>Id người dùng, Connect Id</returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-user-info-by-connect-id")]
        [ProducesResponseType(typeof(ResponseObject<UserConnectModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOrUpdate(string userConnectId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
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

                #endregion Lấy thông tin đơn vị

                var result = await _handler.GetByUserConnectId(userConnectId, u.OrganizationId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách CTS từ 3rd Service
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous, HttpGet, Route("get-user-certificate-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<UserHSMAccountModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserCertificate(string userConnectId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
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

                #endregion Lấy thông tin đơn vị

                var result = await _handler.GetUserCertificateFrom3rd(userConnectId, u.OrganizationId, u.SystemLog);

                return result;
            });
        }

        #endregion 3rd Service

        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="model">Thông tin người dùng</param>
        /// <returns>Id người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] UserCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_USER;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.ObjectCode = CacheConstants.USER;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật người dùng
        /// </summary> 
        /// <param name="model">Thông tin người dùng cần cập nhật</param>
        /// <returns>Id người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] UserUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USER;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.ObjectCode = CacheConstants.USER;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary> 
        /// Cập nhật thông tin người dùng
        /// </summary> 
        /// <param name="model">Thông tin người dùng cần cập nhật</param>
        /// <returns>Id người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("update-user-profile")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUser([FromBody] UserProfileUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USER;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.ObjectCode = CacheConstants.USER;

                model.Id = u.UserId;
                var result = await _handler.UpdateUser(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy thông tin user theo connect id
        ///// </summary>
        ///// <param name="model">Thông tin đơn vị, người dùng</param>
        ///// <returns>Danh sách thông tin người dùng</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpPost, Route("get-user-by-list-connectid")]
        //[ProducesResponseType(typeof(ResponseObject<OrgAndUserConnectInfoModel>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetUsserId([FromBody] OrgAndUserConnectRequestModel model)
        //{
        //    var result = await _handler.GetListUserByListConnectId(model);
        //    return Helper.TransformData(result);
        //}

        /// <summary>
        /// Lấy thông tin người dùng theo id
        /// </summary>
        /// <param name="id">Id người dùng</param>
        /// <returns>Thông tin chi tiết người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<UserModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách người dùng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<UserBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] UserQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.CurrentUserId = u.UserId;
                filter.OrganizationId = filter.OrganizationId;
                filter.OrganizationCurrentId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin người dùng theo id
        /// </summary>
        /// <param name>Id người dùng</param>
        /// <returns>Thông tin chi tiết người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("get-current-user-info")]
        [ProducesResponseType(typeof(ResponseObject<UserModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByAccount()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(u.UserId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin danh sách thiết bị người dùng 
        /// </summary>
        /// <param name>Id người dùng</param>
        /// <returns>Thông tin chi tiết người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("get-list-user-device")]
        [ProducesResponseType(typeof(ResponseObject<List<UserDeviceModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserDevice(Guid userId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListDeviceByUser(userId, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Lấy tất cả danh sách người dùng
        /// </summary>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<UserBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                UserQueryFilter filter = new UserQueryFilter()
                {
                    TextSearch = ts,
                    PageNumber = null,
                    PageSize = null
                };
                filter.OrganizationCurrentId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách người dùng cho combobox
        /// </summary>
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        ///<param name="orgId">Id đơn vị</param>
        /// <returns>Danh sách người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "", Guid? orgId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts, orgId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách người dùng cho combobox by root Org
        /// </summary>
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        ///<param name="orgId">Id đơn vị</param>
        /// <returns>Danh sách người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox-by-root-org")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxByRootOrg(int count = 0, string ts = "", Guid? orgId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxByRootOrg(u.SystemLog, count, ts, orgId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách người dùng cho combobox filter theo người dùng nội bộ hoặc khách hàng
        /// </summary>
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        ///<param name="orgId">Id đơn vị</param>
        ///<param name="isInternalUser">True người dùng nội bộ, false khách hàng</param>
        /// <returns>Danh sách người dùng</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox-filter-internal-user")]
        [ProducesResponseType(typeof(ResponseObject<List<UserSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxFilterInternalUser(bool isInternalUser = true, int count = 0, string ts = "", Guid? orgId = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxFilterInternalOrCustomer(u.SystemLog, count, ts, orgId, isInternalUser);

                return result;
            });
        }

        /// <summary>
        /// Xóa người dùng
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id đơn vị</param>
        /// <returns>Danh sách kết quả xóa</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DELETE_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_DELETE_USER;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.ObjectCode = CacheConstants.USER;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Khóa/Mở khóa người dùng
        /// </summary>
        /// <param name="model">Thông tin người dùng cần cập nhật khóa</param>
        /// <returns>Id người dùng đã cập nhật thành công</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("lock")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LockOrUnlock([FromBody] UserLockModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                if (model.IsLock)
                {
                    u.SystemLog.ActionCode = nameof(LogConstants.ACTION_LOCK_USER);
                    u.SystemLog.ActionName = LogConstants.ACTION_LOCK_USER;
                }
                else
                {
                    u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UNLOCK_USER);
                    u.SystemLog.ActionName = LogConstants.ACTION_UNLOCK_USER;
                }
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.ObjectCode = CacheConstants.USER;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.LockOrUnlock(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Thay đổi mật khẩu người dùng
        /// </summary>
        /// <param name="model">Thông tin người dùng cần cập nhật mật khẩu</param>
        /// <returns>Id người dùng đã cập nhật thành công</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("update-password")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePassword([FromBody] UserUpdatePasswordModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_PASS_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_PASS_USER;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.ObjectCode = CacheConstants.USER;

                model.ModifiedUserId = u.UserId;
                model.UserId = u.UserId;
                var result = await _handler.UpdatePassword(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cấu hình user PIN
        /// </summary> 
        /// <param name="model">Thông tin người dùng cần cập nhật user PIN</param>
        /// <returns>Id người dùng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("update-user-pin")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUserPIN([FromBody] UserUpdatePIN model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USER_PIN);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USER_PIN;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                model.UserId = u.UserId;
                var result = await _handler.UpdateUserPIN(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi OTP xác thực người dùng
        /// </summary>
        /// <returns>Trạng thái gửi OTP</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("send-otp-auth")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOTPUserAuth()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_USER_AUTH);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_USER_AUTH;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.SendOTPAuthToUser(new Guid(u.SystemLog.UserId), u.SystemLog);

                return result;
            });
        }

        #region eKYC

        /// <summary>
        /// Lưu eKYC Front Card của người dùng
        /// </summary>
        /// <param name="formFiles">File CMND mặc trước</param>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("register-front-card")]
        [ProducesResponseType(typeof(ResponseObject<EKYCUserInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterFrontCard([FromForm] RegisterFrontCardModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _handler.RegisterFrontCard(model, u.UserId, u.SystemLog);
            });
        }

        /// <summary>
        /// Lưu eKYC Back Card của người dùng
        /// </summary>
        /// <param name="formFiles">File CMND mặc sau</param>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("register-back-card")]
        [ProducesResponseType(typeof(ResponseObject<EKYCUserInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterBackCard([FromForm] RegisterBackCardModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _handler.RegisterBackCard(model, u.UserId, u.SystemLog);
            });
        }

        /// <summary>
        /// eKYC Face video & Liveness của người dùng
        /// </summary>
        /// <param name="formFiles">Video file</param>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("register-face-video")]
        [ProducesResponseType(typeof(ResponseObject<EKYCUserInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterFaceVideo_Liveness([FromForm] RegisterFaceVideoLivenessModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _handler.RegisterFaceVideo_Liveness(model, u.UserId, u.SystemLog);
            });
        }

        [Authorize, HttpPost, Route("update-user-info-ekyc")]
        [ProducesResponseType(typeof(ResponseObject<EKYCUserInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateEKYC_UserInfo([FromBody] EKYCUserInfo model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                UpdateEKYCUserInfoModel dt = new UpdateEKYCUserInfoModel()
                {
                    UserId = u.UserId,
                    UserInfo = model
                };

                return await _handler.UpdateEKYC_UserInfo(dt, u.SystemLog);
            });
        }

        #endregion eKYC

        #region Mobile API

        /// <summary>
        /// Kiểm tra mật khẩu người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("vaidate-password-from-mobile")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidatePassword([FromBody] ChangePasswordModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_VALIDATE_PASSWORD);
                u.SystemLog.ActionName = LogConstants.ACTION_VALIDATE_PASSWORD;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = u.UserId;
                model.OTP = "";

                var result = await _handler.ValidatePassword(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi OTP để đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [Authorize, HttpPost, Route("send-otp-change-password-from-mobile")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOTPToChangePassword()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_CHANGEPASS);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_CHANGEPASS;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                var result = await _handler.SendOTPToChangePassword(u.UserId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("change-password-from-mobile")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_PASS_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_PASS_USER;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = u.UserId;

                var result = await _handler.ChangePassword(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật user pin
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("change-userpin-from-mobile")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeUserPIN([FromBody] ChangeUserPINModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USERPIN);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USERPIN;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = u.UserId;

                var result = await _handler.ChangeUserPIN(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Đăng ký hình thức ký
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("update-eform-config")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateEFormConfig([FromBody] UpdateEFormConfigModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USER_EFORM_CONFIG);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USER_EFORM_CONFIG;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = u.UserId;

                var result = await _handler.UpdateEFormConfig(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách CTS
        /// </summary>
        /// <returns></returns>
        [Authorize, HttpPost, Route("get-user-certificate")]
        [ProducesResponseType(typeof(ResponseObject<List<UserHSMAccountModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserCertificate()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetUserCertificate(u.UserId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin người dùng gồm trạng thái ký số hay ký điện tử
        /// </summary>
        /// <returns></returns>
        [Authorize, HttpPost, Route("get-user-eformconfig")]
        [ProducesResponseType(typeof(ResponseObject<UserEFormConfigModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserEFormConfig()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetUserEFormConfig(u.UserId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Làm mới CTS
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("add-certificate")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddCertificate([FromBody] AddCertificateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USERPIN);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USERPIN;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = u.UserId;

                var result = await _handler.AddCertificate(model, u.SystemLog);

                return result;
            });
        }

        #endregion Mobile API

        #region Phân quyền

        /// <summary>
        /// Lấy menu - role - right người dùng đang đăng nhập
        /// </summary>
        /// <returns>Danh sách menu, role, right của người dùng</returns>
        [Authorize, HttpGet, Route("get-permission-current-user")]
        [ProducesResponseType(typeof(ResponseObject<ResultGetUserRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRole()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetUserPermission(u.UserId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy dữ liệu nhóm người dùng theo User Id
        /// </summary>
        /// <param name="model">Model bao gồm Id người dùng</param>
        /// <returns>Danh sách Id nhóm người dùng</returns>
        [Authorize, HttpPost, Route("get-user-role")]
        [ProducesResponseType(typeof(ResponseObject<ResultGetUserRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRole([FromBody] GetUserRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetUserRole(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật nhóm người dùng cho user
        /// </summary>
        /// <param name="model">UpdateUserRoleModel gồm User Id và danh sách Id nhóm người dùng</param>
        /// <returns>Id người dùng</returns>
        [Authorize, HttpPost, Route("update-user-role")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_USER_ROLE);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_USER_ROLE;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                var result = await _handler.UpdateUserRole(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật nhóm người dùng cho user
        /// </summary>
        /// <param name="model">SaveListUserRoleModel gồm Role Id và danh sách Id người dùng</param>
        /// <returns>Id nhóm người dùng</returns>
        [Authorize, HttpPost, Route("save-list-user-role")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveListUserRole([FromBody] SaveListUserRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_LIST_USER_ROLE);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_LIST_USER_ROLE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.SaveListUserRole(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách User Id theo Role Id
        /// </summary>
        /// <param name="model">SaveListUserRoleModel gồm Role Id và danh sách Id người dùng</param>
        /// <returns>Id nhóm người dùng</returns>
        [Authorize, HttpPost, Route("get-list-user-id-by-role")]
        [ProducesResponseType(typeof(ResponseObject<ResultGetUserRoleByRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRoleByRoleId([FromBody] GetUserRoleByRoleModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetUserRoleByRoleId(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Khởi tạo phân quyền dữ liệu cho đơn vị theo người dùng
        /// </summary>
        /// <param name="model">Thông tin user + org</param>
        /// <returns>Trạng thái dữ liệu</returns>
        [AllowAnonymous, HttpPost, Route("init-user-admin-org")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> InitUserAdminOrg([FromBody] InitUserAdminOrgModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_INIT_USER_ROLE_ORG);
                u.SystemLog.ActionName = LogConstants.ACTION_INIT_USER_ROLE_ORG;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.InitUserAdminOrg(model, u.SystemLog);

                return result;
            });
        }

        #endregion Phân quyền
    }
}
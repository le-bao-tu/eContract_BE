using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module hợp đồng - VietCredit
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/contract")]
    [ApiExplorerSettings(GroupName = "eContract - 08.2 Contract - Contract controller")]
    public class ContractDocumentController : ApiControllerBase
    {
        private readonly IDocumentHandler _handler;
        private readonly ISignHashHandler _signHashHandler;
        private readonly IUserHandler _userHandler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        public ContractDocumentController(IDocumentHandler handler, IOrganizationConfigHandler orgConfigHandler, IUserHandler userHandler, ISignHashHandler signHashHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _signHashHandler = signHashHandler;
            _userHandler = userHandler;
            _orgConfigHandler = orgConfigHandler;
        }

        #region API kết nối ứng dụng từ bên thứ 3
        /// <summary>
        /// Thêm mới hợp đồng eForm từ ứng dụng bên thứ 3 theo người dùng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("create-eform-use-digital-signature")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateEFormFrom3rd([FromBody] CreateEFormFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_EFORM);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_EFORM;
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

                var result = await _handler.CreateEFormFrom3rd_v2(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký eForm từ ứng dụng bên thứ 3
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("confirm-eform-use-digital-signature")]
        [ProducesResponseType(typeof(ResponseObject<WorkflowDocumentSignFor3rdReponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignEFormFrom3rd([FromBody] SignEFormFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_EFORM);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_EFORM;
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

                u.SystemLog.OperatingSystem.DeviceId = model.DeviceInfo.DeviceId;
                u.SystemLog.OperatingSystem.DeviceName = model.DeviceInfo.DeviceName;

                var result = await _signHashHandler.SignEFormFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// API lấy thông tin thẻ, hợp đồng mới nhất từ khách hàng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("get-latest-document-card-info")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestDocumentUser([FromBody] GetLatestDocumentUserFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_LATEST_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_LATEST_DOCUMENT;
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

                var result = await _handler.GetLatestDocumentUser(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("request-sign-document-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestSignDocumentFrom3rd([FromBody] RequestSignDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD;
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

                var result = await _signHashHandler.RequestSignDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        ///  Lấy thông thông tin hiện tại của tài liệu
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <param name="fileUrlExpireSeconds">Thời gian tồn tại của url file hợp đồng</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-documnet-info")]
        [ProducesResponseType(typeof(ResponseObject<DocumentSignedResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentInfo(string documentCode, int fileUrlExpireSeconds = 0)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOC_INFO);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOC_INFO;
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

                var result = await _handler.GetDocumentInfo(documentCode, fileUrlExpireSeconds, u.SystemLog);

                return result;
            });
        }

        /// <summary> 
        /// Từ chối ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("reject-document-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectDocumentFrom3rd([FromBody] RequestSignDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REJECT_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_REJECT_DOCUMENT_3RD;
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

                var result = await _signHashHandler.RejectDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary> 
        /// Hủy hợp đồng
        /// </summary> 
        /// <returns>Trạng thái</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("delete-document-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteDocumentFrom3rd([FromBody] RequestSignDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DELETE_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_DELETE_DOCUMENT_3RD;
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

                var result = await _signHashHandler.DeleteDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary> 
        /// Lấy mới OTP
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("renew-otp")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RenewOTPFrom3rd([FromBody] RequestOTPFromRequestId model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_RENEW_OTP_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_RENEW_OTP_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                var result = await _signHashHandler.RenewOTPFromRequestId(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách hợp đồng cho service bên thứ 3
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost, Route("get-list-document-from-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBaseModelMobileApp>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListDocumentFrom3rd([FromBody] DocumentQueryFilterMobileApp model)
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
                #endregion

                #region Lấy thông tin người dùng
                var user = await _userHandler.GetUserFromOrganizationAndUserConnect(u.OrganizationId, model.UserConnectId);
                if (user == null)
                {
                    Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin người dùng có mã {model.UserConnectId} thuộc đơn vị đang kết nối có consumerKey: {consumerKey}");

                    return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                }
                model.CurrentUserId = user.Id;
                #endregion

                model.PageSize = null;
                model.PageNumber = null;

                var result = await _handler.GetListDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        #endregion

        #region API cung cấp cho mobile app kết nối trực tiếp
        /// <summary>
        /// Thêm mới hợp đồng eForm từ ứng dụng mobile
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("create-eform-use-digital-signature-from-app")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateEFormFromMobile([FromBody] CreateEFormFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_EFORM);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_EFORM;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = new Guid(u.SystemLog.UserId);
                var result = await _handler.CreateEFormFrom3rd_v2(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký eForm từ mobile app
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("confirm-eform-use-digital-signature-from-app")]
        [ProducesResponseType(typeof(ResponseObject<WorkflowDocumentSignFor3rdReponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignEFormFromApp([FromBody] SignEFormFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_EFORM);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_EFORM;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = new Guid(u.SystemLog.UserId);

                u.SystemLog.OperatingSystem.DeviceId = model.DeviceInfo.DeviceId;
                u.SystemLog.OperatingSystem.DeviceName = model.DeviceInfo.DeviceName;

                var result = await _signHashHandler.SignEFormFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// API lấy thông tin thẻ, hợp đồng mới nhất từ khách hàng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-latest-document-info")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestDocumentUserFromApp([FromBody] GetLatestDocumentUserFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_LATEST_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_LATEST_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = new Guid(u.SystemLog.UserId);

                var result = await _handler.GetLatestDocumentUser(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Lấy danh sách hợp đồng cho mobile app
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("get-list-document-mobile-app")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBaseModelMobileApp>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListDocumentMobileApp([FromBody] DocumentQueryFilterMobileApp model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.CurrentUserId = u.UserId;
                var result = await _handler.GetListDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// API lấy thông tin hợp đồng theo mã hợp đồng cho mobile
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-document-detail-by-code")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestDocumentUserFromApp([FromBody] GetDoumentDetailByCodeFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOC_DETAIL_BY_CODE);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOC_DETAIL_BY_CODE;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = u.UserId;

                var result = await _handler.GetDoumentDetailByCodeFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("request-sign-document-from-app")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestSignDocumentFromApp([FromBody] RequestSignDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                model.UserId = new Guid(u.SystemLog.UserId);

                var result = await _signHashHandler.RequestSignDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("request-sign-document-from-app-form-data")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestSignDocumentFromAppFormData([FromForm] RequestSignDocumentFrom3rdFormDataModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                var requestModel = new RequestSignDocumentFrom3rdModel();

                requestModel.UserId = new Guid(u.SystemLog.UserId);
                requestModel.DocumentCode = model.DocumentCode;
                requestModel.CertificateId = model.CertificateId;

                if (model.EKYC != null)
                {
                    var eKYC = model.EKYC.OpenReadStream();
                    MemoryStream memoryFileEKYC = new MemoryStream();
                    eKYC.CopyTo(memoryFileEKYC);

                    requestModel.ImageBase64 = Convert.ToBase64String(memoryFileEKYC.ToArray());
                }

                if (model.Signature != null)
                {
                    var signature = model.Signature.OpenReadStream();
                    MemoryStream memoryFileSignature = new MemoryStream();
                    signature.CopyTo(memoryFileSignature);

                    requestModel.SignatureBase64 = Convert.ToBase64String(memoryFileSignature.ToArray());
                }

                try
                {
                    var location = JsonSerializer.Deserialize<LocationSign3rdModel>(model.LocationText);
                    requestModel.Location = location;
                }
                catch (Exception e)
                {
                }

                try
                {
                    var deviceInfo = JsonSerializer.Deserialize<OpratingSystemMobileModel>(model.DeviceInfoText);
                    requestModel.DeviceInfo = deviceInfo;
                }
                catch (Exception e)
                {
                }

                var result = await _signHashHandler.RequestSignDocumentFrom3rd(requestModel, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Yêu cầu ký hợp đồng từ mobile cho case vkey
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("request-sign-document-vkey-from-app")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestSignDocumentVkeyFromApp([FromBody] RequestSignDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_SIGN_DOCUMENT_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                model.UserId = new Guid(u.SystemLog.UserId);

                var result = await _signHashHandler.RequestSignDocumentVkeyFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký hợp đồng
        /// </summary> 
        /// <param name="model"></param>
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        ///
        [AllowAnonymous, HttpPost, Route("confirm-sign-document-from-esign")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmSignDocumentFromESign([FromBody] SignConfirmModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CONFIRM_SIGN_DOCUMENT_ESIGN);
                u.SystemLog.ActionName = LogConstants.ACTION_CONFIRM_SIGN_DOCUMENT_ESIGN;
                u.SystemLog.Device = LogConstants.DEVICE_ESIGN;

                var result = await _signHashHandler.ConfirmSignDocumentFromESign(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Từ chối ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("reject-document-from-app")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectDocumentFromApp([FromBody] RequestSignDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REJECT_DOCUMENT_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_REJECT_DOCUMENT_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                model.UserId = u.UserId;

                var result = await _signHashHandler.RejectDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        #endregion

        #region API cung cấp cho web app
        /// <summary>
        /// Lấy thông tin hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("get-document-from-web-app")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentFromWebApp([FromBody] GetDocumentFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOCUMENT_FROM_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOCUMENT_FROM_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _handler.GetDocumentFromWebApp(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi lại mã truy cập hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("resend-email-passcode")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResendEmailPassCodeWebApp([FromBody] GetDocumentFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_RESEND_PASSCODE_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_RESEND_PASSCODE_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _handler.ResendEmailPassCodeWebApp(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Xác nhận eForm
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("confirm-eform-from-web-app")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmEFormFromWebApp([FromBody] ConfirmEformFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CONFIRM_EFORM_FROM_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_CONFIRM_EFORM_FROM_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _signHashHandler.ConfirmEFormFromWebApp(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi OTP để thực hiện ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("send-email-otp")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOTPSignDocumentFromWebApp([FromBody] ResendOTPSignDocumentFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_SIGN_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_SIGN_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _handler.SendOTPSignDocumentFromWebApp(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("sign-document")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignDocumentFromWebApp([FromBody] SignDocumentFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOCUMENT_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOCUMENT_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _signHashHandler.SignDocumentFromWebApp(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Từ chối ký hợp đồng
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("reject-document-from-web-app")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectDocumentFromWebApp([FromBody] RejectDocumentFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REJECT_DOCUMENT_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_REJECT_DOCUMENT_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _signHashHandler.RejectDocumentFromWebApp(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Tải file đã ký lên
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("upload-signed-document-from-web-app")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFileSignedFromWebApp([FromBody] UploadSignedDocumentFromWebAppModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPLOAD_SIGNED_DOCUMENT_WEB_APP);
                u.SystemLog.ActionName = LogConstants.ACTION_UPLOAD_SIGNED_DOCUMENT_WEB_APP;
                u.SystemLog.Device = LogConstants.DEVICE_WEBAPP;

                var result = await _signHashHandler.UploadFileSignedFromWebApp(model, u.SystemLog);

                return result;
            });
        }

        #endregion
    }
}

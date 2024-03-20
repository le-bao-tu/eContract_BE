using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module ký tài liệu qua e-Contract
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/sign")]
    [ApiExplorerSettings(GroupName = "eContract - 09. Sign Document")]
    public class SignDocumentController : ApiControllerBase
    {
        private readonly ISignDocumentHandler _handler;
        private readonly ISignHashHandler _signHashHandler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;

        public SignDocumentController(ISignDocumentHandler handler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler, ISignHashHandler signHashHandler) : base(logHandler)
        {
            _handler = handler;
            _signHashHandler = signHashHandler;
            _orgConfigHandler = orgConfigHandler;
        }

        #region Kết nối ứng dụng từ bên thứ 3

        /// <summary>
        /// Ký điện tử nhiều tài liệu 
        /// </summary>
        /// <param name="model"></param> 
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("sign-electronic-multiple-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignFor3rdReponseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignMultileDocument([FromBody] SignDocumentMultileFor3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
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
                #endregion

                var result = await _handler.SignMultileDocumentDigitalFor3rdV2(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Ký điện tử nhiều tài liệu 
        /// </summary>
        /// <param name="signModel">Thông tin ký</param> 
        /// <param name="signatureBase64">Thông tin chữ ký</param> 
        /// <param name="fileEKYC">Hình ảnh eKYC</param> 
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("sign-electronic-multiple-3rd-form-data")]
        //[Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignFor3rdReponseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignMultileDocumentFormData([FromForm] string signModel, [FromForm] string signatureBase64, [FromForm] IFormFile fileEKYC)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;
                SignDocumentMultileFor3rdModel model = new SignDocumentMultileFor3rdModel();

                model = JsonSerializer.Deserialize<SignDocumentMultileFor3rdModel>(signModel);

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
                #endregion
                
                model.SignatureBase64 = signatureBase64;
                try
                {
                    if (fileEKYC != null)
                    {
                        var fileEKYCStream = fileEKYC.OpenReadStream();
                        MemoryStream memoryFileEKYC = new MemoryStream();
                        fileEKYCStream.CopyTo(memoryFileEKYC);
                        model.EKYCMemoryStream = memoryFileEKYC;
                    }
                }
                catch (Exception)
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra khi convert fileEKYC sang MemoryStream {JsonSerializer.Serialize(model)}");
                }

                var result = await _handler.SignMultileDocumentDigitalFor3rdV2(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Ký tự động nhiều tài liệu
        /// </summary>
        /// <param name="listDocumentCode">Danh sách hợp đồng cần ký tự động</param> 
        /// <returns>Kết quả ký</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("sign-auto-multiple-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<AutomaticSignFileResultModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignMultileDocument([FromBody] List<string> listDocumentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
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

                var result = await _signHashHandler.AutomaticSignDocument(listDocumentCode, u.SystemLog);

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

                //if (u.OrganizationId == null || u.OrganizationId == Guid.Empty)
                //{
                //    return new ResponseError(Code.Forbidden, "X-OrganizationId không hợp lệ");
                //}
                #endregion

                var result = await _handler.GetDocumentInfo(documentCode, fileUrlExpireSeconds, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        ///  Lấy thông thông tin hiện tại của danh sách hợp đồng theo lô
        /// </summary> 
        /// <param name="documentBatchCode">Mã lô hợp đồng</param>
        /// <param name="fileUrlExpireSeconds">Thời gian tồn tại của url file hợp đồng</param>
        /// <returns>Danh sách hợp đồng trong lô</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-documnet-batch-info")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResponseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentBatchInfo(string documentBatchCode, int fileUrlExpireSeconds = 0)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_BATCHDOC_INFO);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_BATCHDOC_INFO;
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
                #endregion

                var result = await _handler.GetDocumentBatchInfo(documentBatchCode, fileUrlExpireSeconds, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        ///  Lấy lại link truy cập 
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-access-link")]
        [ProducesResponseType(typeof(ResponseObject<ResponseAccessLinkModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccessLink(string documentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_ACCESS_LINK);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_ACCESS_LINK;
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
                #endregion

                var result = await _handler.GetAccessLink(documentCode, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        ///  Gửi lại link truy cập cho người ký
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("resend-access-link")]
        [ProducesResponseType(typeof(ResponseObject<ResponseAccessLinkModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResendAccessLink(string documentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_ACCESS_LINK);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_ACCESS_LINK;
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
                #endregion

                var result = await _handler.ResendAccessLink(documentCode, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy OTP theo hợp đồng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách mã hợp đồng</param>
        /// <returns>Danh sách OTP theo mã hợp đồng</returns>
        [AllowAnonymous, HttpPost, Route("get-otp-by-document")]
        [ProducesResponseType(typeof(ResponseObject<List<OTPByDocumentModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOTPDocumentFor3rd(List<string> listDocumentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_OTP_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_OTP_DOCUMENT;
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
                #endregion

                var result = await _handler.GetOTPDocumentFor3rd(listDocumentCode, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi OTP qua mail theo hợp đồng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách mã hợp đồng</param>
        /// <returns>Trạng thái gửi email cho các hợp đồng</returns>
        [AllowAnonymous, HttpPost, Route("send-otp-email-by-document")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMailOTPDocumentFor3rd(List<string> listDocumentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_MAIL_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_MAIL_DOCUMENT;
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
                #endregion

                var result = await _handler.SendMailOTPDocumentFor3rd(listDocumentCode, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi OTP qua SMS theo hợp đồng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách mã hợp đồng</param>
        /// <returns>Trạng thái gửi SMS OTP cho các hợp đồng</returns>
        [AllowAnonymous, HttpPost, Route("send-otp-sms-by-document")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendSMSOTPDocumentFor3rd(List<string> listDocumentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_SMS_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_SMS_DOCUMENT;
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
                #endregion

                var result = await _handler.SendSMSOTPDocumentFor3rd(listDocumentCode, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Gửi OTP qua mail cho người dùng
        /// </summary>
        /// <param name="model">Danh sách mã hợp đồng</param>
        /// <returns>Trạng thái gửi email cho các hợp đồng</returns>
        [AllowAnonymous, HttpPost, Route("send-otp-email-by-user")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMailOTPUserFor3rd(OTPUserRequestModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_MAIL_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_MAIL_USER;
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
                #endregion

                var result = await _handler.SendMailOTPUserFor3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi OTP qua SMS theo hợp đồng
        /// </summary>
        /// <param name="model">Danh sách mã hợp đồng</param>
        /// <returns>Trạng thái gửi SMS OTP cho các hợp đồng</returns>
        [AllowAnonymous, HttpPost, Route("send-otp-sms-by-user")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendSMSOTPUserFor3rd(OTPUserRequestModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP_SMS_USER);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP_SMS_USER;
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
                #endregion

                var result = await _handler.SendSMSOTPUserFor3rd(model, u.SystemLog);

                return result;
            });
        }

        #endregion

        /// <summary>
        /// Lấy thông tin tài liệu theo mã tài liệu và OTP
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <param name="account">Email/SĐT</param>
        /// <param name="otp">Mã OTP</param>
        /// <returns>Thông tin tài liệu</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-document")]
        [ProducesResponseType(typeof(ResponseObject<DocumentResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentByCode(string documentCode, string account, string otp)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOC_BY_CODE_OTP);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOC_BY_CODE_OTP;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.Description = $"documentCode: {documentCode} - account: {account} - otp: {otp}";

                var result = await _handler.GetDocumentByCode(documentCode, account, otp, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Từ chối ký tài liệu
        /// </summary>
        /// <returns>Thông tin tài liệu</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPut, Route("reject-document")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectDocument(RejectDocumentModel data)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REJECT_SIGN_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_REJECT_SIGN_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.Description = $"{data.UserId} từ chối ký hợp đồng {data.DocumentId} với lý do {data.RejectReason}";

                var result = await _handler.RejectDocument(data, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi mã OTP
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("send-otp")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOTP([FromBody] string documentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.Description = $"Gửi mã OTP truy cập hợp đồng {documentCode}";

                var result = await _handler.SendOTP(documentCode, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi mã OTP
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("send-otp-sign-document")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOTPSignDocument([FromBody] string documentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.SendOTP(documentCode, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi mã OTP
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("send-otp-access-document")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendOTPAccessDocument([FromBody] string documentCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEND_OTP);
                u.SystemLog.ActionName = LogConstants.ACTION_SEND_OTP;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.Description = $"Gửi mã OTP truy cập hợp đồng {documentCode}";

                var result = await _handler.SendOTP(documentCode, u.SystemLog, true);

                return result;
            });
        }

        ///// <summary>
        ///// Ký điện tử
        ///// </summary>
        ///// <param name="model"></param> 
        ///// <returns>Đã gửi mã OTP</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpPost, Route("sign")]
        //[ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignReponseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> SignDocument([FromBody] SignDocumentModel model)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
        //        u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
        //        u.SystemLog.Device = LogConstants.DEVICE_WEB;
        //        // u.SystemLog.Description = $"Ký điện tử hợp đồng {model.DocumentCode} với OTP {model.OTP}";

        //        var result = await _handler.SignDocumentDigital(model.DocumentCode, model.OTP, model.SignatureBase64, u.SystemLog);

        //        

        //        return result;
        //    });
        //}

        ///// <summary>
        ///// Ký điện tử nhiều tài liệu 
        ///// </summary>
        ///// <param name="model"></param> 
        ///// <returns>Đã gửi mã OTP</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpPost, Route("sign-multiple")]
        //[ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignReponseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> SignMultileDocument([FromBody] SignDocumentMultileModel model)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
        //        u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
        //        u.SystemLog.Device = LogConstants.DEVICE_WEB;
        //        // u.SystemLog.Description = $"Ký điện tử hợp đồng {model.ListDocumentId} với OTP {model.OTP}";

        //        var result = await _handler.SignMultileDocumentDigital(model.ListDocumentId, model.OTP, model.SignatureBase64, u.SystemLog);

        //        

        //        return result;
        //    });
        //}

        ///// <summary>
        ///// Ký tài liệu bằng HSM
        ///// </summary> 
        ///// <param name="model"></param> 
        ///// <returns>Đã gửi mã OTP</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpPost, Route("sign-hsm")]
        //[ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignReponseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> SignDocumentHSM([FromBody] SignDocumentHSMModel model)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_HSM);
        //        u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_HSM;
        //        u.SystemLog.Device = LogConstants.DEVICE_WEB;
        //        // u.SystemLog.Description = $"Ký HSM hợp đồng {model.DocumentCode}";

        //        var result = await _handler.SignDocumentHSM(model.DocumentCode, model.UserPin, model.Base64Image, u.SystemLog);

        //        

        //        return result;
        //    });
        //}

        ///// <summary>
        ///// Ký tài liệu bằng usb token
        ///// </summary> 
        ///// <returns>Trạng thái ký các tài liệu</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpPost, Route("sign-usb-token")]
        //[ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignReponseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> SignDocumentUsbToken([FromBody] SignDocumentUsbTokenModel model)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_USBTOKEN);
        //        u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_USBTOKEN;
        //        u.SystemLog.Device = LogConstants.DEVICE_WEB;
        //        // u.SystemLog.Description = $"Ký UsbToken hợp đồng {model.DocumentCode}";

        //        var result = await _handler.SignDocumentUsbToken(model.DocumentCode, model.FileBase64, u.SystemLog);

        //        

        //        return result;
        //    });
        //}

        ///// <summary>
        ///// Ký nhiều tài liệu bằng usb token
        ///// </summary> 
        ///// <returns>Trạng thái ký các tài liệu</returns> 
        ///// <response code="200">Thành công</response>
        //[AllowAnonymous, HttpPost, Route("sign-usb-token-multile")]
        //[ProducesResponseType(typeof(ResponseObject<List<WorkflowDocumentSignReponseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> SignMultileDocumentUsbToken([FromBody] List<SignDocumentUsbTokenDataModel> model)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_USBTOKEN);
        //        u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_USBTOKEN;
        //        u.SystemLog.Device = LogConstants.DEVICE_WEB;
        //        string rs = "";
        //        foreach (var item in model)
        //        {
        //            rs += item.DocumentId + ", ";
        //        }
        //        // u.SystemLog.Description = $"Ký UsbToken hợp đồng {rs}";

        //        var result = await _handler.SignMultileDocumentUsbToken(model, u.SystemLog);

        //        

        //        return result;
        //    });
        //}

        /// <summary>
        ///  Lấy tọa độ vùng ký
        /// </summary> 
        /// <param name="documentId">Id tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-coordinate")]
        [ProducesResponseType(typeof(ResponseObject<CoordinateFileModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCoordinateFile(Guid documentId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_COORDINATE);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_COORDINATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                // u.SystemLog.Description = $"Lấy tọa độ vùng ký hợp đồng {documentId}";

                var result = await _handler.GetCoordinateFile(documentId, u.SystemLog);



                return result;
            });
        }

        /// <summary>
        ///  Lấy danh sách tọa độ vùng ký dựa vào danh sách documentId
        /// </summary> 
        /// <param name="listDocumentId">Danh sách id tài liệu</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("get-list-coordinate")]
        [ProducesResponseType(typeof(ResponseObject<CoordinateFileModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCoordinate(List<Guid> listDocumentId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_COORDINATE);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_COORDINATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                string rs = "";
                foreach (var item in listDocumentId)
                {
                    rs += item.ToString() + ", ";
                }
                // u.SystemLog.Description = $"Lấy tọa độ vùng ký hợp đồng {rs}";

                var result = await _handler.GetListCoordinate(listDocumentId, u.SystemLog);



                return result;
            });
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;

namespace NetCore.API
{
    /// <summary>
    /// Module ký hash
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/sign-hash")]
    [ApiExplorerSettings(GroupName = "eContract - 10. Sign Hash")]
    public class SignHashController : ApiControllerBase
    {
        private readonly ISignHashHandler _handler;

        public SignHashController(ISignHashHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Tạo chuỗi hash để ký file
        /// </summary> 
        /// <param name="model">Model tạo chuỗi hash</param>
        /// <returns>Chuỗi hash dùng để ký</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("hash-files")]
        [ProducesResponseType(typeof(ResponseObject<HashFileResponseDataModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> HashFlies([FromBody] NetHashFilesRequestModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HASH_DOC_USBTOKEN);
                u.SystemLog.ActionName = LogConstants.ACTION_HASH_DOC_USBTOKEN;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.HashFiles(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        /// <summary>
        /// Đóng chữ ký vào file
        /// </summary> 
        /// <param name="model">Model chữ ký</param>
        /// <returns>Trạng thái đóng chữ ký</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("attach-files")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResult>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AttachFiles([FromBody] NetAttachFileModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ATTACH_SIGN_TO_FILE);
                u.SystemLog.ActionName = LogConstants.ACTION_ATTACH_SIGN_TO_FILE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.AttachFiles(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        /// <summary>
        /// Ký HSM
        /// </summary> 
        /// <param name="model">Model ký</param>
        /// <returns>Trạng thái ký</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("sign-hsm")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResult>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignHSM([FromBody] SignHSMClientModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_HSM);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_HSM;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.SignHSMFiles(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        [AllowAnonymous, HttpPost, Route("sign-adss")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResult>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignADSSFiles([FromBody] SignADSSClientModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_ADSS);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_ADSS;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;
                model.IsAutoSign = false;
                var result = await _handler.SignADSSFiles(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        /// <summary>
        /// Ký điện tử
        /// </summary> 
        /// <param name="model">Model ký</param>
        /// <returns>Trạng thái ký</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("electtronic-sign")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResult>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ElectronicSign([FromBody] ElectronicSignClientModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.ElectronicSignFiles(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        #region Ký từ signle page truy cập từ email

        /// <summary>
        /// Ký điện tử cho sigle sign page
        /// </summary> 
        /// <param name="model">Model ký</param>
        /// <returns>Trạng thái ký</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("electtronic-sign-from-sign-page")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResult>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ElectronicSignFromSignPage([FromBody] ElectronicSignClientModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
                u.SystemLog.ActionName = LogConstants.ACTION_SIGN_DOC_DIGITAL;
                u.SystemLog.Device = LogConstants.DEVICE_WEB_FROM_EMAIL;

                var result = await _handler.ElectronicSignFiles(model, u.SystemLog, u.UserId, true);

                return result;
            });
        }


        /// <summary>
        /// Tạo chuỗi hash để ký file cho sigle sign page
        /// </summary> 
        /// <param name="model">Model tạo chuỗi hash</param>
        /// <returns>Chuỗi hash dùng để ký</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("hash-files-from-sign-page")]
        [ProducesResponseType(typeof(ResponseObject<HashFileResponseDataModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> HashFliesFromSignPage([FromBody] NetHashFromSinglePageModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_HASH_DOC_USBTOKEN);
                u.SystemLog.ActionName = LogConstants.ACTION_HASH_DOC_USBTOKEN;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.HashFilesFromSinglePage(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        /// <summary>
        /// Đóng chữ ký vào file cho sigle sign page
        /// </summary> 
        /// <param name="model">Model chữ ký</param>
        /// <returns>Trạng thái đóng chữ ký</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("attach-files-from-sign-page")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentSignedResult>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AttachFilesFromSignPage([FromBody] NetAttachFromSinglePageModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ATTACH_SIGN_TO_FILE);
                u.SystemLog.ActionName = LogConstants.ACTION_ATTACH_SIGN_TO_FILE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.AttachFilesFromSinglePage(model, u.SystemLog, u.UserId);

                return result;
            });
        }

        #endregion

    }
}

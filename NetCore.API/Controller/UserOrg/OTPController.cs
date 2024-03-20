using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;

namespace NetCore.API.Controller
{
    /// <summary>
    /// Module OTP
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/otp")]
    [ApiExplorerSettings(GroupName = "User&Org - 03. OTP")]
    public class OTPController : ApiControllerBase
    {
        private readonly IOTPHandler _handler;
        public OTPController(IOTPHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Generate OTP
        /// </summary>
        /// <param name="userName">Tài khoản</param>
        /// <returns>Id người dùng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("generate")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateOTP(string userName)
        {
            var result = await _handler.GenerateOTP(userName);
            return Helper.TransformData(new ResponseObject<string>(result));
        }

        /// <summary>
        /// Validate OTP
        /// </summary>
        /// <param name="model">Tài khoản, mã OTP</param>
        /// <returns>Id người dùng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("validate")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateOTP([FromBody] ValidateOTPModel model)
        {
            var result = await _handler.ValidateOTP(model);
            return Helper.TransformData(new ResponseObject<bool>(result));
        }
    }
}

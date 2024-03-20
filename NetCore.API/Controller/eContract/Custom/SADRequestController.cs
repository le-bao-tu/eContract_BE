using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module hợp đồng - VietCredit
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/sad")]
    [ApiExplorerSettings(GroupName = "eContract - 08.3 Contract - SAD Request controller")]
    public class SADRequestController : ApiControllerBase
    {
        private readonly ISignHashHandler _signHashHandler;
        public SADRequestController(ISignHashHandler signHashHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _signHashHandler = signHashHandler;
        }

        /// <summary>
        /// Ký hợp đồng
        /// </summary> 
        /// <param name="model"></param>
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("request")]
        [ProducesResponseType(typeof(ResponseObject<DocumentInfoFrom3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SADConfirmSignDocumentFromApp([FromBody] SADReqeustSignConfirmModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CONFIRM_SIGN_DOCUMENT_ESIGN);
                u.SystemLog.ActionName = LogConstants.ACTION_CONFIRM_SIGN_DOCUMENT_ESIGN;
                u.SystemLog.Device = LogConstants.DEVICE_ESIGN;

                var result = await _signHashHandler.SADConfirmSignDocumentFromApp(model, u.SystemLog);

                return result;
            });
        }

    }
}

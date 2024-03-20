using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API.Controller.ADSS
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/adss")]
    [ApiExplorerSettings(GroupName = "ADSS Hash")]
    public class ADSSCoreController : ApiControllerBase
    {
        private readonly IADSSCoreHandler _adssHandler;

        public ADSSCoreController(IADSSCoreHandler adssHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _adssHandler = adssHandler;
        }

        [HttpPost, Route("sign-adss-with-existing-blank-signature-field")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignADSSWithExistingBlankSignatureField([FromBody] ADSSCoreModelRequest model)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                var result = _adssHandler.SignADSSWithNoExistingBlankSignatureField(model, u.SystemLog);
                return result;
            });
        }

        [HttpPost, Route("sign-adss-with-existing-signature-field")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignADSSWithExistingSignatureField([FromBody] ADSSCoreModelRequest model)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                var result = _adssHandler.SignADSSWithExistingSignatureField(model, u.SystemLog);
                return result;
            });
        }

        [HttpPost, Route("sign-adss-existing-blank-signature-field")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignADSSExistingBlankSignatureField([FromBody] ADSSCoreModelRequest model)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                var result = _adssHandler.SignADSSExistingBlankSignatureField(model, u.SystemLog);
                return result;
            });
        }
    }
}

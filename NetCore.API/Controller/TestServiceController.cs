using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using NetCore.Business;
using Microsoft.AspNetCore.Authorization;
using NetCore.Shared;
using Microsoft.AspNetCore.Http;

namespace NetCore.API.Controller
{
    /// <summary>
    /// Module test service
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/test-service")]
    [ApiExplorerSettings(GroupName = "00. Test Service", IgnoreApi = false)]
    public class TestServiceController : ApiControllerBase
    {
        private readonly ITestServiceHandler _testHandler;

        public TestServiceController(ITestServiceHandler testHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _testHandler = testHandler;
        }

        [AllowAnonymous, HttpGet, Route("test-postgree-sql")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestPostgreeSQL()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestPostgreeSQL(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-mongo-db")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestMongoDB()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestMongoDB(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-minio")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestMinIO()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestMinIO(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-hash-attach")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestServiceHashAttach()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestServiceHashAttach(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpPost, Route("test-gateway-notify-sms")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestGatewaySMS([FromBody]TestServiceGatewaySMS gatewaySMS)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestGatewaySMS(gatewaySMS, u.SystemLog);
            });
        }

        [AllowAnonymous, HttpPost, Route("test-gateway-notify-notify")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestGatewayNotify([FromBody] TestServiceGatewayNotify gatewayNotify)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestGatewayNotify(gatewayNotify, u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-service-convert-pdf-to-png")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestServiceConvertPdfToPng()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestServiceConvertPdfToPng(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-service-convert-pdfa")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestServiceConvertPDFA()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestServiceConvertPDFA(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-service-ciam")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestServiceCIAM()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestServiceCIAM(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpGet, Route("test-service-otp")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestServiceOTP()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestServiceOTP(u.SystemLog);
            });
        }

        [AllowAnonymous, HttpPost, Route("test-service-adss")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestServiceADSS([FromBody] TestADSSModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _testHandler.TestServiceADSS(model, u.SystemLog);
            });
        }
    }
}

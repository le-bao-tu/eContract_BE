using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module khởi tạo ứng dụng
    /// </summary>
    [ApiVersion("1.0")]
    //[ApiController]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/seed-data")]
    [ApiExplorerSettings(GroupName = "001. Seed Data")]
    public class SeedDataController : ApiControllerBase
    {
        private readonly ISeedDataHandler _handler;
        public SeedDataController(ISeedDataHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Khởi tạo dữ liệu ứng dụng hệ thống
        /// </summary>
        /// <returns>Trạng thái khởi tạo</returns> 
        /// <response code="200">Thành công</response>
        /// <response code="201">Dữ liệu đã tồn tại</response>
        /// <response code="500">Lỗi hệ thống</response>
        [AllowAnonymous, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_SEED_DATA);
                u.SystemLog.ActionName = LogConstants.ACTION_SEED_DATA;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.InitDataSysApplication(u.SystemLog);

                return result;
            });
        }
    }
}

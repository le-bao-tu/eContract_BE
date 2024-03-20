using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Threading.Tasks;

namespace NetCore.API.Controller
{
    /// <summary>
    /// Module cấu hình hiển thi
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/org-config")]
    [ApiExplorerSettings(GroupName = "eContract - 05. Organization Config (cấu hình hiển thị đơn vị)")]
    public class OrgConfigController : ApiControllerBase
    {
        private readonly IOrganizationConfigHandler _handler;
        public OrgConfigController(IOrganizationConfigHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới cấu hình hiển thị
        /// </summary>
        /// <param name="model">Thông tin cấu hình hiển thi</param>
        /// <returns>Id cấu hình hiển thi</returns> 
        /// <response code="200">Thành công</response>
        [ HttpPost, Route("create-or-update")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOrUpdate([FromBody] OrganizationConfigModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORG_CONFIG_CREATE_OR_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORG_CONFIG_CREATE_OR_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.CreateOrUpdate(model,u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin cấu hình hiển thi theo OrganizationId
        /// </summary> 
        /// <param name="id">Id đơn vị</param>
        /// <returns>Thông tin chi tiết cấu hình hiển thi</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<OrganizationConfigModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }
    }
}

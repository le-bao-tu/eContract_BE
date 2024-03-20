using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module trang chủ
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/dashboard")]
    [ApiExplorerSettings(GroupName = "eContract - 01. Dashboard")]
    public class DashboardController : ApiControllerBase
    {
        private readonly IDashboardHandler _handler;
        public DashboardController(IDashboardHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Lấy danh sách số lượng hợp đồng theo trạng thái
        /// </summary>
        /// <returns>Danh sách số lượng hợp đồng theo trạng thái</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("number-document-by-status")]
        [ProducesResponseType(typeof(ResponseObject<DocumentStatusModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNumberDocumentStatus()
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                var result = _handler.GetNumberDocumentStatus(u.UserId, u.OrganizationId);
                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin dashboard gồm số hợp đồng theo trạng thái và gói dịch vụ
        /// </summary>
        /// <returns>Danh sách số lượng hợp đồng theo trạng thái, số lượng loại ký</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("number-document-by-status-package-service")]
        [ProducesResponseType(typeof(ResponseObject<DocumentStatusModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardInfo([FromBody] DashboardRequest requestModel)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                requestModel.CurrentUserId = u.UserId;
                var result = _handler.GetDashboardInfo(requestModel, u.UserId, u.OrganizationId, u.SystemLog);
                return result;
            });
        }
    }
}

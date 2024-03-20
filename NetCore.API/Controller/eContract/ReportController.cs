using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System.Threading.Tasks;

namespace NetCore.API.Controller.eContract
{
    /// <summary>
    /// Module báo cáo
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/report")]
    [ApiExplorerSettings(GroupName = "eContract - 01. Report")]
    public class ReportController : ApiControllerBase
    {
        private readonly ISystemLogHandler _logHandler;

        public ReportController(ISystemLogHandler logHandler) : base(logHandler)
        {
            _logHandler = logHandler;
        }

        /// <summary>
        /// Lấy dữ liệu thống kê liên quan đến hợp đồng theo đơn vị
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("get-report-document-by-org")]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportDocumentByOrgID(OrgReportFilterModel filter)
        {
            return await ExecuteFunction((RequestUser u) =>
            {
                var result = _logHandler.GetReportDocumentByOrgID(filter);
                return result;
            });
        }
    }
}

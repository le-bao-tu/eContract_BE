using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.DataLog;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module nhật ký hệ thống
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/system-log")]
    [ApiExplorerSettings(GroupName = "Log - 01. System Log (Nhật ký hệ thống)")]
    public class SystemLogController : ApiControllerBase
    {
        private readonly ISystemLogHandler _handler;
        public SystemLogController(ISystemLogHandler handler) : base(handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Lấy thông tin nhật ký hệ thống theo id
        /// </summary> 
        /// <param name="id">Id nhật ký hệ thống</param>
        /// <returns>Thông tin chi tiết nhật ký hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<SystemLog>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id); ;

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách nhật ký hệ thống theo điều kiện lọc
        /// </summary> 
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách nhật ký hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<SystemLog>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] SystemLogQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = !string.IsNullOrEmpty(filter.OrganizationId) ? filter.OrganizationId : u.OrganizationId.ToString();
                var result = await _handler.Filter(filter); ;

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách nhật ký hợp đồng theo hợp đồng
        /// </summary> 
        /// <param name="documentId">Id hợp đồng</param>
        /// <returns>Danh sách nhật ký hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("get-by-document")]
        [ProducesResponseType(typeof(ResponseObject<List<SystemLog>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByDocument([FromQuery] string documentId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.FilterByDocument(documentId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách Action Code For Combobox
        /// </summary>      
        /// <returns>Danh sách Action Code và Action Name</returns>    
        [Authorize, HttpGet, Route("get-all-action-code-for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<ActionCodeForComboboxModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActionCodeForCombobox()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetActionCodeForCombobox();

                return result;
            });
        }
    }
}

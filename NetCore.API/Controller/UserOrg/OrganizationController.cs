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
    /// Module đơn vị
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/organization")]
    [ApiExplorerSettings(GroupName = "User&Org - 01. Organization (Đơn vị)")]
    public class OrganizationController : ApiControllerBase
    {
        private readonly IOrganizationHandler _handler;
        public OrganizationController(IOrganizationHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới đơn vị
        /// </summary>
        /// <param name="model">Thông tin đơn vị</param>
        /// <returns>Id đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] OrganizationCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORGANIZATION_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORGANIZATION_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật đơn vị
        /// </summary> 
        /// <param name="model">Thông tin đơn vị cần cập nhật</param>
        /// <returns>Id đơn vị đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] OrganizationUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORGANIZATION_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORGANIZATION_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin đơn vị theo id
        /// </summary> 
        /// <param name="id">Id đơn vị</param>
        /// <returns>Thông tin chi tiết đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<OrganizationModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin đơn vị thuộc người dùng hiện tại
        /// </summary> 
        /// <param name="id">Id đơn vị</param>
        /// <returns>Thông tin chi tiết đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("get-org-info-current-user")]
        [ProducesResponseType(typeof(ResponseObject<OrgLayoutModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrgInfoByCurrentUser()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetOrgHeaderInfo(u.OrganizationId);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin đơn vị theo id
        /// </summary> 
        /// <param name="id">Id đơn vị</param>
        /// <returns>Thông tin chi tiết đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("detail-from-service")]
        [ProducesResponseType(typeof(ResponseObject<OrganizationForServiceModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDettailForServiceById(Guid id)
        {
            var result = await _handler.GetDettailForServiceById(id);

            return Helper.TransformData(result);
        }

        /// <summary>
        /// Lấy danh sách đơn vị theo điều kiện lọc
        /// </summary> 
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<OrganizationBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] OrganizationQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter);

                return result;
            });
        }
        /// <summary>
        /// Lấy tất cả danh sách đơn vị
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<OrganizationBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                OrganizationQueryFilter filter = new OrganizationQueryFilter()
                {
                    TextSearch = ts,
                    PageNumber = null,
                    PageSize = null
                };
                var result = await _handler.Filter(filter);

                return result;
            });
        }

        /// <summary>
        /// Xóa đơn vị
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id đơn vị</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_ORGANIZATION_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_ORGANIZATION_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách đơn vị cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<OrganizationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.UserId, u.OrganizationId, count, ts);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách đơn vị hiện tại của user gồm đơn vị cha và con
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox-current-org-of-user")]
        [ProducesResponseType(typeof(ResponseObject<List<OrganizationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxCurrentOfUser(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxCurrentOrgOfUser(u.UserId, u.OrganizationId, count, ts);

                return result;
            });
        }

        /// <summary>
        /// Lấy All danh sách đơn vị cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách đơn vị</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox-all")]
        [ProducesResponseType(typeof(ResponseObject<List<OrganizationSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxAll(bool? status = null, int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxAll(status, count, ts);

                return result;
            });
        }
    }
}

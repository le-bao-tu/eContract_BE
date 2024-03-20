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
    /// Module chức vụ
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/position")]
    [ApiExplorerSettings(GroupName = "Catalog - 05. Position (Chức vụ)")]
    public class PositionController : ApiControllerBase
    {
        private readonly IPositionHandler _handler;
        public PositionController(IPositionHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới chức vụ
        /// </summary>
        /// <param name="model">Thông tin chức vụ</param>
        /// <returns>Id chức vụ</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody]PositionCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_POSITION_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_POSITION_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Thêm mới chức vụ theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin chức vụ</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("create-many")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMany([FromBody]List<PositionCreateModel> list)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_POSITION_CREATE_MANY);
                u.SystemLog.ActionName = LogConstants.ACTION_POSITION_CREATE_MANY;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                foreach (var item in list)
                {
                    item.CreatedUserId = u.UserId;
                    item.ApplicationId = u.ApplicationId;
                    item.OrganizationId = u.OrganizationId;
                }
                var result = await _handler.CreateMany(list, u.SystemLog);
                return result;
            });
        }
        /// <summary>
        /// Cập nhật chức vụ
        /// </summary> 
        /// <param name="model">Thông tin chức vụ cần cập nhật</param>
        /// <returns>Id chức vụ đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody]PositionUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_POSITION_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_POSITION_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin chức vụ theo id
        /// </summary> 
        /// <param name="id">Id chức vụ</param>
        /// <returns>Thông tin chi tiết chức vụ</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<PositionModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách chức vụ theo điều kiện lọc
        /// </summary> 
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách chức vụ</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<PositionBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody]PositionQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Lấy tất cả danh sách chức vụ
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách chức vụ</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<PositionBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                PositionQueryFilter filter = new PositionQueryFilter()
                {
                    TextSearch = ts,
                    PageNumber = null,
                    PageSize = null
                };
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Xóa chức vụ
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id chức vụ</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody]List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_POSITION_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_POSITION_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách chức vụ cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách chức vụ</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts, u.OrganizationId);

                return result;
            });
        }
    }
}

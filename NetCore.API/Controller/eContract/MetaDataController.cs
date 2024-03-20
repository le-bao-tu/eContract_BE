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
    /// Module thông tin trong hợp đồng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/meta-data")]
    [ApiExplorerSettings(GroupName = "eContract - 03. Meta Data (Thông tin trong hợp đồng)")]
    public class MetaDataController : ApiControllerBase
    {
        private readonly IMetaDataHandler _handler;
        public MetaDataController(IMetaDataHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới thông tin trong hợp đồng
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     Kiểu dữ liệu: (DataType)
        ///         1: Kiểu số
        ///         2: Kiểu chữ
        ///         3. Kiểu ngày tháng
        ///         4. Kiểu radio
        ///         5. Kiểu checkbox
        ///     
        ///     {
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }
        /// </remarks>
        /// <param name="model">Thông tin thông tin trong hợp đồng</param>
        /// <returns>Id thông tin trong hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] MetaDataCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_META_DATA_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_META_DATA_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Thêm mới thông tin trong hợp đồng theo danh sách
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         {
        ///             "code": "Code",
        ///             "name": "Name",
        ///             "status": true,
        ///             "description": "Description",
        ///             "order": 1
        ///         }   
        ///     ]
        /// </remarks>
        /// <param name="list">Danh sách thông tin thông tin trong hợp đồng</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("create-many")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMany([FromBody] List<MetaDataCreateModel> list)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_META_DATA_CREATE_MANY);
                u.SystemLog.ActionName = LogConstants.ACTION_META_DATA_CREATE_MANY;
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
        /// Cập nhật thông tin trong hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }   
        /// </remarks>
        /// <param name="model">Thông tin thông tin trong hợp đồng cần cập nhật</param>
        /// <returns>Id thông tin trong hợp đồng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] MetaDataUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_META_DATA_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_META_DATA_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin thông tin trong hợp đồng theo id
        /// </summary> 
        /// <param name="id">Id thông tin trong hợp đồng</param>
        /// <returns>Thông tin chi tiết thông tin trong hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<MetaDataModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách thông tin trong hợp đồng theo điều kiện lọc
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "textSearch": "",
        ///         "pageSize": 20,
        ///         "pageNumber": 1
        ///     }
        /// </remarks>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách thông tin trong hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<MetaDataBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] MetaDataQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }
        /// <summary>
        /// Lấy tất cả danh sách thông tin trong hợp đồng
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách thông tin trong hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<MetaDataBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                MetaDataQueryFilter filter = new MetaDataQueryFilter()
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
        /// Xóa thông tin trong hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id thông tin trong hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_META_DATA_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_META_DATA_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách thông tin trong hợp đồng cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách thông tin trong hợp đồng</returns> 
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

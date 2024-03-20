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
    /// Module ứng dụng hệ thống
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/system-application")]
    [ApiExplorerSettings(GroupName = "02. System Application (Ứng dụng hệ thống)", IgnoreApi = true)]
    public class SystemApplicationController : ApiControllerBase
    {
        private readonly ISystemApplicationHandler _handler;
        public SystemApplicationController(ISystemApplicationHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới ứng dụng hệ thống
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }
        /// </remarks>
        /// <param name="model">Thông tin ứng dụng hệ thống</param>
        /// <returns>Id ứng dụng hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody]SystemApplicationCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model);

                return result;
            });
        }

        /// <summary>
        /// Thêm mới ứng dụng hệ thống theo danh sách
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
        /// <param name="list">Danh sách thông tin ứng dụng hệ thống</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("create-many")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMany([FromBody]List<SystemApplicationCreateModel> list)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                foreach (var item in list)
                {
                    item.CreatedUserId = u.UserId;
                    item.ApplicationId = u.ApplicationId;
                }
                var result = await _handler.CreateMany(list);
                return result;
            });
        }
        /// <summary>
        /// Cập nhật ứng dụng hệ thống
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
        /// <param name="model">Thông tin ứng dụng hệ thống cần cập nhật</param>
        /// <returns>Id ứng dụng hệ thống đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody]SystemApplicationUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin ứng dụng hệ thống theo id
        /// </summary> 
        /// <param name="id">Id ứng dụng hệ thống</param>
        /// <returns>Thông tin chi tiết ứng dụng hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<SystemApplicationModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách ứng dụng hệ thống theo điều kiện lọc
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
        /// <returns>Danh sách ứng dụng hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<SystemApplicationBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody]SystemApplicationQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter);

                return result;
            });
        }
        /// <summary>
        /// Lấy tất cả danh sách ứng dụng hệ thống
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách ứng dụng hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<SystemApplicationBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                SystemApplicationQueryFilter filter = new SystemApplicationQueryFilter()
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
        /// Xóa ứng dụng hệ thống
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id ứng dụng hệ thống</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody]List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Delete(listId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách ứng dụng hệ thống cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách ứng dụng hệ thống</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(count, ts);

                return result;
            });
        }
    }
}

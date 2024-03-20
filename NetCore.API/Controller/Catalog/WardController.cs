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
    /// Module phường xã
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/ward")]
    [ApiExplorerSettings(GroupName = "Catalog - 04. Ward (Phường xã)")]
    public class WardController : ApiControllerBase
    {
        private readonly IWardHandler _handler;
        public WardController(IWardHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới phường xã
        /// </summary>
        /// <param name="model">Thông tin phường xã</param>
        /// <returns>Id phường xã</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] WardCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WARD_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_WARD_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới phường xã theo danh sách
        ///// </summary>
        ///// <param name="list">Danh sách thông tin phường xã</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<WardCreateModel> list)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        foreach (var item in list)
        //        {
        //            item.CreatedUserId = u.UserId;
        //            item.ApplicationId = u.ApplicationId;
        //        }
        //        var result = await _handler.CreateMany(list);
        //        return result;
        //    });
        //}

        /// <summary>
        /// Cập nhật phường xã
        /// </summary> 
        /// <param name="model">Thông tin phường xã cần cập nhật</param>
        /// <returns>Id phường xã đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] WardUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WARD_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_WARD_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin phường xã theo id
        /// </summary> 
        /// <param name="id">Id phường xã</param>
        /// <returns>Thông tin chi tiết phường xã</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<WardModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách phường xã theo điều kiện lọc
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
        /// <returns>Danh sách phường xã</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<WardBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] WardQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy tất cả danh sách phường xã
        ///// </summary> 
        ///// <param name="ts">Từ khóa tìm kiếm</param>
        ///// <returns>Danh sách phường xã</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpGet, Route("all")]
        //[ProducesResponseType(typeof(ResponseObject<List<WardBaseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetAll(string ts = null)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        WardQueryFilter filter = new WardQueryFilter()
        //        {
        //            TextSearch = ts,
        //            PageNumber = null,
        //            PageSize = null
        //        };
        //        var result = await _handler.Filter(filter);

        //        return result;
        //    });
        //}

        /// <summary>
        /// Xóa phường xã
        /// </summary> 
        /// <param name="listId">Danh sách Id phường xã</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WARD_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_WARD_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách phường xã cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách phường xã</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<WardSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, count, ts);

                return result;
            });
        }
    }
}

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
    /// Module quận huyện
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/district")]
    [ApiExplorerSettings(GroupName = "Catalog - 03. District (Quận huyện)")]
    public class DistrictController : ApiControllerBase
    {
        private readonly IDistrictHandler _handler;

        public DistrictController(IDistrictHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới quận huyện
        /// </summary>
        /// <param name="model">Thông tin quận huyện</param>
        /// <returns>Id quận huyện</returns> 
        /// <response code="200">Thành công</response>
        [HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] DistrictCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DISTRICT_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DISTRICT_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới quận huyện theo danh sách
        ///// </summary>
        ///// <param name="list">Danh sách thông tin quận huyện</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<DistrictCreateModel> list)
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
        /// Cập nhật quận huyện
        /// </summary> 
        /// <param name="model">Thông tin quận huyện cần cập nhật</param>
        /// <returns>Id quận huyện đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] DistrictUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DISTRICT_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DISTRICT_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin quận huyện theo id
        /// </summary> 
        /// <param name="id">Id quận huyện</param>
        /// <returns>Thông tin chi tiết quận huyện</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<DistrictModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {               
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách quận huyện theo điều kiện lọc
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
        /// <returns>Danh sách quận huyện</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<DistrictBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] DistrictQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy tất cả danh sách quận huyện
        ///// </summary> 
        ///// <param name="ts">Từ khóa tìm kiếm</param>
        ///// <returns>Danh sách quận huyện</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpGet, Route("all")]
        //[ProducesResponseType(typeof(ResponseObject<List<DistrictBaseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetAll(string ts = null)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        DistrictQueryFilter filter = new DistrictQueryFilter()
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
        /// Xóa quận huyện
        /// </summary> 
        /// <param name="listId">Danh sách Id quận huyện</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DISTRICT_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_DISTRICT_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách quận huyện cho combobox
        /// </summary>
        /// <param name="provinceId"></param>
        /// <param name="count"></param>
        /// <param name="ts"></param>
        /// <returns>Danh sách quận huyện</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<DistrictSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(Guid? provinceId = null, int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(provinceId, u.SystemLog, count, ts);

                return result;
            });
        }
    }
}

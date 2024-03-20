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
    /// Module quốc gia
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/country")]
    [ApiExplorerSettings(GroupName = "Catalog - 01. Country (Quốc gia)")]
    public class CountryController : ApiControllerBase
    {
        private readonly ICountryHandler _handler;
        public CountryController(ICountryHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới quốc gia
        /// </summary>
        /// <param name="model">Thông tin quốc gia</param>
        /// <returns>Id quốc gia</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody]CountryCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_COUNTRY_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_COUNTRY_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới quốc gia theo danh sách
        ///// </summary>
        ///// <param name="list">Danh sách thông tin quốc gia</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<CountryCreateModel> list)
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
        /// Cập nhật quốc gia
        /// </summary> 
        /// <param name="model">Thông tin quốc gia cần cập nhật</param>
        /// <returns>Id quốc gia đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody]CountryUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_COUNTRY_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_COUNTRY_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin quốc gia theo id
        /// </summary> 
        /// <param name="id">Id quốc gia</param>
        /// <returns>Thông tin chi tiết quốc gia</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<CountryModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách quốc gia theo điều kiện lọc
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
        /// <returns>Danh sách quốc gia</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<CountryBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody]CountryQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy tất cả danh sách quốc gia
        ///// </summary> 
        ///// <param name="ts">Từ khóa tìm kiếm</param>
        ///// <returns>Danh sách quốc gia</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpGet, Route("all")]
        //[ProducesResponseType(typeof(ResponseObject<List<CountryBaseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetAll(string ts = null)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        CountryQueryFilter filter = new CountryQueryFilter()
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
        /// Xóa quốc gia
        /// </summary> 
        /// <param name="listId">Danh sách Id quốc gia</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody]List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_COUNTRY_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_COUNTRY_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách quốc gia cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách quốc gia</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<CountrySelectItemModel>>), StatusCodes.Status200OK)]
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

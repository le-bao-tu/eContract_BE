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
    /// Module tỉnh thành
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/province")]
    [ApiExplorerSettings(GroupName = "Catalog - 02. Province (Tỉnh thành)")]
    public class ProvinceController : ApiControllerBase
    {
        private readonly IProvinceHandler _handler;
        public ProvinceController(IProvinceHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới tỉnh thành
        /// </summary>
        /// <param name="model">Thông tin tỉnh thành</param>
        /// <returns>Id tỉnh thành</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] ProvinceCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_PROVINCE_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_PROVINCE_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới tỉnh thành theo danh sách
        ///// </summary>
        ///// <param name="list">Danh sách thông tin tỉnh thành</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<ProvinceCreateModel> list)
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
        /// Cập nhật tỉnh thành
        /// </summary> 
        /// <param name="model">Thông tin tỉnh thành cần cập nhật</param>
        /// <returns>Id tỉnh thành đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] ProvinceUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_PROVINCE_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_PROVINCE_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin tỉnh thành theo id
        /// </summary> 
        /// <param name="id">Id tỉnh thành</param>
        /// <returns>Thông tin chi tiết tỉnh thành</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<ProvinceModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách tỉnh thành theo điều kiện lọc
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
        /// <returns>Danh sách tỉnh thành</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<ProvinceBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] ProvinceQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Lấy tất cả danh sách tỉnh thành
        ///// </summary> 
        ///// <param name="ts">Từ khóa tìm kiếm</param>
        ///// <returns>Danh sách tỉnh thành</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpGet, Route("all")]
        //[ProducesResponseType(typeof(ResponseObject<List<ProvinceBaseModel>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetAll(string ts = null)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        ProvinceQueryFilter filter = new ProvinceQueryFilter()
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
        /// Xóa tỉnh thành
        /// </summary> 
        /// <param name="listId">Danh sách Id tỉnh thành</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_PROVINCE_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_PROVINCE_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách tỉnh thành cho combobox
        /// </summary>
        /// <param name="districtId"></param>
        /// <param name="count"></param>
        /// <param name="ts"></param>
        /// <returns>Danh sasach tỉnh thành</returns>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<ProvinceSelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(Guid? districtId = null, int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(u.SystemLog, districtId, count, ts);

                return result;
            });
        }
    }
}

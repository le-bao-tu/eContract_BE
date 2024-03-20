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
    /// Module tài khoản email
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/email-account")]
    [ApiExplorerSettings(GroupName = "Email - 01. Email Account (Email)")]
    public class EmailAccountController : ApiControllerBase
    {
        private readonly IEmailAccountHandler _handler;
        public EmailAccountController(IEmailAccountHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới tài khoản email
        /// </summary>
        /// <param name="model">Thông tin tài khoản email</param>
        /// <returns>Id tài khoản email</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody]EmailAccountCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.Create(model);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới tài khoản email theo danh sách
        ///// </summary>
        ///// <param name="list">Danh sách thông tin tài khoản email</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<EmailAccountCreateModel> list)
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
        /// Cập nhật tài khoản email
        /// </summary> 
        /// <param name="model">Thông tin tài khoản email cần cập nhật</param>
        /// <returns>Id tài khoản email đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody]EmailAccountUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model);

                return result;
            });
        }


        /// <summary>
        /// Lấy thông tin tài khoản email theo id
        /// </summary> 
        /// <param name="id">Id tài khoản email</param>
        /// <returns>Thông tin chi tiết tài khoản email</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<EmailAccountModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách tài khoản email theo điều kiện lọc
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
        /// <returns>Danh sách tài khoản email</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<EmailAccountBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody]EmailAccountQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter);

                return result;
            });
        }

        /// <summary>
        /// Lấy tất cả danh sách tài khoản email
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách tài khoản email</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<EmailAccountBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                EmailAccountQueryFilter filter = new EmailAccountQueryFilter()
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
        /// Xóa tài khoản email
        /// </summary> 
        /// <param name="listId">Danh sách Id tài khoản email</param>
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
        /// Lấy danh sách tài khoản email cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách tài khoản email</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<EmailAccountSelectItemModel>>), StatusCodes.Status200OK)]
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

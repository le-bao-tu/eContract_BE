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
    /// Module danh sách email chờ gửi
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/queue-send-email")]
    [ApiExplorerSettings(GroupName = "Email - 02. Queue Send Email (Email chờ gửi)")]
    public class QueueSendEmailController : ApiControllerBase
    {
        private readonly IQueueSendEmailHandler _handler;
        public QueueSendEmailController(IQueueSendEmailHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới danh sách email chờ gửi
        /// </summary>
        /// <param name="model">Thông tin danh sách email chờ gửi</param>
        /// <returns>Id danh sách email chờ gửi</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody]QueueSendEmailCreateModel model)
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
        ///// Thêm mới danh sách email chờ gửi theo danh sách
        ///// </summary>
        ///// <param name="list">Danh sách thông tin danh sách email chờ gửi</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<QueueSendEmailCreateModel> list)
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
        /// Tự động gửi mail ngay khi khởi tạo
        /// </summary>
        /// <param name="model">Model mail cần gửi</param> 
        /// <returns>Thông tin Email đã được gửi</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("send-mail-now")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMailNow(QueueSendEmailCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.SendMailNow(model);

                return result;
            });
        }

        /// <summary>
        /// Tự động gửi mail chưa gửi trong queue
        /// </summary> 
        /// <param name="id">Id danh sách email chờ gửi</param>
        /// <returns>Thông tin chi tiết danh sách email chờ gửi</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("send-mail")]
        [ProducesResponseType(typeof(ResponseObject<List<Guid>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMailInQueue()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.SendMailInQueue();

                return result;
            });
        }
    }
}

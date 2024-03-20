using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API.Controller
{
    /// <summary>
    /// Module quyền người dùng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/user-role")]
    [ApiExplorerSettings(GroupName = "eContract - 06. User Role (quyền người dùng)")]
    public class UserRoleController : ApiControllerBase
    {
        private readonly IUserRoleHandler _handler;
        public UserRoleController(IUserRoleHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới quyền người dùng
        /// </summary>
        /// <param name="model">Thông tin quyền người dùng</param>
        /// <returns>Id quyền người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("add-role")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] UserRoleCreateOrUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.CreateOrUpdate(model);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin quyền người dùng theo id
        /// </summary> 
        /// <param name="id">Id quyền người dùng</param>
        /// <returns>Thông tin chi tiết quyền người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<UserRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetByUserId(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin quyền người dùng theo id
        /// </summary> 
        /// <returns>Thông tin chi tiết quyền người dùng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("owner")]
        [ProducesResponseType(typeof(ResponseObject<UserRoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByIdOwner()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetByUserId(u.UserId);

                return result;
            });
        }
    }
}

using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý quyền người dùng
    /// </summary>
    public interface IUserRoleHandler
    {
        /// <summary>
        /// Thêm mới quyền người dùng
        /// </summary>
        /// <param name="model">Model thêm mới quyền người dùng</param>
        /// <returns>Id quyền người dùng</returns>
        Task<Response> CreateOrUpdate(UserRoleCreateOrUpdateModel model);
        /// <summary>
        /// Lấy quyền người dùng theo Id
        /// </summary>
        /// <param name="id">Id quyền người dùng</param>
        /// <returns>Thông tin quyền người dùng</returns>
        Task<Response> GetByUserId(Guid id);

        /// <summary>
        /// Lấy quyền người dùng theo Id
        /// </summary>
        /// <param name="id">Id quyền người dùng</param>
        /// <returns>Thông tin quyền người dùng</returns>
        Task<UserRoleModel> GetUserRoleById(Guid id);
    }
}

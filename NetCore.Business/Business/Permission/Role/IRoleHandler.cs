using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface IRoleHandler
    {
        /// <summary>
        /// Thêm mới thông tin nhóm người dùng
        /// </summary>
        /// <param name="model">Model thêm mới nhóm người dùng</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id nhóm người dùng</returns>
        Task<Response> Create(RoleCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật thông tin nhóm người dùng
        /// </summary>
        /// <param name="model">Model cập nhật nhóm người dùng</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id nhóm người dùng</returns>
        Task<Response> Update(RoleUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật phân quyền dữ liệu theo nhóm người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> UpdateDataPermission(UpdateDataPermissionModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách quyền người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetDataPermission(GetDataPermissionModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa thông tin nhóm người dùng
        /// </summary>
        /// <param name="listId">Danh sách Id nhóm người dùng</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Danh sách nhóm người dùng theo điều kiện lọc
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> Filter(RoleQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy nhóm người dùng theo Id
        /// </summary>
        /// <param name="id">Id nhóm người dùng</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Thông tin nhóm người dùng</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách nhóm người dùng cho combobox
        /// </summary>
        /// <param name="systemLog">Ghi log</param>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách nhóm người dùng cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "");

        Task<List<RoleSelectItemModel>> GetListRoleFromCache(Guid? orgId = null);

        Task<ResultGetDataPermissionModel> GetRoleDataPermissionFromCacheByListIdAsync(List<Guid> listId);

        /// <summary>
        /// Cập nhật Right theo Role
        /// </summary>
        /// <param name="model">Model gồm RoleId và List Right Id</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id nhóm người dùng</returns>
        Task<Response> UpdateRightByRole(UpdateRightByRoleModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách Right Id theo Role Id
        /// </summary>
        /// <param name="model">Model gồm RoleId </param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Danh sách Id phân quyền chức năng</returns>
        Task<Response> GetListRightIdByRole(GetListRightIdByRoleModel model, SystemLogModel systemLog);

        Task<List<Guid>> GetListRightIdByRoleFromCacheAsync(Guid roleId);

        /// <summary>
        /// Lấy danh sách Menu Id theo Role Id
        /// </summary>
        /// <param name="model">Model gồm RoleId </param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Danh sách NavigationId</returns>
        Task<Response> GetListNavigationByRole(GetListNavigationByRoleModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật Menu theo Role
        /// </summary>
        /// <param name="model">Model gồm RoleId và List Navigation Id</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id nhóm người dùng</returns>
        Task<Response> UpdateNavigationByRole(UpdateNavigationByRoleModel model, SystemLogModel systemLog);
    }
}

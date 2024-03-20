using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface INavigationHandler
    {
        /// <summary>
        /// Thêm mới thông tin Menu
        /// </summary>
        /// <param name="model">Model thêm mới Menu</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id Menu</returns>
        Task<Response> Create(NavigationCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật thông tin Menu
        /// </summary>
        /// <param name="model">Model cập nhật Menu</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id Menu</returns>
        Task<Response> Update(NavigationUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa thông tin Menu
        /// </summary>
        /// <param name="listId">Danh sách Id Menu</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Danh sách Menu theo điều kiện lọc
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> Filter(NavigationQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy Menu theo Id
        /// </summary>
        /// <param name="id">Id Menu</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Thông tin Menu</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách Menu cho combobox
        /// </summary>
        /// <param name="systemLog">Ghi log</param>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách Menu cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", int status = 1);

        Task<List<NavigationSelectItemModel>> GetListNavFromCacheAsync();
    }
}

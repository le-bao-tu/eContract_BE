using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface IRightHandler
    {
        /// <summary>
        /// Thêm mới thông tin phân quyền
        /// </summary>
        /// <param name="model">Model thêm mới phân quyền</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id phân quyền</returns>
        Task<Response> Create(RightCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật thông tin phân quyền
        /// </summary>
        /// <param name="model">Model cập nhật phân quyền</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Id phân quyền</returns>
        Task<Response> Update(RightUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa thông tin phân quyền
        /// </summary>
        /// <param name="listId">Danh sách Id phân quyền</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Danh sách phân quyền theo điều kiện lọc
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> Filter(RightQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy phân quyền theo Id
        /// </summary>
        /// <param name="id">Id phân quyền</param>
        /// <param name="systemLog">Ghi log</param>
        /// <returns>Thông tin phân quyền</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách phân quyền cho combobox
        /// </summary>
        /// <param name="systemLog">Ghi log</param>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách phân quyền cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "");

        Task<List<RightSelectItemModel>> GetListRightFromCacheAsync();
    }
}

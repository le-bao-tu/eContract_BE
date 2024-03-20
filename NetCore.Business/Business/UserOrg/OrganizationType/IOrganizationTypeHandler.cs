using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý loại đơn vị
    /// </summary>
    public interface IOrganizationTypeHandler
    {
        /// <summary>
        /// Tạo mới loại đơn vị
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<Response> Create(OrganizationTypeCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhập loại đơn vị
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<Response> Update(OrganizationTypeUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa loại đơn vị
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lọc theo điều kiện
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<Response> Filter(OrganizationTypeQueryFilter filter);

        /// <summary>
        /// Lấy dữ liệu theo id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Response> GetById(Guid id);

        /// <summary>
        /// Lấy danh sách loại đơn vị
        /// </summary>
        /// <param name="count"></param>
        /// <param name="textSearch"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        Task<Response> GetListCombobox(int count = 0, string textSearch = "", Guid? orgId = null);
    }
}

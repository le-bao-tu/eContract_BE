using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý đơn vị phòng ban
    /// </summary>
    public interface IOrganizationHandler
    {
        /// <summary>
        /// Thêm mới đơn vị phòng ban
        /// </summary>
        /// <param name="model">Model thêm mới đơn vị phòng ban</param>
        /// <returns>Id đơn vị phòng ban</returns>
        Task<Response> Create(OrganizationCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật đơn vị phòng ban
        /// </summary>
        /// <param name="model">Model cập nhật đơn vị phòng ban</param>
        /// <returns>Id đơn vị phòng ban</returns>
        Task<Response> Update(OrganizationUpdateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa đơn vị phòng ban
        /// </summary>
        /// <param name="listId">Danh sách Id đơn vị phòng ban</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách đơn vị phòng ban theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách đơn vị phòng ban</returns>
        Task<Response> Filter(OrganizationQueryFilter filter);

        /// <summary>
        /// Lấy đơn vị phòng ban theo Id
        /// </summary>
        /// <param name="id">Id đơn vị phòng ban</param>
        /// <returns>Thông tin đơn vị phòng ban</returns>
        Task<Response> GetById(Guid id);

        Task<OrganizationModel> GetOrgFromCache(Guid id);

        /// <summary>
        /// Lấy đơn vị phòng ban theo Id
        /// </summary>
        /// <param name="id">Id đơn vị phòng ban</param>
        /// <returns>Thông tin đơn vị phòng ban</returns>
        Task<Response> GetOrgHeaderInfo(Guid id);

        /// <summary>
        /// Lấy đơn vị gốc theo phòng ban theo Id
        /// </summary>
        /// <param name="id">Id đơn vị phòng ban</param>
        /// <returns>Thông tin đơn vị phòng ban</returns>
        Task<Response> GetRootByChidId(Guid id);

        /// <summary>
        /// Lấy đơn vị phòng ban theo mã đơn vị
        /// </summary>
        /// <param name="id">Id đơn vị phòng ban</param>
        /// <returns>Thông tin đơn vị phòng ban</returns>
        Task<OrganizationModel> GetByCode(string code);

        /// <summary>
        /// Lấy đơn vị phòng ban theo Id
        /// </summary>
        /// <param name="id">Id đơn vị phòng ban</param>
        /// <returns>Thông tin đơn vị phòng ban</returns>
        Task<Response> GetDettailForServiceById(Guid id);

        /// <summary>
        /// Lấy danh sách đơn vị phòng ban cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách đơn vị phòng ban cho combobox</returns>
        Task<Response> GetListCombobox(Guid userId, Guid organizationId, int count = 0, string textSearch = "");

        /// <summary>
        /// Lấy danh sách đơn vị theo id đơn vị cha
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        List<Guid> GetListChildOrgByParentID(Guid parentID);

        /// <summary>
        /// Lấy danh sách đơn vị cha và con của người dùng hiện tại
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="count"></param>
        /// <param name="textSearch"></param>
        /// <returns></returns>
        Task<Response> GetListComboboxCurrentOrgOfUser(Guid userId, Guid organizationId, int count = 0, string textSearch = "");

        Task<OrganizationModel> GetRootOrgModelByChidId(Guid id);

        /// <summary>
        /// Lấy all danh sách đơn vị phòng ban cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách all đơn vị phòng ban cho combobox</returns>
        Task<Response> GetListComboboxAll(bool? status = null, int count = 0, string textSearch = "");

        /// <summary>
        /// Lấy danh sách tất cả các đơn vị từ cache
        /// </summary>
        /// <returns></returns>
        Task<List<OrganizationSelectItemModel>> GetAllListOrgFromCacheAsync();        
    }
}

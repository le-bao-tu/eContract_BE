using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý cấu hình hiển thị đơn vị
    /// </summary>
    public interface IOrganizationConfigHandler
    {
        /// <summary>
        /// Thêm mới hoặc cập nhật hiển thị đơn vị
        /// </summary>
        /// <param name="model">Model thêm mới hoặc cập nhật hiển thị đơn vị</param>
        /// <returns>Id hiển thị đơn vị</returns>
        Task<Response> CreateOrUpdate(OrganizationConfigModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy cấu hình hiển thị đơn vị theo OrganizationId
        /// </summary>
        /// <param name="id">Id đơn vị</param>
        /// <returns>Thông tin cấu hình hiển thị đơn vị</returns>
        Task<Response> GetById(Guid organizationId);

        /// <summary>
        /// Lấy thông tin theo cấu hình
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        Task<OrganizationConfig> InternalGetByOrgId(Guid organizationId);

        /// <summary>
        /// Lấy cấu hình hiển thị đơn vị theo OrganizationId
        /// </summary>
        /// <param name="id">Id đơn vị</param>
        /// <returns>Thông tin cấu hình hiển thị đơn vị</returns>
        Task<OrganizationConfigModel> GetByOrgId(Guid organizationId);

        /// <summary>
        /// Lấy danh sách combobox cấu hình đơn vị
        /// </summary>
        /// <param name="count"></param>
        /// <param name="consumerKey"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        Task<Response> GetListCombobox(int count = 0, string consumerKey = "", Guid? orgId = null);
    }
}

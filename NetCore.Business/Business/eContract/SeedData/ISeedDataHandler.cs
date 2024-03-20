using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface khởi tạo dữ liệu
    /// </summary>
    public interface ISeedDataHandler
    {
        /// <summary>
        /// Khởi tạo dữ liệu ứng dụng
        /// </summary>
        /// <returns>Trạng thái khởi tạo</returns>
        Task<Response> InitDataSysApplication(SystemLogModel systemLog);
    }
}

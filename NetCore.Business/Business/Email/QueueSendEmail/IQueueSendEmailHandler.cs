using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý danh sách mail chờ gửi
    /// </summary>
    public interface IQueueSendEmailHandler
    {
        /// <summary>
        /// Thêm mới danh sách mail chờ gửi
        /// </summary>
        /// <param name="model">Model thêm mới danh sách mail chờ gửi</param>
        /// <returns>Id danh sách mail chờ gửi</returns>
        Task<Response> Create(QueueSendEmailCreateModel model);
        /// <summary>
        /// Thêm mới danh sách mail chờ gửi theo danh sách
        /// </summary>
        /// <param name="list">Danh sách thông tin danh sách mail chờ gửi</param>
        /// <returns>Danh sách kết quả thêm mới</returns> 
        Task<Response> CreateMany(List<QueueSendEmailCreateModel> list);

        /// <summary>
        /// Tự động gửi email chưa được gửi
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>kết quả gửi</returns>
        Task<Response> SendMailInQueue();

        /// <summary>
        /// Tự động gửi email ngay thời điểm tạo
        /// </summary>
        /// <param name="filter">Model</param>
        /// <returns>kết quả gửi</returns>
        Task<Response> SendMailNow(QueueSendEmailCreateModel model);
    }
}

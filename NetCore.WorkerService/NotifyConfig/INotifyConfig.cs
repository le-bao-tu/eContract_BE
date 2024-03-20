
using System.Threading.Tasks;

namespace NetCore.WorkerService
{
    public interface INotifyConfig
    {
        Task SendNotifyRemind();
        Task SendNotifyExpire();      

        #region Gửi thông báo nhắc nhở
        Task SendSMSRemindAsync();
        Task SendEmailRemindAsync();
        Task SendNotifyRemindAsync();
        #endregion

        #region Gửi thông báo hết hạn
        Task SendSMSExpiredAsync();
        Task SendEmailExpiredAsync();
        Task SendNotifyExpiredAsync();
        #endregion
    }
}

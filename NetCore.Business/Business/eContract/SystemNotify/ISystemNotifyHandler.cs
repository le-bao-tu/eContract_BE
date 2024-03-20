using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ISystemNotifyHandler
    {
        Task<Response> PushNotificationRemindSignDocumentDaily(SystemLogModel systemLog);
        Task<Response> PushNotificationSignFail(NotificationAutoSignFailModel model, SystemLogModel systemLog);
    }
}

using NetCore.Data;
using NetCore.Shared;
using System;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface INotifyHandler
    {
        /// <summary>
        /// Gửi thông báo về trạng thái tài liệu
        /// </summary>
        /// <returns></returns>
        Task<int> SendNotifyDocumentStatus(Guid orgId, NotifyDocumentModel data, SystemLogModel systemLog, OrganizationConfig orgConf = null);

        Task SendNotificationFirebaseByGateway(NotificationRequestModel model, SystemLogModel systemLog);

        Task SendSMSOTPByGateway(NotificationRequestModel model, SystemLogModel systemLog);

        Task<Response> SendNotificationRemindSignDocumentByGateway(NotificationRemindSignDocumentModel model, SystemLogModel systemLog);

        Task SendOTPChangePasswordByGateway(NotifyChangePasswordModel model, SystemLogModel systemLog);

        Task<Response> SendNotificationFromNotifyConfig(NotificationConfigModel model);

        Task<Response> SendOTPAuthUserByGateway(NotificationSendOTPAuthUserModel model);

        Task<Response> PushNotificationRemindSignDoucmentDaily(NotificationRemindSignDocumentDailyModel model);

        Task<Response> PushNotificationAutoSignFail(NotificationAutoSignFailModel model);
    }
}

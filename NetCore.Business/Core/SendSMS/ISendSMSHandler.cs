using NetCore.Data;
using NetCore.Shared;
using System;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ISendSMSHandler
    {
        /// <summary>
        /// Gửi SMS
        /// </summary>
        /// <returns></returns>
        Task<bool> SendSMS(SendSMSModel data, OrganizationConfig orgConf, SystemLogModel systemLog);
    }
}

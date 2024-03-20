using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface 
    /// </summary>
    public interface INotifyConfigHandler
    {
        Task<Response> Create(NotifyConfigCreateModel model, SystemLogModel systemLog);
        Task<Response> Update(NotifyConfigUpdateModel model, SystemLogModel systemLog);
        Task<Response> Delete(List<Guid> ids, SystemLogModel systemLog);
        Task<Response> Filter(NotifyConfigQueryFilter filter);
        Task<Response> GetById(Guid id);
        Task<Response> GetListCombobox(SystemLogModel systemLog, Guid? orgID);
        Task<Response> GetListComboboxByType(int type, SystemLogModel systemLog, Guid? orgID);
    }
}

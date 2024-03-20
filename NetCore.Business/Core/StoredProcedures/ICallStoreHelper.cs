using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public interface ICallStoreHelper
    {
        Response CallStoreWithStartAndEndDateAsync(string storeName, DateTime startDate, DateTime endDate);
    }
}

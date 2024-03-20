using System;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ICacheService
    {
        Task<TItem> GetOrCreate<TItem>(string key, Func<Task<TItem>> createItem);
        Task<TItem> GetOrCreate<TItem>(string key, Func<TItem> createItem);
        void Remove(string key);
    }
}
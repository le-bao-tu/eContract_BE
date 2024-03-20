using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;

        // Cache expire trong 300s
        private int timeLive = 300;

        public RedisCacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
            int.TryParse(Utils.GetConfig("redis:timeLive"), out timeLive);
            if (timeLive <= 0)
            {
                timeLive = 300;
            }
        }

        public async Task<TItem> GetOrCreate<TItem>(string key, Func<Task<TItem>> createItem)
        {
            TItem cacheEntry;
            try
            {
                var itemString = _distributedCache.GetString(key);
                if (string.IsNullOrEmpty(itemString))
                {
                    // Cache expire trong 300s
                    var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(timeLive));

                    cacheEntry = await createItem();
                    var item = JsonSerializer.Serialize(cacheEntry);

                    // Nạp lại giá trị mới cho cache
                    _distributedCache.SetString(key, item, options);

                    var dataCache = _distributedCache.GetString(key);
                }
                else
                {
                    cacheEntry = JsonSerializer.Deserialize<TItem>(itemString);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi lấy dữ liệu dừ redis");
                cacheEntry = await createItem();
            }
            return cacheEntry;
        }
        public async Task<TItem> GetOrCreate<TItem>(string key, Func<TItem> createItem)
        {
            TItem cacheEntry;
            try
            {
                var itemString = _distributedCache.GetString(key);
                if (string.IsNullOrEmpty(itemString))
                {
                    // Cache expire trong 300s
                    var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(timeLive));

                    cacheEntry = createItem();
                    var item = JsonSerializer.Serialize(cacheEntry);

                    // Nạp lại giá trị mới cho cache
                    await _distributedCache.SetStringAsync(key, item, options);

                    var dataCache = _distributedCache.GetString(key);
                }
                else
                {
                    cacheEntry = JsonSerializer.Deserialize<TItem>(itemString);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi lấy dữ liệu dừ redis");
                cacheEntry = createItem();
            }
            return cacheEntry;
        }

        public void Remove(string key)
        {
            _distributedCache.Remove(key);
        }
    }
}

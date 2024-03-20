using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using NetCore.Business;
using Microsoft.AspNetCore.Authorization;

namespace NetCore.API.Controller
{
    /// <summary>
    /// Module test redis
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/redis")]
    [ApiExplorerSettings(GroupName = "00. Redis-test", IgnoreApi = false)]
    public class RedisController : ApiControllerBase
    {
        private readonly IDistributedCache _distributedCache;

        public RedisController(IDistributedCache distributedCache, ISystemLogHandler logHandler) : base(logHandler)
        {
            _distributedCache = distributedCache;
        }

        [AllowAnonymous, HttpGet, Route("")]
        public string Get()
        {
            var cacheKey = "TheTime";
            var currentTime = DateTime.Now.ToString();
            var cachedTime = _distributedCache.GetString(cacheKey);
            if (string.IsNullOrEmpty(cachedTime))
            {
                // cachedTime = "Expired";
                // Cache expire trong 5s
                var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(5));
                // Nạp lại giá trị mới cho cache
                _distributedCache.SetString(cacheKey, currentTime, options);
                cachedTime = _distributedCache.GetString(cacheKey);
            }
            var result = $"Current Time : {currentTime} \nCached  Time : {cachedTime}";
            return result;
        }
    }
}

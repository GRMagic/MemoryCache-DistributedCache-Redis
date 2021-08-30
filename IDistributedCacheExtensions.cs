using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;

namespace Microsoft.Extensions.Caching.Distributed
{
    public static class IDistributedCacheExtensions
    {
        public static TItem GetOrCreate<TItem>(this IDistributedCache cache, string key, Func<DistributedCacheEntryOptions, TItem> factory, ILogger logger = null) where TItem : class
        {
            TItem obj = null;
            try
            {
                var cached = cache.GetString(key);
                if (cached != null) return JsonSerializer.Deserialize<TItem>(cached);

                var options = new DistributedCacheEntryOptions();
                obj = factory.Invoke(options);

                cache.SetString(key, JsonSerializer.Serialize(obj), options);

                return obj;
            }
            catch(RedisException e)
            {
                logger?.LogWarning(e, "Problemas com o cache distribuido.");
                return obj ?? factory.Invoke(new DistributedCacheEntryOptions());
            }
        }
    }
}

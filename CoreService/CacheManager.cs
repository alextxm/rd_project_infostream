using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;

using InfoStream.Core;
using InfoStream.Metadata;

namespace CoreService
{
    public static class CacheManager
    {
        internal static int cacheDuration = 3600;
        internal static string searchCachePrefix = "coresvclnc";

        internal static ISIndexer GetDataCache()
        {
            ObjectCache cache = MemoryCache.Default;

            ISIndexer data = null;

            // mettiamo in cache le informazioni di base
            if (!cache.Contains(searchCachePrefix, null))
            {
                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy
                {
                    SlidingExpiration = ObjectCache.NoSlidingExpiration,
                    AbsoluteExpiration = DateTime.UtcNow.AddMinutes(cacheDuration) // usa UtcNow per evitare problemi con le TZ
                };

               data = new ISIndexer(
                        System.Configuration.ConfigurationManager.AppSettings["indexFolder"],
                        IndexerStorageMode.FSRAM,
                        IndexerAnalyzer.StandardAnalyzer);

                cache.Set(searchCachePrefix, data, cacheItemPolicy);
            }
            else
            {
                data = (cache[searchCachePrefix] as ISIndexer);
            }

            return data;
        }
    }
}
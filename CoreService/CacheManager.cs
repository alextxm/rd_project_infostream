using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;

using corelib;
using corelib.Interchange;

namespace CoreService
{
    public static class CacheManager
    {
        internal static int cacheDuration = 3600;
        internal static string searchCachePrefix = "coresvclnc";

        internal static IndexerInterop<InterchangeDocument> GetDataCache()
        {
            ObjectCache cache = MemoryCache.Default;

            IndexerInterop<InterchangeDocument> data = null;

            // mettiamo in cache le informazioni di base
            if (!cache.Contains(searchCachePrefix, null))
            {
                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy
                {
                    SlidingExpiration = ObjectCache.NoSlidingExpiration,
                    AbsoluteExpiration = DateTime.UtcNow.AddMinutes(cacheDuration) // usa UtcNow per evitare problemi con le TZ
                };

               data = new IndexerInterop<InterchangeDocument>(
                        System.Configuration.ConfigurationManager.AppSettings["indexFolder"],
                        IndexerStorageMode.FSRAM,
                        IndexerAnalyzer.StandardAnalyzer,
                        new InterchangeDocumentIOH());

                cache.Set(searchCachePrefix, data, cacheItemPolicy);
            }
            else
            {
                data = (cache[searchCachePrefix] as IndexerInterop<InterchangeDocument>);
            }

            return data;
        }
    }
}
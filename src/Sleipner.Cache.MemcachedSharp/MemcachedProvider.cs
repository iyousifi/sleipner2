using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MemcachedSharp;
using Newtonsoft.Json;
using Sleipner.Cache.Extensions;
using Sleipner.Cache.MemcachedSharp.MemcachedWrapper;
using Sleipner.Cache.MemcachedSharp.MemcachedWrapper.Hashing;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Core.Util;

namespace Sleipner.Cache.MemcachedSharp
{
    public class MemcachedProvider<T> : ICacheProvider<T> where T : class
    {
        private MemcachedSharpClientCluster _cluster;
        private JsonSerializer _serializer;

        public MemcachedProvider(IEnumerable<string> endPoints, MemcachedOptions options = null)
        {
            _cluster = new MemcachedSharpClientCluster(endPoints, options);
            _serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.All
            };

        }

        public async Task<CachedObject<TResult>> GetAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy)
        {
            var key = proxiedMethodInvocation.GetHashString();
            var memcachedItem = await _cluster.Get(key);
            if (memcachedItem != null)
            {
                MemcachedObject<TResult> cacheItem;
                using (var zipStream = new GZipStream(memcachedItem.Data, CompressionMode.Decompress))
                {
                    using (var streamReader = new StreamReader(zipStream))
                    {
                        var textReader = new JsonTextReader(streamReader);
                        cacheItem = _serializer.Deserialize<MemcachedObject<TResult>>(textReader);
                    }
                }

                if (cacheItem == null)
                    return new CachedObject<TResult>(CachedObjectState.None, default(TResult));

                if (cacheItem.IsException && cacheItem.Created.AddSeconds(cachePolicy.ExceptionCacheDuration) > DateTime.Now)
                {
                    return new CachedObject<TResult>(CachedObjectState.Exception, cacheItem.Exception);
                }
                else if (cacheItem.IsException)
                {
                    return new CachedObject<TResult>(CachedObjectState.None, default(TResult));
                }

                var fresh = cacheItem.Created.AddSeconds(cachePolicy.CacheDuration) > DateTime.Now;
                var state = fresh ? CachedObjectState.Fresh : CachedObjectState.Stale;
                return new CachedObject<TResult>(state, cacheItem.Object);
            }

            return new CachedObject<TResult>(CachedObjectState.None, default(TResult));
        }

        public async Task StoreAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy, TResult data)
        {
            var key = proxiedMethodInvocation.GetHashString();
            var cachedObject = new MemcachedObject<TResult>()
            {
                Created = DateTime.Now,
                Object = data
            };

            var bytes = SerializeAndZip(cachedObject);

            if (cachePolicy.MaxAge > 0)
            {
                await _cluster.Set(key, bytes, new MemcachedStorageOptions() {ExpirationTime = TimeSpan.FromSeconds(cachePolicy.MaxAge)});
            }
            else
            {
                await _cluster.Set(key, bytes);
            }
        }

        public async Task StoreExceptionAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy, Exception e)
        {
            var key = proxiedMethodInvocation.GetHashString();
            var cachedObject = new MemcachedObject<TResult>()
            {
                Created = DateTime.Now,
                IsException = true,
                Exception = e,
            };

            var bytes = SerializeAndZip(cachedObject);

            if (cachePolicy.MaxAge > 0)
            {
                await _cluster.Set(key, bytes, new MemcachedStorageOptions() { ExpirationTime = TimeSpan.FromSeconds(cachePolicy.MaxAge) });
            }
            else
            {
                await _cluster.Set(key, bytes);
            }
        }

        private byte[] SerializeAndZip<TResult>(MemcachedObject<TResult> item)
        {
            using (var ms = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (var zipStreamWriter = new StreamWriter(compressionStream))
                    {
                        _serializer.Serialize(zipStreamWriter, item);
                    }
                }

                return ms.ToArray();
            }
        }
    }
}
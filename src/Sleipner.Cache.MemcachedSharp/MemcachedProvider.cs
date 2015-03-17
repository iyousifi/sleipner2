using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemcachedSharp;
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
        public MemcachedProvider(IEnumerable<string> endPoints, MemcachedOptions options = null)
        {
            _cluster = new MemcachedSharpClientCluster(endPoints, options);
        }

        public async Task<CachedObject<TResult>> GetAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy)
        {
            var key = proxiedMethodInvocation.GetHashString();
            var item = await _cluster.Get(key);
            if (item != null)
            {
                
            }

            return new CachedObject<TResult>(CachedObjectState.None, default(TResult));
        }

        public Task StoreAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy, TResult data)
        {
            throw new NotImplementedException();
        }

        public Task StoreExceptionAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy, Exception e)
        {
            throw new NotImplementedException();
        }
    }
}
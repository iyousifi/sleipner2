using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemcachedSharp;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Core.Util;

namespace Sleipner.Cache.MemcachedSharp
{
    public class MemcachedProvider<T> : ICacheProvider<T> where T : class
    {
        public MemcachedProvider(IEnumerable<MemcachedClient> clients)
        {
        }

        public Task<CachedObject<TResult>> GetAsync<TResult>(ProxiedMethodInvocation<T, TResult> proxiedMethodInvocation, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
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
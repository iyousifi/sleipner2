using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Sleipner.Cache.LookupHandlers;
using Sleipner.Cache.LookupHandlers.Async;
using Sleipner.Cache.LookupHandlers.Sync;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Core;
using Sleipner.Core.Util;

namespace Sleipner.Cache
{
    public class SleipnerCacheProxyHandler<T> : IProxyHandler<T> where T : class
    {
        private readonly T _implementation;
        private readonly ICachePolicyProvider<T> _cachePolicyProvider;
        private readonly ICacheProvider<T> _cache;
        private readonly AsyncLookupHandler<T> _asyncLookupHandler;
        private readonly SyncLookupHandler<T> _syncLookupHandler;

        private readonly TaskSyncronizer _taskUpdateSyncronizer;

        public SleipnerCacheProxyHandler(T implementation, ICachePolicyProvider<T> cachePolicyProvider, ICacheProvider<T> cache)
        {
            _implementation = implementation;
            _cachePolicyProvider = cachePolicyProvider;
            _cache = cache;

            _asyncLookupHandler = new AsyncLookupHandler<T>(_implementation, _cachePolicyProvider, _cache);
            _syncLookupHandler = new SyncLookupHandler<T>(_implementation, _cachePolicyProvider, _cache);

            _taskUpdateSyncronizer = new TaskSyncronizer();
        }

        public async Task<TResult> HandleAsync<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation)
        {
            return await _asyncLookupHandler.LookupAsync(methodInvocation);
        }

        public TResult Handle<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation)
        {
            return _syncLookupHandler.Lookup(methodInvocation);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Core.Util;

namespace Sleipner.Cache.LookupHandlers
{
    public class AsyncLookupHandler<T> where T : class
    {
        private readonly T _implementation;
        private readonly ICachePolicyProvider<T> _cachePolicyProvider;
        private readonly ICacheProvider<T> _cache;
        private readonly Syncronizer _syncronizer;

        public AsyncLookupHandler(T implementation, ICachePolicyProvider<T> cachePolicyProvider, ICacheProvider<T> cache)
        {
            _implementation = implementation;
            _cachePolicyProvider = cachePolicyProvider;
            _cache = cache;
            _syncronizer = new Syncronizer();
        }

        public async Task<TResult> LookupAsync<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation)
        {
            var cachePolicy = _cachePolicyProvider.GetPolicy(methodInvocation.Method, methodInvocation.Parameters);

            if (cachePolicy == null || cachePolicy.CacheDuration == 0)
            {
                return await methodInvocation.InvokeAsync(_implementation);
            }

            var cachedItem = await _cache.GetAsync(methodInvocation, cachePolicy) ?? new CachedObject<TResult>(CachedObjectState.None, null);

            if (cachedItem.State == CachedObjectState.Fresh)
            {
                return cachedItem.Object;
            }

            if (cachedItem.State == CachedObjectState.Exception)
            {
                throw cachedItem.ThrownException;
            }

            var requestKey = new RequestKey(methodInvocation.Method, methodInvocation.Parameters);
            Task<TResult> awaitableTask;
            if (_syncronizer.TryGetAwaitable(requestKey, () => methodInvocation.InvokeAsync(_implementation), out awaitableTask))
            {
                if (cachedItem.State == CachedObjectState.Stale)
                    return cachedItem.Object;

                return await awaitableTask;
            }

            if (cachedItem.State == CachedObjectState.Stale)
            {
                awaitableTask.ContinueWith(async task =>
                {
                    try
                    {
                        Exception thrownException = null;
                        try
                        {
                            var data = await task;
                            await _cache.StoreAsync(methodInvocation, cachePolicy, data);
                        }
                        catch (Exception e)
                        {
                            thrownException = e;
                        }

                        if (thrownException != null && cachePolicy.BubbleExceptions)
                        {
                            await _cache.StoreExceptionAsync(methodInvocation, cachePolicy, thrownException);
                        }
                    }
                    finally
                    {
                        _syncronizer.Release(requestKey);
                    }
                });

                return cachedItem.Object;
            }

            try
            {
                Exception thrownException = null;
                try
                {
                    var data = await awaitableTask;
                    await _cache.StoreAsync(methodInvocation, cachePolicy, data);
                    return data;
                }
                catch (Exception e)
                {
                    thrownException = e;
                }

                await _cache.StoreExceptionAsync(methodInvocation, cachePolicy, thrownException);
                throw thrownException;
            }
            finally
            {
                _syncronizer.Release(requestKey);
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Sleipner.Cache.LookupHandlers.Sync;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Core.Util;

namespace Sleipner.Cache.LookupHandlers.Async
{
    public class AsyncLookupHandler<T> where T : class
    {
        private readonly T _implementation;
        private readonly ICachePolicyProvider<T> _cachePolicyProvider;
        private readonly ICacheProvider<T> _cache;
        private readonly RequestSyncronizer _syncronizer;

        public AsyncLookupHandler(T implementation, ICachePolicyProvider<T> cachePolicyProvider, ICacheProvider<T> cache)
        {
            _implementation = implementation;
            _cachePolicyProvider = cachePolicyProvider;
            _cache = cache;
            _syncronizer = new RequestSyncronizer();
        }

        public async Task<TResult> LookupAsync<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation)
        {
            var cachePolicy = _cachePolicyProvider.GetPolicy(methodInvocation.Method, methodInvocation.Parameters);

            if (cachePolicy == null || cachePolicy.CacheDuration <= 0)
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
            RequestWaitHandle<TResult> waitHandle;

            if (_syncronizer.ShouldWaitForHandle(requestKey, out waitHandle))
            {
                if (cachedItem.State == CachedObjectState.Stale)
                {
                    return cachedItem.Object;
                }

                return waitHandle.WaitForResult();
            }

            if (cachedItem.State == CachedObjectState.Stale)
            {
                Func<Task<TResult>> loader = () => methodInvocation.InvokeAsync(_implementation);

                loader.BeginInvoke(async callback =>
                {
                    Exception asyncRequestThrownException = null;
                    var asyncResult = default(TResult);
                    try
                    {
                        try
                        {
                            asyncResult = await loader.EndInvoke(callback);
                            await _cache.StoreAsync(methodInvocation, cachePolicy, asyncResult);
                        }
                        catch (Exception e)
                        {
                            asyncRequestThrownException = e;
                        }

                        if (asyncRequestThrownException != null)
                        {
                            if (cachePolicy.BubbleExceptions)
                            {
                                await _cache.StoreExceptionAsync(methodInvocation, cachePolicy, asyncRequestThrownException);
                            }
                            else
                            {
                                await _cache.StoreAsync(methodInvocation, cachePolicy, cachedItem.Object);
                            }
                        }
                    }
                    finally
                    {
                        if (asyncRequestThrownException != null)
                        {
                            _syncronizer.ReleaseWithException<TResult>(requestKey, asyncRequestThrownException);
                        }
                        else
                        {
                            _syncronizer.Release(requestKey, asyncResult);
                        }
                    }
                }, null);

                return cachedItem.Object;
            }

            var realInstanceResult = default(TResult);
            Exception thrownException = null;
            try
            {
                try
                {
                    realInstanceResult = await methodInvocation.InvokeAsync(_implementation); ;
                    await _cache.StoreAsync(methodInvocation, cachePolicy, realInstanceResult);
                    return realInstanceResult;
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
                if (thrownException != null)
                {
                    _syncronizer.ReleaseWithException<TResult>(requestKey, thrownException);
                }
                else
                {
                    _syncronizer.Release(requestKey, realInstanceResult);
                }
            }
            /*
            var realInstanceResult = default(TResult);
            Exception thrownException = null;
            try
            {
                try
                {
                    realInstanceResult = await methodInvocation.InvokeAsync(_implementation);
                    await _cache.StoreAsync(methodInvocation, cachePolicy, realInstanceResult);
                }
                catch (Exception e)
                {
                    thrownException = e;
                }

                if (thrownException != null)
                {
                    await _cache.StoreExceptionAsync(methodInvocation, cachePolicy, thrownException);
                    throw thrownException;
                }
            }
            finally
            {
                if (thrownException != null)
                {
                    _syncronizer.ReleaseWithException<TResult>(requestKey, thrownException);
                }
                else
                {
                    _syncronizer.Release(requestKey, realInstanceResult);
                }
            }

            return realInstanceResult;
             * */
        }
    }
}

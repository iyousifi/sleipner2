using System;
using System.Diagnostics;
using Nito.AsyncEx;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Core.Util;

namespace Sleipner.Cache.LookupHandlers.Sync
{
    public class SyncLookupHandler<T> where T : class
    {
        private readonly T _implementation;
        private readonly ICachePolicyProvider<T> _cachePolicyProvider;
        private readonly ICacheProvider<T> _cache;
        private readonly RequestSyncronizer _syncronizer = new RequestSyncronizer();

        public SyncLookupHandler(T implementation, ICachePolicyProvider<T> cachePolicyProvider, ICacheProvider<T> cache)
        {
            _implementation = implementation;
            _cachePolicyProvider = cachePolicyProvider;
            _cache = cache;
        }

        public TResult Lookup<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation)
        {
            var cachePolicy = _cachePolicyProvider.GetPolicy(methodInvocation.Method, methodInvocation.Parameters);

            if (cachePolicy == null || cachePolicy.CacheDuration == 0)
            {
                return methodInvocation.Invoke(_implementation);
            }

            var cachedItem = AsyncContext.Run(() => _cache.GetAsync(methodInvocation, cachePolicy)) ?? new CachedObject<TResult>(CachedObjectState.None, null);

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
                    return cachedItem.Object;

                return waitHandle.WaitForResult();
            }

            if (cachedItem.State == CachedObjectState.Stale)
            {
                Func<TResult> loader = () => methodInvocation.Invoke(_implementation);

                loader.BeginInvoke(callback =>
                {
                    Exception asyncRequestThrownException = null;
                    var asyncResult = default(TResult);

                    try
                    {
                        asyncResult = loader.EndInvoke(callback);
                        AsyncContext.Run(() => _cache.StoreAsync(methodInvocation, cachePolicy, asyncResult));
                    }
                    catch (Exception e)
                    {
                        if (cachePolicy.BubbleExceptions)
                        {
                            AsyncContext.Run(() => _cache.StoreExceptionAsync(methodInvocation, cachePolicy, e));
                            asyncRequestThrownException = e;
                        }
                        else
                        {
                            asyncResult = cachedItem.Object;
                            AsyncContext.Run(() => _cache.StoreAsync(methodInvocation, cachePolicy, asyncResult));
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

            //At this point nothing is in the cache.
            var realInstanceResult = default(TResult);
            Exception thrownException = null;
            try
            {
                realInstanceResult = methodInvocation.Invoke(_implementation);
                AsyncContext.Run(() => _cache.StoreAsync(methodInvocation, cachePolicy, realInstanceResult));
            }
            catch (Exception e)
            {
                thrownException = e;
                AsyncContext.Run(() => _cache.StoreExceptionAsync(methodInvocation, cachePolicy, thrownException));

                throw;
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
        }
    }
}

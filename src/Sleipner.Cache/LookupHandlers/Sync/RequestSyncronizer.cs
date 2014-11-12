using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sleipner.Cache.LookupHandlers.Sync
{
    public class RequestSyncronizer
    {
        private readonly IDictionary<RequestKey, object> _waitHandles = new ConcurrentDictionary<RequestKey, object>(); 
 
        public bool ShouldWaitForHandle<TResult>(RequestKey key, out RequestWaitHandle<TResult> waitHandle)
        {
            lock (this)
            {
                object waitHandleObject;
                if (_waitHandles.TryGetValue(key, out waitHandleObject))
                {
                    waitHandle = (RequestWaitHandle<TResult>) waitHandleObject;
                    return true;
                }

                waitHandle = new RequestWaitHandle<TResult>();
                _waitHandles[key] = waitHandle;

                return false;
            }
        }

        public void Release<TResult>(RequestKey key, TResult result)
        {
            lock (this)
            {
                var waitHandle = (RequestWaitHandle<TResult>)_waitHandles[key];
                _waitHandles.Remove(key);
                waitHandle.Release(result);
            }
        }

        public void ReleaseWithException<TResult>(RequestKey key, Exception thrownException)
        {
            lock (this)
            {
                var waitHandle = (RequestWaitHandle<TResult>)_waitHandles[key];
                _waitHandles.Remove(key);
                waitHandle.ReleaseWithException(thrownException);
            }
        }
    }
}
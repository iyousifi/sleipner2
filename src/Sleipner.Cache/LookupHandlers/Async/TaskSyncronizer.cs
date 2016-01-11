using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sleipner.Cache.LookupHandlers.Async
{
    public class TaskSyncronizer
    {
        private readonly IDictionary<RequestKey, object> _waitHandles = new ConcurrentDictionary<RequestKey, object>();

        public bool TryGetAwaitable<TResult>(RequestKey key, Task<TResult> loaderTask, out Task<TResult> awaitable)
        {
            object awaitableTask;
            if (_waitHandles.TryGetValue(key, out awaitableTask))
            {
                awaitable = (Task<TResult>) awaitableTask;
                return true;
            }
            else
            {
                //_waitHandles[key] = new {};
                awaitable = loaderTask;
                _waitHandles[key] = awaitable;
                return false;
            }
        }

        public void Release(RequestKey requestKey)
        {
            if (_waitHandles.ContainsKey(requestKey))
            {
                _waitHandles.Remove(requestKey);
            }
        }
    }
}

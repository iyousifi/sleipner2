using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Cache.LookupHandlers
{
    public class Syncronizer
    {
        private readonly IDictionary<RequestKey, object> _waitHandles = new ConcurrentDictionary<RequestKey, object>();

        public bool TryGetAwaitable<TResult>(RequestKey key, Func<Task<TResult>> loaderTask, out Task<TResult> awaitable)
        {
            object awaitableTask;
            if (_waitHandles.TryGetValue(key, out awaitableTask))
            {
                awaitable = (Task<TResult>) awaitableTask;
                return true;
            }
            else
            {
                awaitable = loaderTask();
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

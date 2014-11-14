using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Cache.DictionaryCache
{
    public class DictionaryCacheItem
    {
        public readonly object Object;
        public readonly Exception ThrownException;
        public readonly DateTime Created;
        public readonly TimeSpan Duration;
        public readonly TimeSpan AbsoluteDuration;

        public bool IsExpired
        {
            get { return Created + Duration < DateTime.Now; }
        }

        public DictionaryCacheItem(object obj, TimeSpan duration, TimeSpan absoluteDuration)
        {
            Object = obj;
            Created = DateTime.Now;

            Duration = duration;
            AbsoluteDuration = absoluteDuration;
        }

        public DictionaryCacheItem(Exception exception, TimeSpan duration, TimeSpan absoluteDuration)
        {
            ThrownException = exception;
            Created = DateTime.Now;

            Duration = duration;
            AbsoluteDuration = absoluteDuration;
        }
    }
}

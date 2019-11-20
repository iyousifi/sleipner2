using System;

namespace Sleipner.Cache.RedisSharp
{
    public class RedisObject<TObject>
    {
        public TObject Object;
        public bool IsException;
        public DateTime Created;
        public Exception Exception;
    }
}

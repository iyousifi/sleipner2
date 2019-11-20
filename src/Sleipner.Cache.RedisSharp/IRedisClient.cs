using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Cache.RedisSharp
{
    public interface IRedisClient
    {
        Task<byte[]> Get(string key);
        Task Set(string key, byte[] value, TimeSpan? expiration);
        Task Set(string key, byte[] value);
    }
}

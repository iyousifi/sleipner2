using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Sleipner.Cache.RedisSharp.RedisWrapper
{
    public class RedisClient : IRedisClient
    {
        private readonly ConnectionMultiplexer _multiplexer;

        public RedisClient(ConfigurationOptions options)
        {
            _multiplexer = ConnectionMultiplexer.Connect(options);
        }

        private IDatabase RedisDatabase => _multiplexer.GetDatabase();

        public async Task<byte[]> Get(string key)
        {
            var value = (byte[])await RedisDatabase.StringGetAsync(key);

            return value;
        }

        public async Task Set(string key, byte[] value, TimeSpan? expiration)
        {
            await RedisDatabase.StringSetAsync(key, value, expiration);
        }

        public async Task Set(string key, byte[] value)
        {
            await RedisDatabase.StringSetAsync(key, value);
        }
    }
}

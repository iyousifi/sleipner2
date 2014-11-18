using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemcachedSharp;

namespace Sleipner.Cache.Memcached.MemcachedWrapper
{
    public class NullMemcachedSharpClient : IMemcachedSharpClient
    {
        public async Task<MemcachedItem> Get(string key)
        {
            return null;
        }

        public async Task<MemcachedItem> Gets(string key)
        {
            return null;
        }

        public async Task Set(string key, byte[] value, MemcachedStorageOptions options = null)
        {
        }

        public async Task<bool> Delete(string key)
        {
            return false;
        }

        public async Task<bool> Add(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return false;
        }

        public async Task<bool> Replace(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return false;
        }

        public async Task<bool> Append(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return false;
        }

        public async Task<bool> Prepend(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return false;
        }

        public async Task<ulong?> Increment(string key, ulong value)
        {
            return null;
        }

        public async Task<ulong?> Decrement(string key, ulong value)
        {
            return null;
        }

        public async Task<CasResult> Cas(string key, long casUnique, byte[] value, MemcachedStorageOptions options = null)
        {
            return default(CasResult);
        }
    }
}

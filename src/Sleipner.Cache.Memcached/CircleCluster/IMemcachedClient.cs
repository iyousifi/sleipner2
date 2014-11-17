using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemcachedSharp;

namespace Sleipner.Cache.Memcached.CircleCluster
{
    public interface IMemcachedClient
    {
        Task<MemcachedItem> Get(string key);
        Task<MemcachedItem> Gets(string key);
        Task Set(string key, byte[] value, MemcachedStorageOptions options = null);
        Task<bool> Delete(string key);
        Task<bool> Add(string key, byte[] value, MemcachedStorageOptions options = null);
        Task<bool> Replace(string key, byte[] value, MemcachedStorageOptions options = null);
        Task<bool> Append(string key, byte[] value, MemcachedStorageOptions options = null);
        Task<bool> Prepend(string key, byte[] value, MemcachedStorageOptions options = null);
        Task<ulong?> Increment(string key, ulong value);
        Task<ulong?> Decrement(string key, ulong value);
        Task<CasResult> Cas(string key, long casUnique, byte[] value, MemcachedStorageOptions options = null);
    }
}
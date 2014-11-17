using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemcachedSharp;
using Nito.AsyncEx;

namespace Sleipner.Cache.Memcached.CircleCluster
{
    public class MemcachedClientCluster : IMemcachedClient
    {
        private readonly List<MemcachedClusterNode> _clients = new List<MemcachedClusterNode>();
        private ConsistentHash<MemcachedClusterNode> _nodes;
        private readonly Timer _timer;

        public MemcachedClientCluster(IEnumerable<string> endPoints, MemcachedOptions options = null)
        {
            _timer = new Timer(state => AsyncContext.Run(() => AwakenDeadServers()), null, 10000, 10000);
            _clients = endPoints.Select(a => new MemcachedClusterNode(a, options)).ToList();
            InitRingCluster();
        }

        private void InitRingCluster()
        {
            _nodes = new ConsistentHash<MemcachedClusterNode>();
            _nodes.Init(_clients.Where(a => a.IsAlive));
        }

        /// <summary>
        /// This method runs every 10 seconds and queries memcached to see if a server node has come online again. Reinitializes the collection when one or more reappears.
        /// </summary>
        /// <returns></returns>
        private async Task AwakenDeadServers()
        {
            var updated = false;
            foreach (var server in _clients.Where(a => !a.IsAlive))
            {
                try
                {
                    await server.Client.Get("query-aliveness");
                    updated = true;
                    server.IsAlive = true;
                }
                catch (SocketException e)
                {
                    //Server is still deaded :(
                }
            }

            if (updated)
            {
                InitRingCluster();
            }
        }
        
        public async Task<MemcachedItem> Get(string key)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return null;
            }

            try
            {
                return await server.Client.Get(key);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Get(key);
        }

        public async Task<MemcachedItem> Gets(string key)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return null;
            }

            try
            {
                return await server.Client.Gets(key);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Gets(key);
        }

        public async Task Set(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return;
            }

            try
            {
                await server.Client.Set(key, value, options);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            await Set(key, value, options);
        }

        public async Task<bool> Delete(string key)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return false;
            }

            try
            {
                return await server.Client.Delete(key);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Delete(key);
        }

        public async Task<bool> Add(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return false;
            }

            try
            {
                return await server.Client.Add(key, value, options);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Add(key, value, options);
        }

        public async Task<bool> Replace(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return false;
            }

            try
            {
                return await server.Client.Replace(key, value, options);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Replace(key, value, options);
        }

        public async Task<bool> Append(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return false;
            }

            try
            {
                return await server.Client.Append(key, value, options);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Append(key, value, options);
        }

        public async Task<bool> Prepend(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return false;
            }

            try
            {
                return await server.Client.Prepend(key, value, options);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Prepend(key, value, options);
        }

        public async Task<ulong?> Increment(string key, ulong value)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return null;
            }

            try
            {
                return await server.Client.Increment(key, value);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Increment(key, value);
        }

        public async Task<ulong?> Decrement(string key, ulong value)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return null;
            }

            try
            {
                return await server.Client.Decrement(key, value);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Decrement(key, value);
        }

        public async Task<CasResult> Cas(string key, long casUnique, byte[] value, MemcachedStorageOptions options = null)
        {
            var server = _nodes.GetNode(key);
            if (server == null)
            {
                return default(CasResult);
            }

            try
            {
                return await server.Client.Cas(key, casUnique, value, options);
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Cas(key, casUnique, value, options);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly List<ClusteredMemcachedClient> _clients = new List<ClusteredMemcachedClient>();
        private ConsistentHash<ClusteredMemcachedClient> _nodes;
        private readonly Timer _timer;

        public MemcachedClientCluster(IEnumerable<string> endPoints, MemcachedOptions options = null)
        {
            _timer = new Timer(state => AsyncContext.Run(() => AwakenDeadServers()), null, 10000, 10000);
            _clients = endPoints.Select(a => new ClusteredMemcachedClient(a, options)).ToList();
            InitRingCluster();
        }

        private void InitRingCluster()
        {
            var aliveClients = _clients.Where(a => a.IsAlive).OrderBy(a => a.EndPoint);
            Debug.WriteLine("init " + aliveClients.Count());
            _nodes = new ConsistentHash<ClusteredMemcachedClient>();
            _nodes.Init(aliveClients);
        }

        /// <summary>
        /// This method runs every 10 seconds and queries memcached to see if a server node has come online again. Reinitializes the collection when one or more reappears.
        /// </summary>
        /// <returns></returns>
        private async Task AwakenDeadServers()
        {
            Debug.WriteLine("Testing for dead servers...");
            var updated = false;
            foreach (var server in _clients.Where(a => !a.IsAlive))
            {
                try
                {
                    Debug.WriteLine("Testing: " + server.EndPoint);
                    await server.Get("query-aliveness");
                    updated = true;
                    server.IsAlive = true;
                }
                catch (PoolCreationException e)
                {
                    //Server is still deaded :(
                }
                catch (MemcachedException e)
                {
                    //Server is still deaded :(
                }
            }

            if (updated)
            {
                Debug.WriteLine("AWAKEING!");
                InitRingCluster();
            }
        }
        
        public async Task<MemcachedItem> Get(string key)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return null;
            }

            try
            {
                return await server.Get(key);
            }
            catch (PoolCreationException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            catch (MemcachedException e)
            {
                server.IsAlive = false;
                InitRingCluster();
            }
            return await Get(key);
        }

        public async Task<MemcachedItem> Gets(string key)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return null;
            }

            try
            {
                return await server.Gets(key);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Gets(key);
        }

        public async Task Set(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return;
            }

            try
            {
                await server.Set(key, value, options);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            await Set(key, value, options);
        }

        public async Task<bool> Delete(string key)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return false;
            }

            try
            {
                return await server.Delete(key);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Delete(key);
        }

        public async Task<bool> Add(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return false;
            }

            try
            {
                return await server.Add(key, value, options);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Add(key, value, options);
        }

        public async Task<bool> Replace(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return false;
            }

            try
            {
                return await server.Replace(key, value, options);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Replace(key, value, options);
        }

        public async Task<bool> Append(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return false;
            }

            try
            {
                return await server.Append(key, value, options);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Append(key, value, options);
        }

        public async Task<bool> Prepend(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return false;
            }

            try
            {
                return await server.Prepend(key, value, options);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Prepend(key, value, options);
        }

        public async Task<ulong?> Increment(string key, ulong value)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return null;
            }

            try
            {
                return await server.Increment(key, value);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Increment(key, value);
        }

        public async Task<ulong?> Decrement(string key, ulong value)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return null;
            }

            try
            {
                return await server.Decrement(key, value);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Decrement(key, value);
        }

        public async Task<CasResult> Cas(string key, long casUnique, byte[] value, MemcachedStorageOptions options = null)
        {
            ClusteredMemcachedClient server;
            if (!_nodes.TryGetNode(key, out server))
            {
                return default(CasResult);
            }

            try
            {
                return await server.Cas(key, casUnique, value, options);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    server.IsAlive = false;
                    InitRingCluster();
                }
            }
            return await Cas(key, casUnique, value, options);
        }
    }
}

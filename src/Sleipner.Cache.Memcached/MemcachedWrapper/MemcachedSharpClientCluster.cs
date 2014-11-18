using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MemcachedSharp;
using Nito.AsyncEx;
using Sleipner.Cache.Memcached.MemcachedWrapper.Hashing;

namespace Sleipner.Cache.Memcached.MemcachedWrapper
{
    public class MemcachedSharpClientCluster : IMemcachedSharpClient
    {
        private readonly List<MemcachedSharpClient> _clients = new List<MemcachedSharpClient>();
        private ConsistentHash<MemcachedSharpClient> _nodes;
        private readonly Timer _timer;

        public MemcachedSharpClientCluster(IEnumerable<string> endPoints, MemcachedOptions options = null)
        {
            _timer = new Timer(state => AsyncContext.Run(() => AwakenDeadServers()), null, 10000, 10000);
            _clients = endPoints.Select(a => new MemcachedSharpClient(a, options)).ToList();

            _nodes = new ConsistentHash<MemcachedSharpClient>(_clients);
        }

        /// <summary>
        /// This method runs every 10 seconds and queries memcached to see if a server node has come online again. Reinitializes the collection when one or more reappears.
        /// </summary>
        /// <returns></returns>
        private async Task AwakenDeadServers()
        {
            foreach (var server in _clients.Where(a => !a.IsAlive))
            {
                try
                {
                    Debug.WriteLine("Testing: " + server.EndPoint);
                    await server.Get("query-aliveness");
                    server.IsAlive = true;
                }
                catch (PoolCreationException e)
                {
                    //Server still deaded
                }
                catch (SocketException e)
                {
                    //Server still deaded
                }
            }
        }

        private bool TryGetClient(string key, out MemcachedSharpClient client)
        {
            MemcachedSharpClient server;
            if (_nodes.TryGetNode(key, out server))
            {
                if (!server.IsAlive)
                {
                    Debug.WriteLine("Server: " + server.EndPoint + " is dead. Selecting the next one");
                    if (!_clients.Any(a => a.IsAlive))
                    {
                        client = null;
                        return false;
                    }
                    var index = _clients.IndexOf(server);
                    var initialIndex = index;

                    if (++index >= _clients.Count)
                    {
                        index = 0;
                    }

                    while (!(server = _clients[index++]).IsAlive)
                    {
                        if (index == initialIndex) //We're right back where we started. No server is online.
                        {
                            Debug.WriteLine("No server is up. Returning null-server");
                            client = null;
                            return false;
                        }
                    }

                    Debug.WriteLine("Failing over to: " + server.EndPoint);
                }

                client = server;
                return true;
            }

            client = null;
            return false;
        }

        private async Task<T> Execute<T>(string key, Func<IMemcachedSharpClient, Task<T>> func)
        {
            MemcachedSharpClient server;
            if (!TryGetClient(key, out server))
            {
                return default(T);
            }

            try
            {
                return await func(server);
            }
            catch (PoolCreationException e)
            {
                server.IsAlive = false;
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
            }
            return await Execute(key, func);
        }

        private async Task Execute(string key, Func<IMemcachedSharpClient, Task> func)
        {
            MemcachedSharpClient server;
            if (!TryGetClient(key, out server))
            {
                return;
            }

            try
            {
                await func(server);
            }
            catch (PoolCreationException e)
            {
                server.IsAlive = false;
            }
            catch (SocketException e)
            {
                server.IsAlive = false;
            }
            await Execute(key, func);
        }

        public async Task<MemcachedItem> Get(string key)
        {
            return await Execute(key, a => a.Get(key));
        }

        public async Task<MemcachedItem> Gets(string key)
        {
            return await Execute(key, a => a.Gets(key));
        }

        public async Task Set(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            await Execute(key, a => a.Set(key, value, options));
        }

        public async Task<bool> Delete(string key)
        {
            return await Execute(key, a => a.Delete(key));
        }

        public async Task<bool> Add(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return await Execute(key, a => a.Add(key, value, options));
        }

        public async Task<bool> Replace(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return await Execute(key, a => a.Replace(key, value, options));
        }

        public async Task<bool> Append(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return await Execute(key, a => a.Append(key, value, options));
        }

        public async Task<bool> Prepend(string key, byte[] value, MemcachedStorageOptions options = null)
        {
            return await Execute(key, a => a.Prepend(key, value, options));
        }

        public async Task<ulong?> Increment(string key, ulong value)
        {
            return await Execute(key, a => a.Increment(key, value));
        }

        public async Task<ulong?> Decrement(string key, ulong value)
        {
            return await Execute(key, a => a.Decrement(key, value));
        }

        public async Task<CasResult> Cas(string key, long casUnique, byte[] value, MemcachedStorageOptions options = null)
        {
            return await Execute(key, a => a.Cas(key, casUnique, value, options));
        }
    }
}
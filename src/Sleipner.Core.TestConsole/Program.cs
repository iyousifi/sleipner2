using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MemcachedSharp;
using Nito.AsyncEx;
using Sleipner.Cache;
using Sleipner.Cache.Configuration.Expressions;
using Sleipner.Cache.MemcachedSharp;
using Sleipner.Cache.MemcachedSharp.MemcachedWrapper;
using Sleipner.Core.Util;
using SleipnerTestSite.Model.Contract;
using SleipnerTestSite.Service;

namespace Sleipner.Core.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));

            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
        {
            var memcachedCluster = new MemcachedSharpClientCluster(new[] { "localhost:11211" });
            var service = new CrapService();
            var cacheProvider = new MemcachedProvider<ICrapService>(memcachedCluster);
            var sleipnerProxy = new SleipnerCache<ICrapService>(service, cacheProvider);

            sleipnerProxy.Config(a => a.DefaultIs().CacheFor(1).DiscardStale());
            var cachedService = sleipnerProxy.CreateCachedInstance();

            /*while (true)
            {
                var i13 = await cachedService.GetCrapAsync("", 1);

                await cacheProvider.DeleteAsync(a => a.GetCrapAsync("", 1));

                var i2 = await cachedService.GetCrapAsync("", 1);

                Console.ReadLine();
            }*/
        }
    }
}

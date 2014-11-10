using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Cache.Policies;
using Sleipner.Core;

namespace Sleipner.Cache
{
    public class SleipnerCache<T> where T : class
    {
        private readonly T _implementation;
        private readonly ICacheProvider<T> _cache;

        public readonly ICachePolicyProvider<T> CachePolicyProvider;
        internal IList<IConfiguredMethod<T>> ConfiguredMethods = new List<IConfiguredMethod<T>>(); 

        public SleipnerCache(T implementation, ICacheProvider<T> cache)
        {
            _implementation = implementation;
            _cache = cache;

            CachePolicyProvider = new BasicConfigurationProvider<T>();
        }

        public T GetCachedInstance()
        {
            var sleipnerProxy = new SleipnerProxy<T>(_implementation);
            var proxyHandler = new SleipnerCacheProxyHandler<T>(_implementation, CachePolicyProvider, _cache);

            return sleipnerProxy.WrapWith(proxyHandler);
        }

        public void Config(Action<ICachePolicyProvider<T>> expression)
        {
            expression(CachePolicyProvider);
        }
    }
}
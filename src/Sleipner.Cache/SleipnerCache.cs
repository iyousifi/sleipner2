using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Cache.Policies;
using Sleipner.Core;
using Sleipner.Core.Util;

namespace Sleipner.Cache
{
    public class SleipnerCache<T> where T : class
    {
        private readonly T _implementation;
        private readonly ICacheProvider<T> _cache;

        public readonly ICachePolicyProvider<T> CachePolicyProvider;
        private readonly IProxyHandler<T> _proxyHandler;
 
        internal IList<IConfiguredMethod<T>> ConfiguredMethods = new List<IConfiguredMethod<T>>(); 

        public SleipnerCache(T implementation, ICacheProvider<T> cache)
        {
            _implementation = implementation;
            _cache = cache;

            CachePolicyProvider = new BasicConfigurationProvider<T>();
            _proxyHandler = new SleipnerCacheProxyHandler<T>(_implementation, CachePolicyProvider, _cache);
        }

        public T CreateCachedInstance()
        {
            var sleipnerProxy = new SleipnerProxy<T>(_implementation);
            return sleipnerProxy.WrapWith(_proxyHandler);
        }

        public void Config(Action<ICachePolicyProvider<T>> expression)
        {
            expression(CachePolicyProvider);
        }
    }
}
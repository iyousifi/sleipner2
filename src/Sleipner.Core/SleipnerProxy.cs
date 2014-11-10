using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sleipner.Core
{
    public class SleipnerProxy<TInterface> where TInterface : class
    {
        private readonly TInterface _implementation;
        private static readonly IDictionary<Type, Type> _typeCache = new ConcurrentDictionary<Type, Type>();

        public SleipnerProxy(TInterface implementation)
        {
            _implementation = implementation;
        }

        public TInterface WrapWith(IProxyHandler<TInterface> handler)
        {
            var interfaceType = typeof (TInterface);
            Type proxyDelegatorType;
            if (!_typeCache.TryGetValue(interfaceType, out proxyDelegatorType))
            {
                proxyDelegatorType = IlGeneratorProxyGenerator.CreateProxyFor<TInterface>();
                _typeCache[interfaceType] = proxyDelegatorType;
            }

            var proxyInstance = (TInterface)Activator.CreateInstance(proxyDelegatorType, _implementation, handler);
            return proxyInstance;
        }
    }
}

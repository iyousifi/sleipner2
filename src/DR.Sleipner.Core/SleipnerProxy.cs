using System;

namespace Sleipner.Core
{
    public class SleipnerProxy<TInterface> where TInterface : class
    {
        private readonly TInterface _implementation;

        public SleipnerProxy(TInterface implementation)
        {
            _implementation = implementation;
        }

        public TInterface WrapWith(IProxyHandler<TInterface> handler)
        {
            var proxyDelegator = IlGeneratorProxyGenerator.CreateProxyFor<TInterface>();
            var proxyInstance = (TInterface)Activator.CreateInstance(proxyDelegator, _implementation, handler);

            return proxyInstance;
        }
    }
}

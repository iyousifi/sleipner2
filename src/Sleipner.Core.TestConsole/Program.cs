using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Core.Util;

namespace Sleipner.Core.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var sleipnerProxy = new SleipnerProxy<ITestInterface>(new TestImplementation());
            var kk = sleipnerProxy.WrapWith(new Handler());

            kk.GetStuff("sdss");
        }
    }

    public class Handler : IProxyHandler<ITestInterface>
    {
        public Task<TResult> HandleAsync<TResult>(ProxiedMethodInvocation<ITestInterface, TResult> methodInvocation)
        {
            throw new NotImplementedException();
        }

        public TResult Handle<TResult>(ProxiedMethodInvocation<ITestInterface, TResult> methodInvocation)
        {
            throw new NotImplementedException();
        }
    }

    public interface ITestInterface
    {
        T GetStuff<T>(T bla) where T : class;
    }

    public class TestImplementation : ITestInterface
    {
        public T GetStuff<T>(T bla) where T : class
        {
            return bla;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Cache;
using Sleipner.Cache.Configuration.Expressions;
using Sleipner.Cache.MemcachedSharp;
using Sleipner.Cache.MemcachedSharp.MemcachedWrapper;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            (new Program()).DoTest();
        }

        public async void DoTest()
        {
            var cluster = new MemcachedSharpClientCluster(new[] { "localhost:11211" });
            var sleipner = new SleipnerCache<ITestClass>((new TestClass()), new MemcachedProvider<ITestClass>(cluster));
            sleipner.Config(a => a.DefaultIs().CacheFor(10).DiscardStale());
            var proxy = sleipner.CreateCachedInstance();
            var provider = new MemcachedProvider<ITestClass>(cluster);

            var val1 = proxy.GetNewValue();
            var val2 = proxy.GetNewValue();

            if (val1 != val2) throw  new Exception("Fejl: intet caches");

            val1 = proxy.GetNewValue();
            await provider.DeleteAsync(a => a.GetNewValue());
            val2 = proxy.GetNewValue();

            if (val1 == val2) throw new Exception("Fejl: cachet værdi slettes ikke");
        }
    }

    public interface ITestClass
    {
        Guid GetNewValue();
    }

    public class TestClass : ITestClass
    {
        public Guid GetNewValue()
        {
            return Guid.NewGuid();
        }
    }
}

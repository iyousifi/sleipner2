using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Sleipner.Cache.Memcached.MemcachedWrapper.Hashing;

namespace Sleipner.Cache.Memcached.Test
{
    [TestFixture]
    public class ConsistentHashTest
    {
        [Test]
        public void TestDistribution()
        {
            var i1 = new Mock<ITestInterface>();
            var i2 = new Mock<ITestInterface>();
            var consistentHash = new ConsistentHash<ITestInterface>(new[] { i1.Object, i2.Object });

            var iterations = 10000;
            var hash = new SHA256Managed();
            var random = new RNGCryptoServiceProvider();
            for (var i = 0; i < iterations; i++)
            {
                var bytes = hash.ComputeHash(BitConverter.GetBytes(i));
                
                var randomBytes = new byte[10];
                random.GetBytes(randomBytes);

                var key = Convert.ToBase64String(bytes.Concat(randomBytes).ToArray());
                ITestInterface node;
                if (!consistentHash.TryGetNode(key, out node))
                {
                    Assert.Fail("Wat, didn't return node?");
                }

                node.Balls();
            }

            i1.Verify(a => a.Balls(), Times.AtLeast(4000));
            i2.Verify(a => a.Balls(), Times.AtLeast(4000));
        }
    }
}

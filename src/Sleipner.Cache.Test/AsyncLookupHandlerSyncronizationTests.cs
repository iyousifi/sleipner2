using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Sleipner.Cache.LookupHandlers.Async;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Cache.Test.Model;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Test
{
    [TestFixture]
    public class AsyncLookupHandlerSyncronizationTests
    {
        [Test]
        public async void TestEmptyCacheSyncronization()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            const int implReturnValue = 3;
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(implReturnValue);

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.None, null);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject);

            var cacheStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, implReturnValue)).Returns(() =>
            {
                cacheStoreTask.Start();
                return cacheStoreTask;
            });

            const int taskCount = 10;
            var tasks = new Task<int>[taskCount];
            for (var i = 0; i < taskCount; i++)
            {
                tasks[i] = lookupHandler.LookupAsync(invocation);
            }

            await Task.WhenAll(tasks);
            Assert.IsTrue(tasks.All(a => a.Result == implReturnValue));
            implementation.Verify(a => a.AddNumbersAsync(1, 2), Times.Once);
            Assert.IsTrue(cacheStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
            cache.Verify(a => a.StoreAsync(invocation, cachePolicy, implReturnValue), Times.Once);
        }

        [Test]
        public async void TestEmptyCacheExceptionSyncronization()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;
            
            var thrownException = new TestException();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(thrownException);

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.None, null);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject);

            var cacheStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, thrownException)).Returns(() =>
            {
                cacheStoreTask.Start();
                return cacheStoreTask;
            });

            const int taskCount = 10;
            var tasks = new Task<int>[taskCount];
            for (var i = 0; i < taskCount; i++)
            {
                tasks[i] = lookupHandler.LookupAsync(invocation);
            }

            foreach (var t in tasks)
            {
                try
                {
                    await t;
                    Assert.Fail("Task didn't throw exception");
                }
                catch (Exception e)
                {
                }
            }
            
            implementation.Verify(a => a.AddNumbersAsync(1, 2), Times.Once);
            Assert.IsTrue(cacheStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
            cache.Verify(a => a.StoreExceptionAsync(invocation, cachePolicy, thrownException), Times.Once);
        }

        [Test]
        public async void TestStakeCacheSyncronization()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            const int implReturnValue = 3;
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(implReturnValue);

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            const int cachedValue = 7;
            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, cachedValue);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject);

            var cacheStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, implReturnValue)).Returns(() =>
            {
                cacheStoreTask.Start();
                return cacheStoreTask;
            });

            const int taskCount = 10;
            var tasks = new Task<int>[taskCount];
            for (var i = 0; i < taskCount; i++)
            {
                tasks[i] = lookupHandler.LookupAsync(invocation);
            }

            await Task.WhenAll(tasks);
            Assert.IsTrue(tasks.All(a => a.Result == cachedValue));
            implementation.Verify(a => a.AddNumbersAsync(1, 2), Times.Once);
            Assert.IsTrue(cacheStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
            cache.Verify(a => a.StoreAsync(invocation, cachePolicy, implReturnValue), Times.Once);
        }
    }
}

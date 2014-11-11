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
    public class AsyncLookupHandlerTests
    {
        [Test]
        public async void TestNoCachePolicyNull()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(() => null);
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(1 + 2);

            await lookupHandler.LookupAsync(invocationSignature);
        }

        [Test]
        public async void TestNoCachePolicyExpireNow()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(() => new CachePolicy() {CacheDuration = 0});
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(1 + 2);

            await lookupHandler.LookupAsync(invocationSignature);
        }

        [Test]
        public async void TestCachePolicyFresh()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            var cachePolicy = new CachePolicy() {CacheDuration = 20};
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);
            cache.Setup(a => a.GetAsync(invocationSignature, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.Fresh, 1 + 2));
            
            var result = await lookupHandler.LookupAsync(invocationSignature);
            Assert.AreEqual(3, result);
        }

        [Test]
        [ExpectedException(typeof(TestException))]
        public async void TestCachePolicyWithException()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);
            cache.Setup(a => a.GetAsync(invocationSignature, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.Exception, new TestException()));

            var result = await lookupHandler.LookupAsync(invocationSignature);
        }

        [Test]
        public async void TestCachePolicyNone()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(1 + 2);

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.None, null));
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, 1+2)).Returns(Task.Factory.StartNew(() => { }));

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, 3);
        }

        [Test]
        [ExpectedException(typeof(TestException))]
        public async void TestCachePolicyNoneException()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var exceptionThrown = new TestException();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(exceptionThrown);

            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.None, null));
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, exceptionThrown)).Returns(Task.Factory.StartNew(() => { }));

            var result = await lookupHandler.LookupAsync(invocation);
        }

        [Test]
        public async void TestCachePolicyStaleAsyncUpdate()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject);

            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(10);

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, 10)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            });

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, cachedObject.Object);
            Assert.IsTrue(awaitableStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
        }

        [Test]
        public async void TestCachePolicyStaleAsyncUpdateExceptionSupress()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20, BubbleExceptions = false};
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject);

            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(new TestException());

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, cachedObject.Object)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            });

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, cachedObject.Object);
            Assert.IsTrue(awaitableStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
        }

        [Test]
        public async void TestCachePolicyStaleAsyncUpdateExceptionBubble()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20, BubbleExceptions = true };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject);

            var thrownException = new TestException();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(thrownException);

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, thrownException)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            });

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, cachedObject.Object);
            Assert.IsTrue(awaitableStoreTask.Wait(5000), "StoreException action on cache did not appear to have been called");
        }
    }
}

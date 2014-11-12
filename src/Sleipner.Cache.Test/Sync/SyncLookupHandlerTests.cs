using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nito.AsyncEx;
using NUnit.Framework;
using Sleipner.Cache.LookupHandlers.Sync;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Cache.Test.Model;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Test.Sync
{
    [TestFixture]
    public class SyncLookupHandlerTests
    {
        [Test]
        public void TestNoCachePolicyNull()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(() => null).Verifiable();
            implementation.Setup(a => a.AddNumbers(1, 2)).Returns(1 + 2).Verifiable();

            lookupHandler.Lookup(invocationSignature);
            implementation.Verify(a => a.AddNumbers(1, 2), Times.Once);

            policyProvider.VerifyAll();
            implementation.VerifyAll();
        }


        [Test]
        public void TestNoCachePolicyExpireNow()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(() => new CachePolicy() { CacheDuration = 0 });
            implementation.Setup(a => a.AddNumbers(1, 2)).Returns(1 + 2);

            lookupHandler.Lookup(invocationSignature);
            policyProvider.VerifyAll();
            implementation.VerifyAll();
        }

        [Test]
        public void TestCachePolicyFresh()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();
            cache.Setup(a => a.GetAsync(invocationSignature, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.Fresh, 1 + 2)).Verifiable();

            var result = lookupHandler.Lookup(invocationSignature);
            Assert.AreEqual(3, result);
            policyProvider.VerifyAll();
            cache.VerifyAll();
        }

        [Test]
        [ExpectedException(typeof(TestException))]
        public void TestCachePolicyWithException()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocationSignature = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocationSignature.Method;
            var parameters = invocationSignature.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();
            cache.Setup(a => a.GetAsync(invocationSignature, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.Exception, new TestException())).Verifiable();

            var result = lookupHandler.Lookup(invocationSignature);
            policyProvider.VerifyAll();
            cache.VerifyAll();
        }

        [Test]
        public void TestCachePolicyNone()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            implementation.Setup(a => a.AddNumbers(1, 2)).Returns(1 + 2);

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.None, null)).Verifiable();
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, 1 + 2)).Returns(Task.Factory.StartNew(() => { })).Verifiable();

            var result = lookupHandler.Lookup(invocation);
            Assert.AreEqual(result, 3);

            policyProvider.VerifyAll();
            cache.VerifyAll();
        }

        [Test]
        [ExpectedException(typeof(TestException))]
        public void TestCachePolicyNoneException()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            var exceptionThrown = new TestException();
            implementation.Setup(a => a.AddNumbers(1, 2)).Throws(exceptionThrown).Verifiable();

            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.None, null)).Verifiable();
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, exceptionThrown)).Returns(Task.Factory.StartNew(() => { })).Verifiable();

            var result = lookupHandler.Lookup(invocation);
            policyProvider.VerifyAll();
            implementation.VerifyAll();
            cache.VerifyAll();
        }

        [Test]
        public async void TestCachePolicyStaleAsyncUpdate()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject).Verifiable();

            var implementationValue = 10;
            implementation.Setup(a => a.AddNumbers(1, 2)).Returns(implementationValue).Verifiable();

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, implementationValue)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            }).Verifiable();

            var task = await Task.Factory.StartNew(async () =>
            {
                var res = lookupHandler.Lookup(invocation);
                await awaitableStoreTask;
                return res;
            });

            var result = await task;
            Assert.AreEqual(result, cachedObject.Object);
            policyProvider.VerifyAll();
            cache.VerifyAll();
            implementation.VerifyAll();
        }

        [Test]
        public async void TestCachePolicyStaleAsyncUpdateExceptionSupress()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20, BubbleExceptions = false };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject).Verifiable();

            implementation.Setup(a => a.AddNumbers(1, 2)).Throws(new TestException()).Verifiable();

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, cachedObject.Object)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            }).Verifiable();

            var task = await Task.Factory.StartNew(async () =>
            {
                var res = lookupHandler.Lookup(invocation);
                await awaitableStoreTask;
                return res;
            });

            var result = await task;
            Assert.AreEqual(result, cachedObject.Object);

            policyProvider.VerifyAll();
            cache.VerifyAll();
            implementation.VerifyAll();
        }

        [Test]
        public async void TestCachePolicyStaleAsyncUpdateExceptionBubble()
        {
            var implementation = new Mock<ITestInterface>(MockBehavior.Strict);
            var policyProvider = new Mock<ICachePolicyProvider<ITestInterface>>(MockBehavior.Strict);
            var cache = new Mock<ICacheProvider<ITestInterface>>(MockBehavior.Strict);

            var lookupHandler = new SyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20, BubbleExceptions = true };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject).Verifiable();

            var thrownException = new TestException();
            implementation.Setup(a => a.AddNumbers(1, 2)).Throws(thrownException).Verifiable();

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, thrownException)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            }).Verifiable();

            var task = await Task.Factory.StartNew(async () =>
            {
                var res = lookupHandler.Lookup(invocation);
                await awaitableStoreTask;
                return res;
            });

            var result = await task;
            Assert.AreEqual(result, cachedObject.Object);
            policyProvider.VerifyAll();
            cache.VerifyAll();
            implementation.VerifyAll();
        }
    }
}

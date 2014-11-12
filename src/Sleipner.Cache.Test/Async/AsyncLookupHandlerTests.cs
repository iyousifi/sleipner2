using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Sleipner.Cache.LookupHandlers.Async;
using Sleipner.Cache.Model;
using Sleipner.Cache.Policies;
using Sleipner.Cache.Test.Model;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Test.Async
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

            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(() => null).Verifiable();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(1 + 2).Verifiable();

            await lookupHandler.LookupAsync(invocationSignature);
            policyProvider.VerifyAll();
            implementation.VerifyAll();
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

            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(() => new CachePolicy() {CacheDuration = 0}).Verifiable();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(1 + 2).Verifiable();

            await lookupHandler.LookupAsync(invocationSignature);
            policyProvider.VerifyAll();
            implementation.VerifyAll();
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
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();
            cache.Setup(a => a.GetAsync(invocationSignature, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.Fresh, 1 + 2)).Verifiable();
            
            var result = await lookupHandler.LookupAsync(invocationSignature);
            Assert.AreEqual(3, result);
            policyProvider.VerifyAll();
            cache.VerifyAll();
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
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();
            cache.Setup(a => a.GetAsync(invocationSignature, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.Exception, new TestException())).Verifiable();

            var result = await lookupHandler.LookupAsync(invocationSignature);
            policyProvider.VerifyAll();
            cache.VerifyAll();
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
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.None, null)).Verifiable();
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, 1+2)).Returns(Task.Factory.StartNew(() => { })).Verifiable();

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, 3);

            policyProvider.VerifyAll();
            cache.VerifyAll();
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
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            var exceptionThrown = new TestException();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(exceptionThrown).Verifiable();

            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(new CachedObject<int>(CachedObjectState.None, null)).Verifiable();
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, exceptionThrown)).Returns(Task.Factory.StartNew(() => { })).Verifiable();

            var result = await lookupHandler.LookupAsync(invocation);
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

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20 };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject).Verifiable();

            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(10).Verifiable();

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, 10)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            });

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, cachedObject.Object);
            Assert.IsTrue(awaitableStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
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

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20, BubbleExceptions = false};
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy).Verifiable();

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject).Verifiable();

            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(new TestException()).Verifiable();

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreAsync(invocation, cachePolicy, cachedObject.Object)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            }).Verifiable();

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, cachedObject.Object);
            Assert.IsTrue(awaitableStoreTask.Wait(5000), "Store action on cache did not appear to have been called");
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

            var lookupHandler = new AsyncLookupHandler<ITestInterface>(implementation.Object, policyProvider.Object, cache.Object);
            var invocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            var method = invocation.Method;
            var parameters = invocation.Parameters;

            var cachePolicy = new CachePolicy() { CacheDuration = 20, BubbleExceptions = true };
            policyProvider.Setup(a => a.GetPolicy(method, parameters)).Returns(cachePolicy);

            var cachedObject = new CachedObject<int>(CachedObjectState.Stale, 3);
            cache.Setup(a => a.GetAsync(invocation, cachePolicy)).ReturnsAsync(cachedObject).Verifiable();

            var thrownException = new TestException();
            implementation.Setup(a => a.AddNumbersAsync(1, 2)).ThrowsAsync(thrownException).Verifiable();

            var awaitableStoreTask = new Task(() => { });
            cache.Setup(a => a.StoreExceptionAsync(invocation, cachePolicy, thrownException)).Returns(() =>
            {
                awaitableStoreTask.Start();
                return awaitableStoreTask;
            }).Verifiable();

            var result = await lookupHandler.LookupAsync(invocation);
            Assert.AreEqual(result, cachedObject.Object);
            Assert.IsTrue(awaitableStoreTask.Wait(5000), "StoreException action on cache did not appear to have been called");
            policyProvider.VerifyAll();
            cache.VerifyAll();
            implementation.VerifyAll();
        }
    }
}

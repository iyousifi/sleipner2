using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Sleipner.Cache.Configuration;
using Sleipner.Cache.Policies;
using Sleipner.Cache.Test.Model;
using Sleipner.Cache.Configuration.Expressions;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Test.Policies
{
    [TestFixture]
    public class CachePolicyProviderTests
    {
        public ICachePolicyProvider<T> Config<T>(Action<ICachePolicyProvider<T>> expression) where T : class
        {
            var policies = new BasicConfigurationProvider<T>();
            expression(policies);

            return policies;
        }

        [Test]
        public void TestDefaultIsDisabledCache()
        {
            var cachePolicies = Config<ITestInterface>(a =>
            {
                a.DefaultIs().DisableCache();
            });

            var policies = new[]
            {
                cachePolicies.GetPolicy(a => a.AddNumbers(1, 2)),
                cachePolicies.GetPolicy(a => a.AddNumbersAsync(1, 2))
            };
            Assert.IsTrue(policies.All(a => a.CacheDuration == 0));
        }

        [Test]
        public void TestDefaultIs10Sec()
        {
            var cachePolicies = Config<ITestInterface>(a =>
            {
                a.DefaultIs().CacheFor(10);
            });

            var policies = new[]
            {
                cachePolicies.GetPolicy(a => a.AddNumbers(1, 2)),
                cachePolicies.GetPolicy(a => a.AddNumbersAsync(1, 2))
            };
            Assert.IsTrue(policies.All(a => a.CacheDuration == 10));
        }

        [Test]
        public void TestDefaultIsDisabledSpecificIs10()
        {
            var cachePolicies = Config<ITestInterface>(a =>
            {
                a.DefaultIs().DisableCache();
                a.For(b => b.AddNumbersAsync(Param.IsAny<int>(), Param.IsAny<int>())).CacheFor(10);
            });

            Assert.AreEqual(cachePolicies.GetPolicy(a => a.AddNumbers(1, 2)).CacheDuration, 0);
            Assert.AreEqual(cachePolicies.GetPolicy(a => a.AddNumbersAsync(1, 2)).CacheDuration, 10);
        }

        [Test]
        public void TestDefaultIsIs10SpecificDisabled()
        {
            var cachePolicies = Config<ITestInterface>(a =>
            {
                a.DefaultIs().CacheFor(10);
                a.For(b => b.AddNumbersAsync(Param.IsAny<int>(), Param.IsAny<int>())).DisableCache();
            });

            Assert.AreEqual(cachePolicies.GetPolicy(a => a.AddNumbers(1, 2)).CacheDuration, 10);
            Assert.AreEqual(cachePolicies.GetPolicy(a => a.AddNumbersAsync(1, 2)).CacheDuration, 0);
        }

        [Test]
        public void TestParameterless()
        {
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.ParameterlessMethod());
            var proxyContext = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.ParameterlessMethod());

            var matched = configuredMethod.IsMatch(proxyContext.Method, proxyContext.Parameters);
            Assert.IsTrue(matched, "MethodConfig didn't match");
        }

        [Test]
        public void TestParameterless_NoMatch()
        {
            var methodCachePolicy = new CachePolicy();
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.ParameteredMethod("", 1));
            var proxyContext = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.ParameterlessMethod());

            var matched = configuredMethod.IsMatch(proxyContext.Method, proxyContext.Parameters);
            Assert.IsFalse(matched, "MethodConfig didn't match");
        }

        [Test]
        public void TestIsAny()
        {
            var methodCachePolicy = new CachePolicy();
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.ParameteredMethod(Param.IsAny<string>(), Param.IsAny<int>()));

            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("", 0)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod(string.Empty, 123)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("asdfasdf", -1000)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("435wasg", int.MaxValue)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("q435owiderfglæhw354t", int.MinValue)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("sdfhgert", 4654543)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("asdfzxbsergt", 593487348)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("asdfzxbsergt", -45345423)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("asdf3", 989899)));
        }

        [Test]
        public void TestBetweenInts()
        {
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.ParameteredMethod(Param.IsAny<string>(), Param.IsBetween(-1000, 1000)));

            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod(null, -1001)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("0", 1001)));

            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("a", 1000)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("b", 900)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("c", 2)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("dicks", 1)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("eellers", 0)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("f", -900)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("g", -1000)));

            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("h", 1050)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("æabc", 10000)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("hest", int.MaxValue)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("lol", int.MinValue)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("omfg", -102032)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("trololol", 343423)));
        }

        [Test]
        public void TestBetweenDateTimeDelegates()
        {
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.DatedMethod(0, Param.IsBetween<DateTime>(() => DateTime.Now.AddHours(-2), () => DateTime.Now)));

            Thread.Sleep(2000);
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(0, DateTime.Now)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(0, DateTime.Now.AddSeconds(-100))));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(0, DateTime.Now.AddSeconds(-1000))));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(0, DateTime.Now.AddHours(-1))));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(0, DateTime.Now.AddHours(-1))));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.DatedMethod(0, DateTime.Now.AddHours(-2).AddSeconds(-1))));
        }

        [Test]
        public void TestBetweenDateTimeExacts()
        {
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.DatedMethod(100, Param.IsBetween(DateTime.Now.AddHours(-2), DateTime.Now)));

            Thread.Sleep(2000);
            Assert.IsFalse(configuredMethod.IsMatch(a => a.DatedMethod(100, DateTime.Now)));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(100, DateTime.Now.AddSeconds(-100))));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(100, DateTime.Now.AddSeconds(-1000))));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(100, DateTime.Now.AddHours(-1))));
            Assert.IsTrue(configuredMethod.IsMatch(a => a.DatedMethod(100, DateTime.Now.AddHours(-2).AddSeconds(-1))));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.DatedMethod(100, DateTime.Now.AddHours(-2).AddSeconds(-4))));
        }

        [Test]
        public void TestConstants()
        {
            var configuredMethod = GetConfiguredMethod<ITestInterface>(a => a.ParameteredMethod("aaa", 200));

            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("aaa", 201)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("bbb", 200)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod("a", 1)));
            Assert.IsFalse(configuredMethod.IsMatch(a => a.ParameteredMethod(null, 0)));

            Assert.IsTrue(configuredMethod.IsMatch(a => a.ParameteredMethod("aaa", 200)));
        }

        private IConfiguredMethod<T> GetConfiguredMethod<T>(Expression<Action<T>> action) where T : class
        {
            var configuredMethod = new ExpressionConfiguredMethod<T>(action);
            return configuredMethod;
        }
    }
}

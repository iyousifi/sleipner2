using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Sleipner.Core.Test.Model;
using Moq;
using Sleipner.Core.Util;

namespace Sleipner.Core.Test
{
    [TestFixture]
    public class SleipnerTests
    {
        [Test]
        public void TestDirectCallInvocation()
        {
            var implementationMock = new Mock<ITestInterface>(MockBehavior.Strict);
            implementationMock.Setup(a => a.AddNumbers(1, 2)).Returns(1 + 2);
            
            var handlerMock = new Mock<IProxyHandler<ITestInterface>>(MockBehavior.Strict); 
            var proxiedMethodInvocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbers(1, 2));
            handlerMock.Setup(a => a.Handle(proxiedMethodInvocation)).Returns(proxiedMethodInvocation.Invoke(implementationMock.Object));

            var sleipner = new SleipnerProxy<ITestInterface>(implementationMock.Object);
            var proxiedObject = sleipner.WrapWith(handlerMock.Object);

            proxiedObject.AddNumbers(1, 2);
        }

        [Test]
        public void TestPassthroughInvocation()
        {
            var implementationMock = new Mock<ITestInterface>(MockBehavior.Strict);
            implementationMock.Setup(a => a.PassthroughMethod());

            var handlerMock = new Mock<IProxyHandler<ITestInterface>>(MockBehavior.Strict);
            
            var sleipner = new SleipnerProxy<ITestInterface>(implementationMock.Object);
            var proxiedObject = sleipner.WrapWith(handlerMock.Object);

            proxiedObject.PassthroughMethod();
        }

        [Test]
        public async void TestDirectCallInvocationAsync()
        {
            var implementationMock = new Mock<ITestInterface>(MockBehavior.Strict);
            implementationMock.Setup(a => a.AddNumbersAsync(1, 2)).ReturnsAsync(1 + 2);

            var handlerMock = new Mock<IProxyHandler<ITestInterface>>(MockBehavior.Strict);
            var proxiedMethodInvocation = ProxiedMethodInvocationGenerator<ITestInterface>.FromExpression(a => a.AddNumbersAsync(1, 2));
            handlerMock.Setup(a => a.HandleAsync(proxiedMethodInvocation)).Returns(proxiedMethodInvocation.InvokeAsync(implementationMock.Object));

            var sleipner = new SleipnerProxy<ITestInterface>(implementationMock.Object);
            var proxiedObject = sleipner.WrapWith(handlerMock.Object);

            await proxiedObject.AddNumbersAsync(1, 2);
        }
    }
}

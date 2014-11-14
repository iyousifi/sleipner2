using System.Collections.Generic;
using System.Reflection;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Policies
{
    public interface ICachePolicyProvider<T> where T : class
    {
        CachePolicy GetPolicy(MethodInfo methodInfo, IEnumerable<object> arguments);
        CachePolicy GetPolicy<TResult>(ProxiedMethodInvocation<T, TResult> invocation);
        CachePolicy RegisterMethodConfiguration(IConfiguredMethod<T> methodConfiguration);
        CachePolicy GetDefaultPolicy();
    }
}
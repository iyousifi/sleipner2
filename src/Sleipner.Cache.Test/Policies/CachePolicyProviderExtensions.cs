using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Cache.Policies;
using Sleipner.Cache.Test.Model;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Test.Policies
{
    public static class CachePolicyProviderExtensions
    {
        public static CachePolicy GetPolicy<T, TResult>(this ICachePolicyProvider<T> provider, Expression<Func<T, TResult>> expression) where T : class
        {
            var invocation = ProxiedMethodInvocationGenerator<T>.FromExpression(expression);
            return provider.GetPolicy(invocation);
        }
    }
}

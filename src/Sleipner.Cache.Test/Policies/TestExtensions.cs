using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Cache.Policies;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Test.Policies
{
    public static class TestExtensions
    {
        public static bool IsMatch<T, TResult>(this IConfiguredMethod<T> method, Expression<Func<T, TResult>> expression) where T : class
        {
            var request = ProxiedMethodInvocationGenerator<T>.FromExpression(expression);
            return method.IsMatch(request.Method, request.Parameters);
        }
    }
}

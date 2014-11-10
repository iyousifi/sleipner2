using System.Threading.Tasks;
using Sleipner.Core.Util;

namespace Sleipner.Core
{
    public interface IProxyHandler<T> where T : class
    {
        Task<TResult> HandleAsync<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation);
        TResult Handle<TResult>(ProxiedMethodInvocation<T, TResult> methodInvocation);
    }
}

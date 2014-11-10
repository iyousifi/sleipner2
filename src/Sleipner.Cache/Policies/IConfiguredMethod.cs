using System.Collections.Generic;
using System.Reflection;

namespace Sleipner.Cache.Policies
{
    public interface IConfiguredMethod<T> where T : class
    {
        bool IsMatch(MethodInfo method, IEnumerable<object> arguments);
    }
}
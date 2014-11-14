using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sleipner.Cache.Test.Model
{
    public interface ITestInterface
    {
        void PassthroughMethod();
        int AddNumbers(int a, int b);
        Task<int> AddNumbersAsync(int a, int b);
        IEnumerable<string> DatedMethod(int a, DateTime time);

        void VoidMethod();
        IEnumerable<string> FaulyCachedMethod();
        IEnumerable<string> FaulyNonCachedMethod();
        IEnumerable<string> NonCachedMethod();
        IEnumerable<string> ParameterlessMethod();
        IEnumerable<string> ParameteredMethod(string a, int b);
        IEnumerable<string> ParameteredMethod(string a, int b, IList<string> list);
        IList<T> GenericMethod<T>(string str, int number);
        IDictionary<TKey, TValue> StrangeGenericMethod<TValue, TKey>(TKey keys, IEnumerable<TValue> values);
        object LolMethod();
        int RoflMethod();
    }
}

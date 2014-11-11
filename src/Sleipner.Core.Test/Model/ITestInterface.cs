using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Core.Test.Model
{
    public interface ITestInterface
    {
        void PassthroughMethod();
        int AddNumbers(int a, int b);
        Task<int> AddNumbersAsync(int a, int b);

        T GetStuff<T>(T bla) where T : class;
        Task<T> GetStuffAsync<T>(T bla) where T : class;
    }
}

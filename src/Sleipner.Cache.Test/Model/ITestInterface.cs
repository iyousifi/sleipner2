using System.Threading.Tasks;

namespace Sleipner.Cache.Test.Model
{
    public interface ITestInterface
    {
        void PassthroughMethod();
        int AddNumbers(int a, int b);
        Task<int> AddNumbersAsync(int a, int b);
    }
}

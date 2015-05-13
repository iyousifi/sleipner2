using System.Collections.Generic;
using System.Threading.Tasks;

namespace SleipnerTestSite.Model.Contract
{
    public interface ICrapService
    {
        Task<IEnumerable<Crap>> GetCrapAsync(string bla, int rofl);
        IEnumerable<Crap> GetCrap();
    }
}

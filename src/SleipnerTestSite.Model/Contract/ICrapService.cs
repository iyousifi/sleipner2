using System.Collections.Generic;
using System.Threading.Tasks;

namespace SleipnerTestSite.Model.Contract
{
    public interface ICrapService
    {
        Task<IEnumerable<Crap>> GetCrapAsync();
        IEnumerable<Crap> GetCrap();
    }
}

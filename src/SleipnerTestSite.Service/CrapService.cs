using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SleipnerTestSite.Model;
using SleipnerTestSite.Model.Contract;

namespace SleipnerTestSite.Service
{
    public class CrapService : ICrapService
    {
        public async Task<IEnumerable<Crap>> GetCrapAsync()
        {
            return await Task.Factory.StartNew(() => new[]
            {
                new Crap()
                {
                    CrapID = 1,
                    Name = "Ultra crap"
                },
                new Crap()
                {
                    CrapID = 2,
                    Name = "Mega crap"
                }
            });
        }

        public IEnumerable<Crap> GetCrap()
        {
            return new[]
            {
                new Crap()
                {
                    CrapID = 1,
                    Name = "Ultra crap"
                },
                new Crap()
                {
                    CrapID = 2,
                    Name = "Mega crap"
                }
            };
        }
    }
}
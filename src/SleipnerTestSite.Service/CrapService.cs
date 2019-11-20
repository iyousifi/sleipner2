using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SleipnerTestSite.Model;
using SleipnerTestSite.Model.Contract;

namespace SleipnerTestSite.Service
{
    public class CrapService : ICrapService
    {
        public async Task<IEnumerable<Crap>> GetCrapAsync(string bla, int rofl)
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

        public async Task<Crap> GetEvenMoreCrap(int crapId)
        {
            Thread.Sleep(2000);
            return await Task.Factory.StartNew(() =>

                new Crap()
                {
                    CrapID = 1337,
                    Name = "Ultra crap"
                }
        );
        }
    }
}
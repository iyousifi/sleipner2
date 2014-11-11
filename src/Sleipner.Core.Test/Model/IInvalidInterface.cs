using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Core.Test.Model
{
    public interface IInvalidInterface
    {
        T GetStuff<T>(T crap);
    }
}

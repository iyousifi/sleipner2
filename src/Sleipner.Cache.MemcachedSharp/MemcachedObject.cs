using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Cache.MemcachedSharp
{
    public class MemcachedObject<TObject>
    {
        public TObject Object;
        public bool IsException;
        public DateTime Created;
        public Exception Exception;
    }
}

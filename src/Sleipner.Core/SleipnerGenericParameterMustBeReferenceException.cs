using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sleipner.Core
{
    public class SleipnerGenericParameterMustBeReferenceException : Exception
    {
        public SleipnerGenericParameterMustBeReferenceException(MethodInfo method, Type t) : base("You must constraint method " + method.Name + " with generic parameter " + t.Name + " to be a reference type only (where " + t.Name + " : class)")
        {
            
        }
    }
}

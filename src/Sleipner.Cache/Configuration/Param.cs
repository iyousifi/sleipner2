using System;

namespace Sleipner.Cache.Configuration
{
    public static class Param
    {
        public static TResult IsAny<TResult>()
        {
            return default(TResult);
        }

        public static TResult IsBetween<TResult>(TResult lower, TResult upper) where TResult : IComparable
        {
            return default(TResult);
        }

        public static TResult IsBetween<TResult>(Func<IComparable> lower, Func<IComparable> upper) where TResult : IComparable
        {
            return default(TResult);
        }
    }
}
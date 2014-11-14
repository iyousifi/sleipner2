using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Sleipner.Core.Util;

namespace Sleipner.Cache.Extensions
{
    public static class ProxiedMethodInvocationExtensions
    {
        public static byte[] GetHashBytes<T, TResult>(this ProxiedMethodInvocation<T, TResult> invocation, string scrambleString = null) where T : class
        {
            var sb = new StringBuilder();
            sb.Append(typeof(T).FullName);
            sb.Append(" - ");
            sb.Append(invocation.Method);
            sb.Append(" - ");
            sb.AddParameterRepresentations(invocation.Parameters);
            if (!string.IsNullOrEmpty(scrambleString))
            {
                sb.Append(" - ");
                sb.Append(scrambleString);
            }
            
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hashAlgorithm = new SHA256Managed();
            var hash = hashAlgorithm.ComputeHash(bytes);

            return hash;
        }

        public static string GetHashString<T, TResult>(this ProxiedMethodInvocation<T, TResult> invocation, string scrambleString = null) where T : class
        {
            return Convert.ToBase64String(GetHashBytes(invocation, scrambleString));
        }

        private static void AddParameterRepresentations(this StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append(".null");
            }
            else if (value is string)
            {
                builder.Append("\"" + value + "\"");
            }
            else if (value is IEnumerable)
            {
                var ienum = (IEnumerable)value;
                var collection = ienum.Cast<object>().ToArray();
                builder.Append("[");
                for (var i = 0; i < collection.Length; i++)
                {
                    builder.AddParameterRepresentations(collection[i]);
                    if (i < collection.Length - 1)
                    {
                        builder.Append(",");
                    }

                }
                builder.Append("]");
            }
            else if (value is DateTime)
            {
                var dt = (DateTime)value;
                builder.Append(dt.ToString(CultureInfo.InvariantCulture));
            }
            else if (value is Boolean)
            {
                builder.Append(((bool)value).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(value.ToString());
            }
        }
    }
}

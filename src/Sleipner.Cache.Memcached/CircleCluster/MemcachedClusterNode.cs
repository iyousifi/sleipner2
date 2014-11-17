using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemcachedSharp;

namespace Sleipner.Cache.Memcached.CircleCluster
{
    public class MemcachedClusterNode
    {
        private readonly string _endPoint;
        public MemcachedClient Client;
        public bool IsAlive;

        public MemcachedClusterNode(string endPoint, MemcachedOptions options = null)
        {
            _endPoint = endPoint;
            Client = new MemcachedClient(endPoint, options);
            IsAlive = true;
        }

        protected bool Equals(MemcachedClusterNode other)
        {
            return string.Equals(_endPoint, other._endPoint);
        }

        public override int GetHashCode()
        {
            return (_endPoint != null ? _endPoint.GetHashCode() : 0);
        }
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MemcachedClusterNode) obj);
        }
    }
}

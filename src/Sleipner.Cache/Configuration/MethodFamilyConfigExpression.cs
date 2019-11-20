﻿using System;
using Sleipner.Cache.Policies;

namespace Sleipner.Cache.Configuration
{
    public class MethodFamilyConfigExpression : IMethodFamilyConfigurationExpression
    {
        private readonly CachePolicy _policy;

        public MethodFamilyConfigExpression(CachePolicy policy)
        {
            _policy = policy;
        }

        public IMethodFamilyConfigurationExpression CacheFor(int duration)
        {
            _policy.CacheDuration = duration;

            if (_policy.MaxAge == 0)
                _policy.MaxAge = duration;

            return this;
        }

        public IMethodFamilyConfigurationExpression CacheExceptionsFor(int duration)
        {
            _policy.ExceptionCacheDuration = duration;

            return this;
        }

        public IMethodFamilyConfigurationExpression ExpireAfter(int maxDuration)
        {
            if(maxDuration <= _policy.CacheDuration)
            {
                throw new ArgumentException("Duration of expirey must be larger than Cache duration");
            }

            _policy.MaxAge = maxDuration;

            return this;
        }

        public void DisableCache()
        {
            _policy.CacheDuration = 0;
        }

        public IMethodFamilyConfigurationExpression SupressExceptionsWhenStale()
        {
            _policy.BubbleExceptions = false;

            return this;
        }

        public IMethodFamilyConfigurationExpression BubbleExceptionsWhenStale()
        {
            _policy.BubbleExceptions = true;

            return this;
        }

        public IMethodFamilyConfigurationExpression DiscardStale()
        {
            _policy.DiscardStale = true;

            return this;
        }
    }
}

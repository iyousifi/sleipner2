// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IoC.cs" company="Web Advanced">
// Copyright 2012 Web Advanced (www.webadvanced.com)
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


using MemcachedSharp;
using Sleipner.Cache;
using Sleipner.Cache.Configuration.Expressions;
using Sleipner.Cache.DictionaryCache;
using Sleipner.Cache.Memcached;
using SleipnerTestSite.Model.Contract;
using SleipnerTestSite.Service;
using StructureMap;
using StructureMap.Graph;
namespace SleipnerTestSite.DependencyResolution {
    public static class IoC {
        public static IContainer Initialize()
        {
            var proxy = new SleipnerCache<ICrapService>(new CrapService(), new DictionaryCacheProvider<ICrapService>());

            proxy.Config(a =>
            {
                a.DefaultIs().CacheFor(10);
            });

            ObjectFactory.Initialize(x =>
            {
                x.For<ICrapService>().Singleton().Use(proxy.CreateCachedInstance());
            });
            return ObjectFactory.Container;
        }
    }
}
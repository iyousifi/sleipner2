using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MemcachedSharp;
using Sleipner.Cache.Memcached.CircleCluster;
using SleipnerTestSite.Model.Contract;

namespace SleipnerTestSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICrapService _crapService;
        private readonly IMemcachedClient _client;

        public HomeController(ICrapService crapService, IMemcachedClient client)
        {
            _crapService = crapService;
            _client = client;
        }

        public async Task<ActionResult> Index()
        {
            await _client.Get("blaaa");
            await _client.Get("blaaa1");
            await _client.Get("blaaa2");
            await _client.Get("blaaa3");


            string data = null; //_crapService.GetCrap();
            return Json(new { balls = data }, JsonRequestBehavior.AllowGet);
        }
    }
}
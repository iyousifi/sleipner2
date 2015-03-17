using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MemcachedSharp;
using Sleipner.Cache.MemcachedSharp.MemcachedWrapper;
using SleipnerTestSite.Model.Contract;

namespace SleipnerTestSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICrapService _crapService;
        private readonly IMemcachedSharpClient _client;

        public HomeController(ICrapService crapService, IMemcachedSharpClient client)
        {
            _crapService = crapService;
            _client = client;
        }

        public async Task<ActionResult> Index()
        {
            await _client.Get("blaaa");
            await _client.Get("1234asf");


            string data = null; //_crapService.GetCrap();
            return Json(new { balls = data }, JsonRequestBehavior.AllowGet);
        }
    }
}
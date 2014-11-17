using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MemcachedSharp;
using SleipnerTestSite.Model.Contract;

namespace SleipnerTestSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICrapService _crapService;

        public HomeController(ICrapService crapService)
        {
            _crapService = crapService;
        }

        public async Task<ActionResult> Index()
        {
            var memcached = new MemcachedClient("localhost:11211");
            var mcdata = await memcached.Get("blaaa");

            var kk = "";

            string data = null; //_crapService.GetCrap();
            return Json(new {balls = mcdata}, JsonRequestBehavior.AllowGet);
        }
    }
}
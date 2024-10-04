using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Test()
        {
            return View("~/Views/TestView.cshtml");
        }
    }
}






//Issuer Id : 11111111 - 1111 - 1111 - 1111 - 111111111111
//Certificate path : C:\Certs\SP_Certificate.pfx
//Client Id : e8cb3d4d-a7cc-4810-8285-3576afa873b4



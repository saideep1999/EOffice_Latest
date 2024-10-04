using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class CommonViewController : Controller
    {
        // GET: CommonView
        //public ActionResult Index()
        //{
        //    return View();
        //}

        public ActionResult View()
        {
            return View("~/Views/Shared/ViewName.cshtml");
        }
    }
}
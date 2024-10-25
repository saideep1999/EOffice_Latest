using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class ERFController : BaseController
    {
        ERFDAL erfDAL;
        // GET: ERF
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> SaveERF(FormCollection form)
        {
            erfDAL = new ERFDAL();
            var result = await erfDAL.SaveERF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
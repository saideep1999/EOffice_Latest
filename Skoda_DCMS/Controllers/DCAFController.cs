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
    public class DCAFController : BaseController
    {
        // GET: DCAF
        public ActionResult Index()
        {
            return View();
        }
        DCAFDAL dcafDAL;

        [HttpPost]
        public async Task<ActionResult> SaveDCAF(FormCollection form)
        {
            dcafDAL = new DCAFDAL();
            var result = await dcafDAL.SaveDCAF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
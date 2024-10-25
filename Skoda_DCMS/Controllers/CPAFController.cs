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
    public class CPAFController : BaseController
    {
        // GET: CPAF
        public ActionResult Index()
        {
            return View();
        }
        CPAFDAL CPAFDAL;

        [HttpPost]
        public async Task<ActionResult> SaveCPAF(FormCollection form)
        {
            CPAFDAL = new CPAFDAL();
            var result = await CPAFDAL.SaveCPAF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
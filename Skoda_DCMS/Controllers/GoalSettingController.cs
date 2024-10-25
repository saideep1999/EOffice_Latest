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
    public class GoalSettingController : Controller
    {
        GSFDAL GSFDAL;
        // GET: GoalSetting
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveGSFDATA(FormCollection form)
        {
            HttpPostedFileBase file = null;

            GSFDAL = new GSFDAL();
            var result = await GSFDAL.SaveGSFDAL(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
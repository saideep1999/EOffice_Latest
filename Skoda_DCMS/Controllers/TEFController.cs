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
    public class TEFController : BaseController
    {
        // GET: TEF
        public ActionResult Index()
        {
            return View();
        }
        TEFDAL tefDAL;

        [HttpPost]
        public async Task<ActionResult> SaveTEF(FormCollection form)
        {
            tefDAL = new TEFDAL();
            var result = await tefDAL.SaveTEF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
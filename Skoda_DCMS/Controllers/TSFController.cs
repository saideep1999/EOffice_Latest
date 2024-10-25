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
    public class TSFController : BaseController
    {
        TSFDAL tsfDAL;

        [HttpPost]
        public async Task<ActionResult> SaveTSF(FormCollection form)
        {
            tsfDAL = new TSFDAL();
            var result = await tsfDAL.SaveTSF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // GET: TSF
        public ActionResult Index()
        {
            return View();
        }
    }
}
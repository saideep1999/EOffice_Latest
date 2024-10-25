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
    public class NEIFController : BaseController
    {
        NEIFDAL neifDAL;

        [HttpPost]
        public async Task<ActionResult> SaveNEIF(FormCollection form)
        {
            neifDAL = new NEIFDAL();
            var result = await neifDAL.SaveNEIF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // GET: NEIF
        public ActionResult Index()
        {
            return View();
        }
    }
}
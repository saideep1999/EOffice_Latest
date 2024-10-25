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
    public class LAFController : BaseController
    {
        // GET: LAF
        LAFDAL lafDAL;
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> SaveLAF(FormCollection form)
        {

            lafDAL = new LAFDAL();
            var result = await lafDAL.SaveLAF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
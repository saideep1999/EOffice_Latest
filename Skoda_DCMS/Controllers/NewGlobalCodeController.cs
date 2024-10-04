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
    public class NewGlobalCodeController : Controller
    {
        NewGlobalCodeDAL NewGlobalCodeDAL;

        [HttpPost]
        public async Task<ActionResult> SaveNewGlobalCode(NewGlobalCodeData model)
        {
            NewGlobalCodeDAL = new NewGlobalCodeDAL();
            var result = await NewGlobalCodeDAL.SaveNewGlobalCode(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<ActionResult> UpdateNGCFData(FormCollection form)
        {
            NewGlobalCodeDAL = new NewGlobalCodeDAL();
            var result = await NewGlobalCodeDAL.UpdateNGCFData(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
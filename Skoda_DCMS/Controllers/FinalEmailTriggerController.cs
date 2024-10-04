using Skoda_DCMS.App_Start;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class FinalEmailTriggerController : Controller
    {
        public UserData user;

        // GET: FinalEmailTrigger
        public ActionResult FinalEmail()
        {
            return View();
        }
               

        [HttpPost]
        public async Task<ActionResult> SendFinalEmail(FormCollection form)
        {
            FinalEmailDAL FinalEmailDAL = new FinalEmailDAL();
            var result = FinalEmailDAL.SendFinalEmail(form);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> SendFinalEmailWithoutApprover(FormCollection form)
        {
            FinalEmailDAL FinalEmailDAL = new FinalEmailDAL();
            var result = FinalEmailDAL.SendFinalEmailWithoutApprover(form);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
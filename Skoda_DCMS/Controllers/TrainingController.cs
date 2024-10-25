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
    public class TrainingController : Controller
    {
        TRFDAL TRFDAL;
        // GET: Training
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> SaveTRFDATA(FormCollection form)
        { 
            TRFDAL = new TRFDAL();
            var result = await TRFDAL.SaveTRFDAL(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
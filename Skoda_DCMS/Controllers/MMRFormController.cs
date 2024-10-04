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
    public class MMRFormController : Controller
    {
        MMRFormDAL MMRFDAL;
        // GET: MMRForm
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveMMRForm(MMRData model)
        {
            HttpPostedFileBase file = null;
            HttpPostedFileBase file1 = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
                //if (model.PSAttach1 == 1)
                //{
                //    file = files[0];
                //}
            }
            if (Request.Files.Count > 0 && Request.Files[1].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file1 = files[1];
            }
            MMRFDAL = new MMRFormDAL();
            var result = await MMRFDAL.SaveMMRForm(model, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateData(MMRData model)
        {
            MMRFDAL = new MMRFormDAL();
            var result = await MMRFDAL.UpdateData(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

      
    }
}
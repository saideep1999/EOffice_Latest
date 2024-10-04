using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System.Web.Mvc;
using System.Threading.Tasks;

namespace Skoda_DCMS.Controllers
{
    public class FixtureRequisitionController : Controller
    {
        FixtureRequisitionDAL QFRFDAL;
        // GET: FixtureRequisition
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveQFRFForm(FixtureRequisitionData model)
        {
            HttpPostedFileBase file = null;
            HttpPostedFileBase file1 = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
              
            }
            if (Request.Files.Count > 0 && Request.Files[1].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file1 = files[1];
            }
            QFRFDAL = new FixtureRequisitionDAL();
            var result = await QFRFDAL.SaveQFRFForm(model, (UserData)Session["UserData"], file, file1);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateData(FixtureRequisitionData model)
        {
            QFRFDAL = new FixtureRequisitionDAL();
            var result = await QFRFDAL.UpdateData(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateCondition(QualityMeisterbockCubingData model)
        {
            QFRFDAL = new FixtureRequisitionDAL();
            var result = await QFRFDAL.UpdateCondition(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


    }
}
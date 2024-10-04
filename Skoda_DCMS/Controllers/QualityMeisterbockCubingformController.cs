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
    public class QualityMeisterbockCubingformController : BaseController
    {
        QualityMeisterbockCubingDAL QMCRFDAL;
        // GET: QualityMeisterbockCubingform
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveQMCRForm(QualityMeisterbockCubingData model)
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
            QMCRFDAL = new QualityMeisterbockCubingDAL();
            var result = await QMCRFDAL.SaveQMCRForm(model, (UserData)Session["UserData"], file, file1);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateData(QualityMeisterbockCubingData model)
        {
            QMCRFDAL = new QualityMeisterbockCubingDAL();
            var result = await QMCRFDAL.UpdateData(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateCondition(QualityMeisterbockCubingData model)
        {
            QMCRFDAL = new QualityMeisterbockCubingDAL();
            var result = await QMCRFDAL.UpdateCondition(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
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

    public class OCRFController : BaseController
    {
        OCRFDAL OCRFDAL;

        public ActionResult GetOCRFEmployeeDetails(string searchText)
        {
            OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetOCRFEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetDetByReportingMgrTo(string otherEmpUserId)
        {
            OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetDetByReportingMgrTo(otherEmpUserId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetOCRFExistingEmployeeDetails(string otherEmpUserId)
        {
            OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetOCRFExistingEmployeeDetails(otherEmpUserId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetOCRFCCDetails(string searchText)
        {
            OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetCCDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> SaveOCRF(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            OCRFDAL = new OCRFDAL();
            var result = await OCRFDAL.SaveOCRF(form, (UserData)Session["UserData"], file);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetDivisionMaster(string divId, string deptId)
        {
            OCRFDAL OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetDivisionMaster(divId, deptId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> GetDepartmentMaster(string divId, string deptId)
        {
            OCRFDAL OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetDepartmentMaster(divId, deptId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> GetSubDepartmentMaster(string divId, string deptId)
        {
            OCRFDAL OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.GetSubDepartmentMaster(divId, deptId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> ActionUpdate(FormCollection form)
        {
            OCRFDAL OCRFDAL = new OCRFDAL();
            var result = OCRFDAL.ActionUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
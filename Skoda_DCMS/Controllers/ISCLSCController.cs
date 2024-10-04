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
    public class ISCLSCController : BaseController
    {
        ISCLSCDAL ISCLSCDAL;
        // GET: ISCLSC
        public ActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> SaveISCLSCForm(ISCLSData model)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            ISCLSCDAL = new ISCLSCDAL();
            var result = await ISCLSCDAL.SaveISCLSCForm(model, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<ActionResult> UpdateData(ISCLSData model)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            ISCLSCDAL = new ISCLSCDAL();
            var result = await ISCLSCDAL.UpdateData(model, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateBidApprovalDate(ISCLSData model)
        {
            ISCLSCDAL = new ISCLSCDAL();
            model.BidderApprovalDate=System.DateTime.Now;
            var result = await ISCLSCDAL.UpdateBidApprovalDate(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> ValidateGPNo(string GlobalProcessNo)
        {
            ISCLSCDAL = new ISCLSCDAL();
            var result = await ISCLSCDAL.ValidateGPNo(GlobalProcessNo);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateDataForLST(ISCLSData model)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            ISCLSCDAL = new ISCLSCDAL();
            model.BidderApprovalDate = System.DateTime.Now;
            var result = await ISCLSCDAL.UpdateDataForLST(model, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
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
    public class IMACFormController : BaseController
    {

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveData(IMACFormModel model)
        {
            var dal = new IMACDAL();
            var result = await dal.SaveData(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
            //var dal = new IMACDAL();
            //var result = await dal.SaveData(model);
            //return Json(result, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public async Task<ActionResult> UpdateData(IMACFormModel model)
        {

            IMACDAL IMACDAL = new IMACDAL();
            var result = await IMACDAL.UpdateData(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetAssets()
        {
            IMACDAL IMACDAL = new IMACDAL();
            var result = await IMACDAL.GetAssets();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> GetSubAssets(string AssetName)
        {
            IMACDAL IMACDAL = new IMACDAL();
            var result = await IMACDAL.GetSubAssets(AssetName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetIMACFormEmployeeDetails(string searchText)
        {
            IMACDAL IMACDAL = new IMACDAL();
            var result = IMACDAL.GetIMACEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        //public async Task<ActionResult> UpdateBidApprovalDate(IMACFormModel model)
        //{
        //    IMACDAL IMACDAL = new IMACDAL();
        //    model.BidderApprovalDate = System.DateTime.Now;
        //    var result = await IMACDAL.UpdateBidApprovalDate(model, (UserData)Session["UserData"]);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

       

    }
}
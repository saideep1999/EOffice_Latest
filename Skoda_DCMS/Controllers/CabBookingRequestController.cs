using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class CabBookingRequestController : BaseController
    {
        CabBookingRequestDAL CabBookingRequestDAL;


        [HttpPost]
        public async Task<ActionResult> SaveCBRF(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];             
            }
            CabBookingRequestDAL = new CabBookingRequestDAL();
            var result = await CabBookingRequestDAL.SaveCBRF(form, (UserData)Session["UserData"], file);
            if(result.Status==200)
            {

            }
            //if (result.one == 1)
            //{

            //}
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> TypeOfCarUpdate(FormCollection form)
        {
            CabBookingRequestDAL = new CabBookingRequestDAL();
            var result = await CabBookingRequestDAL.TypeOfCarUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetCBRFApprovalList()
        {
            CabBookingRequestDAL = new CabBookingRequestDAL();
            var result = await CabBookingRequestDAL.GetCBRFApprovalList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetTypeofCar()
        {
            CabBookingRequestDAL = new CabBookingRequestDAL();
            var result = await CabBookingRequestDAL.GetTypeofCar();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetVehicleNumber(string carId)
        {
            CabBookingRequestDAL = new CabBookingRequestDAL();
            var result = await CabBookingRequestDAL.GetVehicleNumber(carId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
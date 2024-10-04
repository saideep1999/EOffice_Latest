using Microsoft.AspNetCore.Http;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class DrivingAuthorizationController : BaseController
    {
        DrivingAuthorizationDAL DrivingAuthorizationDAL;

        [HttpPost]
        public async Task<ActionResult> SaveDAF(FormCollection form)
        {
            HttpPostedFileBase file = null, file1 = null;

            if (Request.Files.Count > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                //file will be Photo only and file1 => LicensePhotoCopy
                file = Request.Files[0].ContentLength > 0 ? files["photo"] : null;
                file1 = Request.Files[1].ContentLength > 0 ? files["licensePhotoCopy"] : null;
              
            }
            DrivingAuthorizationDAL = new DrivingAuthorizationDAL();
            var result = await DrivingAuthorizationDAL.SaveDAF(form, (UserData)Session["UserData"], file, file1);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<ActionResult> SaveApproverResponse(FormCollection form)
        {
            DrivingAuthorizationDAL = new DrivingAuthorizationDAL();
            var result = DrivingAuthorizationDAL.SaveApproverResponse(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

       
    }
}
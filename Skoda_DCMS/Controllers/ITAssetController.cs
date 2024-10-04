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
    public class ITAssetController : BaseController
    {
        ITAssetDAL iTAssetDAL;
        /// <summary>
        /// IT Asset Requisition Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateITAssetRequisitionRequest(FormCollection form)
        {
            iTAssetDAL = new ITAssetDAL();
            var result = await iTAssetDAL.CreateITAssetRequisitionRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        //public async Task<ActionResult> ITARFApprovalUpdate(FormCollection form)
        //{
        //    iTAssetDAL = new ITAssetDAL();
        //    var result = iTAssetDAL.ITARFApprovalUpdate(form, (UserData)Session["UserData"]);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
    }
}
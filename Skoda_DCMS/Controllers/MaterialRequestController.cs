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
    public class MaterialRequestController : Controller
    {
        MaterialRequestDAL materialRequestDAL;
        /// <summary>
        /// ReissueIDCard-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreateMaterialRequest(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            materialRequestDAL = new MaterialRequestDAL();
            var result = await materialRequestDAL.CreateMaterialRequest(form, (UserData)Session["UserData"],file);
            if (result.Status == 200 && result.IsResubmit == false)
            {
                await UpdateRequestNumber(result.RowId);
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateRequestNumber(string rowId)
        {
            materialRequestDAL = new MaterialRequestDAL();
            var result = await materialRequestDAL.UpdateRequestNumber((UserData)Session["UserData"], rowId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
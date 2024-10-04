using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Skoda_DCMS.Filters;
using Skoda_DCMS.Helpers;

namespace Skoda_DCMS.Controllers
{
    public class ITClearanceRequestController : BaseController
    {
        ITClearanceRequestDAL iTClearanceRequestDAL;
        /// <summary>
        /// IT Clearance Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        //[ListActionFilter]
        public async Task<ActionResult> CreateITClearanceRequest(FormCollection form)
        {
            iTClearanceRequestDAL = new ITClearanceRequestDAL();
            var result = await iTClearanceRequestDAL.CreateITClearanceRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
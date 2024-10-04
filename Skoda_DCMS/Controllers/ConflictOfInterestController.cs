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
    public class ConflictOfInterestController : BaseController
    {
        ConflictOfInterestDAL conflictOfInterestDAL;
        /// <summary>
        /// Conflict Of Interest Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateConflictOfInterestRequest(FormCollection form)
        {
            conflictOfInterestDAL = new ConflictOfInterestDAL();
            var result = await conflictOfInterestDAL.CreateConflictOfInterestRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
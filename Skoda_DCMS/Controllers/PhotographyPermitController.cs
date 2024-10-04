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
    public class PhotographyPermitController : BaseController
    {
        PhotographyPermitDAL PhotographyPermitDAL;
        
        /// <summary>
        /// PAF-It is used for Saving data.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SavePPF(FormCollection form)
        {
            PhotographyPermitDAL = new PhotographyPermitDAL();
            var result = await PhotographyPermitDAL.SavePPF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// PAF-Allows the approver to select the validity dates for the Photography Form.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ValidityUpdate(FormCollection form)
        {
            PhotographyPermitDAL = new PhotographyPermitDAL();
            var result = await PhotographyPermitDAL.ValidityUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Areas()
        {
            PhotographyPermitDAL PhotographyPermitDAL = new PhotographyPermitDAL();
            var result = await PhotographyPermitDAL.GetAreas();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
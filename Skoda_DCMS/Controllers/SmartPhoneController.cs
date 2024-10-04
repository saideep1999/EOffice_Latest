using Rotativa;
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
    public class SmartPhoneController : BaseController
    {

        SmartPhoneDAL smartPhoneDAL;

        /// <summary>
        /// SmartPhone-It is used save data in sharepoint list.
        /// </summary>
        /// <returns></returns>

        public async Task<ActionResult> CreateSmartPhoneRequisitionRequest(FormCollection form)
        {
            smartPhoneDAL = new SmartPhoneDAL();
            var result = await smartPhoneDAL.CreateSmartPhoneRequisitionRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
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
    public class KSRMUserIdController : BaseController
    {
        KSRMUserIdDAL kSRMUserIdDAL;
        /// <summary>
        /// KSRM User Id Creation-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateKSRMUserIdCreationRequest(FormCollection form)
        {
            kSRMUserIdDAL = new KSRMUserIdDAL();
            var result = await kSRMUserIdDAL.CreateKSRMUserIdCreationRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
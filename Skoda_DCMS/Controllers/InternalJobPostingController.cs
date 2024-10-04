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
    public class InternalJobPostingController : BaseController
    {
        InternalJobPostingDAL InternalJobPostingDAL;
        public async Task<ActionResult> CreateInternalJobPostingFormRequest(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            InternalJobPostingDAL = new InternalJobPostingDAL();
            var result = await InternalJobPostingDAL.CreateInternalJobPostingFormRequest(form, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
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
    public class ReissueIDCardController : Controller
    {
        ReissueIDCardDAL reissueIDCardDAL;
        /// <summary>
        /// ReissueIDCard-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreateReissueIDCardRequest(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if(Request.Files.Count >0 && Request.Files[0].ContentLength>0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            reissueIDCardDAL = new ReissueIDCardDAL();
            var result = await reissueIDCardDAL.CreateReissueIDCardRequest(form, (UserData)Session["UserData"],file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public async Task<ActionResult> RIDCFValidityUpdate(FormCollection form)
        {
            reissueIDCardDAL = new ReissueIDCardDAL();
            var result = reissueIDCardDAL.RIDCFValidityUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ReasonforReissue()
        {
            reissueIDCardDAL = new ReissueIDCardDAL();
            var list = reissueIDCardDAL.ReasonforReissue();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

    }
}
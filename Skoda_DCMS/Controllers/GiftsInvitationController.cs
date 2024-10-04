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
    public class GiftsInvitationController : Controller
    {
        GiftsInvitationDAL giftsInvitationDAL;

        [HttpPost]
        public async Task<ActionResult> SaveGiftsInvitation(GiftsInvitationData model)
        {
            HttpPostedFileBase  file1 = null;
            if (Request.Files.Count > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                //file = Request.Files[0].ContentLength > 0 ? files["fileToUpload"] : null;
                file1 = Request.Files[0].ContentLength > 0 ? files["docUpload"] : null;
            }
            giftsInvitationDAL = new GiftsInvitationDAL();
            var result = await giftsInvitationDAL.SaveGiftsInvitation(model, (UserData)Session["UserData"],file1);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public async Task<ActionResult> UpdateGAIFData(FormCollection form)
        {
            giftsInvitationDAL = new GiftsInvitationDAL();
            var result = await giftsInvitationDAL.UpdateGAIFData(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
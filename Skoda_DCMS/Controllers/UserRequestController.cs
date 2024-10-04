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
    public class UserRequestController : Controller
    {
        UserRequestDAL UserRequestDAL;

        [HttpPost]
        public async Task<ActionResult> SaveData(UserRequestData model)
        {
            HttpPostedFileBase file = null;
            HttpPostedFileBase file1 = null;
            if (Request.Files.Count > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = Request.Files[0].ContentLength > 0 ? files["docUploadGeKo"] : null;
                file1 = Request.Files[0].ContentLength > 0 ? files["docUploadSAGA2"] : null;
            }
            UserRequestDAL = new UserRequestDAL();
            var result = await UserRequestDAL.SaveData(model, (UserData)Session["UserData"], file, file1);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetApplicationCategoryData()
        {
            UserRequestDAL = new UserRequestDAL();
            var result = UserRequestDAL.GetApplicationCategoryData();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
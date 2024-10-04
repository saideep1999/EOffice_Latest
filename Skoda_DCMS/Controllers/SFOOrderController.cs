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
    public class SFOOrderController : BaseController
    {
        SFOOrderDAL SFOOrderDAL;

        [HttpPost]
        public async Task<ActionResult> SaveOrder(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            SFOOrderDAL = new SFOOrderDAL();
            var result = await SFOOrderDAL.SaveOrder(form, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
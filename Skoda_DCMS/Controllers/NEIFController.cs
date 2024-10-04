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
    public class NEIFController : BaseController
    {
        NEIFDAL neifDAL;

        //[HttpPost]
        //public async Task<ActionResult> SaveDAF(FormCollection form)
        //{
        //    HttpPostedFileBase file = null, file1 = null;

        //    if (Request.Files.Count > 0)
        //    {
        //        HttpFileCollectionBase files = Request.Files;
        //        //file will be Photo only and file1 => LicensePhotoCopy
        //        file = Request.Files[0].ContentLength > 0 ? files["photo"] : null;
        //        file1 = Request.Files[1].ContentLength > 0 ? files["licensePhotoCopy"] : null;

        //    }
        //    neifDAL = new NEIFDAL();
        //    var result = await neifDAL.SaveDAF(form, (UserData)Session["UserData"], file, file1);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        // GET: NEIF
        public ActionResult Index()
        {
            return View();
        }
    }
}
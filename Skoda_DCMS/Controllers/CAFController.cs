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
    public class CAFController : BaseController
    {
        CAFDAL cafDAL;

        [HttpPost]
        public async Task<ActionResult> SaveCAF(FormCollection form)
        {
            HttpPostedFileBase file = null;
            HttpFileCollectionBase files = null;

            // Check if any files were uploaded
            if (Request.Files.Count > 0)
            {
                files = Request.Files;
                file = files[0]; 
                
            }

            cafDAL = new CAFDAL();
            var result = await cafDAL.SaveCAF(form, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // GET: CAF
        public ActionResult Index()
        {
            return View();
        }
    }
}
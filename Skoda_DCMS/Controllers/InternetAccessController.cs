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
    public class InternetAccessController : BaseController
    {
        InternetAccessDAL internetAccessDAL;
        public async Task<ActionResult> CreateInternetAccessRequest(FormCollection form)
        {
            internetAccessDAL = new InternetAccessDAL();
            var result = await internetAccessDAL.CreateInternetAccessRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
      
    }
}
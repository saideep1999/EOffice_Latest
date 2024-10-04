using DocumentFormat.OpenXml.Math;
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
    public class EQSAccessController : Controller
    {
        // GET: EQSAccess
        [HttpPost]
        public async Task<ActionResult> SaveData(EQSAccessmModelData model)
        {
            var dal = new EQSAccessDAL();
            var result = await dal.SaveData(model, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
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
    public class DesktopLaptopInstallationChecklistController : BaseController
    {
        [HttpPost]
        public async Task<ActionResult> SaveData(DesktopLaptopInstallationChecklistModel model)
        {
            var dal = new DesktopLaptopInstallationChecklistDAL();
            var result = await dal.SaveData(model);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
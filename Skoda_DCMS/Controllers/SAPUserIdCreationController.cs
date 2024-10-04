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
    public class SAPUserIdCreationController : Controller
    {
        SAPUserIdCreationDAL dal = new SAPUserIdCreationDAL();

        [HttpPost]
        public async Task<ActionResult> SaveData(SAPUserIdCreationModel data)
        {
            var result = await dal.SaveData(data);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
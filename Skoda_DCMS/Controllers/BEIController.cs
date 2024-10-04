using Skoda_DCMS.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class BEIController : BaseController
    {
        BEIDAL BEIDAL;

        /// <summary>
        /// BEI-It is used for Saving data.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveBei(FormCollection form)
        {
            BEIDAL = new BEIDAL();
            var result = await BEIDAL.SaveBei(form);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
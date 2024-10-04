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
    public class CourierRequestController : BaseController
    {
        CourierRequestDAL CourierRequestDAL;


        /// <summary>
        /// Courier request Form-It is used to Save Courier Request Form.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveCourierRequestForm(FormCollection form)
        {
            CourierRequestDAL = new CourierRequestDAL();
            var result = await CourierRequestDAL.SaveCourierRequestForm(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> WeighingUpdate(FormCollection form)
        {
            CourierRequestDAL = new CourierRequestDAL();
            var result = await CourierRequestDAL.WeighingUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
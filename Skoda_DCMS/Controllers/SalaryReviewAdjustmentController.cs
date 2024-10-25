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
    public class SalaryReviewAdjustmentController : Controller
    {
        // GET: SalaryReviewAdjustment
        SalaryReviewAdjustmentDAL SalaryReviewAdjustment;
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> SaveSRAFDATA(FormCollection form)
        {
            SalaryReviewAdjustment = new SalaryReviewAdjustmentDAL();
            var result = await SalaryReviewAdjustment.SaveSRAFDAL(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
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
    public class CreateApprovalsController : Controller
    {
        CreateApprovalsDAL createApprovalsDAL;
        // GET: CreateApprovals
        [HttpPost]
        public async Task<ActionResult> CreateApprovalsTracking(CreateApprovalsModels form)
        {
            createApprovalsDAL = new CreateApprovalsDAL();
            var result = await createApprovalsDAL.CreateApprovalsTracking(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetApprovalTrackingData(string Id)
        {
            createApprovalsDAL = new CreateApprovalsDAL();
            var result = createApprovalsDAL.GetApprovalTrackingData(Id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
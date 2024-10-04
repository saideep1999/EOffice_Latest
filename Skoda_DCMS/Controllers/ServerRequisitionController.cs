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
    public class ServerRequisitionController : BaseController
    {
        ServerRequisitionDAL ServerRequisitionDAL;
        public async Task<ActionResult> SaveServerRequisition(FormCollection form)
        {
            ServerRequisitionDAL = new ServerRequisitionDAL();
            var result = await ServerRequisitionDAL.SaveServerRequisition(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetOSName()
        {
            ServerRequisitionDAL = new ServerRequisitionDAL();
            var result = await ServerRequisitionDAL.GetOSName();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetDBName()
        {
            ServerRequisitionDAL = new ServerRequisitionDAL();
            var result = await ServerRequisitionDAL.GetDBName();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetLinuxOSName()
        {
            ServerRequisitionDAL = new ServerRequisitionDAL();
            var result = await ServerRequisitionDAL.GetLinuxOSName();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
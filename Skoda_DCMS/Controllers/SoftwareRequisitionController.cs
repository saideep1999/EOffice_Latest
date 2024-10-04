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
    public class SoftwareRequisitionController : BaseController
    {
        SoftwareRequisitionDAL softwareRequisitionDAL;
        /// <summary>
        /// SoftwareRequisition-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateSoftwareRequisitionRequest(FormCollection form)
        {
            softwareRequisitionDAL = new SoftwareRequisitionDAL();
            var result = await softwareRequisitionDAL.CreateSoftwareRequisitionRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAvailableSoftwares(string searchText)
        {
            softwareRequisitionDAL = new SoftwareRequisitionDAL();
            var result = softwareRequisitionDAL.GetAvailableSoftwares(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAvailableSoftwareDetails(string softwareName)
        {
            softwareRequisitionDAL = new SoftwareRequisitionDAL();
            var result = softwareRequisitionDAL.GetAvailableSoftwareDetails(softwareName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}
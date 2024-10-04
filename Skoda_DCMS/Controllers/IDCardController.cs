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
    public class IDCardController : BaseController
    {
        IDCardDAL IDCardDAL;

        /// <summary>
        /// Id Card-It is used to Save ID Card.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveIDCard(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.SaveIDCard(form, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> IDCardValidityUpdate(FormCollection form)
        {
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.IDCardValidityUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetIDCFEmployeeDetails(string searchText)
        {
            IDCardDAL = new IDCardDAL();
            var result = IDCardDAL.GetIDCFEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// IDCF-It is used to get the approval list for Id Card form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetIDCFApprovalList()
        {
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.GetIDCFApprovalList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// ID Card-It is used to get the Area Master Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> AreaMaster(string Locationid)
        {
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.GetAreaMaster(Locationid);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// NewId Card-It is used to get the Sub Area Master Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> SubAreaMaster(string Locationid, string AreaID)
        {
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.GetSubAreaMaster(Locationid, AreaID);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// RIDCF-It is used to get the approval list for Reissue Id Card form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetRIDCFApprovalList()
        {
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.GetRIDCFApprovalList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// DARF-It is used to get the approval list for Door Access Request form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetDARFApprovalList()
        {
            IDCardDAL = new IDCardDAL();
            var result = await IDCardDAL.GetDARFApprovalList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> SaveApproverResponse(FormCollection form)
        {
            IDCardDAL id = new IDCardDAL();
            bool result = id.SaveApproverResponse(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
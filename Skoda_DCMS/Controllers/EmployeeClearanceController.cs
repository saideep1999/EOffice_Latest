using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using Skoda_DCMS.Filters;

namespace Skoda_DCMS.Controllers
{
    public class EmployeeClearanceController : BaseController
    {
        public EmployeeClearanceDAL empClrDAL;

        /// <summary>
        /// ECF-Auto fill submitter data in Employee Clearance form.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetECFEmployeeDetails(string searchText)
        {
            empClrDAL = new EmployeeClearanceDAL();
            var result = empClrDAL.GetECFEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// ECF-Auto fill submitter data in Employee Clearance form.
        /// </summary>
        /// <returns></returns>
        //public ActionResult GetECFExistingEmployeeDetails(string otherEmpUserId)
        //{
        //    empClrDAL = new EmployeeClearanceDAL();
        //    var result = empClrDAL.GetECFExistingEmployeeDetails(otherEmpUserId);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        /// <summary>
        /// ECF-It is used to get the approval list for Employee Clearance form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetECFSequentialApprovalList()
        {
            empClrDAL = new EmployeeClearanceDAL();
            var result = await empClrDAL.GetECFSequentialApprovalList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// ECF Form-It is used to get the Employee Data in ECF Form - Auto Complete code.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetECFChargeHandOverEmployeeDetails(string searchText)
        {
            empClrDAL = new EmployeeClearanceDAL();
            var result = empClrDAL.GetECFChargeHandOverEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// ECF Form-It is used to get the Employee Data in ECF Form - Auto Complete code.
        /// </summary>
        /// <returns></returns>
        //public ActionResult GetECFChargeHandOverExistingEmployeeDetails(string searchText)
        //{
        //    empClrDAL = new EmployeeClearanceDAL();
        //    var result = empClrDAL.GetECFHandOverExistingEmployeeDetails(searchText);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        /// <summary>
        /// ECF-Save Employee Clearance form data in SharePoint.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveECF(FormCollection form)
        {
            empClrDAL = new EmployeeClearanceDAL();
            var result = await empClrDAL.SaveECF(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// ECF-Function to allow approvers to edit form data.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> ECFValidityUpdate(FormCollection form)
        {
            empClrDAL = new EmployeeClearanceDAL();
            var result = await empClrDAL.ECFValidityUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SaveApproverResponse(FormCollection form)
        {
            EmployeeClearanceDAL id = new EmployeeClearanceDAL();
            bool result = id.SaveApproverResponse(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }

    
}

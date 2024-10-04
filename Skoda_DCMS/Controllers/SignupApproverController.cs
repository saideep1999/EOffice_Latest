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
    public class SignupApproverController : Controller
    {
        // GET: SignupApprover
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> SignupApproverList()
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = await saFormDAL.SignupApproverList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> UserdataList()
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = await saFormDAL.UserdataList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetApproversData(long EmpNum)
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = saFormDAL.GetApproversData(EmpNum);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult SaveApproveData(string Id, string FirstName, string MiddleName, string LastName, string EmailID, string PhoneNumber, string Department, string SubDepartment, string CostCenter, string ManagerEmployeeNumber, string Action, string RejectReason)
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = new ResponseData();
            if (Action.ToLower() == "approve")
               result = saFormDAL.SaveApproveData(Id, FirstName, MiddleName, LastName, EmailID, PhoneNumber, Department, SubDepartment, CostCenter, ManagerEmployeeNumber);
            else
                result = saFormDAL.SaveRejectData(Id, EmailID, RejectReason);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, ValidateInput(false)]
        public ActionResult SaveUsersummaryData(string EmployeeNumber, string FirstName, string MiddleName, string LastName, string EmailID, string PhoneNumber, string Department, string SubDepartment, string CostCenter, string ManagerEmployeeNumber)
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = saFormDAL.SaveUsersummaryData(EmployeeNumber, FirstName, MiddleName, LastName, EmailID, PhoneNumber, Department, SubDepartment, CostCenter, ManagerEmployeeNumber);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult SaveRejectData(string Id,string EmailID)
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = saFormDAL.SaveRejectData(Id, EmailID,"");
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Isactiveuser(long empnum)
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = saFormDAL.Isactiveuser(empnum);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetActiveApproversData(long empnum)
        {
            SignupApproverDAL saFormDAL = new SignupApproverDAL();
            var result = saFormDAL.GetActiveApproversData(empnum);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Skoda_DCMS.Helpers
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            bool authorize = false;
            var userId = Convert.ToString(httpContext.Session["UserId"]);
            return authorize;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {

            //if (filterContext.HttpContext.Request.Form is null)
            //    return;
            //var model = filterContext.HttpContext.Request.Form;
            //long empCode = 0, ccNum = 0;

            //switch (model)
            //{
            //    case FormCollection m:
            //        {
            //            var requestSubmissionFor = m["drpRequestSubmissionFor"];
            //            var otherEmpType = m["rdOnBehalfOptionSelected"] ?? "";
            //            ccNum = requestSubmissionFor.ToLower() == "Self".ToLower()
            //                ? Convert.ToInt64(m["txtCostcenterCode"])
            //                : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
            //                    ? Convert.ToInt64(m["txtOtherCostcenterCode"])
            //                    : Convert.ToInt64(m["txtOtherNewCostcenterCode"]));
            //            empCode = requestSubmissionFor.ToLower() == "Self".ToLower()
            //                ? Convert.ToInt64(m["txtEmployeeCode"])
            //                : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
            //                    ? Convert.ToInt64(m["txtOtherEmployeeCode"])
            //                    : Convert.ToInt64(m["txtOtherNewEmployeeCode"]));
            //            break;
            //        }

            //    case ApplicantDataModel m:
            //        {
            //            var requestSubmissionFor = m.RequestSubmissionFor;
            //            var otherEmpType = m.OnBehalfOption;
            //            ccNum = requestSubmissionFor.ToLower() == "Self".ToLower()
            //                ? Convert.ToInt64(m.EmployeeCCCode)
            //                : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
            //                    ? Convert.ToInt64(m.OtherEmployeeCCCode)
            //                    : Convert.ToInt64(m.OtherNewCostcenterCode));
            //            empCode = requestSubmissionFor.ToLower() == "Self".ToLower()
            //                ? Convert.ToInt64(m.EmployeeCode)
            //                : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
            //                    ? Convert.ToInt64(m.OtherEmployeeCode)
            //                    : Convert.ToInt64(m.OtherEmployeeCode));
            //            break;
            //        }
            //}
            //var requestSubmissionFor = model["drpRequestSubmissionFor"];
            //var otherEmpType = model["rdOnBehalfOptionSelected"] ?? "";
            //ccNum = requestSubmissionFor.ToLower() == "Self".ToLower()
            //    ? Convert.ToInt64(model["txtCostcenterCode"])
            //    : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
            //        ? Convert.ToInt64(model["txtOtherCostcenterCode"])
            //        : Convert.ToInt64(model["txtOtherNewCostcenterCode"]));
            //empCode = requestSubmissionFor.ToLower() == "Self".ToLower()
            //    ? Convert.ToInt64(model["txtEmployeeCode"])
            //    : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
            //        ? Convert.ToInt64(model["txtOtherEmployeeCode"])
            //        : Convert.ToInt64(model["txtOtherNewEmployeeCode"]));
            //if (empCode != 0 && ccNum != 0)
            //{
            //    var response = new CommonDAL().VerifyCostCenterAndManager(empCode, ccNum);
            //    string content = JsonConvert.SerializeObject(response);
                //r.Content = new StringContent(content);
                //filterContext.HttpContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            //}
            //-------------------------
            //filterContext.Result = new RedirectToRouteResult(
            //   new RouteValueDictionary
            //   {
            //        { "controller", "Dashboard" },
            //        { "action", "Index" }
            //   });
        }
    }
}
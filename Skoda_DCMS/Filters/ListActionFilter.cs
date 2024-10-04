using Newtonsoft.Json;
using Skoda_DCMS.Controllers;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Skoda_DCMS.Helpers
{
    public class ListActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var route = filterContext.RouteData;
            var controllerName = route.Values["controller"].ToString();
            var actionName = route.Values["action"].ToString();
            if (!string.IsNullOrEmpty(controllerName) && controllerName.ToLower() == "list"
                && !string.IsNullOrEmpty(actionName) && actionName.ToLower() == "createform")
            {
                var uniqueFormName = Convert.ToString(filterContext.ActionParameters["uniqueFormName"]);
                var user = (UserData)filterContext.HttpContext.Session["UserData"];
                if (user != null)
                {
                    var result = FormListHelper.IsFormAccessibleToUser(user, uniqueFormName);
                    if (!result)
                    {
                        filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary(new { controller = "Dashboard", action = "Index" })
                        );
                    }
                }
            }
            else
            {
                if (filterContext.ActionParameters is null)
                    return;
                var model = filterContext.ActionParameters.ElementAt(0);
                long empCode = 0, ccNum = 0;

                switch (model.Value)
                {
                    case FormCollection m:
                        {
                            if (m.HasKeys())
                            {
                                var requestSubmissionFor = m["drpRequestSubmissionFor"] ?? "";
                                var otherEmpType = m["rdOnBehalfOptionSelected"] ?? "";
                                ccNum = requestSubmissionFor.ToLower() == "Self".ToLower()
                                    ? Convert.ToInt64(m["txtCostcenterCode"])
                                    : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
                                        ? Convert.ToInt64(m["txtOtherCostcenterCode"])
                                        : Convert.ToInt64(m["txtOtherNewCostcenterCode"]));
                                empCode = requestSubmissionFor.ToLower() == "Self".ToLower()
                                    ? Convert.ToInt64(m["txtEmployeeCode"])
                                    : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
                                        ? Convert.ToInt64(m["txtOtherEmployeeCode"])
                                        : Convert.ToInt64(m["txtOtherNewEmployeeCode"]));
                            }
                            break;
                        }

                    case ApplicantDataModel m:
                        {
                            var requestSubmissionFor = m.RequestSubmissionFor;
                            var otherEmpType = m.OnBehalfOption;
                            ccNum = requestSubmissionFor.ToLower() == "Self".ToLower()
                                ? Convert.ToInt64(m.EmployeeCCCode)
                                : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
                                    ? Convert.ToInt64(m.OtherEmployeeCCCode)
                                    : Convert.ToInt64(m.OtherNewCostcenterCode));
                            empCode = requestSubmissionFor.ToLower() == "Self".ToLower()
                                ? Convert.ToInt64(m.EmployeeCode)
                                : (otherEmpType.ToLower() == "SAVWIPLEmployee".ToLower()
                                    ? Convert.ToInt64(m.OtherEmployeeCode)
                                    : Convert.ToInt64(m.OtherEmployeeCode));
                            break;
                        }
                }
                if (empCode != 0 && ccNum != 0)
                {
                    var response = new CommonDAL().VerifyCostCenterAndManager(empCode, ccNum);
                    if (response.Status != 200)
                    {
                        filterContext.Result = new JsonResult()
                        {
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                            Data = response
                        };
                    }
                    //filterContext.HttpContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    //{
                    //    Content = new StringContent(content)
                    //};
                }
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            //
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            //
        }
    }
}
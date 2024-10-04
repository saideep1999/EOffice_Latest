using Skoda_DCMS.App_Start;
using Skoda_DCMS.Filters;
using Skoda_DCMS.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    [CustomAuthenticationFilter]
    public class BaseController : Controller
    {
        public BaseController()
        {
            if (System.Web.HttpContext.Current != null)
                GlobalClass.ApplicationUrl = GetApplicationURL(System.Web.HttpContext.Current);
        }

        public string GetApplicationURL(HttpContext httpContext)
        {
            try
            {
                return httpContext.Request.Url.GetLeftPart(UriPartial.Authority);
            }
            catch (Exception tx)
            {
                return "";
            }

            //return new Uri(string.Format("{0}://{1}{2}",
            //   Request.Url.Scheme,
            //   hostHeader,
            //   Request.RawUrl)).ToString();
        }
        protected override void OnException(ExceptionContext filterContext)
        {

            if (filterContext.ExceptionHandled)
            {
                return;
            }
            filterContext.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Error.cshtml"
            };
            filterContext.ExceptionHandled = true;
            Log.Error("Error", filterContext.Exception);
            //Log.Error("OnException Start");
            //Log.Error(filterContext.Exception.Message);
            //Log.Error("OnException End");

        }
    }
}
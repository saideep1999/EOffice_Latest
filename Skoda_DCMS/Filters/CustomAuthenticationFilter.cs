using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using System.Web.Routing;

namespace Skoda_DCMS.Filters
{
    public class CustomAuthenticationFilter : ActionFilterAttribute, IAuthenticationFilter
    {
        public void OnAuthentication(AuthenticationContext filterContext)
        {
            var user = filterContext.HttpContext.Session["UserData"] as UserData;
            //var ad_user = filterContext.HttpContext.User;
            //Console.WriteLine(ad_user.Identity.Name);
            if (user == null || string.IsNullOrEmpty(user.UserName))
            {
                //filterContext.Result = new HttpUnauthorizedResult();
                //filterContext.Result = new RedirectResult("~/Login/Index");

                HttpContext.Current.Response.AddHeader("REQUIRES_AUTH", "1");
                //HttpContext.Current.Response.End();
                //filterContext.Result = new HttpStatusCodeResult(401);
                filterContext.Result = new RedirectResult("~/Login/Index");
            }
        }
        public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
        {
            //if (filterContext.Result == null || filterContext.Result is HttpUnauthorizedResult)
            //{
            //    //Redirecting the user to the Login View of Account Controller  
            //    //RedirectResult
            //    filterContext.Result = new RedirectToRouteResult(
            //    new RouteValueDictionary
            //    {
            //         { "controller", "Login" },
            //         { "action", "Index" }
            //    });
            //}
        }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                HttpContext ctx = HttpContext.Current;
                var session = HttpContext.Current.Session["UserData"];
                if (session == null)
                {
                    if (!HttpContext.Current.Response.Headers.AllKeys.Contains("REQUIRES_AUTH"))
                    {
                        HttpContext.Current.Response.AddHeader("REQUIRES_AUTH", "1");
                        //HttpContext.Current.Response.End();
                        //filterContext.Result = new HttpStatusCodeResult(401);
                        filterContext.Result = new RedirectResult("~/Login/Index");
                    }
                    return;
                }
                base.OnActionExecuting(filterContext);
            }
            catch (Exception e)
            {
                if (!HttpContext.Current.Response.Headers.AllKeys.Contains("REQUIRES_AUTH"))
                {
                    HttpContext.Current.Response.AddHeader("REQUIRES_AUTH", "1");
                    //HttpContext.Current.Response.End();
                    //filterContext.Result = new HttpStatusCodeResult(401);
                    filterContext.Result = new RedirectResult("~/Login/Index");
                }
                return;
            }
        }
    }
}
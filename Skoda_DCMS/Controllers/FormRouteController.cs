using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class FormRouteController : Controller
    {
        // GET: FormRoute
        public ActionResult GetForm(string formName = "")
        {
            if (Session["UserData"] as UserData == null)
            {
                return new RedirectResult("~/Login/Index?formName=" + formName);
            }
            else
            {
                //string formUrl = await GetCreateFormUrl(formName);
                return RedirectToAction("CreateFormUrlAndRedirect", "List", new { uniqueFormName = formName });
                //return new RedirectResult(formUrl);
            }
        }
    }
}
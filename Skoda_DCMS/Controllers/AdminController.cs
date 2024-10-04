using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin

        public ActionResult Admin()
        {
            return View();
        }

      
        //[HttpPost]
        //public async Task<ActionResult> UserNameUpdate()
        //{
        //    CommonDAL obj = new CommonDAL();
        //    var result = await obj.UpdateUserName();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        [HttpPost]
        public ActionResult Admin(FormCollection form)
        {
            string username = form["txtusername"];
            ListDAL obj = new ListDAL();
            var result = new UserData();

            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(username);

            if (match.Success)
            {
                result = obj.GetEmpDetByEmailId(username);       
            }
            else
            {
                result = obj.GetEmpDetByUserName(username);
            }

            ViewBag.UserName = result.UserName;
            ViewBag.ObjectSid = result.ObjectSid;
            ViewBag.Email = result.Email;
            ViewBag.Name = result.EmployeeName;
            ViewBag.Domain = result.DomainName;
            return View();
        }

       
    }
}
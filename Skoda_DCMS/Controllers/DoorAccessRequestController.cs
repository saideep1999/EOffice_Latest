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
    public class DoorAccessRequestController : Controller
    {
        DoorAccessRequestDAL doorAccessRequestDAL;
        /// <summary>
        /// DoorAccessRequest-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreateDoorAccessRequest(FormCollection form)
        {
            doorAccessRequestDAL = new DoorAccessRequestDAL();
            var result = await doorAccessRequestDAL.CreateDoorAccessRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //public ActionResult GetLocations()
        //{
        //    doorAccessRequestDAL = new DoorAccessRequestDAL();
        //    var result = doorAccessRequestDAL.GetLocations();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult GetDepartments(string loc)
        //{
        //    doorAccessRequestDAL = new DoorAccessRequestDAL();
        //    var result = doorAccessRequestDAL.GetDepartments(loc);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult GetAccessDoors(string dept)
        //{
        //    doorAccessRequestDAL = new DoorAccessRequestDAL();
        //    var result = doorAccessRequestDAL.GetAccessDoors(dept);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult GetAccessDoorListData()
        {
            doorAccessRequestDAL = new DoorAccessRequestDAL();
            var result = doorAccessRequestDAL.GetAccessDoorListData();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}

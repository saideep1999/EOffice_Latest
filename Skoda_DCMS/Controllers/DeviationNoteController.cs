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
    public class DeviationNoteController : BaseController
    {
        DeviationNoteDAL DeviationNoteDAL;

        /// <summary>
        /// Courier request Form-It is used to get the Department Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Department()
        {
            DeviationNoteDAL = new DeviationNoteDAL();
            var result = await DeviationNoteDAL.GetDepartment();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Deviation Note Form-It is used to Save Courier Request Form.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> SaveDeviationNoteForm(FormCollection form)
        {
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            DeviationNoteDAL = new DeviationNoteDAL();
            var result = await DeviationNoteDAL.SaveDeviationNoteForm(form, (UserData)Session["UserData"],file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetDeviationDate()
        {
            DeviationNoteDAL = new DeviationNoteDAL();
            var result = DeviationNoteDAL.GetDeviationDate();
            return Json(result.ToString("dd-MM-yyyy"), JsonRequestBehavior.AllowGet);
        }
    }
}
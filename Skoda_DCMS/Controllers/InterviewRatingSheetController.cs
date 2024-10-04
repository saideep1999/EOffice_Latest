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
    public class InterviewRatingSheetController : BaseController
    {
        InterviewRatingSheetDAL InterviewRatingSheetDAL;

        public async Task<ActionResult> SaveInterviewRatingSheet(FormCollection form)
        {            
            InterviewRatingSheetDAL = new InterviewRatingSheetDAL();
            var result = await InterviewRatingSheetDAL.SaveInterviewRatingSheet(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
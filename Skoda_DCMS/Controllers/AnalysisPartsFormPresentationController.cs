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
    public class AnalysisPartsFormPresentationController : Controller
    {
        AnalysisPartsFormPresentationDAL analysisPartsFormPresentationDAL;
        // GET: AnalysisPartsFormPresentation
        //public ActionResult AnalysisPartsFormPresentation()
        //{
        //    return View();
        //}
        public async Task<ActionResult> SaveAnalysisPartsForm(AnalysisPartsFormPresentationData data)
        {
            analysisPartsFormPresentationDAL = new AnalysisPartsFormPresentationDAL();
            var result = await analysisPartsFormPresentationDAL.SaveAnalysisPartsForm(data, (UserData)Session["UserData"]);
           
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        

    }
}
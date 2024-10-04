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
    public class IPAFFormController : Controller
    {
        IPAFFormDAL IPAFDAL;
        // GET: IPAFForm
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveIPAFForm(IPAFData model)
        {
            HttpPostedFileBase file = null;
            HttpPostedFileBase file1 = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
                //if (model.PSAttach1 == 1)
                //{
                //    file = files[0];
                //}
            }
            if (Request.Files.Count > 0 && Request.Files[1].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file1 = files[1];
            }
            IPAFDAL = new IPAFFormDAL();
            var result = await IPAFDAL.SaveIPAFForm(model, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        
        public ActionResult UpdateData(string ApplicationUrl)
        {
            IPAFDAL = new IPAFFormDAL();
            var list = IPAFDAL.GetTypeAcessbyLink(ApplicationUrl);
            return Json(list, JsonRequestBehavior.AllowGet);
        }


        public ActionResult GetTypeAcessData()
        {
            IPAFDAL = new IPAFFormDAL();
            var list = IPAFDAL.Getdropdata();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetApplicationurldata()
        {
            IPAFDAL = new IPAFFormDAL();
            var list = IPAFDAL.getapplictionurl();
            return Json(list, JsonRequestBehavior.AllowGet);
        }


    }
}
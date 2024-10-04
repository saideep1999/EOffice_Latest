using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace Skoda_DCMS.Controllers
{
    public class DLADController : Controller
    {
        DLADDAL DLADDAL;
        // GET: DLAD
        public async Task<ActionResult> SaveDLAD(DLADFormModel data)
        {

            //IndividualAdminServerList tableItems = JsonConvert.DeserializeObject<IndividualAdminServerList>(data.IndividualAdminServerListDataList);

            JavaScriptSerializer js = new JavaScriptSerializer();
            List<IndividualAdminServerList> items = js.Deserialize<List<IndividualAdminServerList>>(data.IndividualAdminServerListDataList);
            data.IndividualAdminServerList = items;
            HttpPostedFileBase file = null;

            if (Request.Files.Count > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            DLADDAL = new DLADDAL();
            var result = await DLADDAL.SaveDLAD(data, (UserData)Session["UserData"], file);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> UpdateData(DLADFormModel data)
        {
            DLADDAL = new DLADDAL();
            var result = await DLADDAL.UpdateData(data, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
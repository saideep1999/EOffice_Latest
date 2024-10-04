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
    public class ResourceAccountAndDLController : BaseController
    {
        ResourceAccountAndDLDAL resourceAccountAndDLDAL;
        /// <summary>
        /// Resource Account And Distribution List Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateResourceAccountDLRequest(FormCollection form)
        {
            resourceAccountAndDLDAL = new ResourceAccountAndDLDAL();
            var result = await resourceAccountAndDLDAL.CreateResourceAccountDLRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Resource Account & Distribution List-It is used to get the locations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetResourceAccountLocation()
        {
            resourceAccountAndDLDAL = new ResourceAccountAndDLDAL();
            var result = await resourceAccountAndDLDAL.GetResourceAccountLocation();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
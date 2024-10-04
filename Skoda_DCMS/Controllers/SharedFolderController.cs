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
    public class SharedFolderController : BaseController
    {
        SharedFolderDAL sharedFolderDAL;
        /// <summary>
        /// Shared Folder Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateSharedFolderRequest(FormCollection form)
        {
            sharedFolderDAL = new SharedFolderDAL();
            var result = await sharedFolderDAL.CreateSharedFolderRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
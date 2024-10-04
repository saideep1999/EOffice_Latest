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
    public class GaneshUserIdController : BaseController
    {  
        GaneshUserIdDAL ganeshUserIdDAL;  
        public async Task<ActionResult> CreateGaneshUserIdCreationRequest(FormCollection form)
        {
            ganeshUserIdDAL = new GaneshUserIdDAL();
            var result = await ganeshUserIdDAL.CreateGaneshUserIdCreationRequest(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
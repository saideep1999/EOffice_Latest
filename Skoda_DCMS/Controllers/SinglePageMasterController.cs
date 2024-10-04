using Newtonsoft.Json;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class SinglePageMasterController : Controller
    {
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        SPMFFormDAL SPMDAL;
        // GET: SinglePageMaster
        public ActionResult SinglePageMaster()
        {
            return View();
        }

        public ActionResult GetMasterData()
        {
            SPMDAL = new SPMFFormDAL();
            var list = SPMDAL.GetMastersdata();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetMasterinfo(string tablename)
        {
            SPMDAL = new SPMFFormDAL();
            var list = SPMDAL.GetMasterinfo(tablename);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDynamicTables(string tablename)
        {
            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds = new DataSet();

            try
            {

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetDynamicTables", con);
                cmd.Parameters.Add(new SqlParameter("@table", tablename));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

            }
            catch (Exception ex)
            {

            }

            //int UserID = Convert.ToInt16(Session["userid"]);
            //dt = db.sub_GetDatatable("USP_GetExpensesEntryPendingForApprovel_DB'" + tablename + "' ");
            var summaryDet = JsonConvert.SerializeObject(ds);
            var jsonResult = Json(summaryDet, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }


    }
}
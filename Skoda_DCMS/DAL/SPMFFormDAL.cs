using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.WebControls;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Xml;

namespace Skoda_DCMS.DAL
{
   

    public class SPMFFormDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        public async Task<dynamic> GetMastersdata()
        {
            List<SPMFData> list = new List<SPMFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();


                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetAllMasters", con);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();



                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        SPMFData spmdata = new SPMFData();

                        spmdata.TableName = Convert.ToString(ds.Tables[0].Rows[i]["TableName"]);
                        spmdata.TableNicName = Convert.ToString(ds.Tables[0].Rows[i]["TableNicName"]);
                        list.Add(spmdata);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }
        public async Task<dynamic> GetMasterinfo(string tablename)
        {
            List<SPMFData> list = new List<SPMFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();


                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_Getmastertableinfo", con);
                cmd.Parameters.Add(new SqlParameter("@Mastertablename", tablename));
                cmd.Parameters.Add(new SqlParameter("@sql", ""));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();



                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        SPMFData spmdata = new SPMFData();

                        spmdata.GenderName = Convert.ToString(ds.Tables[0].Rows[i]["GenderName"]);
                        spmdata.AddedBy = Convert.ToString(ds.Tables[0].Rows[i]["AddedBy"]);
                        spmdata.AddedOn = Convert.ToString(ds.Tables[0].Rows[i]["AddedOn"]);
                        spmdata.ModifiedBy = Convert.ToString(ds.Tables[0].Rows[i]["ModifiedBy"]);
                        spmdata.ModifiedOn = Convert.ToString(ds.Tables[0].Rows[i]["ModifiedOn"]);
                        spmdata.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                       
                        list.Add(spmdata);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }




    }
}
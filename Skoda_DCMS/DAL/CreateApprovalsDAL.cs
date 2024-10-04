using ExcelDataReader.Log;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.DAL
{
    public class CreateApprovalsDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        public async Task<dynamic> ViewCreateApprovalsData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();
            try
            {
                ListDAL listDAL = new ListDAL();
                var item = listDAL.GetFormsList_SQL();
                URCFData.one = item;
            }
            catch (Exception ex)
            {
            }
            return URCFData;
        }
        public async Task<ResponseModel<object>> CreateApprovalsTracking(CreateApprovalsModels form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();

            try
            {

                string pattern = ",";
                string ddForms = Convert.ToString(form.FormId);
                var drpId = string.Empty;
                var Name = string.Empty;
                var Role = string.Empty;
                var AppLevel = string.Empty;
                var Logic = string.Empty;

                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                string CAIds = "";
                foreach (var IDs in form.CAFormData)
                {
                    CAIds += IDs.CAId + ",";
                }

                foreach (var item in form.CAFormData)
                {
                    DataSet ds1 = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveApprovalsTracking", con);
                    cmd_form.Parameters.Add(new SqlParameter("@Id", item.CAId == null || item.CAId == "0" ? 0 : Convert.ToInt64(item.CAId)));
                    cmd_form.Parameters.Add(new SqlParameter("@EmployeeId", item.EmpId == null || item.EmpId == "0" ? 0 : Convert.ToInt64(item.EmpId)));
                    cmd_form.Parameters.Add(new SqlParameter("@Names", item.selName ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@Role", item.selRole ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@Level", item.selAppLevel ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@Logic", item.selLogic ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@FormID", Convert.ToInt64(ddForms)));
                    cmd_form.Parameters.Add(new SqlParameter("@UserBy", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@UserDate", DateTime.Now));
                    cmd_form.Parameters.Add(new SqlParameter("@CAIds", CAIds));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds1);
                    con.Close();

                    if (ds1.Tables[0].Rows.Count > 0 && ds1.Tables[0] != null)
                    {
                        for (int j = 0; j < ds1.Tables[0].Rows.Count; j++)
                        {
                            result.Status = Convert.ToInt32(ds1.Tables[0].Rows[j]["Status"]);
                            result.Message = Convert.ToString(ds1.Tables[0].Rows[j]["message"]); ;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
            }
            return result;
        }

        public CreateApprovalsModels GetApprovalTrackingData(string Id)
        {
            CreateApprovalsModels CreateApprovalList = new CreateApprovalsModels();
            List<CAFormDataList> ModellList = new List<CAFormDataList>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetApprovalsTrackingData", con);
                cmd.Parameters.Add(new SqlParameter("@Id", Id));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        CAFormDataList model = new CAFormDataList();
                        model.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["Id"]);
                        model.CAId = Convert.ToString(ds.Tables[0].Rows[i]["Id"]).Trim();
                        model.selName = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeName"]).Trim();
                        model.EmpId = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeId"]).Trim();
                        model.selRole = Convert.ToString(ds.Tables[0].Rows[i]["Role"]).Trim();
                        model.selAppLevel = Convert.ToString(ds.Tables[0].Rows[i]["ApproverLevel"]).Trim();
                        model.selLogic = Convert.ToString(ds.Tables[0].Rows[i]["Logic"]);
                        ModellList.Add(model);
                        CreateApprovalList.FormId = Convert.ToString(ds.Tables[0].Rows[i]["FormParentId"]).Trim();
                    }
                    CreateApprovalList.CAFormData = ModellList;
                }
                return CreateApprovalList;
            }
            catch (Exception ex)
            {
                return new CreateApprovalsModels();
            }
        }
    }
}
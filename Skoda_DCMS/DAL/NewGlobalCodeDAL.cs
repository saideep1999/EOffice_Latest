using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class NewGlobalCodeDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        //public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        //public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        //public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;

        public async Task<ResponseModel<object>> SaveNewGlobalCode(NewGlobalCodeData model, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            //Web web = _context.Web;
            string formShortName = "NGCF";
            string formName = "New Global GL code form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }
            int prevItemId = Convert.ToInt32(model.FormSrId);
            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            formId = Convert.ToInt32(model.FormId);
            bool IsResubmit = formId == 0 ? false : true;
            int AppRowId = Convert.ToInt32(model.AppRowId);

            var requestSubmissionFor = model.RequestSubmissionFor;
            var otherEmpType = model.OnBehalfOption ?? "";
            bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
            long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCCCode) : Convert.ToInt64(model.OtherNewCostcenterCode));
            long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCode) : Convert.ToInt64(model.OtherNewEmployeeCode));
            string requestType = model.RequestType;


            var response = await GetApprovalNGCF(empNum, ccNum, requestType);
            if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
            {
                result.Status = 500;
                result.Message = response.Message;
                return result;
            }

            var approvers = response.Model;
            SqlCommand cmd_form = new SqlCommand();
            SqlDataAdapter adapter_form = new SqlDataAdapter();
            DataSet ds_form = new DataSet();

            try
            {
                if (formId == 0)
                {
                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                    cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                    cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterId", DBNull.Value));
                    if (formId == 0)
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Submitted"));
                    }
                    else
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Resubmitted"));
                    }
                    cmd_form.Parameters.Add(new SqlParameter("@UniqueFormName", formShortName));
                    if (requestSubmissionFor == "Self")
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Location", model.EmployeeLocation));
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", model.OtherEmployeeLocation));
                        }
                        else
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", model.OtherNewEmpLocation));
                        }

                    }

                    cmd_form.Parameters.Add(new SqlParameter("@Modified", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@TimeStamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@Department", user.Department));
                    cmd_form.Parameters.Add(new SqlParameter("@DataRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "NewGlobalCode"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", model.BusinessNeed));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 38));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con_form.Open();
                    adapter_form.Fill(ds_form);
                    con_form.Close();

                    if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                    {
                        for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                        {
                            formId = Convert.ToInt32(ds_form.Tables[0].Rows[i]["FormID"]);
                        }
                    }

                }
                else
                {
                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                    cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                    cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterId", DBNull.Value));
                    if (formId == 0)
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Submitted"));
                    }
                    else
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Resubmitted"));
                    }
                    cmd_form.Parameters.Add(new SqlParameter("@UniqueFormName", formShortName));
                    if (requestSubmissionFor == "Self")
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Location", model.EmployeeLocation));
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", model.OtherEmployeeLocation));
                        }
                        else
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", model.OtherNewEmpLocation));
                        }

                    }

                    cmd_form.Parameters.Add(new SqlParameter("@Modified", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@TimeStamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@Department", user.Department));
                    cmd_form.Parameters.Add(new SqlParameter("@DataRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "NewGlobalCode"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", model.BusinessNeed));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 38));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con_form.Open();
                    adapter_form.Fill(ds_form);
                    con_form.Close();

                    if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                    {
                        for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                        {
                            formId = Convert.ToInt32(ds_form.Tables[0].Rows[i]["FormID"]);
                        }
                    }
                }

                #region Comment
                //List list = _context.Web.Lists.GetByTitle("Forms");
                //ListItem item = list.GetItemById(formId);
                //item["FormName"] = formName;
                //item["UniqueFormName"] = formShortName;
                //item["FormParentId"] = 38;
                //item["ListName"] = listName;
                //item["SubmitterUserName"] = user.UserName;
                //item["Status"] = "Resubmitted";
                //item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //item["Department"] = user.Department;
                //item["ControllerName"] = "NewGlobalCode";
                //item["BusinessNeed"] = model.BusinessNeed ?? "";
                //if (requestSubmissionFor == "Self")
                //{
                //    item["Location"] = model.EmployeeLocation;
                //}
                //else
                //{
                //    if (otherEmpType == "SAVWIPLEmployee")
                //    {
                //        item["Location"] = model.OtherEmployeeLocation;
                //    }
                //    else
                //    {
                //        item["Location"] = model.OtherNewEmpLocation;
                //    }
                //}
                //item.Update();
                //_context.Load(item);
                //_context.ExecuteQuery();

                //formId = item.Id;
                #endregion
                ListDAL dal = new ListDAL();
                var resubmitResult = await dal.ResubmitUpdateForSQL(formId);

                if (AppRowId != 0)
                {
                    #region Comment
                    //List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                    //ListItem listItem = listApprovalMaster.GetItemById(AppRowId);
                    //listItem["ApproverStatus"] = "Resubmitted";
                    //listItem["IsActive"] = 0;
                    //listItem.Update();
                    //_context.Load(listItem);
                    //_context.ExecuteQuery();
                    #endregion
                    int Status = 0;
                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataSet ds1 = new DataSet();
                    List<ApprovalMatrix> appList1 = new List<ApprovalMatrix>();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("sp_UpdateApprovalMasterWithRowId", con);
                    cmd1.Parameters.Add(new SqlParameter("@AppRowId", AppRowId));
                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Resubmitted"));
                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 0));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(ds1);
                    con.Close();
                    if (ds1.Tables[0] != null && ds1.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds1.Tables[0].Rows.Count; i++)
                        {
                            Status = Convert.ToInt32(ds1.Tables[0].Rows[i]["Status"]);
                        }
                    }
                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(model, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SaveNewGlobalCodeForm", con);
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", model.BusinessNeed));
                cmd.Parameters.Add(new SqlParameter("@RequestType", model.RequestType));
                cmd.Parameters.Add(new SqlParameter("@NameOfGLToOpen", model.NameOfGLToOpen));
                cmd.Parameters.Add(new SqlParameter("@NatureOfTranInGL", model.NatureOfTranInGL));
                cmd.Parameters.Add(new SqlParameter("@Purpose", model.Purpose));
                cmd.Parameters.Add(new SqlParameter("@DateToOpenNewGL", model.DateToOpenNewGL));
                cmd.Parameters.Add(new SqlParameter("@GLCode", model.GLCode));
                cmd.Parameters.Add(new SqlParameter("@GLName", model.GLName));
                cmd.Parameters.Add(new SqlParameter("@GLSeries", model.GLSeries));
                cmd.Parameters.Add(new SqlParameter("@formId", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId", userDetailsResponse.RowId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                #region Comment
                //var newRow = userDetailsResponse.Model;

                //newRow["BusinessNeed"] = model.BusinessNeed;
                //newRow["RequestType"] = model.RequestType;
                //newRow["NameOfGLToOpen"] = model.NameOfGLToOpen;
                //newRow["NatureOfTranInGL"] = model.NatureOfTranInGL;
                //newRow["Purpose"] = model.Purpose;
                //newRow["DateToOpenNewGL"] = model.DateToOpenNewGL;
                //newRow["GLCode"] = model.GLCode;
                //newRow["GLName"] = model.GLName;
                //newRow["GLSeries"] = model.GLSeries;

                //newRow["FormID"] = formId;
                //newRow.Update();
                //_context.Load(newRow);
                //_context.ExecuteQuery();
                #endregion
                RowId = Convert.ToInt32(userDetailsResponse.RowId);
                result.Status = 200;
                result.Message = formId.ToString();

                var approverIdList = response.Model;

                //Task Entry in Approval Master List
                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, model.BusinessNeed ?? "", RowId, formId);

                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }

                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, RowId);
                foreach (var approver in approverIdList)
                {
                    var data = new UserData()
                    {
                        EmployeeName = approver.FName + " " + approver.LName,
                        Email = approver.EmailId,
                        ApprovalLevel = approver.ApprovalLevel,
                        IsApprover = true
                    };
                    userList.Add(data);
                }


                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
                    FormName = formName
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
            }
            return result;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalNGCF(long empNum, long ccNum, string requestType)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_NewGlobalCodeApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@RequestType", requestType));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        ApprovalMatrix app = new ApprovalMatrix();
                        app.EmpNumber = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(ds.Tables[0].Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailID"]);
                        app.Designation = Convert.ToString(ds.Tables[0].Rows[i]["desg"]);
                        app.Logic = Convert.ToString(ds.Tables[0].Rows[i]["logic"]);
                        app.ApprovalLevel = (int)ds.Tables[0].Rows[i]["approvalLevel"];
                        app.ControllerName = "NewGlobalCode";
                        app.FormParentId = 38;
                        app.ApproverStatus = "Pending";
                        appList.Add(app);
                    }
                }
                else
                {
                    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver not found." }; ;
                }

                if (appList.ContainsAllLevels() == 0)
                {
                    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver is missing." }; ;
                }
                #region comment
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.Timeout = TimeSpan.FromSeconds(10);
                #endregion
                var count = appList.Count;

                //AD Code
                ListDAL obj = new ListDAL();
                for (int i = 0; i < count; i++)
                {
                    string eml = appList[i].EmailId;
                    string currentId = obj.GetUserIdByEmailId(eml);
                    if (string.IsNullOrEmpty(currentId))
                    {
                        return new ResponseModel<List<ApprovalMatrix>> { Status = 500, Message = "User not found in AD", Model = new List<ApprovalMatrix>() };
                    }
                    appList[i].ApproverUserName = Convert.ToString(currentId);
                    appList[i].ApproverName = obj.GetApproverNameByEmailId(eml);
                }
                //AD Code


                if (appList.CheckApproverUserName() == 0)
                {
                    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver User Id is missing." }; ;
                }

                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
            }
        }

        public async Task<dynamic> GetNewGlobalCodeDetails(int rowId, int formId)
        {
            dynamic NGCFData = new ExpandoObject();
            try
            {
                #region Comment
                //GlobalClass gc = new GlobalClass();
                //var user = gc.GetCurrentUser();

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client1 = new HttpClient(handler);
                //client1.BaseAddress = new Uri(conString);
                //client1.DefaultRequestHeaders.Accept.Clear();
                //client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('NewGlobalCodeForm')/items?$select=*"
                //+ "&$filter=(ID eq '" + rowId + "')");
                //var responseText1 = await response1.Content.ReadAsStringAsync();
                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //if (!string.IsNullOrEmpty(responseText1))
                //{
                //    var NGCFResult = JsonConvert.DeserializeObject<NewGlobalCodeModel>(responseText1, settings);
                //    NGCFData.one = NGCFResult.NGCFResults.NewGlobalCodeData;
                //}
                #endregion

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<NewGlobalCodeData> appList = new List<NewGlobalCodeData>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetNewGlobalCodeFormById", con);
                cmd.Parameters.Add(new SqlParameter("@AppRowId", rowId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        NewGlobalCodeData app = new NewGlobalCodeData();
                        app.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["FormID"]);
                        item1.CreatedDate = ds.Tables[0].Rows[i]["Created"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds.Tables[0].Rows[i]["Created"]);
                        app.FormIDNGCF = item1;
                        app.EmployeeType = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeType"]);
                        app.EmployeeCode = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeCode"]);
                        app.EmployeeCCCode = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeCCCode"]);
                        app.EmployeeUserId = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeUserId"]);
                        app.EmployeeName = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeName"]);
                        app.EmployeeContactNo = ds.Tables[0].Rows[i]["EmployeeContactNo"] == DBNull.Value ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeContactNo"]);
                        app.ExternalOrganizationName = ds.Tables[0].Rows[i]["ExternalOrganizationName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["ExternalOrganizationName"]);
                        app.OtherExternalOrganizationName = ds.Tables[0].Rows[i]["ExternalOtherOrganizationName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["ExternalOtherOrganizationName"]);
                        app.EmployeeLocation = ds.Tables[0].Rows[i]["EmployeeLocation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeLocation"]);
                        app.EmployeeDepartment = ds.Tables[0].Rows[i]["EmployeeDepartment"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeDepartment"]);
                        app.EmployeeDesignation = ds.Tables[0].Rows[i]["EmployeeDesignation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeDesignation"]);
                        app.RequestSubmissionFor = ds.Tables[0].Rows[i]["RequestSubmissionFor"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["RequestSubmissionFor"]);
                        app.OnBehalfOption = ds.Tables[0].Rows[i]["OnBehalfOption"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OnBehalfOption"]);
                        //app.EmployeeEmailId = ds.Tables[0].Rows[i]["EmployeeEmailId"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeEmailId"]);
                        app.OtherEmployeeType = ds.Tables[0].Rows[i]["OtherEmployeeType"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeType"]);
                        app.OtherEmployeeCode = ds.Tables[0].Rows[i]["OtherEmployeeCode"] == DBNull.Value ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["OtherEmployeeCode"]);
                        app.OtherEmployeeCCCode = ds.Tables[0].Rows[i]["OtherEmployeeCCCode"] == DBNull.Value ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["OtherEmployeeCCCode"]);
                        app.OtherEmployeeContactNo = ds.Tables[0].Rows[i]["OtherEmployeeContactNo"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeContactNo"]);
                        app.OtherEmployeeUserId = ds.Tables[0].Rows[i]["OtherEmployeeUserId"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeUserId"]);
                        app.OtherEmployeeName = ds.Tables[0].Rows[i]["OtherEmployeeName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeName"]);
                        app.OtherEmployeeDepartment = ds.Tables[0].Rows[i]["OtherEmployeeDepartment"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeDepartment"]);
                        app.OtherEmployeeLocation = ds.Tables[0].Rows[i]["OtherEmployeeLocation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeLocation"]);
                        app.OtherEmployeeDesignation = ds.Tables[0].Rows[i]["OtherEmployeeDesignation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeDesignation"]);
                        app.OtherExternalOrganizationName = ds.Tables[0].Rows[i]["OtherExternalOrganizationName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherExternalOrganizationName"]);
                        app.OtherEmployeeEmailId = ds.Tables[0].Rows[i]["OtherEmployeeEmailId"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeEmailId"]);
                        app.BusinessNeed = ds.Tables[0].Rows[i]["BusinessNeed"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["BusinessNeed"]);
                        app.RequestType = ds.Tables[0].Rows[i]["RequestType"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["RequestType"]);
                        app.NameOfGLToOpen = ds.Tables[0].Rows[i]["NameOfGLToOpen"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["NameOfGLToOpen"]);
                        app.NatureOfTranInGL = ds.Tables[0].Rows[i]["NatureOfTranInGL"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["NatureOfTranInGL"]);
                        app.Purpose = ds.Tables[0].Rows[i]["Purpose"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["Purpose"]);
                        app.DateToOpenNewGL = ds.Tables[0].Rows[i]["DateToOpenNewGL"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds.Tables[0].Rows[i]["DateToOpenNewGL"]);
                        app.GLCode = ds.Tables[0].Rows[i]["GLCode"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["GLCode"]);
                        app.GLName = ds.Tables[0].Rows[i]["GLName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["GLName"]);
                        app.GLSeries = ds.Tables[0].Rows[i]["GLSeries"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["GLSeries"]);
                        app.NewGLNo = ds.Tables[0].Rows[i]["NewGLNo"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["NewGLNo"]);
                        app.CommitmentItem = ds.Tables[0].Rows[i]["CommitmentItem"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["CommitmentItem"]);
                        appList.Add(app);
                    }
                    NGCFData.one = appList;
                }
                else
                {
                    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver not found." }; ;
                }

                #region COmment
                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,Designation,ApproverName,ApproverUserName,Level,Logic,TimeStamp,IsActive,Comment,NextApproverId,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                //var responseText2 = await response2.Content.ReadAsStringAsync();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);
                #endregion

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                List<ApprovalDataModel> modelData = new List<ApprovalDataModel>();

                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("sp_GetApprovalMaster", con);
                cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                cmd1.Parameters.Add(new SqlParameter("@FormId", formId));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter1.SelectCommand = cmd1;
                con.Open();
                adapter1.Fill(ds1);
                con.Close();
                if (ds1.Tables[0].Rows.Count > 0 && ds1.Tables[0] != null)
                {
                    for (int i = 0; i < ds1.Tables[0].Rows.Count; i++)
                    {
                        ApprovalDataModel app = new ApprovalDataModel();
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(ds1.Tables[0].Rows[i]["FormID"]);
                        item1.CreatedDate = ds1.Tables[0].Rows[i]["Created"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds1.Tables[0].Rows[i]["Created"]);
                        app.FormId = item1;
                        Author item2 = new Author();
                        item2.Submitter = Convert.ToString(ds1.Tables[0].Rows[i]["Title"]);
                        app.Author = item2;
                        app.ApproverId = ds1.Tables[0].Rows[i]["ApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["ApproverId"]);
                        app.ApproverStatus = ds1.Tables[0].Rows[i]["ApproverStatus"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["ApproverStatus"]);
                        app.Modified = ds1.Tables[0].Rows[i]["Modified"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds1.Tables[0].Rows[i]["Modified"]);
                        app.Designation = ds1.Tables[0].Rows[i]["Designation"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["Designation"]);
                        app.ApproverName = ds1.Tables[0].Rows[i]["ApproverName"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["ApproverName"]);
                        app.Level = ds1.Tables[0].Rows[i]["Level"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["Level"]);
                        app.Logic = ds1.Tables[0].Rows[i]["Logic"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["Logic"]);
                        app.TimeStamp = ds1.Tables[0].Rows[i]["TimeStamp"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds1.Tables[0].Rows[i]["TimeStamp"]);
                        app.IsActive = ds1.Tables[0].Rows[i]["IsActive"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["IsActive"]);
                        app.NextApproverId = ds1.Tables[0].Rows[i]["NextApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["NextApproverId"]);
                        app.Comment = ds1.Tables[0].Rows[i]["Comment"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["Comment"]);
                        app.ApproverUserName = ds1.Tables[0].Rows[i]["ApproverUserName"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["ApproverUserName"]);
                        modelData.Add(app);
                    }
                }
                if (modelData.Count > 0)
                {
                    //var clientApp = new HttpClient(handler);
                    //clientApp.BaseAddress = new Uri(conString);
                    //clientApp.DefaultRequestHeaders.Accept.Clear();
                    //clientApp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var names = new List<string>();

                    var items = modelData;

                    //AD Code
                    ListDAL obj = new ListDAL();
                    for (int i = 0; i < items.Count; i++)
                    {
                        //string approverId = items[i].ApproverUserName;
                        //string appName = obj.GetApproverNameFromAD(approverId);
                        string appName = items[i].ApproverName;
                        names.Add(appName);
                    }
                    //AD Code

                    if (items.Count == names.Count)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i].UserName = names[i];
                        }
                    }

                    items = items.OrderBy(x => x.UserLevel).ToList();

                    if (modelData.Count > 0)
                    {
                        //dynamic data3 = Json.Decode(responseText2);
                        NGCFData.two = modelData;
                        NGCFData.three = items;
                    }

                }
                else
                {
                    NGCFData.two = new List<string>();
                    NGCFData.three = new List<string>();
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return NGCFData;
        }

        public async Task<int> UpdateNGCFData(System.Web.Mvc.FormCollection form, UserData user)
        {
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("NGCF") ? GlobalClass.ListNames["NGCF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                newItem["NewGLNo"] = form["txtNewGLNo"];
                newItem["CommitmentItem"] = form["txtCommitmentItem"];
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }
    }
}
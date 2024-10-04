using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
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
    public class CommonDAL
    {
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        protected readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        protected UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        private (List<ApprovalMatrix>, long) GetMDAssistant()
        {
            List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
            ApprovalMatrix app;
            long MDEmpNum = 0;
            try
            {
                //sp_Asst_MD
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> approverList = new List<ApprovalMatrix>();

                var con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_Asst_MD", con);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlDataAdapter.SelectCommand = sqlCommand;
                con.Open();
                sqlDataAdapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        app = new ApprovalMatrix();
                        app.EmpNumber = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(ds.Tables[0].Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailID"]);
                        app.Designation = Convert.ToString(ds.Tables[0].Rows[i]["desg"]);
                        appList.Add(app);
                    }
                }
                if (ds.Tables[1].Rows.Count > 0 && ds.Tables[1] != null)
                {
                    for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                    {
                        MDEmpNum = Convert.ToInt64(ds.Tables[1].Rows[i]["MDEmpNum"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return (new List<ApprovalMatrix>(), 0);
            }
            return (appList, MDEmpNum);
        }

        public List<ApprovalMatrix> AddMDAssistantToList(List<ApprovalMatrix> appList)
        {
            var (asstList, MDEmpNum) = GetMDAssistant();
            try
            {
                if (appList.Any(b => b.EmpNumber == MDEmpNum))
                {
                    var MDEmpApproverList = appList.Where(x => x.EmpNumber == MDEmpNum).ToList();
                    foreach (var manager in MDEmpApproverList)
                    {
                        List<ApprovalMatrix> list = new List<ApprovalMatrix>();
                        foreach (var asst in asstList.ToList())
                        {
                            var assist = asst.Clone();
                            assist.ApprovalLevel = manager.ApprovalLevel;
                            manager.Logic = assist.Logic = "OR";
                            assist.ApproverUserName = Convert.ToString(new ListDAL().GetUserIdByEmailId(assist.EmailId));
                            //manager.DisplayDesignation = "Managing Director / Assistant to Managing Director";
                            assist.Designation = "Assistant to " + manager.Designation;
                            //Temp adding EmpNumber instead of UserId will change this while saving data in SaveApprovalMasterData()
                            assist.AssistantForEmpNum = manager.EmpNumber;
                            assist.RelationWith = manager.RelationId;
                            assist.RelationId = appList.Max(x => x.RelationId) + 1;
                            appList.Insert((appList.IndexOf(manager) + 1), assist);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
            return appList;
        }

        public List<ApprovalMatrix> ChangeDelegateApprover(List<ApprovalMatrix> appList)
        {
            var TempList = new List<ApprovalMatrix>();
            try
            {
                var approverIDList = String.Join(",", appList.Select(x => x.EmpNumber.ToString()));
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                var con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetDelegateEmployees", con);
                sqlCommand.Parameters.Add(new SqlParameter("@commaSeparatedIDs", approverIDList));
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlDataAdapter.SelectCommand = sqlCommand;
                con.Open();
                sqlDataAdapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        var EmpNumber = Convert.ToInt64(ds.Tables[0].Rows[i]["ReplacementEmployeeNumber"]);
                        var EmpList = appList.Where(x => x.EmpNumber == EmpNumber);
                        TempList = appList.Where(x => x.EmpNumber != EmpNumber).ToList();
                        if (EmpList != null && EmpList.Count() > 0)
                        {
                            foreach (var Emp in EmpList)
                            {
                                var newEmp = new ApprovalMatrix()
                                {
                                    FName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]),
                                    LName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]),
                                    EmpNumber = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeNumber"]),
                                    EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailID"]),
                                    Designation = Emp.Designation,
                                    ApprovalLevel = Emp.ApprovalLevel,
                                    Logic = Emp.Logic,
                                    DelegatedByEmpNum = EmpNumber
                                };
                                TempList.Add(newEmp);
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
            if (TempList == null || TempList.Count == 0)
            {
                return appList;
            }
            else
            {
                return TempList;
            }
        }

        //private ApprovalMatrix CloneApprover(ApprovalMatrix ap) => ap.Clone();

        public List<ApprovalMatrix> AddEmployeeAssistantToList(List<ApprovalMatrix> appList)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
                List<ApprovalMatrix> approverList = new List<ApprovalMatrix>();

                var con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetEmployeeAssistantApprovers", con);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlDataAdapter.SelectCommand = sqlCommand;
                con.Open();
                List<KeyValuePair<long, List<ApprovalMatrix>>> temp = new List<KeyValuePair<long, List<ApprovalMatrix>>>();
                foreach (ApprovalMatrix approver in appList)
                {
                    List<ApprovalMatrix> asstList = new List<ApprovalMatrix>();
                    sqlCommand.Parameters.Add(new SqlParameter("@EmpNum", approver.EmpNumber));
                    DataSet ds = new DataSet();
                    sqlDataAdapter.Fill(ds);
                    if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            var app = new ApprovalMatrix();
                            app.EmpNumber = Convert.ToInt64(ds.Tables[0].Rows[i]["AssistantEmployeeNumber"]);
                            app.FName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]);
                            app.LName = Convert.ToString(ds.Tables[0].Rows[i]["LastName"]);
                            app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailID"]);
                            //app.Designation = Convert.ToString(ds.Tables[0].Rows[i]["AssistantDesignation"]);
                            app.Designation = "Assistant to " + approver.Designation;
                            app.Logic = Convert.ToString(ds.Tables[0].Rows[i]["Logic"]);
                            app.ExtraDetails = Convert.ToString(ds.Tables[0].Columns.Contains("Contents") ? ds.Tables[0].Rows[i]["Contents"] : "");
                            app.ApprovalLevel = approver.ApprovalLevel;
                            //Temp adding EmpNumber instead of UserId will change this while saving data in SaveApprovalMasterData()
                            app.AssistantForEmpNum = approver.EmpNumber;
                            app.RelationWith = approver.RelationId;
                            app.RelationId = appList.Max(x => x.RelationId) + 1; ;
                            asstList.Add(app);
                        }
                        if (approver.Logic.ToLower() == "not")
                        {
                            approver.Logic = "OR";
                        }
                    }
                    sqlCommand.Parameters.Clear();
                    temp.Add(new KeyValuePair<long, List<ApprovalMatrix>>(approver.EmpNumber, asstList));
                }
                Dictionary<long, int> dc = new Dictionary<long, int>();
                foreach (var group in temp)
                {
                    int value, index = 0;
                    if (dc.TryGetValue(group.Key, out value))
                        index = appList.FindIndex(++value, x => x.EmpNumber == group.Key);
                    else
                        index = appList.FindIndex(x => x.EmpNumber == group.Key);
                    if (!dc.ContainsKey(group.Key))
                        dc.Add(group.Key, index);
                    else
                        dc[group.Key] = index;
                    appList.InsertRange(++index, group.Value);
                }
                con.Close();
                //appList.AddRange(asstList);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
            return appList;
        }

        //Common Save Code
        public ResponseModel<ListItem> SaveSubmitterAndApplicantDetails(Web web, System.Web.Mvc.FormCollection form, string listName, int formIdInput = 0)
        {
            try
            {
                //var (_client, web) = CreateClientContextAndWeb();
                List List = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newRow = List.AddItem(itemCreateInfo);
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                var requestSubmissionFor = form["drpRequestSubmissionFor"] ?? "";

                //if (formIdInput == 0)
                //{
                //    newRow["TriggerCreateWorkflow"] = "Yes";
                //}
                //else
                //{
                //    newRow["TriggerCreateWorkflow"] = "No";
                //}
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["EmployeeType"] = form["chkEmployeeType"];
                // newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOrganizationName"] = form["txtExternalOrganizationName"] ?? "";
                newRow["EmployeeCode"] = form["txtEmployeeCode"];
                newRow["EmployeeCCCode"] = form["txtCostcenterCode"];
                newRow["EmployeeUserId"] = form["txtUserId"];
                newRow["EmployeeName"] = form["txtEmployeeName"];
                newRow["EmployeeDepartment"] = form["txtDepartment"];
                newRow["EmployeeContactNo"] = form["txtContactNo"];
                newRow["EmployeeDesignation"] = form["chkEmployeeType"] == "External" ? "Team Member" : form["ddEmpDesignation"];
                newRow["EmployeeLocation"] = form["ddEmpLocation"];
                newRow["EmployeeEmailId"] = user.Email;
                //Other Employee Details
                newRow["OnBehalfOption"] = otherEmpType;
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        newRow["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                        newRow["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";
                        newRow["OtherEmployeeDesignation"] = form["chkOtherEmployeeType"] == "External" ? "Team Member" : form["ddOtherEmpDesignation"] ?? "";
                        newRow["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? "";
                        newRow["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? "";
                        newRow["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? "";
                        newRow["OtherEmployeeDepartment"] = form["txtOtherDepartment"] ?? "";
                        newRow["OtherEmployeeContactNo"] = form["txtOtherContactNo"] ?? "";
                        newRow["OtherEmployeeEmailId"] = form["txtOtherEmailId"] ?? "";
                        newRow["OnBehalfOption"] = form["rdOnBehalfOption"] ?? "";
                        newRow["OtherEmployeeType"] = form["chkOtherEmployeeType"] ?? "";
                        // newRow["OtherExternalOrganizationName"] = form["ddOtherExternalOrganizationName"] ?? "";
                        newRow["OtherExternalOrganizationName"] = form["txtOtherExternalOrganizationName"] ?? "";
                    }
                    else
                    {
                        newRow["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                        newRow["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";
                        newRow["OtherEmployeeDesignation"] = form["chkOtherNewEmployeeType"] == "External" ? "Team Member" : form["ddOtherNewEmpDesignation"] ?? "";
                        newRow["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? "";
                        newRow["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? "";
                        newRow["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? "";
                        newRow["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                        newRow["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                        newRow["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                        newRow["OtherEmployeeType"] = form["chkOtherNewEmployeeType"] ?? "";
                        //newRow["OtherExternalOrganizationName"] = form["ddOtherNewExternalOrganizationName"] ?? "";
                        newRow["OtherExternalOrganizationName"] = form["txtOtherNewExternalOrganizationName"] ?? "";
                    }
                }
                return new ResponseModel<ListItem> { Message = "Updated Successfully", Status = 200, Model = newRow };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<ListItem> { Message = "Error occurred while adding data in FormList", Status = 500 };
            }
        }


        public async Task<ResponseModel<object>> SaveDataApprovalMasterData(List<ApprovalMatrix> approverIdList, string businessNeed, int RowId, int formId)
        {

            try
            {
                SqlConnection con;

                int isactive = 0;

                for (var i = 0; i < approverIdList.Count; i++)
                {
                    SqlCommand cmd_Approver = new SqlCommand();
                    SqlDataAdapter adapter_App = new SqlDataAdapter();
                    DataSet ds = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_Approver = new SqlCommand("USP_SaveApproverDetails", con);
                    cmd_Approver.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@RowId", RowId));
                    if (approverIdList[i].ApprovalLevel == 1)
                    {
                        isactive = 1;
                    }
                    else
                    {
                        isactive = 0;
                    }
                    cmd_Approver.Parameters.Add(new SqlParameter("@IsActive", isactive));
                    cmd_Approver.Parameters.Add(new SqlParameter("@NextAppId", DBNull.Value));

                    //cmd_Approver.Parameters.Add(new SqlParameter("@ApproverStatus", "Approved"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverStatus", "Pending"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Department", ""));
                    cmd_Approver.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Email", approverIdList[i].EmailId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed ?? ""));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Level", approverIdList[i].ApprovalLevel));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Logic", approverIdList[i].Logic));
                    cmd_Approver.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Designation", approverIdList[i].Designation));
                    cmd_Approver.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", RowId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverUserName", approverIdList[i].ApproverUserName));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverName", approverIdList[i].ApproverName));

                    cmd_Approver.CommandType = CommandType.StoredProcedure;
                    adapter_App.SelectCommand = cmd_Approver;
                    con.Open();
                    adapter_App.Fill(ds);
                    con.Close();
  
                    
                }
                return new ResponseModel<object> { Message = "Added Successfully in ApprovalMaster", Status = 200 };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<object> { Message = "Error occurred while adding data in ApprovalMaster", Status = 500 };

            }
        }
        
        public async Task<ResponseModel<object>> SaveApprovalMasterData(List<ApprovalMatrix> approverIdList, string businessNeed, int RowId, int formId)
            {
            try
            {
                approverIdList = CallAssistantAndDelegateFunc(approverIdList);
                var list = await GetApproverUserID(approverIdList);
                if (list.Status == 500 && list.Model != null)
                {
                    return new ResponseModel<object> { Message = list.Message, Status = 500, Model = null };
                }
                approverIdList = list.Model;
                approverIdList.OrderBy(x => x.ApprovalLevel);
                //Logic to add ObjectSSID of Employee for Assistant in list(If he/she has assistant)
                foreach (var approver in approverIdList)
                {
                    var tmpList = approverIdList.Where(x => x.AssistantForEmpNum == approver.EmpNumber);
                    if (tmpList.Count() > 0)
                        foreach (var item in tmpList)
                        {
                            item.AssistantForEmpUserName = approver.ApproverUserName;
                        }

                }

                var rowid = RowId;
                var (_context, web) = CreateClientContextAndWeb();
                List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                for (var i = 0; i < approverIdList.Count; i++)
                {
                    ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                    ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);

                    approvalMasteritem["FormId"] = formId;
                    approvalMasteritem["RowId"] = rowid;
                    approvalMasteritem["ApproverUserName"] = approverIdList[i].ApproverUserName;
                    approvalMasteritem["Designation"] = approverIdList[i].Designation;
                    approvalMasteritem["Level"] = approverIdList[i].ApprovalLevel;
                    approvalMasteritem["Logic"] = approverIdList[i].Logic;
                    if (approverIdList[i].ApprovalLevel == 1)
                        approvalMasteritem["IsActive"] = 1;
                    else
                        approvalMasteritem["IsActive"] = 0;

                    if (approverIdList[i].ApprovalLevel == approverIdList.Max(x => x.ApprovalLevel))
                        approvalMasteritem["NextApproverId"] = 0;
                    else
                    {
                        //var currentApproverLevel = approverIdList[i].ApprovalLevel;
                        //approvalMasteritem["NextApproverId"] = approverIdList.Any(x => x.ApprovalLevel == currentApproverLevel + 1) ? approverIdList.Where(x => x.ApprovalLevel == currentApproverLevel + 1).FirstOrDefault().ApproverUserName : "";
                        approvalMasteritem["NextApproverId"] = 0;
                    }
                    approvalMasteritem["ApproverStatus"] = "Pending";
                    approvalMasteritem["RunWorkflow"] = "No";
                    approvalMasteritem["BusinessNeed"] = businessNeed ?? "";
                    approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;
                    approvalMasteritem["ExtraDetails"] = approverIdList[i].ExtraDetails;
                    //approvalMasteritem["AssistantForEmployeeUserId"] = approverIdList[i].AssistantForEmpUserId;
                    approvalMasteritem["AssistantForEmployeeUserName"] = approverIdList[i].AssistantForEmpUserName;
                    approvalMasteritem["RelationId"] = approverIdList[i].RelationId;
                    approvalMasteritem["RelationWith"] = approverIdList[i].RelationWith;
                    approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;
                    approvalMasteritem.Update();
                    _context.Load(approvalMasteritem);
                    _context.ExecuteQuery();
                }
                return new ResponseModel<object> { Message = "Added Successfully", Status = 200 };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<object> { Message = "Error occurred while adding data in ApprovalMaster", Status = 500 };
            }
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApproverUserID(List<ApprovalMatrix> appList)
        {

            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            var count = appList.Count;
            var emailString = "";
            try
            {

                //AD Code
                ListDAL obj = new ListDAL();
                for (int i = 0; i < count; i++)
                {
                    string eml = appList[i].EmailId;
                    string currentId = obj.GetUserIdByEmailId(eml);
                    if (string.IsNullOrEmpty(currentId))
                    {
                        return new ResponseModel<List<ApprovalMatrix>> { Status = 500, Message = $"Approver {appList[i].FName} {appList[i].LName}, data not found in AD", Model = new List<ApprovalMatrix>() };
                    }
                    appList[i].ApproverUserName = (currentId);
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
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }

        public ResponseModel<object> UpdateDataRowIdInFormsList(int RowId, int formId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                var con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_updateRowIDInForms", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowID", RowId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();


                return new ResponseModel<object> { Message = "Updated Successfully", Status = 200 };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<object> { Message = "Error occurred while adding data in ApprovalMaster", Status = 500 };
            }
        }

        public (ClientContext, Web) CreateClientContextAndWeb()
        {
            try
            {
                ClientContext _context = new ClientContext(new Uri(conString));
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                return (_context, _context.Web);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return (null, null);
            };
        }

        public async Task<(ResponseModel<List<ApprovalDataModel>>, ResponseModel<List<ApprovalDataModel>>)> GetApproversData(UserData user, int rowId, int formId)
        {
            try
            {
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                ////client.DefaultRequestHeaders.Accept.Clear();
                ////client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //var response2 = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=*,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                //var responseText2 = await response2.Content.ReadAsStringAsync();
                List<ApprovalDataModel> modeldatalist = new List<ApprovalDataModel>();
               
                ApprovalMasterModel approvalMasterModel = new ApprovalMasterModel();
                NodeClass nodeClass = new NodeClass();
                var con = new SqlConnection(sqlConString);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();

                cmd = new SqlCommand("USP_GetApproversData", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(dt, settings);


                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        ApprovalDataModel modelData = new ApprovalDataModel();
                        //modelData.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        FormLookup item = new FormLookup();
                        item.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        modelData.FormId = item;
                        modelData.IsActive = Convert.ToInt32(dt.Rows[i]["IsActive"]);
                        modelData.NextApproverId = dt.Rows[i]["NextApproverId"] != null || dt.Rows[i]["NextApproverId"] != DBNull.Value || dt.Rows[i]["NextApproverId"] != "0" ? Convert.ToInt32(dt.Rows[i]["NextApproverId"]) : 0;
                        modelData.ApproverStatus = dt.Rows[i]["ApproverStatus"] != null || dt.Rows[i]["ApproverStatus"] != DBNull.Value || dt.Rows[i]["ApproverStatus"] != "0" ? Convert.ToString(dt.Rows[i]["ApproverStatus"]) : null; 
                        modelData.Comment = dt.Rows[i]["Comment"] != null || dt.Rows[i]["Comment"] != DBNull.Value ? Convert.ToString(dt.Rows[i]["Comment"]) : null; 
                        modelData.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
                        modelData.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                        modelData.Designation = Convert.ToString(dt.Rows[i]["Designation"]);
                        modelData.ApproverUserName = Convert.ToString(dt.Rows[i]["ApproverUserName"]);
                        modelData.ApproverName = Convert.ToString(dt.Rows[i]["ApproverName"]);
                        DateTime TimeStamp = Convert.ToDateTime("1/1/0001 00:00 AM");
                        if (dt.Rows[i]["TimeStamp"] != DBNull.Value)
                            TimeStamp = Convert.ToDateTime(dt.Rows[i]["TimeStamp"]);
                        modelData.TimeStamp = TimeStamp;
                        modeldatalist.Add(modelData);

                    }
                }

                nodeClass.Data = modeldatalist;
                approvalMasterModel.Node=nodeClass;





                if (approvalMasterModel.Node.Data.Count > 0)
                {
                    //var client3 = new HttpClient(handler);
                    //client3.BaseAddress = new Uri(conString);
                    //client3.DefaultRequestHeaders.Accept.Clear();
                    //client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var names = new List<string>();
                    var responseText3 = "";

                    var items = approvalMasterModel.Node.Data;
                    var idString = "";


                    //AD Code
                    ListDAL obj = new ListDAL();
                    for (int i = 0; i < items.Count; i++)
                    {
                        //string objectSid = user.ObjectSid;
                        //string approverId = items[i].ApproverUserName;
                        //string appName = obj.GetApproverNameFromAD(approverId);
                        string appName = items[i].ApproverName;
                        names.Add(appName);
                    }
                   // AD Code


                     items = items.OrderBy(x => x.ApproverId).ToList();
                    if (items.Count == names.Count)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i].UserName = names[i];
                        }
                    }

                    items = items.OrderBy(x => x.UserLevel).ToList();

                    if (items.Count > 0)
                    {
                        var modelData1 = approvalMasterModel;
                        //dynamic data = Json.Decode(responseText2);
                        var data1 = new ResponseModel<List<ApprovalDataModel>> { Status = 200, Model = modelData1.Node.Data };
                        var data2 = new ResponseModel<List<ApprovalDataModel>> { Status = 200, Model = items };
                        return (data1, data2);
                    }

                }
                var r1 = new ResponseModel<List<ApprovalDataModel>> { Status = 500, Model = new List<ApprovalDataModel>(), Message = "Approver data not found" };
                var r2 = new ResponseModel<List<ApprovalDataModel>> { Status = 500, Model = new List<ApprovalDataModel>(), Message = "Approver data not found" };
                return (r1, r2);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                var r1 = new ResponseModel<List<ApprovalDataModel>> { Message = "Error while fetching Approvers Data.", Status = 500 };
                var r2 = new ResponseModel<List<ApprovalDataModel>> { Message = "Error while fetching Approvers Data.", Status = 500 };
                return (r1, r2);
            }
        }

        public List<ApprovalMatrix> CallAssistantAndDelegateFunc(List<ApprovalMatrix> appList)
        {
            appList = AddMDAssistantToList(appList);
            appList = AddEmployeeAssistantToList(appList);
            appList = ChangeDelegateApprover(appList);
            appList = SetApproverRelationWithData(appList);
            return appList;
        }

        public async Task<ResponseModel<List<Attachments>>> DownloadAttachmentData(string listName, int itemId)
        {
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('" + listName + "')/items?$select=AttachmentFiles"
                + "&$filter=(ID eq '" + itemId + "')&$expand=AttachmentFiles");
                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                AttachmentFilesResults AttachmentList = null;
                if (!string.IsNullOrEmpty(responseText))
                {
                    var DAFResults = JsonConvert.DeserializeObject<DrivingAuthorizationFormModel>(responseText, settings);
                    AttachmentList = DAFResults?.daflist?.dafData?[0].AttachmentFiles;
                }
                if (AttachmentList != null && AttachmentList.Attachments != null && AttachmentList.Attachments.Count > 0)
                {
                    foreach (var Attachment in AttachmentList.Attachments)
                    {
                        ClientContext _context = new ClientContext(new Uri(conString));
                        _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                        var web = _context.Web;
                        int attachmentID = itemId;

                        List docs = web.Lists.GetByTitle(listName);
                        ListItem itemAttach = docs.GetItemById(attachmentID);

                        var attInfo = new AttachmentCreationInformation();

                        attInfo.FileName = Attachment.FileName;

                        var file = _context.Web.GetFileByServerRelativeUrl(Attachment.ServerRelativeUrl);
                        _context.Load(file);
                        _context.ExecuteQuery();
                        ClientResult<Stream> data = file.OpenBinaryStream();
                        _context.Load(file);
                        _context.ExecuteQuery();
                        using (System.IO.MemoryStream mStream = new System.IO.MemoryStream())
                        {
                            if (data != null)
                            {
                                data.Value.CopyTo(mStream);
                                Attachment.file_data = mStream.ToArray();
                            }
                        }
                    }
                }
                return new ResponseModel<List<Attachments>> { Status = 200, Model = AttachmentList.Attachments, Message = "Image downloaded successfully." };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<Attachments>>
                {
                    Status = 500,
                    Message = "There were some issue while copying file.",
                    Model = null
                };
            }
        }

        public async Task<string> UpdateUserName()
        {
            int recordCount = 0;
            try
            {
                ListDAL obj = new ListDAL();
                var st = new System.Diagnostics.Stopwatch();
                st.Start();

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                ////Forms List Update
                //var formResult = new List<FormData>();
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,*&$top=10000")).Result;
                //var responseText = await response.Content.ReadAsStringAsync();
                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                //    formResult = modelResult.Data.Forms;
                //}

                //var submitterGroupList = formResult.GroupBy(x => x.SubmitterId).ToList();

                //ClientContext _context = new ClientContext(new Uri(conString));
                //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //List list;
                //ListItem listItem;

                //foreach (var subGroup in submitterGroupList)
                //{
                //    var objectSid = subGroup.Key;
                //    string preObjectSid = user.ObjectSid;
                //    string output = preObjectSid + objectSid;
                //    var submitterUserName = obj.GetUserNameFromAD(output);
                //    subGroup.ToList().ForEach(x => x.SubmitterUserName = submitterUserName);

                //    foreach (var submitter in subGroup.ToList())
                //    {
                //        int rowId = submitter.Id;
                //        list = _context.Web.Lists.GetByTitle("Forms");
                //        listItem = list.GetItemById(rowId);
                //        listItem.RefreshLoad();
                //        _context.ExecuteQuery();
                //        listItem["SubmitterUserName"] = submitterUserName.ToLower();
                //        listItem.Update();
                //        _context.Load(listItem);
                //        _context.ExecuteQuery();
                //        recordCount++;
                //    }

                //}

                //Approval Master List Update
                var appResult = new List<FormData>();
                var handlerApp = new HttpClientHandler();
                handlerApp.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                var clientApp = new HttpClient(handlerApp);
                clientApp.BaseAddress = new Uri(conString);
                clientApp.DefaultRequestHeaders.Accept.Clear();
                clientApp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseApp = Task.Run(() => clientApp.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,*&$top=20000")).Result;
                var responseTextApp = await responseApp.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                if (!string.IsNullOrEmpty(responseTextApp))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseTextApp, settings);
                    appResult = modelResult.Data.Forms;
                }

                var approverGroupList = appResult.GroupBy(x => x.ApproverUserName).ToList();

                ClientContext _contextApp = new ClientContext(new Uri(conString));
                _contextApp.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                List listApp;
                ListItem listItemApp;

                foreach (var appGroup in approverGroupList)
                {
                    var appUserName = appGroup.Key;
                    var approverName = obj.GetApproverNameByUserName(appUserName);
                    appGroup.ToList().ForEach(x => x.ApproverName = approverName);

                    foreach (var approver in appGroup.ToList())
                    {
                        int approwId = approver.Id;
                        listApp = _contextApp.Web.Lists.GetByTitle("ApprovalMaster");
                        listItemApp = listApp.GetItemById(approwId);
                        listItemApp.RefreshLoad();
                        _contextApp.ExecuteQuery();
                        listItemApp["ApproverName"] = approverName;
                        listItemApp.Update();
                        _contextApp.Load(listItemApp);
                        _contextApp.ExecuteQuery();
                        recordCount++;
                    }

                }

                st.Stop();
                var time = st.ElapsedMilliseconds;

                string returnMsg = $"{time} : Total Time: Update UserName. Total records Updated - {recordCount}";
                Log.Error(returnMsg);

                return returnMsg;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return ex.Message + " Total Records updated - " + recordCount;
            }
        }

        private List<ApprovalMatrix> SetApproverRelationWithData(List<ApprovalMatrix> appList)
        {
            try
            {
                foreach (var item in appList)
                {
                    if (item.LogicWith != 0)
                    {
                        var a = appList.Find(x => x.LogicId == item.LogicWith);
                        if (a != null)
                        {
                            item.RelationWith = a.RelationId;
                        }
                    }
                }
                var tempList = new List<ApprovalMatrix>();
                foreach (var item in appList)
                {
                    if (item.RelationWith == 0)
                    {
                        tempList.Add(item);
                        var list = GetSubApprovers(appList, item);
                        if (list != null && list.Count() > 0)
                            tempList.AddRange(list);
                    }
                }
                return tempList;
            }
            catch (Exception ex)
            {

                Log.Error(ex.Message, ex);
                return null;
            }
        }

        private List<ApprovalMatrix> GetSubApprovers(List<ApprovalMatrix> appList, ApprovalMatrix mainApprover)
        {
            try
            {
                var tempList = new List<ApprovalMatrix>();
                var a = appList.Where(x => x.RelationWith == mainApprover.RelationId && x.RelationWith != 0);
                if (a != null && a.Count() > 0)
                {
                    foreach (var item in a)
                    {
                        tempList.Add(item);
                        var list = GetSubApprovers(appList, item);
                        if (list != null && list.Count() > 0)
                            tempList.AddRange(list);
                    }
                }
                return tempList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        public HttpClientHandler GetHttpClientHandler()
        {
            UserData user = new UserData();
            GlobalClass obj = new GlobalClass();
            user = obj.GetCurrentUser();
            var handler = new HttpClientHandler();
            try
            {
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            };
            return handler;
        }

        public bool ApprovalLevlCheck(List<ApprovalMatrix> appList)
        {
            var has = appList.Any(x => x.ApprovalLevel == 1);

            if (has)
                return true;
            else
            {
                return false;
            }
        }

        public ResponseModel<ListItem> SaveSubmitterAndApplicantDetailsModelData1( System.Web.Mvc.FormCollection form, string listName, int formIdInput = 0 ,int FormId=0)
        {
            try
            {
                //var otherEmpType = model.OnBehalfOption ?? "";
                var otherEmpType = form["rdOnBehalfOption"] ?? "";
                var requestSubmissionFor = form["drpRequestSubmissionFor"] ?? "";
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                var con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_SaveSubmitterAndApplicantDetails", con);

                if (FormId == 0)
                {
                    cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", "Yes")); ;
                }
                else
                {
                    
                    cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", "No")); ;
                }
                cmd.Parameters.Add(new SqlParameter("@TableName", listName));
                cmd.Parameters.Add(new SqlParameter("@FormID", formIdInput));
                cmd.Parameters.Add(new SqlParameter("@EmployeeType", form["chkEmployeeType"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCode", form["txtEmployeeCode"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCCCode", form["txtCostcenterCode"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeUserId", form["txtUserId"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeName", form["txtEmployeeName"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDepartment", form["txtDepartment"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeContactNo", form["txtContactNo"]));
                cmd.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(new SqlParameter("@ExternalOrganizationName", form["txtExternalOrganizationName"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@ExternalOtherOrganizationName", form["txtOtherExternalOrganizationName"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@EmployeeLocation", form["ddEmpLocation"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDesignation", form["chkEmployeeType"] == "External" ? "Team Member" : form["ddEmpDesignation"]));
               
                cmd.Parameters.Add(new SqlParameter("@RequestSubmissionFor", form["drpRequestSubmissionFor"]));
                cmd.Parameters.Add(new SqlParameter("@OnBehalfOption", otherEmpType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeEmailId", user.Email));
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", form["chkOtherEmployeeType"]  ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", form["txtOtherEmployeeCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", form["txtOtherCostcenterCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", form["txtOtherContactNo"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", form["txtOtherUserId"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", form["txtOtherEmployeeName"]));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", form["txtOtherDepartment"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", form["ddOtherEmpLocation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", form["chkOtherEmployeeType"] == "External" ? "Team Member" : form["ddOtherEmpDesignation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", form["txtOtherExternalOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", form["txtOtherExternalOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", form["txtOtherEmailId"] ?? ""));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", form["chkOtherEmployeeType"]  ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", form["txtOtherNewCostcenterCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", form["txtOtherNewCostcenterCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", form["txtOtherNewContactNo"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", form["txtOtherNewUserId"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", form["txtOtherNewEmployeeName"]));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", form["txtOtherNewDepartment"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", form["ddOtherNewEmpLocation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", form["chkOtherNewEmployeeType"] == "External" ? "Team Member" : form["ddOtherNewEmpDesignation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", form["txtOtherNewExternalOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", form["txtOtherNewExternalOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", form["txtOtherNewEmailId"] ?? ""));
                    }
                }

                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                string maxID = "";
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        maxID = Convert.ToString(ds.Tables[0].Rows[i]["MaxID"]);
                    }
                }


                return new ResponseModel<ListItem> { Message = "Updated Successfully", Status = 200, RowId = maxID };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<ListItem> { Message = "Error occurred while adding data in FormList", Status = 500 };
            }
        }
        public ResponseModel<ListItem> SaveSubmitterAndApplicantDetailsModelData(Web web, ApplicantDataModel model, string listName, int formIdInput = 0)
        {
            try
            {
                var otherEmpType = model.OnBehalfOption ?? "";
                var requestSubmissionFor = model.RequestSubmissionFor ?? "";
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                var con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_SaveSubmitterAndApplicantDetails", con);
                cmd.Parameters.Add(new SqlParameter("@TableName", listName));
                cmd.Parameters.Add(new SqlParameter("@FormID", formIdInput));
                cmd.Parameters.Add(new SqlParameter("@EmployeeType", model.EmployeeType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCode", model.EmployeeCode));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCCCode", model.EmployeeCCCode));
                cmd.Parameters.Add(new SqlParameter("@EmployeeUserId", model.EmployeeUserId));
                cmd.Parameters.Add(new SqlParameter("@EmployeeName", model.EmployeeName));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDepartment", model.EmployeeDepartment));
                cmd.Parameters.Add(new SqlParameter("@EmployeeContactNo", model.EmployeeContactNo));
                cmd.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(new SqlParameter("@ExternalOrganizationName", model.ExternalOrganizationName ?? ""));
                cmd.Parameters.Add(new SqlParameter("@ExternalOtherOrganizationName", model.ExternalOrganizationName ?? ""));
                cmd.Parameters.Add(new SqlParameter("@EmployeeLocation", model.EmployeeLocation));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDesignation", model.EmployeeType == "External" ? "Team Member" : model.EmployeeDesignation));
                cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", ""));
                cmd.Parameters.Add(new SqlParameter("@RequestSubmissionFor", model.RequestSubmissionFor));
                cmd.Parameters.Add(new SqlParameter("@OnBehalfOption", otherEmpType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeEmailId", user.Email));
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", model.OtherEmployeeType ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", model.OtherEmployeeCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", model.OtherEmployeeCCCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", model.OtherEmployeeContactNo.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", model.OtherEmployeeUserId ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", model.OtherEmployeeName));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", model.OtherEmployeeDepartment ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", model.OtherEmployeeLocation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", model.OtherEmployeeType == "External" ? "Team Member" : model.OtherEmployeeDesignation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", model.OtherExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", model.OtherExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", model.OtherEmployeeEmailId ?? ""));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", model.OtherEmployeeType ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", model.OtherNewEmployeeCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", model.OtherNewCostcenterCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", model.OtherNewContactNo.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", model.OtherNewUserId ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", model.OtherNewEmployeeName));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", model.OtherNewDepartment ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", model.OtherNewEmpLocation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", model.OtherEmployeeType == "External" ? "Team Member" : model.OtherEmployeeDesignation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", model.OtherNewExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", model.OtherNewExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", model.OtherNewEmailId ?? ""));
                    }
                }

                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                string maxID = "";
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        maxID = Convert.ToString(ds.Tables[0].Rows[i]["MaxID"]);
                    }
                }


                return new ResponseModel<ListItem> { Message = "Updated Successfully", Status = 200, RowId = maxID };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<ListItem> { Message = "Error occurred while adding data in FormList", Status = 500 };
            }



        }

        public ResponseModel<ListItem> SaveSubmitterAndApplicantDetailsModelData(ApplicantDataModel model, string listName, int formIdInput = 0)
        {
            var con = new SqlConnection(sqlConString);
            try
            {
                var otherEmpType = model.OnBehalfOption ?? "";
                var requestSubmissionFor = model.RequestSubmissionFor ?? "";
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                cmd = new SqlCommand("USP_SaveSubmitterAndApplicantDetails", con);
                cmd.Parameters.Add(new SqlParameter("@TableName", listName));
                cmd.Parameters.Add(new SqlParameter("@FormID", formIdInput));
                cmd.Parameters.Add(new SqlParameter("@EmployeeType", model.EmployeeType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCode", model.EmployeeCode));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCCCode", model.EmployeeCCCode));
                cmd.Parameters.Add(new SqlParameter("@EmployeeUserId", model.EmployeeUserId));
                cmd.Parameters.Add(new SqlParameter("@EmployeeName", model.EmployeeName));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDepartment", model.EmployeeDepartment));
                cmd.Parameters.Add(new SqlParameter("@EmployeeContactNo", model.EmployeeContactNo));
                cmd.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(new SqlParameter("@ExternalOrganizationName", model.ExternalOrganizationName ?? ""));
                cmd.Parameters.Add(new SqlParameter("@ExternalOtherOrganizationName", model.ExternalOrganizationName ?? ""));
                cmd.Parameters.Add(new SqlParameter("@EmployeeLocation", model.EmployeeLocation));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDesignation", model.EmployeeType == "External" ? "Team Member" : model.EmployeeDesignation));
                cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", ""));
                cmd.Parameters.Add(new SqlParameter("@RequestSubmissionFor", model.RequestSubmissionFor));
                cmd.Parameters.Add(new SqlParameter("@OnBehalfOption", otherEmpType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeEmailId", user.Email));
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", model.OtherEmployeeType ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", model.OtherEmployeeCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", model.OtherEmployeeCCCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", model.OtherEmployeeContactNo.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", model.OtherEmployeeUserId ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", model.OtherEmployeeName));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", model.OtherEmployeeDepartment ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", model.OtherEmployeeLocation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", model.OtherEmployeeType == "External" ? "Team Member" : model.OtherEmployeeDesignation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", model.OtherExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", model.OtherExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", model.OtherEmployeeEmailId ?? ""));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", model.OtherNewEmployeeType ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", model.OtherNewEmployeeCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", model.OtherNewCostcenterCode.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", model.OtherNewContactNo.ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", model.OtherNewUserId ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", model.OtherNewEmployeeName));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", model.OtherNewDepartment ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", model.OtherNewEmpLocation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", model.OtherNewEmployeeType == "External" ? "Team Member" : model.OtherNewEmpDesignation ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", model.OtherNewExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", model.OtherNewExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", model.OtherNewEmailId ?? ""));
                    }
                }

                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                string maxID = "";
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        maxID = Convert.ToString(ds.Tables[0].Rows[i]["MaxID"]);
                    }
                }


                return new ResponseModel<ListItem> { Message = "Updated Successfully", Status = 200, RowId = maxID };
            }
            catch (Exception ex)
            {
                con.Close();
                Log.Error(ex.Message, ex);
                return new ResponseModel<ListItem> { Message = "Error occurred while adding data in FormList", Status = 500 };
            }



        }
        public ResponseModel<string> UploadPhotoAndLicenseDetails(HttpPostedFileBase file, int itemId, string listName, string fileName = "")
        {
            try
            {
                //File Upload
                var (_context, web) = CreateClientContextAndWeb();
                var list = _context.Web.Lists.GetByTitle(listName);
                var listItem = list.GetItemById(itemId);
                _context.Load(listItem, li => li.AttachmentFiles);
                _context.ExecuteQuery();
                //delete existing file with same name
                foreach (var a in listItem.AttachmentFiles.ToList())
                {
                    if (a.FileName.Contains(Path.GetFileNameWithoutExtension(fileName)))
                        a.DeleteObject();
                }
                _context.ExecuteQuery();
                if (file != null)
                {
                    int attachmentID = itemId;

                    List docs = web.Lists.GetByTitle(listName);
                    ListItem itemAttach = docs.GetItemById(attachmentID);

                    var attInfo = new AttachmentCreationInformation();

                    attInfo.FileName = fileName;

                    byte[] fileData = null;
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        fileData = binaryReader.ReadBytes(file.ContentLength);
                    }

                    attInfo.ContentStream = new MemoryStream(fileData);

                    Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                    _context.Load(att);
                    _context.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<string>
                {
                    Status = 500,
                    Message = "There were some issue while uploading file."
                };
            }
            return new ResponseModel<string>
            {
                Status = 200,
                Message = "File Uploaded Successfully"
            };
        }
        public async Task<ResponseModel<string>> CopyAttachmentFromOneItemToAnother(int prevItemId, int itemId, string listName)
        {
            dynamic DAFDataList = new ExpandoObject();
            try
            {
                //File Upload
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('" + listName + "')/items?$select=AttachmentFiles"
                + "&$filter=(ID eq '" + prevItemId + "')&$expand=AttachmentFiles");
                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                AttachmentFilesResults AttachmentList = null;
                if (!string.IsNullOrEmpty(responseText))
                {
                    var DAFResults = JsonConvert.DeserializeObject<DrivingAuthorizationFormModel>(responseText, settings);
                    AttachmentList = DAFResults?.daflist?.dafData?[0].AttachmentFiles;
                }
                if (AttachmentList != null && AttachmentList.Attachments != null && AttachmentList.Attachments.Count > 0)
                {
                    foreach (var Attachment in AttachmentList.Attachments)
                    {
                        var (_context, web) = CreateClientContextAndWeb();
                        int attachmentID = itemId;

                        List docs = web.Lists.GetByTitle(listName);
                        ListItem itemAttach = docs.GetItemById(attachmentID);

                        var attInfo = new AttachmentCreationInformation();

                        attInfo.FileName = Attachment.FileName;

                        string imgBase64Str = "";
                        var file = _context.Web.GetFileByServerRelativeUrl(Attachment.ServerRelativeUrl);
                        _context.Load(file);
                        _context.ExecuteQuery();
                        ClientResult<System.IO.Stream> data = file.OpenBinaryStream();
                        _context.Load(file);
                        _context.ExecuteQuery();
                        byte[] fileData = null;
                        using (System.IO.MemoryStream mStream = new System.IO.MemoryStream())
                        {
                            if (data != null)
                            {
                                data.Value.CopyTo(mStream);
                                fileData = mStream.ToArray();
                            }
                        }
                        attInfo.ContentStream = new MemoryStream(fileData);

                        Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                        _context.Load(att);
                        _context.ExecuteQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<string>
                {
                    Status = 500,
                    Message = "There were some issue while copying file."
                };
            }
            return new ResponseModel<string>
            {
                Status = 200,
                Message = "File Copied Successfully"
            };
        }

        public ResponseModel<string> VerifyCostCenterAndManager(long EmpCode, long CostCenter)
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                SqlConnection con = new SqlConnection(sqlConString);
                SqlCommand cmd = new SqlCommand("sp_VerifyCostCenterAndManger", con);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@EmpNum", EmpCode));
                //cmd.Parameters.Add(new SqlParameter("@CCNum", CostCenter));
                //cmd.Parameters.Add(new SqlParameter("@MngrNum", EmpManagerCode));

                sqlDataAdapter.SelectCommand = cmd;
                con.Open();
                sqlDataAdapter.Fill(ds);
                con.Close();

                if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                {
                    //Only one row will be returned from the proc.
                    var row = ds.Tables[0].Rows[0];
                    long costCenter = Convert.ToInt64(row["CostCenter"] is null ? 0 : row["CostCenter"]);
                    long mngrNum = Convert.ToInt64(row["ManagerCode"] is null ? 0 : row["ManagerCode"]);
                    //sbyte status = Convert.ToSByte(row["Status"] is null ? 0 : row["Status"]);
                    //string message = Convert.ToString(row["Message"]);

                    bool isCostCenterMissing = costCenter == 0;
                    bool isManagerMissing = mngrNum == 0;
                    return new ResponseModel<string>
                    {
                        Status = isManagerMissing || isCostCenterMissing ? 500 : 200,
                        Message = isManagerMissing || isCostCenterMissing
                            ? $@"{(isCostCenterMissing ? "Cost Center" : "")}
                                {(isCostCenterMissing || isManagerMissing ? " and" : "")}
                                {(isManagerMissing ? " Manager Employee Number" : "")}
                                 of Employee Number {EmpCode} not found. Kindly contact HR for this issue."
                            : "CostCenter and Manager found successfully."
                    };
                }
                else
                {
                    return new ResponseModel<string> {
                        Status = 500,
                        Message = $"Cost Center for Employee Number {EmpCode} not found. Kindly contact HR for this issue."
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<string>
                {
                    Status = 500,
                    Message = $"There were some issue while verfiying Cost Center and Manager of Employee Number {EmpCode}."
                };
            }
        }
        public ResponseModel<ListItem> SaveSubmitterAndApplicantDetailsModelData_IA(System.Web.Mvc.FormCollection form, string listName, int formIdInput = 0, int FormId = 0)
        {
            try
            {
                //var otherEmpType = model.OnBehalfOption ?? "";
                var otherEmpType = form["rdOnBehalfOption"] ?? "";
                var requestSubmissionFor = form["drpRequestSubmissionFor"] ?? "";
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                var con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_SaveSubmitterAndApplicantDetails", con);

                if (FormId == 0)
                {
                    cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", "Yes")); ;
                }
                else
                {

                    cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", "No")); ;
                }
                cmd.Parameters.Add(new SqlParameter("@TableName", listName));
                cmd.Parameters.Add(new SqlParameter("@FormID", formIdInput));
                cmd.Parameters.Add(new SqlParameter("@EmployeeType", form["chkEmployeeType"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCode", form["txtEmployeeCode"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCCCode", form["txtCostcenterCode"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeUserId", form["txtUserId"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeName", form["txtEmployeeName"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDepartment", form["txtDepartment"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeContactNo", form["txtContactNo"]));
                cmd.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(new SqlParameter("@ExternalOrganizationName", form["txtExternalOrganizationName"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@ExternalOtherOrganizationName", form["txtOtherExternalOrganizationName"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@EmployeeLocation", form["ddEmpLocation"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDesignation", form["chkEmployeeType"] == "External" ? "Team Member" : form["ddEmpDesignation"]));

                cmd.Parameters.Add(new SqlParameter("@RequestSubmissionFor", form["drpRequestSubmissionFor"]));
                cmd.Parameters.Add(new SqlParameter("@OnBehalfOption", otherEmpType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeEmailId", user.Email));
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", form["chkOtherEmployeeType"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", form["txtOtherEmployeeCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", form["txtOtherCostcenterCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", form["txtOtherContactNo"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", form["txtOtherUserId"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", form["txtOtherEmployeeName"]));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", form["txtOtherDepartment"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", form["ddOtherEmpLocation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", form["chkOtherNewEmployeeType"] == "External" ? "Team Member" : form["ddOtherEmpDesignation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", form["txtOtherExternalOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", form["txtOtherExternalOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", form["txtOtherEmailId"] ?? ""));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", form["chkOtherNewEmployeeType"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", form["txtOtherNewEmployeeCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", form["txtOtherNewCostcenterCode"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", form["txtOtherNewContactNo"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", form["txtOtherNewUserId"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", form["txtOtherNewEmployeeName"]));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", form["txtOtherNewDepartment"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", form["ddOtherNewEmpLocation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", form["chkOtherNewEmployeeType"] == "External" ? "Team Member" : form["ddOtherNewEmpDesignation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", form["txtOtherNewExternalOtherOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", form["txtOtherNewExternalOtherOrganizationName"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", form["txtOtherNewEmailId"] ?? ""));
                    }
                }

                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                string maxID = "";
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        maxID = Convert.ToString(ds.Tables[0].Rows[i]["MaxID"]);
                    }
                }


                return new ResponseModel<ListItem> { Message = "Updated Successfully", Status = 200, RowId = maxID };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<ListItem> { Message = "Error occurred while adding data in FormList", Status = 500 };
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Skoda_DCMS.Models;
using Skoda_DCMS.Helpers;
using System.Data.SqlClient;
using System.Data;
using Microsoft.SharePoint.Client;
using System.Dynamic;
using System.Xml;
using System.Web.Helpers;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Models.CommonModels;
using static Skoda_DCMS.Helpers.Flags;
using Skoda_DCMS.Extension;

namespace Skoda_DCMS.DAL
{
    public class EmployeeClearanceDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        /// <summary>
        /// ECF-It is used to get autocomplete data for employee code.
        /// </summary>
        /// <returns></returns>
        public List<UserData> GetECFEmployeeDetails(string empCode)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetECFEmployee", con);
                cmd.Parameters.Add(new SqlParameter("@EmpCode", empCode));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        UserData user = new UserData();
                        user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        //user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        //user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        //user.EmployeeName = user.FirstName + " " + user.LastName;
                        //user.CostCentreFrom = ds.Tables[0].Rows[i]["CostCenter"].ToString();
                        //user.DepartmentFrom= ds.Tables[0].Rows[i]["Department"].ToString();
                        //user.SubDepartmentFrom = ds.Tables[0].Rows[i]["SubDepartment"].ToString();
                        //user.ReportingManagerFrom = ds.Tables[0].Rows[i]["ManagerEmployeeNumber"].ToString();

                        //user.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return users;
        }

        //public ECFData GetECFExistingEmployeeDetails(string otherEmpUserId)
        //{
        //    ECFData user = new ECFData();
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_GetECFExistingEmpDet", con);
        //        cmd.Parameters.Add(new SqlParameter("@UserId", otherEmpUserId));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
        //                user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
        //                user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
        //                user.EmployeeName = user.FirstName + " " + user.LastName;
        //                user.Department = ds.Tables[0].Rows[i]["Department"].ToString();
        //                user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
        //                //user.Location = ds.Tables[0].Rows[i]["DepartmentDIVId"].ToString();
        //            }
        //        }
        //    }
        //    catch (Exception ex) { Log.Error(ex.Message, ex); }
        //    return user;
        //}

        /// <summary>
        /// ECF-It is used to get the approval list for ECF form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetECF()
        {
            ECFResults ecfData = new ECFResults();
            dynamic result = ecfData;
            try
            {
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('EmployeeClearanceFormApprovalMaster')/items?$select=Section,Department,SubDepartment,Location,ID&$filter=(IsActive eq '1' and Location eq 'Pune')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var ECFResult = JsonConvert.DeserializeObject<ECFModel>(responseText);
                    ecfData = ECFResult.ecflist;
                }
                result = ecfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// ECF-It is used to get the approval list for Employee Clearance form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetECFSequentialApprovalList()
        {
            ECFResults ecfData = new ECFResults();
            dynamic result = ecfData;
            try
            {
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('EmployeeClearanceFormSequentialApproverInformationMaster')/items?$select=Department,SubDepartment,Location,ApproverEmployeeCode,ApproverEmailId,ID&$filter=(IsActive eq '1')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var ECFResult = JsonConvert.DeserializeObject<ECFModel>(responseText);
                    ecfData = ECFResult.ecflist;
                }
                result = ecfData.ecfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// ECF Form-It is used to get the Employee Data in ECF Form.
        /// </summary>
        /// <returns></returns>
        public List<UserData> GetECFChargeHandOverEmployeeDetails(string empName)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetPAFEmployeeDetails", con);
                cmd.Parameters.Add(new SqlParameter("@FirstName", empName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        UserData user = new UserData();
                        user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        //user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        //user.UserName = user.FirstName + " " + user.LastName;
                        user.UserName = user.FirstName;
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }

        /// <summary>
        /// ECF Form-It is used to get the exisitng Employee Data in ECF Form.
        /// </summary>
        /// <returns></returns>
        //public ECFData GetECFHandOverExistingEmployeeDetails(string otherEmpUserId)
        //{
        //    ECFData user = new ECFData();
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_GetPAFExistingEmployeeDetails", con);
        //        cmd.Parameters.Add(new SqlParameter("@UserId", otherEmpUserId));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
        //                user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
        //                user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
        //                user.EmployeeName = user.FirstName + " " + user.LastName;
        //                user.Department = ds.Tables[0].Rows[i]["Department"].ToString();
        //                user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
        //            }
        //        }
        //    }
        //    catch (Exception ex) { Log.Error(ex.Message, ex); }
        //    return user;
        //}

        /// <summary>
        /// ECF-It is used to save ECF form to sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> SaveECF(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();

            ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential(spUsername, spPass);
            _context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("ECF") ? GlobalClass.ListNames["ECF"] : "";
            string formShortName = "ECF";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List name not found.";    
                return result;
            }
            int formId = 0;
            int FormId = Convert.ToInt32(form["formId"]);
            bool IsResubmit = FormId == 0 ? false : true;
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            int RowId = 0;
            string formName = "";
            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                var payrollEmail = form["hiddenPayrollApprover"];
                bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                //int empLocationId = isSelf ? Convert.ToInt32(form["EmpLocationID"]) : (isSAVWIPL ? Convert.ToInt32(form["OtherEmpLocationID"]) : Convert.ToInt32(form["OtherNewEmpLocationID"]));
                string empLoc = requestSubmissionFor.ToLower() == "onbehalf"
                    ? otherEmpType.ToLower() == "savwiplemployee"
                        ? form["ddOtherEmpLocation"] ?? ""
                        : form["ddOtherNewEmpLocation"] ?? ""
                    : form["ddEmpLocation"] ?? "";
                int empLocationId = empLoc.ToLower().Contains("pune") ? 1 : empLoc.ToLower().Contains("aurangabad") ? 3 : 2;
                var response = await GetApprovalECF(empNum, ccNum, empLocationId);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approverIdList = response.Model;

                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Employee Clearance Form";
                    item["UniqueFormName"] = "ECF";
                    item["FormParentId"] = 6;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "EmployeeClearance";
                    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = form["ddOtherEmpLocation"];
                        }
                        else
                        {
                            item["Location"] = form["ddOtherNewEmpLocation"];
                        }
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(FormId);
                    item["FormName"] = "Employee Clearance Form";
                    item["UniqueFormName"] = "ECF";
                    item["FormParentId"] = 6;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "EmployeeClearance";
                    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = form["ddOtherEmpLocation"];
                        }
                        else
                        {
                            item["Location"] = form["ddOtherNewEmpLocation"];
                        }
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;

                    ListDAL dal = new ListDAL();
                    var resubmitResult = await dal.ResubmitUpdate(formId);

                    if (AppRowId != 0)
                    {
                        List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                        ListItem listItem = listApprovalMaster.GetItemById(AppRowId);
                        listItem["ApproverStatus"] = "Resubmitted";
                        listItem["IsActive"] = 0;
                        listItem.Update();
                        _context.Load(listItem);
                        _context.ExecuteQuery();
                    }
                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetails(web, form, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                var newRow = userDetailsResponse.Model;
                //<Form Transaction Fields>
                newRow["DateOfJoining"] = form["txtDateOfJoining"];
                newRow["DateOfRelieving"] = form["txtDateOfRelieving"];
                newRow["BusinessNeed"] = form["txtBusinessNeed"];
                newRow["ResignationGivenDate"] = form["txtResignationGivenDate"];
                newRow["ChargeHandOverToEmpName"] = form["txtChargeHandedOverToEmpName"];
                newRow["ChargeHandOverToEmpNum"] = form["txtChargeHandedOverToEmpNumber"];
                newRow["FormID"] = formId;
                //newRow["ResignationReceivedDate"] = form["txtResignationReceivedDate"] ?? "";
                //newRow["ResignationGivenDate"] = form["txtResignationGivenDate"] ?? "";
                //newRow["ApplicableDays"] = form["txtNoticeApplicableDays"] ?? "";
                //newRow["NoticePeriod"] = form["chkNotice"] ?? "";
                //newRow["Gratuity"] = form["chkGratuity"] ?? "";
                //</Form Transaction Fields>
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                RowId = newRow.Id;
                result.Status = 200;
                result.Message = formId.ToString();

                var approvalResponse = await SaveApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }

                //Data Row ID Update in Forms List
                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

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
                    Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
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
                return result;
            }
            return result;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalECF(long empNum, long ccNum, long empLocationId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_EmployeeClearance", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", empNum));
                cmd.Parameters.Add(new SqlParameter("@CCNum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@LocationId", empLocationId));
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
                        app.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(ds.Tables[0].Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailId"]);
                        app.Designation = Convert.ToString(ds.Tables[0].Rows[i]["desg"]);
                        app.ApprovalLevel = (int)ds.Tables[0].Rows[i]["approvalLevel"];
                        app.ExtraDetails = Convert.ToString(ds.Tables[0].Rows[i]["Contents"]);
                        app.Logic = Convert.ToString(ds.Tables[0].Rows[i]["logic"]);
                        app.LogicId = Convert.ToInt64(ds.Tables[0].Rows[i]["LogicId"]);
                        app.LogicWith = Convert.ToInt64(ds.Tables[0].Rows[i]["LogicWith"]);
                        app.RelationId = Convert.ToInt64(ds.Tables[0].Rows[i]["RelationId"]);
                        app.RelationWith = Convert.ToInt64(ds.Tables[0].Rows[i]["RelationWith"]);
                        appList.Add(app);
                    }
                }
                //else
                //{
                //    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver not found." }; ;
                //}


                //if (appList.ContainsAllLevels() == 0)
                //{
                //    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver is missing." }; ;
                //}
                //appList = AddMDAssistantToList(appList);
                //appList = ChangeDelegateApprover(appList);
                //return await GetApproverUserID(appList);
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }

        }

        //public List<string> GetHRCEmailID(long ccNum)
        //{
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();
        //        List<string> appList = new List<string>();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_HRCEmailID", con);
        //        cmd.Parameters.Add(new SqlParameter("@CCNum", ccNum));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                string app = Convert.ToString(ds.Tables[0].Rows[i]["EmailId"]);
        //                appList.Add(app);
        //            }
        //        }
        //        //appList = AddMDAssistantToList(appList);
        //        //appList = ChangeDelegateApprover(appList);
        //        //return await GetApproverUserID(appList);
        //        return appList;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //        return new List<string>();
        //    }
        //}

        public async Task<dynamic> ViewECFData(int rowId, int formId)
        {
            dynamic ECFDataList = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client1 = new HttpClient(handler);
                client1.BaseAddress = new Uri(conString);
                client1.DefaultRequestHeaders.Accept.Clear();
                client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('EmployeeClearanceForm')/items?$select=*," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var ECFResult = JsonConvert.DeserializeObject<ECFModel>(responseText1, settings);
                    ECFDataList.one = ECFResult.ecflist.ecfData;
                }

                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                ECFDataList.two = r1.Model;
                ECFDataList.three = r2.Model;

                //var client4 = new HttpClient(handler);
                //client4.BaseAddress = new Uri(conString);
                //client4.DefaultRequestHeaders.Accept.Clear();
                //client4.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                //var response4 = await client4.GetAsync("_api/web/lists/GetByTitle('EmployeeClearanceFormApprovalMaster')/items?$select=Section,Department,SubDepartment,Location,ID&$filter=(IsActive eq '1')");
                //var responseText4 = await response4.Content.ReadAsStringAsync();

                //if (!string.IsNullOrEmpty(responseText4))
                //{
                //    var ECFResult = JsonConvert.DeserializeObject<ECFModel>(responseText4);
                //    ECFDataList.four = ECFResult.ecflist.ecfData;
                //}

                //var Parallelsettings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};

                //var clientParallel = new HttpClient(handler);
                //clientParallel.BaseAddress = new Uri(conString);
                //clientParallel.DefaultRequestHeaders.Accept.Clear();
                //clientParallel.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                ////var responseParallelApprovalMaster = await client.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,ApproverStatus,RowId,Modified,Author/Title,FormId/FormName," +
                ////              "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(FormId eq '" + formId + "' and ApproverId eq '" + user.UserId + "')&$expand=FormId,Author");

                //var responseParallelApprovalMaster = await clientParallel.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,ApprovalType,Comment,ApproverStatus,Department,Section,RowId,Modified,Author/Title,FormId/FormName," +
                //         "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                //var responseTextParallelApprovalMaster = await responseParallelApprovalMaster.Content.ReadAsStringAsync();
                //var modelDataParallel = JsonConvert.DeserializeObject<ParallelApprovalMasterModel>(responseTextParallelApprovalMaster, Parallelsettings);
                //dynamic modelDataParallelData = System.Web.Helpers.Json.Decode(responseTextParallelApprovalMaster);
                //var Parallelitems = new List<ParallelApprovalDataModel>();
                //if (modelDataParallelData.d.results.Length != 0)
                //{
                //    var Parallelclient1 = new HttpClient(handler);
                //    Parallelclient1.BaseAddress = new Uri(conString);
                //    Parallelclient1.DefaultRequestHeaders.Accept.Clear();
                //    Parallelclient1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //    Parallelitems = modelDataParallel.ParallelNode.Data;
                //    var Parallelnames = new List<string>();
                //    var ParalleidString = "";
                //    var ParallelresponseText3 = "";
                //    for (int i = 0; i < Parallelitems.Count; i++)
                //    {
                //        var id = Parallelitems[i];
                //        //ParalleidString += $"Id eq '{id.ApproverId}' {(i != Parallelitems.Count - 1 ? "or " : "")}";
                //        ParalleidString = $"Id eq '{id.ApproverId}'";
                //        Parallelitems[i].UserLevel = i + 1;//
                //        var Parallelresponse3 = await Parallelclient1.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + ParalleidString + ")");
                //        ParallelresponseText3 = await Parallelresponse3.Content.ReadAsStringAsync();
                //        dynamic Paralleldata4 = Json.Decode(ParallelresponseText3);

                //        if (Paralleldata4.Count != 0)
                //        {
                //            foreach (var name in Paralleldata4.d.results)
                //            {
                //                Parallelnames.Add(name.Title as string);
                //            }
                //        }
                //    }

                //    Parallelitems = Parallelitems.OrderBy(x => x.UserLevel).ToList();
                //    if (Parallelitems.Count == Parallelnames.Count)
                //    {
                //        for (int k = 0; k < Parallelitems.Count; k++)
                //        {
                //            Parallelitems[k].UserName = Parallelnames[k];
                //        }
                //    }
                //    Parallelitems = Parallelitems.OrderBy(x => x.UserLevel).ToList();

                //    if (!string.IsNullOrEmpty(responseTextParallelApprovalMaster))
                //    {
                //        dynamic Paralleldata2 = Json.Decode(responseTextParallelApprovalMaster);
                //        ECFDataList.five = Paralleldata2.d.results;
                //        ECFDataList.six = Parallelitems;
                //    }

                //}

                return ECFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return ECFDataList;
            }
        }

        public async Task<int> ECFValidityUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            string listName = string.Empty;

            Web web = _context.Web;
            var department = form["departmentName"];
            int formId = Convert.ToInt32(form["formId"]);

            listName = GlobalClass.ListNames.ContainsKey("ECF") ? GlobalClass.ListNames["ECF"] : "";

            if (listName == "")
            {
                return 0;
            }

            try
            {
                if (department == "Immediate Supervisor")
                {
                    List list = _context.Web.Lists.GetByTitle(listName);
                    ListItem newItem = list.GetItemById(formId);

                    newItem["DateOfRelieving"] = form["txtDateOfRelieving"];

                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();
                }
                else if (department == "Payroll")
                {
                    List list = _context.Web.Lists.GetByTitle(listName);
                    ListItem newItem = list.GetItemById(formId);

                    newItem["DateOfRelieving"] = form["txtDateOfRelieving"];
                    newItem["ResignationGiven"] = form["txtResignationGiven"];
                    newItem["ApplicableDays"] = form["txtApplicableDays"];
                    newItem["EligibleForGratuity"] = form["drpEligibleForGratuity"];
                    newItem["NoticePeriod"] = form["drpNoticePeriod"];

                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();
                }

            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

        public bool SaveApproverResponse(System.Web.Mvc.FormCollection form, UserData user)
        {
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            var listName = GlobalClass.ListNames.ContainsKey("ECF") ? GlobalClass.ListNames["ECF"] : "";
            if (listName == "")
                return false;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem item = list.GetItemById(formId);
                item["DisciplinaryAction"] = form["chkDisciplinaryAction"];
                item["CreditCard"] = form["chkCreditCard"];
                item["ResignationReceivedDate"] = form["txtResignationReceivedDate"];
                item["ApplicableDays"] = form["txtNoticeApplicableDays"] ?? "";
                item["NoticePeriod"] = form["chkNoticePeriod"];
                item["Gratuity"] = form["chkGratuity"];
                item.Update();
                _context.Load(item);
                _context.ExecuteQuery();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return false;
            }
            return true;
        }

    }
}
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Xml;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class ConflictOfInterestDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        /// <summary>
        /// Conflict Of Interest Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        //public async Task<dynamic> CreateConflictOfInterestRequest(System.Web.Mvc.FormCollection form, UserData user)
        public async Task<ResponseModel<object>> CreateConflictOfInterestRequest(System.Web.Mvc.FormCollection form, UserData user)
        {

            int currentMon = DateTime.Now.Month;
            string currentMonth = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentMon);
            string currentYear = DateTime.Now.Year.ToString();
            string businessNeed = "COI" + " - " + currentMonth + " " + currentYear;
            ListDAL dal = new ListDAL();
            ResponseModel<object> result = new ResponseModel<object>();
            //  dynamic result = new ExpandoObject();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "COIF";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                //result.one = 0;
                //result.two = 0;
                return result;
            }

            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            int FormId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);

            bool IsResubmit = FormId == 0 ? false : true;
            var approverIdList = new List<ApprovalMatrix>();

            long txtEmployeeCode = Convert.ToInt32(form["txtEmployeeCode"]);
            long txtCostcenterCode = Convert.ToInt32(form["txtCostcenterCode"]);
            string isQ1Yes = form["chkIsQ1Yes"];
            string isQ2Yes = form["chkIsQ2Yes"];
            string isQ3Yes = form["chkIsQ3Yes"];
            try
            {

                if (isQ1Yes == "No" && isQ2Yes == "No" && isQ3Yes == "No")
                {

                    approverIdList.Count();
                }
                else
                {
                    //    approverIdList = await GetApprovalForConflictOfInterest(txtEmployeeCode, txtCostcenterCode, isQ1Yes, isQ2Yes, isQ3Yes);
                    //    //approverIdList = new CommonDAL().AddMDAssistantToList(approverIdList);
                    //}
                    //if (approverIdList == null)
                    //{
                    //    result.one = 0;
                    //    result.two = 0;
                    //    return result;
                    //}
                    var response = await GetApprovalForConflictOfInterest(txtEmployeeCode, txtCostcenterCode, isQ1Yes, isQ2Yes, isQ3Yes);

                    if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                    {
                        result.Status = 500;
                        result.Message = response.Message;
                        return result;
                    }
                    var approverIdList1 = response.Model;
                }

                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Conflict Of Interest Form";
                    item["UniqueFormName"] = "COIF";
                    item["FormParentId"] = 23;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ConflictOfInterest";
                    item["BusinessNeed"] = businessNeed;
                    item["Location"] = form["ddEmpLocation"];
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(FormId);
                    item["FormName"] = "Conflict Of Interest Form";
                    item["UniqueFormName"] = "COIF";
                    item["FormParentId"] = 23;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ConflictOfInterest";
                    item["BusinessNeed"] = businessNeed;
                    item["Location"] = form["ddEmpLocation"];
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();
                    formId = item.Id;


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
                List FormList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                ListItem newRow = FormList.AddItem(itemInfo);
                if (FormId == 0)
                {
                    newRow["TriggerCreateWorkflow"] = "Yes";
                }
                else
                {
                    newRow["TriggerCreateWorkflow"] = "No";
                }
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["EmployeeType"] = form["chkEmployeeType"];
                newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newRow["EmployeeCode"] = form["txtEmployeeCode"];
                newRow["EmployeeDesignation"] = form["ddEmpDesignation"] ?? "";// DropDown selection
                newRow["EmployeeLocation"] = form["ddEmpLocation"]; //Dropdown selection
                newRow["EmployeeCCCode"] = form["txtCostcenterCode"]; //
                newRow["EmployeeUserId"] = form["txtUserId"]; //SharePoint user Id
                newRow["EmployeeName"] = form["txtEmployeeName"];
                newRow["EmployeeDepartment"] = form["txtDepartment"];
                newRow["EmployeeContactNo"] = form["txtContactNo"];
                newRow["EmployeeEmailId"] = user.Email;

                newRow["BusinessNeed"] = businessNeed;
                newRow["Elaboration1"] = form["txtElaboration1"];
                newRow["Elaboration2"] = form["txtElaboration2"];
                newRow["Elaboration3"] = form["txtElaboration3"];
                newRow["IsQ1Yes"] = form["chkIsQ1Yes"];
                newRow["IsQ2Yes"] = form["chkIsQ2Yes"];
                newRow["IsQ3Yes"] = form["chkIsQ3Yes"];

                //Added Changes//
                //A Question
                newRow["Chkaq1"] = form["Chkaq1"];
                newRow["txtQustionA_1_data"] = form["txtQustionA_1_data"];
                newRow["soChk"] = form["soChk"];
                newRow["shareChk"] = form["shareChk"];
                newRow["partnershpChk"] = form["partnershpChk"];
                newRow["viaChk"] = form["viaChk"];
                newRow["ChkOther"] = form["ChkOther"];
                newRow["txtOther"] = form["txtOther"];
                newRow["Chkaq2"] = form["Chkaq2"];
                newRow["txtQustionA_2_data"] = form["txtQustionA_2_data"];

                //B Question           
                newRow["txtQustionB_1_data"] = form["txtQustionB_1_data"];

                //C Question             
                newRow["txtQustionC_1_data"] = form["txtQustionC_1_data"];

                //Added Changes//

                //for (int i = 0; i < approverIdList.Count; i++)
                //{
                //    newRow["Approver" + (i + 1)] = approverIdList[i].UserId;
                //}
                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                //result.one = 1;
                //result.two = formId;
                result.Status = 200;
                result.Message = formId.ToString();

                //Task Entry in Approval Master List

                var rowid = newRow.Id;
                int level = 1;
                List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");

                for (var i = 0; i < approverIdList.Count; i++)
                {
                    ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                    ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);

                    approvalMasteritem["FormId"] = formId;
                    approvalMasteritem["RowId"] = rowid;
                    approvalMasteritem["ApproverUserName"] = approverIdList[i].ApproverUserName;


                    //if (approverIdList[i].IsIS == true)
                    //{
                    //    approvalMasteritem["Level"] = 1;
                    //    approvalMasteritem["Logic"] = "NOT";

                    //}
                    //else if (approverIdList[i].IsIS == false)
                    //{
                    //    approvalMasteritem["Level"] = 2;
                    //    approvalMasteritem["Logic"] = "OR";

                    //}
                    approvalMasteritem["Level"] = approverIdList[i].ApprovalLevel;
                    approvalMasteritem["Logic"] = approverIdList[i].Logic;
                    approvalMasteritem["Designation"] = approverIdList[i].Designation;

                    if (approverIdList[i].ApprovalLevel == 1)
                    {
                        approvalMasteritem["IsActive"] = 1;
                    }
                    else
                    {
                        approvalMasteritem["IsActive"] = 0;
                    }
                    if (approverIdList[i].ApprovalLevel == approverIdList.Max(x => x.ApprovalLevel))
                    {
                        approvalMasteritem["NextApproverId"] = 0;
                    }
                    else
                    {
                        //var currentApproverLevel = approverIdList[i].ApprovalLevel;
                        //approvalMasteritem["NextApproverId"] = approverIdList.Any(x => x.ApprovalLevel == currentApproverLevel + 1) ? approverIdList.Where(x => x.ApprovalLevel == currentApproverLevel + 1).FirstOrDefault().ApproverUserName : "";
                        approvalMasteritem["NextApproverId"] = 0;
                    }

                    approvalMasteritem["ApproverStatus"] = "Pending";

                    approvalMasteritem["RunWorkflow"] = "No";

                    approvalMasteritem["BusinessNeed"] = businessNeed;

                    approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                    approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                    approvalMasteritem.Update();
                    _context.Load(approvalMasteritem);
                    _context.ExecuteQuery();
                    level++;
                }
                //Task Entry in Approval Master List

                //Data Row ID Update in Forms List
                List formslist = _context.Web.Lists.GetByTitle("Forms");
                ListItem newItem = formslist.GetItemById(formId);
                newItem.RefreshLoad();
                _context.ExecuteQuery();
                newItem["DataRowId"] = rowid;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();

                var completeFormName = await dal.GetFormNameByUniqueName(formShortName);

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, rowid);
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
                    FormName = completeFormName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                    CurrentUser = user
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                return result;
            }
        }
        /// <summary>
        /// Conflict Of Interest Form-It is used for viewing software requisition form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetConflictOfInterestDetails(int rowId, int formId)
        {
            dynamic COI = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ConflictOfInterest')/items?$select=ID,EmployeeCode,EmployeeType," +
                     "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,BusinessNeed," +
                    "IsQ1Yes,IsQ2Yes,IsQ3Yes,Elaboration1,Elaboration2,Elaboration3,Chkaq1,soChk,shareChk,viaChk,partnershpChk,ChkOther,txtOther,txtQustionA_1_data,Chkaq2,txtQustionA_2_data,txtQustionB_1_data,txtQustionC_1_data,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<ConflictOfInterestModel>(responseText, settings);
                    COI.one = result.List.COIList;
                }

                //approval start
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,Created,IsActive,Comment,NextApproverId,ApproverName,ApproverUserName,Designation,Level,Logic,TimeStamp,Designation,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                if (modelData.Node.Data.Count > 0)
                {
                    var client3 = new HttpClient(handler);
                    client3.BaseAddress = new Uri(conString);
                    client3.DefaultRequestHeaders.Accept.Clear();
                    client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var items = modelData.Node.Data;
                    var idString = "";
                    var names = new List<string>();


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
                    //AD Code


                    if (items.Count == names.Count)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i].UserName = names[i];
                        }
                    }
                    items = items.OrderBy(x => x.UserLevel).ToList();

                    if (!string.IsNullOrEmpty(responseText2))
                    {
                        dynamic data2 = Json.Decode(responseText2);
                        COI.two = data2.d.results;
                        COI.three = items;
                        COI.four = 1;
                    }
                }
                else
                {
                    COI.two = new List<string>();
                    COI.three = new List<string>();
                    COI.four = 0;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return COI;
        }
        /// <summary>
        /// Conflict Of Interest Form-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForConflictOfInterest(long txtEmployeeCode, long txtCostcenterCode, string isQ1Yes, string isQ2Yes, string isQ3Yes)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ConflictOfInterestApproval", con);
                if (isQ1Yes == "No" && isQ2Yes == "No" && isQ3Yes == "No")
                {
                    //cmd.Parameters.Add(new SqlParameter("@emailId", ""));
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", ""));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", ""));
                }
                else
                {
                    //cmd.Parameters.Add(new SqlParameter("@emailId", user.Email));
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostcenterCode));
                }

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
                        app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailId"]);
                        app.Designation = Convert.ToString(ds.Tables[0].Rows[i]["desg"]);
                        app.ApprovalLevel = (int)ds.Tables[0].Rows[i]["approvalLevel"];
                        app.Logic = Convert.ToString(ds.Tables[0].Rows[i]["logic"]);
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
                var common = new CommonDAL();
                appList = common.CallAssistantAndDelegateFunc(appList);

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.Timeout = TimeSpan.FromSeconds(10);
                var emailString = "";
                var count = appList.Count;


                //AD Code
                ListDAL obj = new ListDAL();
                for (int i = 0; i < count; i++)
                {
                    string eml = appList[i].EmailId;
                    string currentId = obj.GetUserIdByEmailId(eml);
                    appList[i].ApproverUserName = Convert.ToString(currentId);
                    appList[i].ApproverName = obj.GetApproverNameByEmailId(eml);
                }
                //AD Code

                if (appList.CheckApproverUserName() == 0)
                {
                    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver User Id is missing." }; ;
                }

                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
                // return appList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
                //return null;
            }

        }
    }
}
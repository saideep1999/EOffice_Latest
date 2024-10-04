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
    public class SmartPhoneDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        /// <summary>
        /// SmartPhone-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        //public async Task<dynamic> CreateSmartPhoneRequisitionRequest(System.Web.Mvc.FormCollection form, UserData user)
        public async Task<ResponseModel<object>> CreateSmartPhoneRequisitionRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            // dynamic result = new ExpandoObject();
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;

            Web web = _context.Web;
            string formShortName = "SPRF";
            string formName = "SmartPhone Requisition Form";
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

            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                long empNum = requestSubmissionFor == "Self" ? user.EmpNumber : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                long ccNum = requestSubmissionFor == "Self" ? user.CostCenter : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));


                var response = await GetApprovalForSmartPhone(empNum, ccNum);
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
                    item["FormName"] = "Smart Phone Requisition Form";
                    item["UniqueFormName"] = "SPRF";
                    item["FormParentId"] = 9;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SmartPhoneRequest";
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
                    item["FormName"] = "Smart Phone Requisition Form";
                    item["UniqueFormName"] = "SPRF";
                    item["FormParentId"] = 9;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SmartPhoneRequest";
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
                List SmartPhoneList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                ListItem newRow = SmartPhoneList.AddItem(itemInfo);
                if (FormId == 0)
                {
                    newRow["TriggerCreateWorkflow"] = "Yes";
                }
                else
                {
                    newRow["TriggerCreateWorkflow"] = "No";
                }
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newRow["EmployeeType"] = form["chkEmployeeType"];
                newRow["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                newRow["EmployeeCode"] = form["txtEmployeeCode"] ?? "";
                newRow["EmployeeDesignation"] = form["TempDesignation"] ?? "";// DropDown selection
                newRow["EmployeeLocation"] = form["ddEmpLocation"] ?? ""; //Dropdown selection
                newRow["EmployeeCCCode"] = form["txtCostcenterCode"] ?? ""; //
                newRow["EmployeeUserId"] = form["txtUserId"] ?? ""; //SharePoint user Id
                newRow["EmployeeName"] = form["txtEmployeeName"] ?? "";
                newRow["EmployeeDepartment"] = form["txtDepartment"] ?? "";
                newRow["EmployeeContactNo"] = form["txtContactNo"] ?? "";
                newRow["EmployeeEmailId"] = user.Email;
                //Other Employee Details
                newRow["OnBehalfOption"] = otherEmpType;

                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        newRow["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                        newRow["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";
                        newRow["OtherEmployeeDesignation"] = form["TempOtherDesignation"] ?? "";// DropDown selection
                        newRow["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? ""; //Dropdown selection
                        newRow["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? ""; //
                        newRow["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? ""; //SharePoint user Id
                        newRow["OtherEmployeeDepartment"] = form["txtOtherDepartment"] ?? "";
                        newRow["OtherEmployeeContactNo"] = form["txtOtherContactNo"] ?? "";
                        newRow["OtherEmployeeEmailId"] = form["txtOtherEmailId"] ?? "";
                        newRow["OnBehalfOption"] = form["rdOnBehalfOption"] ?? "";
                        newRow["OtherEmployeeType"] = form["chkOtherEmployeeType"] ?? "";
                        newRow["OtherExternalOrganizationName"] = form["ddOtherExternalOrganizationName"] ?? "";
                        newRow["OtherExternalOtherOrgName"] = form["txtOtherExternalOtherOrganizationName"] ?? "";

                    }
                    else
                    {
                        newRow["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                        newRow["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";
                        newRow["OtherEmployeeDesignation"] = form["TempOtherDesignation"] ?? "";// DropDown selection
                        newRow["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? ""; //Dropdown selection
                        newRow["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? ""; //
                        newRow["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? ""; //SharePoint user Id
                        newRow["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                        newRow["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                        newRow["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                        newRow["OtherEmployeeType"] = form["chkOtherNewEmployeeType"] ?? "";
                        newRow["OtherExternalOrganizationName"] = form["ddOtherNewExternalOrganizationName"] ?? "";
                        newRow["OtherExternalOtherOrgName"] = form["txtOtherNewExternalOtherOrganizationName"] ?? "";
                    }
                }
                string Designation = "";
                if (requestSubmissionFor == "OnBehalf")
                {
                    Designation = form["TempOtherDesignation"] ?? "";
                }
                else
                {
                    Designation = form["TempDesignation"] ?? "";
                }

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                result.Status = 200;
                result.Message = formId.ToString();
                //result.one = 1;
                //result.two = formId;

                //Task Entry in Approval Master List
                var rowid = newRow.Id;

                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, rowid);
                bool ifApproverExists = false;

                if (Designation == "Team Member" || Designation == "Graduate Engineer Trainee" || Designation == "Officer"
                    || Designation == "Senior Officer" || Designation == "Assistant Manager" || Designation == "Deputy Manager"
                    || Designation == "Manager" || Designation == "Senior Manager" || Designation == "Chief Manager"
                    || Designation == "Intern" || Designation == "Apprentice")
                {
                    ifApproverExists = true;
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

                        //approvalMasteritem["ApproverUserName"] = approverIdList[i].ApproverUserName;

                        approvalMasteritem["RunWorkflow"] = "No";

                        approvalMasteritem["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                        approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                        approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                        approvalMasteritem.Update();
                        _context.Load(approvalMasteritem);
                        _context.ExecuteQuery();

                    }
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
                }

                //Data Row ID Update in Forms List
                List formslist = _context.Web.Lists.GetByTitle("Forms");
                ListItem newItem = formslist.GetItemById(formId);
                newItem.RefreshLoad();
                _context.ExecuteQuery();
                newItem["DataRowId"] = rowid;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = ifApproverExists ? (IsResubmit ? FormStates.ReSubmit : FormStates.Submit) : FormStates.FinalApproval,
                    Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                    FormName = formName,
                    CurrentUser = user,
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                //result.one = 0;
                //result.two = 0;
                return result;
            }
        }

        /// <summary>
        /// SmartPhone-It is used for viewing the smartphone form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetSmartPhoneRequisitionDetails(int rowId, int formId)
        {
            dynamic smartPhone = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('SmartPhoneRequisition')/items?$select=ID,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,BusinessNeed," +
                    "EmployeeType,RequestSubmissionFor,EmployeeEmailId,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName," +
                    "OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeLocation,OtherEmployeeDepartment,OtherEmployeeEmailId,OtherEmployeeType,Designation,OnBehalfOption,ExternalOrganizationName,ExternalOtherOrganizationName" +
                    ",FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<SmartPhoneRequisitionModel>(responseText, settings);
                    smartPhone.one = result.List.SmartPhoneList;
                }
                //approval start
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverName,ApproverUserName,NextApproverId,Level,Logic,TimeStamp,Designation,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                if (modelData.Node.Data.Count > 0)
                {
                    var client3 = new HttpClient(handler);
                    client3.BaseAddress = new Uri(conString);
                    client3.DefaultRequestHeaders.Accept.Clear();
                    client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var names = new List<string>();
                    var responseText3 = "";

                    var items = modelData.Node.Data;
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
                        smartPhone.two = data2.d.results;
                        smartPhone.three = items;
                        smartPhone.four = 1;
                    }
                }
                else
                {
                    smartPhone.two = new List<string>();
                    smartPhone.three = new List<string>();
                    smartPhone.four = 0;
                }

                //approval end
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return smartPhone;
        }
        /// <summary>
        /// SmartPhone-It is used to get approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForSmartPhone(long empNum, long ccNum)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SmartPhoneApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
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

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
                // return new List<ApprovalMatrix>();
            }
        }
    }
}
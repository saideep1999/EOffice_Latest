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
    public class ResourceAccountAndDLDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        /// <summary>
        /// Resource Account & Distribution List-It is used to get the locations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<List<ResourceAccountLocation>> GetResourceAccountLocation()
        {
            List<ResourceAccountLocation> locations = new List<ResourceAccountLocation>();//constructor called, always expandoobject for dynamic dataType

            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ResourceAccountLocation')/items?$select=ResAcctLocId,Location");
                var responseText = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<ResourceAccountLocationModel>(responseText);
                    locations = result.List.ResourceAccountLocations;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<ResourceAccountLocation>();
            }
            return locations;
        }
        /// <summary>
        ///Resource Account And Distribution List Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        //public async Task<dynamic> CreateResourceAccountDLRequest(System.Web.Mvc.FormCollection form, UserData user)
        public async Task<ResponseModel<object>> CreateResourceAccountDLRequest(System.Web.Mvc.FormCollection form, UserData user)
        {

            ResponseModel<object> result = new ResponseModel<object>();
            //dynamic result = new ExpandoObject();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "RADLF";
            string formName = "Resource Account And Distribution List Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "List not found.";
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
                long ccNum = requestSubmissionFor == "Self" ? user.CostCenter : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                long empNum = requestSubmissionFor == "Self" ? user.EmpNumber : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                var actionType = form["chkActionType"] ?? "";
                var owner = Convert.ToInt64(form["txtAccountOwnerEmpNumber"] ?? "");


                var response = await GetApprovalForResourceAccountDL(ccNum, empNum, owner, actionType);
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
                    item["FormName"] = "Resource Account And Distribution List Form";
                    item["UniqueFormName"] = "RADLF";
                    item["FormParentId"] = 18;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ResourceAccountAndDL";
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

                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;

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
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"] ?? "";
                newRow["EmployeeType"] = form["chkEmployeeType"] ?? "";
                newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
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
                newRow["BusinessNeed"] = form["txtBusinessNeed"];
                newRow["RequestFor"] = form["chkRequestFor"] ?? "";
                newRow["ResourceAccountLocation"] = form["ddResourceAccountLocation"] ?? "";
                newRow["ActionType"] = form["chkActionType"] ?? "";

                newRow["RequestType"] = form["chkRequestType"] ?? "";
                newRow["TempFrom"] = form["txtTempFrom"] == "" ? null : form["txtTempFrom"];
                newRow["TempTo"] = form["txtTempTo"] == "" ? null : form["txtTempTo"];
                newRow["AccountOwner"] = form["txtAccountOwner"] ?? "";
                var request = form["chkRequestFor"];
                newRow["DLResourceAccountName"] = form["txtDLResourceAccountName"] ?? "";
                newRow["EmailAddress"] = form["txtEmailAddress"];
                newRow["DomainId"] = form["ddDomainId"] ?? "";
                newRow["AccountOwnerEmpNumber"] = form["txtAccountOwnerEmpNumber"];

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                //result.one = 1;
                //result.two = formId;
                result.Status = 200;
                result.Message = formId.ToString();

                int requestId = newRow.Id;

                List UserIdList = web.Lists.GetByTitle("UserIdResourceAccountDL");

                string pattern = ",";

                var EmployeeUserId = form["txtEmployeeUserId"];
                var ActionType = form["drpActionType"];

                var employeeUserIds = EmployeeUserId.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                employeeUserIds = employeeUserIds.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var actionTypes = ActionType.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                actionTypes = actionTypes.Where(s => !string.IsNullOrEmpty(s)).ToList();

                for (int i = 0; i < employeeUserIds.Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newItem = UserIdList.AddItem(itemCreate);
                    newItem["ResourceAccountRequestId"] = requestId;
                    newItem["EmployeeUserId"] = employeeUserIds[i];
                    newItem["ActionType"] = actionTypes[i];
                    newItem["FormID"] = formId;
                    newItem.Update();
                    _context.ExecuteQuery();
                }

                //Task Entry in Approval Master List
                var level = 1;
                var rowid = newRow.Id;
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
                    level++;
                }

                //Data Row ID Update in Forms List
                List formslist = _context.Web.Lists.GetByTitle("Forms");
                ListItem newFormItem = formslist.GetItemById(formId);
                newFormItem.RefreshLoad();
                _context.ExecuteQuery();
                newFormItem["DataRowId"] = rowid;
                newFormItem.Update();
                _context.Load(newFormItem);
                _context.ExecuteQuery();

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, rowid);
                foreach (var approver in approverIdList)
                {
                    var data = new UserData()
                    {
                        EmployeeName = approver.FName + " " + approver.LName,
                        Email = approver.EmailId,
                        IsApprover = true
                    };
                    userList.Add(data);
                }

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    Recipients = new List<UserData>() { userList.Where(x => x.IsApprover).FirstOrDefault() },
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                    FormName = formName
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
        /// IT Clearance Form-It is used for viewing software requisition form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetResourceAccountDLDetails(int rowId, int formId)
        {
            dynamic resourceAccountDL = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ResourceAccountAndDistributionList')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,RequestFor,ActionType,AccountOwner,AccountOwnerEmpNumber,DLResourceAccountName,EmailAddress," +
                    "BusinessNeed,EmployeeLocation,ResourceAccountLocation,EmployeeDesignation,DomainId,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<ResourceAccountDLRequisitionModel>(responseText, settings);
                    resourceAccountDL.one = result.List.ResourceAccountDLList;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('UserIdResourceAccountDL')/items?$select=EmployeeUserId,ActionType&$filter=(ResourceAccountRequestId eq '" + rowId + "')");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var result1 = JsonConvert.DeserializeObject<UserIdResourceAccountDLModel>(responseText2);
                resourceAccountDL.two = result1.List.UserIdResourceAccountDLList;
                resourceAccountDL.Count = resourceAccountDL.two.Count;

                //approval start
                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response3 = await client3.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,ApproverName,ApproverUserName,Level,Logic,TimeStamp,Designation,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText3 = await response3.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText3, settings);

                if (modelData.Node.Data.Count > 0)
                {
                    var client4 = new HttpClient(handler);
                    client4.BaseAddress = new Uri(conString);
                    client4.DefaultRequestHeaders.Accept.Clear();
                    client4.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    var names = new List<string>();
                    var responseText4 = "";

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

                    if (!string.IsNullOrEmpty(responseText3))
                    {
                        dynamic data3 = Json.Decode(responseText3);
                        resourceAccountDL.three = data3.d.results;
                        resourceAccountDL.four = items;
                    }

                }
                else
                {
                    resourceAccountDL.two = new List<string>();
                    resourceAccountDL.three = new List<string>();
                    resourceAccountDL.four = new List<string>();
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return resourceAccountDL;
        }

        /// <summary>
        /// ResourceAccountD-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForResourceAccountDL(long ccNum, long empNum, long owner, string actionType)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ResourceAccountDLApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@owner", owner));
                cmd.Parameters.Add(new SqlParameter("@actionType", actionType));
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
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }

    }
}
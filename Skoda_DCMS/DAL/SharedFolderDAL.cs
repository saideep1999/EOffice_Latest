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
    public class SharedFolderDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        /// <summary>
        ///Shared Folder Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
       // public async Task<dynamic> CreateSharedFolderRequest(System.Web.Mvc.FormCollection form, UserData user)
        public async Task<ResponseModel<object>> CreateSharedFolderRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            //  dynamic result = new ExpandoObject();
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "SFF";
            string formName = "Shared Folder Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }
            var requestSubmissionFor = form["drpRequestSubmissionFor"];
            var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
            long empNum = requestSubmissionFor == "Self" ? user.EmpNumber : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
            long ccNum = requestSubmissionFor == "Self" ? user.CostCenter : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
            var loc = requestSubmissionFor == "Self" ? form["ddEmpLocation"]
                   : (otherEmpType == "SAVWIPLEmployee" ? form["ddOtherEmpLocation"]
                       : (otherEmpType == "Others" ? form["ddOtherNewEmpLocation"] : ""));
            var approverIdList = new List<ApprovalMatrix>(); 
            var requestFor = form["chkRequestFor"] ?? "";

            var fileServer = requestFor == "AddRemoveMembers" ? "" : requestFor == "Change" ? form["drpChangeFileServerName"] ?? "" : form["drpFileServerName_1"] ?? "";
            var folderOwnerEmpNumber = requestFor == "AddRemoveMembers" ? Convert.ToInt64(form["txtFolderOwnerEmpNumber_1"]) : 0;


            var response = await GetApprovalForSharedFolder(empNum, ccNum, requestFor, fileServer, folderOwnerEmpNumber);
            if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
            {
                result.Status = 500;
                result.Message = response.Message;
                return result;
            }
            approverIdList = response.Model;


            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            int FormId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            bool IsResubmit = FormId == 0 ? false : true;
            try
            {
                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Shared Folder Form";
                    item["UniqueFormName"] = "SFF";
                    item["FormParentId"] = 19;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SharedFolder";
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
                    item["FormName"] = "Shared Folder Form";
                    item["UniqueFormName"] = "SFF";
                    item["FormParentId"] = 19;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SharedFolder";
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
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["EmployeeType"] = form["chkEmployeeType"];
                newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newRow["EmployeeCode"] = form["txtEmployeeCode"];
                newRow["EmployeeDesignation"] = form["TempDesignation"];// DropDown selection
                newRow["EmployeeLocation"] = form["ddEmpLocation"]; //Dropdown selection
                newRow["EmployeeCCCode"] = form["txtCostcenterCode"]; //
                newRow["EmployeeUserId"] = form["txtUserId"]; //SharePoint user Id
                newRow["EmployeeName"] = form["txtEmployeeName"];
                newRow["EmployeeDepartment"] = form["txtDepartment"];
                newRow["EmployeeContactNo"] = form["txtContactNo"];
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
                newRow["RequestType"] = form["chkRequestType"] ?? "";
                newRow["TempFrom"] = form["txtTempFrom"] == "" ? null : form["txtTempFrom"];
                newRow["TempTo"] = form["txtTempTo"] == "" ? null : form["txtTempTo"];
                newRow["RequestFor"] = form["chkRequestFor"] ?? "";
                var request = form["chkRequestFor"] ?? "";
                newRow["ChangeType"] = form["chkChangeType"] ?? "";
                newRow["ChangeFileServerName"] = form["drpChangeFileServerName"] ?? "";
                newRow["ChangeFolderPath"] = form["txtChangeFolderPath"] ?? "";
                newRow["ChangeSize"] = form["txtChangeSize"] ?? "";
                newRow["FolderOwnerName"] = form["txtCurrentFolderOwnerName"] ?? "";
                newRow["FolderOwnerEmployeeNumber"] = form["txtCurrentFolderOwnerEmployeeNumber"] ?? "";
                newRow["ProposedFolderOwnerName"] = form["txtProposedFolderOwnerName"] ?? "";
                newRow["ProposedFolderOwnerEmpNum"] = form["txtProposedFolderOwnerEmployeeNumber"] ?? "";

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                //result.one = 1;
                //result.two = formId;

                result.Status = 200;
                result.Message = formId.ToString();

                if (requestFor == "Creation")
                {
                    var creationCount = Convert.ToInt32(form["totalAddedRows"]);
                    int requestId = newRow.Id;

                    List creationList = web.Lists.GetByTitle("SharedFolderCreationDetails");

                    string pattern = ",";

                    var FileServerName = string.Empty;
                    var FolderName = string.Empty;
                    var FolderSize = string.Empty;
                    var FolderOwnerName = string.Empty;
                    var OwnerEmployeeNumber = string.Empty;

                    for (var i = 1; i < (creationCount + 1); i++)
                    {
                        FileServerName += form["drpFileServerName_" + i + ""] + ",";
                        FolderName += form["txtCreationFolderPath_" + i + ""] + ",";
                        FolderSize += form["txtSize_" + i + ""] + ",";
                        FolderOwnerName += form["txtCreationFolderOwnerName_" + i + ""] + ",";
                        OwnerEmployeeNumber += form["txtCreationOwnerEmployeeNumber_" + i + ""] + ",";
                    }

                    var fileServerNames = FileServerName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    fileServerNames = fileServerNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var folderNames = FolderName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    folderNames = folderNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var folderSizes = FolderSize.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    folderSizes = folderSizes.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var folderOwnerNames = FolderOwnerName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    folderOwnerNames = folderOwnerNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var ownerEmployeeNumbers = OwnerEmployeeNumber.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    ownerEmployeeNumbers = ownerEmployeeNumbers.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    for (int i = 0; i < fileServerNames.Count; i++)
                    {
                        ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                        ListItem newItem = creationList.AddItem(itemCreate);
                        newItem["SharedFolderID"] = requestId;
                        newItem["FileServerName"] = fileServerNames[i];

                        newItem["FolderPath"] = folderNames[i];
                        newItem["Size"] = Convert.ToDecimal(folderSizes[i]);
                        newItem["OwnerName"] = folderOwnerNames[i];
                        newItem["OwnerEmployeeNumber"] = ownerEmployeeNumbers[i];
                        newItem["FormID"] = formId;
                        newItem.Update();
                        _context.ExecuteQuery();
                    }
                }
                else if (requestFor == "AddRemoveMembers")
                {
                    var count = Convert.ToInt32(form["totalAddRemoveRows"]);
                    int requestId = newRow.Id;

                    List creationList = web.Lists.GetByTitle("SharedFolderAddRemoveUserDetails");

                    string pattern = ",";

                    var FileServerName = string.Empty;
                    var FolderName = string.Empty;
                    var FolderOwnerName = string.Empty;
                    var UserId = string.Empty;
                    var ReadAccess = string.Empty;
                    var ReadWriteAccess = string.Empty;
                    var RemoveAccess = string.Empty;
                    var EmployeeNumber = string.Empty;
                    var EmployeeEmail = string.Empty;

                    for (var i = 1; i < count + 1; i++)
                    {
                        FileServerName += form["ddFileServerName_" + i + ""] + ",";
                        FolderName += form["txtFolderName_" + i + ""] + ",";
                        FolderOwnerName += form["txtFolderOwnerName_" + i + ""] + ",";
                        UserId += form["txtUserId_" + i + ""] + ",";
                        ReadAccess += form["chkReadAccess_" + i + ""] == null ? "No" + "," : "Yes" + ",";
                        ReadWriteAccess += form["chkReadWriteAccess_" + i + ""] == null ? "No" + "," : "Yes" + ",";
                        RemoveAccess += form["chkRemoveAccess_" + i + ""] == null ? "No" + "," : "Yes" + ",";
                        EmployeeNumber += form["txtFolderOwnerEmpNumber_" + i + ""] + ",";
                        EmployeeEmail += form["txtFolderOwnerEmail_" + i + ""] + ",";
                    }

                    var fileServerNames = FileServerName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    fileServerNames = fileServerNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var folderNames = FolderName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    folderNames = folderNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var folderOwnerNames = FolderOwnerName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    folderOwnerNames = folderOwnerNames.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    var userIds = UserId.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    userIds = userIds.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    var readAccess = ReadAccess.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    readAccess = readAccess.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    var readWriteAccess = ReadWriteAccess.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    readWriteAccess = readWriteAccess.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    var removeAccess = RemoveAccess.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    removeAccess = removeAccess.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    var employeeNumbers = EmployeeNumber.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    employeeNumbers = employeeNumbers.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    var employeeEmails = EmployeeEmail.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                    employeeEmails = employeeEmails.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    for (int i = 0; i < fileServerNames.Count; i++)
                    {
                        ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                        ListItem newItem = creationList.AddItem(itemCreate);
                        newItem["SharedFolderID"] = requestId;
                        newItem["FileServerName"] = fileServerNames[i];
                        newItem["FolderPath"] = folderNames[i];
                        newItem["FolderOwnerName"] = folderOwnerNames[i];
                        newItem["UserId"] = userIds[i];
                        newItem["Read"] = readAccess[i] == "Yes" ? true : false;
                        newItem["ReadWrite"] = readWriteAccess[i] == "Yes" ? true : false;
                        newItem["Remove"] = removeAccess[i] == "Yes" ? true : false;
                        newItem["OwnerEmployeeNumber"] = employeeNumbers[i];
                        newItem["Email"] = employeeEmails[i];
                        newItem["FormID"] = formId;
                        newItem.Update();
                        _context.ExecuteQuery();
                    }
                }

                //Task Entry in Approval Master List
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

                    approvalMasteritem["RunWorkflow"] = "No";

                    approvalMasteritem["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                    approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                    approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                    approvalMasteritem.Update();
                    _context.Load(approvalMasteritem);
                    _context.ExecuteQuery();

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
                    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
                    FormName = formName
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
        /// Shared Folder Form-It is used for viewing software requisition form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetSharedFolderDetails(int rowId, int formId)
        {
            dynamic sharedFolder = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('SharedFolder')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,RequestFor,ChangeType,ChangeFileServerName,ChangeFolderPath,ChangeSize,FolderOwnerName," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "BusinessNeed,EmployeeLocation,EmployeeDesignation,RequestSubmissionFor,ProposedFolderOwnerName,ProposedFolderOwnerEmpNum,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<SharedFolderModel>(responseText, settings);
                    sharedFolder.one = result.List.SharedFolderList;
                }
                if (sharedFolder.one[0].RequestFor == "AddRemoveMembers")
                {
                    var client2 = new HttpClient(handler);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('SharedFolderAddRemoveUserDetails')/items?$select=FileServerName,FolderOwnerName,FolderPath,UserId,Read,ReadWrite,Remove,OwnerEmployeeNumber,Email&$filter=(SharedFolderID eq '" + rowId + "')");
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    //dynamic data2 = Json.Decode(responseText2);
                    //sharedFolder.two = data2.d.results;
                    var result = JsonConvert.DeserializeObject<SharedFolderAddRemoveUserModel>(responseText2, settings);
                    sharedFolder.two = result.AddRemoveList.AddRemoveUsersList;
                    sharedFolder.Count = sharedFolder.two.Count;
                    sharedFolder.Request = "AddRemoveMembers";
                }
                else if (sharedFolder.one[0].RequestFor == "Creation")
                {
                    var client2 = new HttpClient(handler);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('SharedFolderCreationDetails')/items?$select=FileServerName,OwnerName,FolderPath,Size,OwnerEmployeeNumber&$filter=(SharedFolderID eq '" + rowId + "')");
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    //dynamic data2 = Json.Decode(responseText2);
                    //sharedFolder.two = data2.d.results;
                    var result = JsonConvert.DeserializeObject<SharedFolderCreationModel>(responseText2, settings);
                    sharedFolder.two = result.List.CreationList;
                    sharedFolder.Count = sharedFolder.two.Count;
                    sharedFolder.Request = "Creation";
                }
                else
                {
                    sharedFolder.Count = 0;
                    sharedFolder.Request = "Change";
                }


                //approval start
                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response3 = await client3.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverName,ApproverUserName,NextApproverId,Level,Logic,TimeStamp,Designation,"
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

                    //items = items.OrderBy(x => x.UserLevel).ToList();

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
                        sharedFolder.three = data3.d.results;
                        sharedFolder.four = items;
                    }
                }
                else
                {
                    sharedFolder.two = new List<string>();
                    sharedFolder.three = new List<string>();
                    sharedFolder.four = new List<string>();
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return sharedFolder;
        }

        /// <summary>
        /// Shared Folder Form-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForSharedFolder(long empNum, long ccNum, string requestFor, string fileServer, long folderOwnerEmpNum)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SharedFolderApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@fileServer", fileServer));
                cmd.Parameters.Add(new SqlParameter("@requestFor", requestFor));
                if (folderOwnerEmpNum == 0)
                    cmd.Parameters.Add(new SqlParameter("@folderOwner", null));
                else
                    cmd.Parameters.Add(new SqlParameter("@folderOwner", folderOwnerEmpNum));
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

                //return appList;
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                //return new List<ApprovalMatrix>();
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
            }

        }

    }
}
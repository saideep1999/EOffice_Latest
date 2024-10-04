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
    public class DeviationNoteDAL
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

        /// <summary>
        /// DNF-It is used to get the Department Dropdown data.
        /// </summary>
        /// <returns></ret
        public async Task<dynamic> GetDepartment()
        {
            DNFResults DNFData = new DNFResults();
            dynamic result = DNFData;
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('DepartmentDetails')/items?$select=ID,Department");
                var responseText = await response.Content.ReadAsStringAsync();

             
                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<DeviationNoteModel>(responseText);
                    DNFData = locResult.dnflist;
                }
                result = DNFData.dnfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// DNF-It is used for Saving data.
        /// </summary>
        /// <returns></returns>
        //public async Task<dynamic> SaveDeviationNoteForm(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        public async Task<ResponseModel<object>> SaveDeviationNoteForm(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            ResponseModel<object> result = new ResponseModel<object>();

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            string formShortName = "DNF";
            string formName = "Deviation Note Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }
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

                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Deviation Note Form";
                    item["UniqueFormName"] = "DNF";
                    item["FormParentId"] = 25;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "DeviationNote";
                    item["BusinessNeed"] = form["txtReason"];
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
                    item["FormName"] = "Deviation Note Form";
                    item["UniqueFormName"] = "DNF";
                    item["FormParentId"] = 25;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "DeviationNote";
                    item["BusinessNeed"] = form["txtReason"];
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

                List CourierRequestList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = CourierRequestList.AddItem(itemCreateInfo);
                if (FormId == 0)
                {
                    newItem["TriggerCreateWorkflow"] = "No";
                }
                else
                {
                    newItem["TriggerCreateWorkflow"] = "Yes";
                }
                newItem["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newItem["EmployeeType"] = form["chkEmployeeType"];
                var empType = form["chkEmployeeType"];
                newItem["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newItem["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newItem["EmployeeCode"] = form["txtEmployeeCode"];
                if (empType == "External")
                {
                    newItem["EmployeeDesignation"] = "Team Member";
                }
                else
                {
                    newItem["EmployeeDesignation"] = form["ddEmpDesignation"];// DropDown selection
                }
                newItem["EmployeeLocation"] = form["ddEmpLocation"]; //Dropdown selection
                newItem["EmployeeCCCode"] = form["txtCostcenterCode"]; //
                newItem["EmployeeUserId"] = form["txtUserId"]; //SharePoint user Id
                newItem["EmployeeName"] = form["txtEmployeeName"];
                newItem["EmployeeDepartment"] = form["txtDepartment"];
                newItem["EmployeeContactNo"] = form["txtContactNo"];
                newItem["EmployeeEmailId"] = user.Email;

                //Other Employee Details
                // newItem["OnBehalfOption"] = otherEmpType;
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        newItem["OnBehalfOption"] = otherEmpType;
                        var otherEmployeeType = form["chkOtherEmployeeType"];
                        if (otherEmployeeType == "External")
                        {
                            newItem["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newItem["OtherEmployeeDesignation"] = form["ddOtherEmpDesignation"] ?? "";// DropDown selection
                        }
                        newItem["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                        newItem["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";

                        newItem["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? ""; //Dropdown selection
                        newItem["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? ""; //
                        newItem["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? ""; //SharePoint user Id
                        newItem["OtherEmployeeDepartment"] = form["txtOtherDepartment"] ?? "";
                        newItem["OtherEmployeeContactNo"] = form["txtOtherContactNo"] ?? "";
                        newItem["OtherEmployeeEmailId"] = form["txtOtherEmailId"] ?? "";
                        newItem["OtherExternalOrganizationName"] = form["ddOtherExternalOrganizationName"] ?? "";
                        newItem["OtherExternalOtherOrgName"] = form["txtOtherExternalOtherOrganizationName"] ?? "";
                    }
                    else
                    {
                        newItem["OnBehalfOption"] = otherEmpType;
                        var otherNewEmployeeType = form["chkOtherNewEmployeeType"];
                        if (otherNewEmployeeType == "External")
                        {
                            newItem["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newItem["OtherEmployeeDesignation"] = form["ddOtherNewEmpDesignation"] ?? "";// DropDown selection
                        }
                        newItem["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                        newItem["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";

                        newItem["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? ""; //Dropdown selection
                        newItem["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? ""; //
                        newItem["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? ""; //SharePoint user Id
                        newItem["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                        newItem["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                        newItem["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                        newItem["OtherExternalOrganizationName"] = form["ddOtherNewExternalOrganizationName"] ?? "";
                        newItem["OtherExternalOtherOrgName"] = form["txtOtherNewExternalOtherOrganizationName"] ?? "";
                    }
                }


                newItem["Supplier"] = form["txtSupplier"];
                newItem["Description"] = form["txtDescription"];
                newItem["Currency"] = form["drpCurrency"];
                newItem["Budget"] = form["txtBudget"];
                decimal conversionValue = 0;
                if (form["drpCurrency"] == "Rupee")
                {
                    newItem["ConversionValue"] = 1;
                    conversionValue = 1;
                }
                else
                {
                    newItem["ConversionValue"] = form["txtConvert"];
                    conversionValue = Convert.ToDecimal(form["txtConvert"]);
                }
                decimal budgetRupeeConversion = Convert.ToDecimal(newItem["Budget"]) * conversionValue;

                newItem["Department"] = form["drpDepartment"] ?? "";
                newItem["Brand"] = form["txtBrand"];
                newItem["Reason1"] = form["checkBoxReason1"];
                newItem["Reason2"] = form["checkBoxReason2"];
                newItem["Reason3"] = form["checkBoxReason3"];

                newItem["Reason4"] = form["checkBoxReason4"];
                newItem["Reason"] = form["txtReason"];
                newItem["DeviationDate"] = form["txtDeviationDate"];
                newItem["DeviationNote"] = form["checkBoxDeviationNote"];

                newItem["FormID"] = formId;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
                //result.one = 1;
                //result.two = formId;
                result.Status = 200;
                result.Message = formId.ToString();

                if (form["checkBoxDeviationNote"] == "DeviationNote")
                {
                    if (file != null)
                    {
                        int attachmentID = newItem.Id;

                        string path = file.FileName;
                        path = path.Replace(" ", "");
                        string FileName = path;

                        List docs = web.Lists.GetByTitle(listName);
                        ListItem itemAttach = docs.GetItemById(attachmentID);

                        var attInfo = new AttachmentCreationInformation();

                        attInfo.FileName = FileName;

                        byte[] fileData = null;
                        using (var binaryReader = new BinaryReader(file.InputStream))
                        {
                            fileData = binaryReader.ReadBytes(file.ContentLength);
                        }

                        attInfo.ContentStream = new MemoryStream(fileData);

                        Attachment att = itemAttach.AttachmentFiles.Add(attInfo); //Add to File

                        _context.Load(att);
                        _context.ExecuteQuery();
                    }

                    //File Upload on Edit mode
                    var attachedfile = form["attachedfile"];
                    if (attachedfile != null && attachedfile != "")
                    {
                        int startListID = Convert.ToInt32(form["FormSrId"]);

                        Site oSite = _context.Site;
                        _context.Load(oSite);
                        _context.ExecuteQuery();

                        _context.Load(web);
                        _context.ExecuteQuery();

                        CamlQuery query = new CamlQuery();
                        query.ViewXml = @"";

                        List oList = _context.Web.Lists.GetByTitle(listName);
                        _context.Load(oList);
                        _context.ExecuteQuery();

                        ListItemCollection items = oList.GetItems(query);
                        _context.Load(items);
                        _context.ExecuteQuery();
                        byte[] fileContents = null;

                        Folder folder = web.GetFolderByServerRelativeUrl(oSite.Url + "/Lists/" + listName + "/Attachments/" + startListID);

                        _context.Load(folder);
                        _context.ExecuteQuery();

                        FileCollection attachments = folder.Files;
                        _context.Load(attachments);
                        _context.ExecuteQuery();

                        foreach (Microsoft.SharePoint.Client.File oFile in folder.Files)
                        {

                            FileInfo myFileinfo = new FileInfo(oFile.Name);
                            WebClient clientFile = new WebClient();
                            clientFile.Credentials = _context.Credentials;

                            string SharepointSiteURL = ConfigurationManager.AppSettings["SharepointSiteURL"];

                            fileContents = clientFile.DownloadData(SharepointSiteURL + oFile.ServerRelativeUrl);

                        }

                        var attachedfileName = form["attachedfileName"];
                        int attachmentID = newItem.Id;

                        string path = attachedfileName;
                        path = path.Replace(" ", "");
                        string FileName = path;

                        List docs = web.Lists.GetByTitle(listName);
                        ListItem itemAttach = docs.GetItemById(attachmentID);

                        var attInfo = new AttachmentCreationInformation();

                        attInfo.FileName = FileName;

                        attInfo.ContentStream = new MemoryStream(fileContents);

                        Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                        _context.Load(att);
                        _context.ExecuteQuery();
                    }
                }

                var level = 0;
                if (budgetRupeeConversion <= 500000)
                {
                    level = 1;
                }
                else if (budgetRupeeConversion > 500000)
                {
                    level = 2;
                }

                var response = await GetApprovalDeviationNoteForm(empNum, ccNum, level);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approverIdList = response.Model;

                //Task Entry in Approval Master List
                var rowid = newItem.Id;
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

                    if (approverIdList[i].ApprovalLevel == approverIdList.Max(p => p.ApprovalLevel))
                    {
                        approvalMasteritem["NextApproverId"] = 0;
                    }
                    else
                    {
                        //var currentApproverLevel = approverIdList[i].ApprovalLevel;
                        //approvalMasteritem["NextApproverId"] = approverIdList.Any(p => p.ApprovalLevel == currentApproverLevel + 1) ? approverIdList.Where(p => p.ApprovalLevel == currentApproverLevel + 1).FirstOrDefault().ApproverUserName : "";
                        approvalMasteritem["NextApproverId"] = 0;
                    }

                    approvalMasteritem["ApproverStatus"] = "Pending";
                    approvalMasteritem["RunWorkflow"] = "No";
                    approvalMasteritem["BusinessNeed"] = form["inputTextDesc"] ?? "";

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
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                return result;
            }

            return result;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalDeviationNoteForm(long empNum, long ccNum, int level)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_DeviationNoteForm", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@level", level));
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

                // return appList;
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                //return new List<ApprovalMatrix>();
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
            }
        }

        public async Task<dynamic> ViewDeviationNoteFormData(int rowId, int formId)
        {
            dynamic DNFDataList = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('DeviationNoteForm')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment," +
                    "EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,OtherExternalOrganizationName,OtherExternalOtherOrgName,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption," +
                    "Supplier,Description,Currency,Budget,ConversionValue,Department,Brand,Location,OnBehalfLocation,Reason,Reason1,Reason2,Reason3,Reason4,DeviationDate,AttachmentFiles,DeviationNote" +
                    "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var DNFResult = JsonConvert.DeserializeObject<DeviationNoteModel>(responseText, settings);
                    DNFDataList.one = DNFResult.dnflist.dnfData;
                }

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
                        DNFDataList.two = data2.d.results;
                        DNFDataList.three = items;
                    }
                }
                else
                {
                    DNFDataList.two = new List<string>();
                    DNFDataList.three = new List<string>();
                }
                return DNFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return DNFDataList;
            }
        }

        public DateTime GetDeviationDate()
        {
            DateTime date = new DateTime();
            //var date = "";
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetDeviationDate", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        date = Convert.ToDateTime(ds.Tables[0].Rows[i]["DeviationDate"]);
                        // date = ds.Tables[0].Rows[i]["DeviationDate"].ToString();
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }

            return date;
        }

    }
}
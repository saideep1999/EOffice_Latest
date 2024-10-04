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
    public class InternalJobPostingDAL
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


        public async Task<ResponseModel<object>> CreateInternalJobPostingFormRequest(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            string formShortName = "IJPF";
            string formName = "Internal Job Posting Form";
            string listName = string.Empty;

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;

            listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }

            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                long txtEmployeeCode = Convert.ToInt32(form["txtEmployeeCode"]);
                long txtCostCenterNo = Convert.ToInt32(form["txtCostcenterCode"]);
                long txtOnBehalfEmpId = 0;
                long txtOnBehalfCostCenterNo = 0;

                if (requestSubmissionFor == "OnBehalf")
                {
                    txtOnBehalfEmpId = Convert.ToInt32(form["txtOnBehalfEmpId"]);
                    txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOnBehalfCostCenterNumber"]);
                }

                var response = await GetApprovalIJPF(user, requestSubmissionFor, txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo);

                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {

                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }

                var approverIdList = response.Model;
                int formId = 0;
                int formIdInput = Convert.ToInt32(form["FormId"]);
                int AppRowId = Convert.ToInt32(form["AppRowId"]);
                bool IsResubmit = formIdInput == 0 ? false : true;


                if (formIdInput == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Internal Job Posting Form";
                    item["UniqueFormName"] = "IJPF";
                    item["FormParentId"] = 24;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "InternalJobPosting";
                    //item["BusinessNeed"] = form["txtBusinessNeed"];
                    item["BusinessNeed"] = "Internal Job Posting";

                    if (requestSubmissionFor == "OnBehalf")
                    {
                        item["Location"] = form["ddOnBehalfLocation"];
                    }
                    else
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(formIdInput);
                    item["FormName"] = "Internal Job Posting Form";
                    item["UniqueFormName"] = "IJPF";
                    item["FormParentId"] = 24;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "InternalJobPosting";
                    //item["BusinessNeed"] = form["txtBusinessNeed"];
                    item["BusinessNeed"] = "Internal Job Posting";
                    if (requestSubmissionFor == "OnBehalf")
                    {
                        item["Location"] = form["ddOnBehalfLocation"];
                    }
                    else
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();
                    formId = item.Id;

                    ListDAL dal = new ListDAL();
                    var resubmitResult = dal.ResubmitUpdate(formId);

                    if (AppRowId != 0)
                    {
                        List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                        ListItem listItem = listApprovalMaster.GetItemById(AppRowId);
                        listItem["ApproverStatus"] = "Resubmitted";
                        listItem["IsActive"] = 0;
                        listItem["IsEnquired"] = "Yes";
                        listItem.Update();
                        _context.Load(listItem);
                        _context.ExecuteQuery();
                    }
                }

                List FormList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                ListItem newRow = FormList.AddItem(itemInfo);
                if (formIdInput == 0)
                {
                    newRow["TriggerCreateWorkflow"] = "No";
                }
                else
                {
                    newRow["TriggerCreateWorkflow"] = "Yes";
                }


                newRow["RequestSubmissionFor"] = requestSubmissionFor;

                newRow["EmployeeType"] = form["chkEmployeeType"];
                var empType = form["chkEmployeeType"];

                newRow["ExternalOrganizationName"] = form["txtExternalOrganizationName"] ?? "";

                newRow["EmployeeCode"] = form["txtEmployeeCode"];
                if (empType == "External")
                {
                    newRow["EmployeeDesignation"] = "Team Member";
                }
                else
                {
                    newRow["EmployeeDesignation"] = form["ddEmpDesignation"];// DropDown selection
                }
                newRow["EmployeeLocation"] = form["ddEmpLocation"];
                newRow["EmployeeCCCode"] = form["txtCostcenterCode"];
                newRow["EmployeeUserId"] = form["txtUserId"];
                newRow["EmployeeName"] = form["txtEmployeeName"];
                newRow["EmployeeDepartment"] = form["txtDepartment"];
                newRow["EmployeeContactNo"] = form["txtContactNo"];
                newRow["EmployeeEmailId"] = user.Email;


                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        newRow["OnBehalfOption"] = otherEmpType;
                        newRow["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                        newRow["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";
                        var otherEmployeeType = form["chkOtherEmployeeType"];
                        if (otherEmployeeType == "External")
                        {
                            newRow["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newRow["OtherEmployeeDesignation"] = form["ddOtherEmpDesignation"] ?? "";// DropDown selection
                        }
                        newRow["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? ""; //Dropdown selection
                        newRow["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? ""; //
                        newRow["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? ""; //SharePoint user Id
                        newRow["OtherEmployeeDepartment"] = form["txtOtherDepartment"] ?? "";
                        newRow["OtherEmployeeContactNo"] = form["txtOtherContactNo"] ?? "";
                        newRow["OtherEmployeeEmailId"] = form["txtOtherEmailId"] ?? "";
                        newRow["OtherEmployeeType"] = form["chkOtherEmployeeType"] ?? "";
                        newRow["OtherExternalOtherOrgName"] = form["txtOtherExternalOtherOrganizationName"] ?? "";
                    }
                    else
                    {
                        newRow["OnBehalfOption"] = otherEmpType;
                        newRow["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                        newRow["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";
                        var otherNewEmployeeType = form["chkOtherNewEmployeeType"];
                        if (otherNewEmployeeType == "External")
                        {
                            newRow["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newRow["OtherEmployeeDesignation"] = form["ddOtherNewEmpDesignation"] ?? "";// DropDown selection
                        }
                        newRow["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? ""; //Dropdown selection
                        newRow["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? ""; //
                        newRow["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? ""; //SharePoint user Id
                        newRow["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                        newRow["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                        newRow["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                        newRow["OtherEmployeeType"] = form["chkOtherNewEmployeeType"] ?? "";
                        newRow["OtherExternalOtherOrgName"] = form["txtOtherNewExternalOtherOrganizationName"] ?? "";
                    }
                }



                //Transaction Section
                newRow["IJPReference"] = form["IJPReference"];
                newRow["MPRReference"] = form["MPRReference"];
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["DateOfJoining"] = form["txtDateOfJoining"];
                newRow["Qualification"] = form["txtQualification"];
                //newRow["BusinessNeed"] = form["txtBusinessNeed"];
                newRow["BusinessNeed"] = "Internal Job Posting";

                //newRow["Level"] = form["txtLevel"];
                newRow["CurrentRole"] = form["txtCurrentRole"];
                newRow["CurrentDepartmentDuration"] = form["txtCurrentDepartmentDuration"];
                newRow["CurrentRoleDuration"] = form["txtCurrentRoleDuration"];
                newRow["CurrentReportingManagerName"] = form["txtCurrentReportingManagerName"];
                newRow["PositionAndDepartmentAppliedFor"] = form["txtPositionAndDepartmentAppliedFor"];
                newRow["ReasonForChangingJobProfile"] = form["txtReasonForChangingJobProfile"];
                //newRow["Achievements"] = form["txtAchievements"];
                //newRow["AboutRoleAppliedFor"] = form["txtAboutRoleAppliedFor"];

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                RowId = newRow.Id;
                result.Status = 200;
                result.Message = formId.ToString();

                var count = Convert.ToInt32(form["totalrows"]);

                int EmploymentDetailsId = newRow.Id;

                List IJPFEmploymentDetailsList = web.Lists.GetByTitle("IJPFEmploymentDetails");

                string pattern = "||";
                var Organisation = string.Empty;
                var Designation = string.Empty;
                var FromDate = string.Empty;
                var ToDate = string.Empty;
                var MainResponsibilities = string.Empty;


                for (var i = 1; i < count + 1; i++)
                {
                    Organisation += form["txtOrganisation_" + i + ""] + "||";
                    Designation += form["txtDesignation_" + i + ""] + "||";
                    FromDate += form["txtFromDate_" + i + ""] + "||";
                    ToDate += form["txtToDate_" + i + ""] + "||";
                    MainResponsibilities += form["txtMainResponsibilites_" + i + ""] + "||";
                }


                var organisations = Organisation.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                organisations = organisations.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var designations = Designation.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                designations = designations.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var fromDates = FromDate.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                fromDates = fromDates.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var toDates = ToDate.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                toDates = toDates.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var mainResponsibilities = MainResponsibilities.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                mainResponsibilities = mainResponsibilities.Where(s => !string.IsNullOrEmpty(s)).ToList();

                for (int i = 0; i < organisations.Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newItemEmp = IJPFEmploymentDetailsList.AddItem(itemCreate);
                    newItemEmp["IJPFID"] = EmploymentDetailsId;
                    newItemEmp["Organisation"] = organisations[i];
                    newItemEmp["Designation"] = designations[i];
                    newItemEmp["FromDate"] = fromDates[i];
                    newItemEmp["ToDate"] = toDates[i];
                    newItemEmp["MainResponsibilities"] = mainResponsibilities[i];
                    newItemEmp["FormID"] = formId;
                    newItemEmp.Update();
                    _context.ExecuteQuery();
                }

                //File Upload on Create mode
                if (file != null)
                {
                    int attachmentID = newRow.Id;

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
                    int attachmentID = newRow.Id;

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


                //Task Entry in Approval Master List
                var rowid = RowId;
                int level = 1;
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

                    //approvalMasteritem["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                    approvalMasteritem["BusinessNeed"] = "Internal Job Posting";

                    approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                    approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                    approvalMasteritem.Update();
                    _context.Load(approvalMasteritem);
                    _context.ExecuteQuery();
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

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalIJPF(UserData user, string requestSubmissionFor, long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_InternalJobPostingApproval", con);

                if (requestSubmissionFor == "OnBehalf")
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtOnBehalfEmpId));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtOnBehalfCostCenterNo));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostCenterNo));
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
                        app.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
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
                //appList = common.AddMDAssistantToList(appList);
                //appList = common.ChangeDelegateApprover(appList);

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
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
            }

        }

        public async Task<dynamic> ViewIJPFData(int rowId, int formId)
        {
            dynamic IJPFDataList = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('InternalJobPosting')/items?$select=*,FormID/ID"
             + "&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles");

                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var InternalJobPostingResults = JsonConvert.DeserializeObject<InternalJobPostingModel>(responseText, settings);
                    IJPFDataList.one = InternalJobPostingResults.List.IJPList;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,Designation,Level,Logic,ApproverName,ApproverUserName,TimeStamp,IsActive,Comment,NextApproverId,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                if (modelData.Node.Data.Count > 0)
                {
                    var clientApp = new HttpClient(handler);
                    clientApp.BaseAddress = new Uri(conString);
                    clientApp.DefaultRequestHeaders.Accept.Clear();
                    clientApp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var names = new List<string>();

                    var items = modelData.Node.Data;

                    if (adCode.ToLower() == "yes")
                    {
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
                    }

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
                        dynamic data3 = Json.Decode(responseText2);
                        IJPFDataList.two = data3.d.results;
                        IJPFDataList.three = items;
                    }

                }
                else
                {
                    IJPFDataList.two = new List<string>();
                    IJPFDataList.three = new List<string>();
                }


                //Employment Details
                var clientEmp = new HttpClient(handler);
                clientEmp.BaseAddress = new Uri(conString);
                clientEmp.DefaultRequestHeaders.Accept.Clear();
                clientEmp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseEmployment = await clientEmp.GetAsync("_api/web/lists/GetByTitle('IJPFEmploymentDetails')/items?$select=ID,Organisation,Designation,FromDate,ToDate,MainResponsibilities"
                + "&$filter=(IJPFID eq '" + rowId + "' and FormID eq '" + formId + "')");
                var responseTextEmployment = await responseEmployment.Content.ReadAsStringAsync();
                var settingsEmployment = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextEmployment))
                {
                    var InternalJobPostingResults = JsonConvert.DeserializeObject<IJPFEmploymentDetailsModel>(responseTextEmployment, settingsEmployment);
                    IJPFDataList.four = InternalJobPostingResults.List.IJPEDList;
                }

                return IJPFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return IJPFDataList;
            }
        }

    }
}
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
    public class IDCardDAL
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
        /// Id Card-It is used to Save ID Card.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> SaveIDCard(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            string formShortNameDARF = "DARF";
            string formNameDARF = "Door Access Request Form";
            //dynamic result = new ExpandoObject();
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            string listName = string.Empty;
            int RowId = 0;
            Web web = _context.Web;
            var ScreenId = form["ScreenId"];
            string formShortName = "IDCF";
            string formName = "ID Card";
            var ExternalEmployeeName = "";
            //if (ScreenId == "New")
            //{
            listName = GlobalClass.ListNames.ContainsKey("IDCF") ? GlobalClass.ListNames["IDCF"] : "";
            //}
            //else if (ScreenId == "Reissue")
            //{
            //    listName = GlobalClass.ListNames.ContainsKey("RIDCF") ? GlobalClass.ListNames["RIDCF"] : "";
            //}
            //else if (ScreenId == "Door Access")
            //{
            //    listName = GlobalClass.ListNames.ContainsKey(formNameDARF) ? GlobalClass.ListNames[formNameDARF] : "";
            //}
            //else
            //{
            //    //result.one = 0;
            //    //result.two = 0;
            //    result.Status = 500;
            //    result.Message = "No Screen data found";
            //    return result;
            //}

            if (listName == "")
            {
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }

            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";

                var locationId = form["drpLocationValue"];
                var itSecurityEmail = form["hiddenITSecurityApprover"];
                var securityEmail = form["hiddenSecurityApprover"];
                var onBehalfEmail = form["hiddentxtEmail"];
                var selfOnBehalf = "";
                if (ScreenId == "Door Access")
                {
                    selfOnBehalf = form["drpSelfOnBehalf"];
                }
                else
                {
                    selfOnBehalf = form["drpRequestSubType"];
                }
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
                bool isInternalEmp = (isSelf
                    ? form["chkEmployeeType"]
                    : (isSAVWIPL ? Convert.ToString(form["chkOtherEmployeeType"]) : Convert.ToString(form["chkOtherNewEmployeeType"]))
                ) == "Internal";
                string ExtEmpOrgName = isSelf
                        ? form["ddExternalOrganizationName"] ?? ""
                        : (isSAVWIPL ? form["txtOtherExternalOrganizationName"] ?? "" : form["txtOtherNewExternalOrganizationName"] ?? "");
                bool isPKI = form["ddTypeOfCard"] == "PKI";
                //var approverIdList = await GetApprovalIDCF(locationId, ScreenId, itSecurityEmail, securityEmail, onBehalfEmail, selfOnBehalf);
                //We don't have data for location so that's why added 1 as hard coded value
                var response = await GetApprovalOfIDCF(empNum, ccNum, isPKI, isInternalEmp, ExtEmpOrgName, empLocationId);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    //result.one = 0;
                    //result.two = 0;
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approverIdList = response.Model;
                int formId = 0;
                int formIdInput = Convert.ToInt32(form["FormId"]);
                //int FormEditId = Convert.ToInt32(form["FormSrId"]);
                int AppRowId = Convert.ToInt32(form["AppRowId"]);
                bool IsResubmit = formIdInput == 0 ? false : true;


                if (formIdInput == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    if (ScreenId == "New")
                    {
                        item["FormName"] = "ID Card Form";
                        item["UniqueFormName"] = "IDCF";
                        item["FormParentId"] = 5;
                    }
                    //else if (ScreenId == "Reissue")
                    //{
                    //    item["FormName"] = "Reissue ID Card Form";
                    //    item["UniqueFormName"] = "RIDCF";
                    //    item["FormParentId"] = 8;
                    //}

                    //else if (ScreenId == "Door Access")
                    //{
                    //    item["FormName"] = "Door Access Request Form";
                    //    item["UniqueFormName"] = "DARF";
                    //    item["FormParentId"] = 12;     
                    //    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                    //}

                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "IDCard";
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
                    //result.two = formId;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(formIdInput);
                    if (ScreenId == "New")
                    {
                        item["FormName"] = "ID Card Form";
                        item["UniqueFormName"] = "IDCF";
                        item["FormParentId"] = 5;
                    }
                    //else if (ScreenId == "Reissue")
                    //{
                    //    item["FormName"] = "Reissue ID Card Form";
                    //    item["UniqueFormName"] = "RIDCF";
                    //    item["FormParentId"] = 8;
                    //}
                    //else if (ScreenId == "Door Access")
                    //{
                    //    item["FormName"] = "Door Access Request Form";
                    //    item["UniqueFormName"] = "DARF";
                    //    item["FormParentId"] = 12;
                    //}

                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["ControllerName"] = "IDCard";
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
                    //result.two = formId;

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


                if (ScreenId == "New")
                {
                    List IDcardDetailsList = web.Lists.GetByTitle(listName);
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newRow = IDcardDetailsList.AddItem(itemCreateInfo);

                    //Old Code
                    //newItem["Priority"] = form["drpPriority"];
                    //newItem["EmployeeType"] = form["drpEmployeeType"];
                    //newItem["Surname"] = form["txtSurname"];
                    //newItem["EmployeeName"] = form["txtEmployeeName"];
                    //newItem["DateofJoining"] = form["txtDateofJoining"];
                    //newItem["EmployeeNumber"] = form["txtEmployeeNumber"];
                    //newItem["CostCentre"] = form["drpCostCentre"];
                    //newItem["Department"] = form["drpDepartment"];
                    //if (form["drpEmployeeType"] == "External")
                    //{
                    //    newItem["Company"] = form["txtCompany"];
                    //}
                    ////newItem["DateofIssue"] = form["txtDateofIssue"];
                    ////newItem["ActiveFrom"] = form["txtActiveFrom"];
                    //if (form["drpEmployeeType"] == "Intern")
                    //{
                    //    newItem["EndDate"] = form["txtEndDate"];
                    //    newItem["DropdownEndDate"] = form["drpEndDate"];
                    //}
                    //newItem["Department"] = form["drpDepartment"];
                    ////newItem["IDCardNumber"] = form["txtIDCardNumber"];
                    //newItem["AurangabadProfile"] = form["AurangabadCheckbox"];
                    //newItem["MumbaiProfile"] = form["MumbaiCheckbox"];
                    //newItem["PuneProfile"] = form["PuneCheckbox"];
                    //newItem["LocationName"] = form["drpLocationValue"];
                    //newItem["LocationId"] = form["drpLocationName"];
                    //newItem["FormParentId"] = 5;
                    //for (int i = 0; i < approverIdList.Count; i++)
                    //{
                    //    newItem["Approver" + (i + 1)] = approverIdList[i].UserId;
                    //}

                    //newItem["FormID"] = formId;

                    //if (FormId == 0)
                    //{
                    //    newItem["TriggerCreateWorkflow"] = "Yes";
                    //}
                    //else
                    //{
                    //    newItem["TriggerCreateWorkflow"] = "No";
                    //}
                    //var IsParallel = form["areaArr"];
                    //if (IsParallel != "[]")
                    //{
                    //    newItem["IsParallel"] = 1;
                    //}
                    //else
                    //{
                    //    newItem["IsParallel"] = 0;
                    //}
                    //Old Code

                    //New Code
                    if (formIdInput == 0)
                    {
                        newRow["TriggerCreateWorkflow"] = "Yes";
                    }
                    else
                    {
                        newRow["TriggerCreateWorkflow"] = "No";
                    }
                    newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                    newRow["EmployeeType"] = form["chkEmployeeType"];
                    // newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                    newRow["ExternalOrganizationName"] = form["txtExternalOrganizationName"] ?? "";
                    newRow["EmployeeCode"] = form["txtEmployeeCode"];
                    newRow["EmployeeCCCode"] = form["txtCostcenterCode"]; //
                    newRow["EmployeeUserId"] = form["txtUserId"]; //SharePoint user Id
                    newRow["EmployeeName"] = form["txtEmployeeName"];
                    newRow["EmployeeDepartment"] = form["txtDepartment"];
                    newRow["EmployeeContactNo"] = form["txtContactNo"];
                    newRow["EmployeeDesignation"] = form["chkEmployeeType"] == "External" ? "Team Member" : form["ddEmpDesignation"];// DropDown selection
                    newRow["EmployeeLocation"] = form["ddEmpLocation"];//Dropdown selection
                    newRow["EmployeeEmailId"] = user.Email;
                    //Other Employee Details
                    newRow["OnBehalfOption"] = otherEmpType;
                    if (requestSubmissionFor == "OnBehalf")
                    {
                       
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            ExternalEmployeeName= form["txtOtherEmployeeName"];
                            newRow["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                            newRow["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";
                            newRow["OtherEmployeeDesignation"] = form["chkOtherEmployeeType"] == "External" ? "Team Member" : form["ddOtherEmpDesignation"] ?? "";// DropDown selection
                            newRow["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? ""; //Dropdown selection
                            newRow["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? ""; //
                            newRow["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? ""; //SharePoint user Id
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
                            ExternalEmployeeName = form["txtOtherEmployeeName"];
                            newRow["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                            newRow["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";
                            newRow["OtherEmployeeDesignation"] = form["chkOtherNewEmployeeType"] == "External" ? "Team Member" : form["ddOtherNewEmpDesignation"] ?? "";// DropDown selection
                            newRow["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? ""; //Dropdown selection
                            newRow["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? ""; //
                            newRow["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? ""; //SharePoint user Id
                            newRow["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                            newRow["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                            newRow["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                            newRow["OtherEmployeeType"] = form["chkOtherNewEmployeeType"] ?? "";
                            //newRow["OtherExternalOrganizationName"] = form["ddOtherNewExternalOrganizationName"] ?? "";
                            newRow["OtherExternalOrganizationName"] = form["txtOtherNewExternalOrganizationName"] ?? "";
                        }
                    }
                    newRow["DateOfJoining"] = form["txtDateofJoining"];
                    newRow["TypeOfCard"] = form["ddTypeOfCard"];
                    newRow["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                    newRow["ValidityStartDate"] = form["txtValidityStartDate"] == "" ? null : form["txtValidityStartDate"];
                    newRow["ValidityEndDate"] = form["txtValidityEndDate"] == "" ? null : form["txtValidityEndDate"];
                    //if (!string.IsNullOrEmpty(form["txtValidityStartDate"]))
                    //    newRow["ValidityStartDate"] = form["txtValidityStartDate"] == "0001-01-01" ? "" : form["txtValidityStartDate"] ?? "";
                    //if (!string.IsNullOrEmpty(form["txtValidityEndDate"]))
                    //    newRow["ValidityEndDate"] = form["txtValidityEndDate"] == "0001-01-01" ? "" : form["txtValidityEndDate"] ?? "";
                    newRow["FormID"] = formId;
                    //New Code

                    newRow.Update();
                    _context.Load(newRow);
                    _context.ExecuteQuery();
                    RowId = newRow.Id;
                    //result.one = 1;
                    //result.two = formId;
                    result.Status = 200;
                    result.Message = formId.ToString();
                }
                if (file != null)
                {
                    int attachmentID = RowId;

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
                    int attachmentID = RowId;

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
                //else if (ScreenId == "Reissue")
                //{
                //    List RIDcardDetailsList = web.Lists.GetByTitle(listName);
                //    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                //    ListItem newItem = RIDcardDetailsList.AddItem(itemCreateInfo);
                //    newItem["ReasonforReissue"] = form["drpReasonforReissue"];
                //    newItem["EmployeeType"] = form["drpEmployeeType"];
                //    newItem["Surname"] = form["txtSurname"];
                //    newItem["Name"] = form["txtFirstName"];
                //    newItem["DateofJoining"] = form["txtDateofJoining"];
                //    newItem["EmployeeNumber"] = form["txtEmployeeNumber"];
                //    newItem["CostCentre"] = form["drpCostCentre"];
                //    newItem["Department"] = form["drpDepartment"];
                //    newItem["SelfMobile"] = form["txtMobile"];
                //    newItem["SelfTelephone"] = form["txtTelephone"];
                //    newItem["SelfEmailID"] = form["txtEmail"];
                //    newItem["SelfOnBehalf"] = form["drpRequestSubType"];
                //    if (selfOnBehalf == "OnBehalf")
                //    {
                //        newItem["OnBehalfFirstName"] = form["txtOnBehalfFirstName"];
                //        newItem["OnBehalfSurname"] = form["txtOnBehalfSurname"];
                //        newItem["OnBehalfEmployeeIDNo"] = form["txtOnBehalfEmpId"];
                //        newItem["OnBehalfDepartment"] = form["drpOnBehalfDepartment"];
                //        newItem["OnBehalfMobile"] = form["txtOnBehalfMobile"];
                //        newItem["OnBehalfTelephone"] = form["txtOnBehalfTelephone"];
                //        newItem["OnBehalfEmailID"] = form["txtOnBehalfEmail"];
                //        newItem["OnBehalfCostCenter"] = form["drpOnBehalfCostCenter"];
                //    }


                //    newItem["DateofIssue"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //    newItem["ActiveFrom"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //    newItem["EndDate"] = form["txtEndDate"];
                //    newItem["Department"] = form["drpDepartment"];
                //    newItem["IDCardNumber"] = form["txtIDCardNumber"];
                //    newItem["AurangabadProfile"] = form["AurangabadCheckbox"];
                //    newItem["MumbaiProfile"] = form["MumbaiCheckbox"];
                //    newItem["PuneProfile"] = form["PuneCheckbox"];
                //    newItem["LocationName"] = form["drpLocationValue"];
                //    newItem["LocationId"] = form["drpLocationName"];
                //    newItem["FormParentId"] = 8;
                //    for (int i = 0; i < approverIdList.Count; i++)
                //    {
                //        newItem["Approver" + (i + 1)] = approverIdList[i].UserId;
                //    }

                //    newItem["FormID"] = formId;

                //    if (FormId == 0)
                //    {
                //        newItem["TriggerCreateWorkflow"] = "Yes";
                //    }
                //    else
                //    {
                //        newItem["TriggerCreateWorkflow"] = "No";
                //    }
                //    var IsParallel = form["areaArr"];
                //    if (IsParallel != "[]")
                //    {
                //        newItem["IsParallel"] = 1;
                //    }
                //    else
                //    {
                //        newItem["IsParallel"] = 0;
                //    }

                //    newItem.Update();
                //    _context.Load(newItem);
                //    _context.ExecuteQuery();
                //    RowId = newItem.Id;

                //}

                //else if (ScreenId == "Door Access")
                //{
                //    List DARFDetailsList = web.Lists.GetByTitle(listName);
                //    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                //    ListItem newItem = DARFDetailsList.AddItem(itemCreateInfo);
                //    newItem["EmployeeType"] = form["drpEmployeeType"];
                //    newItem["SelfOnBehalf"] = form["drpSelfOnBehalf"];
                //    newItem["Surname"] = form["txtSurname"];
                //    newItem["EmployeeName"] = form["txtEmployeeName"];
                //    newItem["EmployeeNumber"] = form["txtEmployeeNumber"];
                //    newItem["CostCentre"] = form["drpCostCentre"];
                //    newItem["Department"] = form["drpDepartment"];
                //    newItem["SelfMobile"] = form["txtMobile"];
                //    newItem["SelfTelephone"] = form["txtTelephone"];
                //    newItem["SelfEmailID"] = form["txtEmail"];
                //    if (form["drpSelfOnBehalf"] == "OnBehalf")
                //    {
                //        newItem["OnBehalfFirstName"] = form["txtOnBehalfFirstName"];
                //        newItem["OnBehalfSurname"] = form["txtOnBehalfSurname"];
                //        newItem["OnBehalfEmployeeIDNo"] = form["txtOnBehalfEmpId"];
                //        newItem["OnBehalfDepartment"] = form["drpOnBehalfDepartment"];
                //        newItem["OnBehalfMobile"] = form["txtOnBehalfMobile"];
                //        newItem["OnBehalfTelephone"] = form["txtOnBehalfTelephone"];
                //        newItem["OnBehalfEmailID"] = form["txtOnBehalfEmail"];
                //        newItem["OnBehalfCostCenter"] = form["drpOnBehalfCostCenter"];
                //    }
                //    newItem["AurangabadProfile"] = form["AurangabadCheckbox"];
                //    newItem["MumbaiProfile"] = form["MumbaiCheckbox"];
                //    newItem["PuneProfile"] = form["PuneCheckbox"];
                //    newItem["LocationName"] = form["drpLocationValue"];
                //    newItem["LocationId"] = form["drpLocationName"];
                //    newItem["FormParentId"] = 12;
                //    for (int i = 0; i < approverIdList.Count; i++)
                //    {
                //        newItem["Approver" + (i + 1)] = approverIdList[i].UserId;
                //    }

                //    newItem["FormID"] = formId;

                //    if (FormId == 0)
                //    {
                //        newItem["TriggerCreateWorkflow"] = "Yes";
                //    }
                //    else
                //    {
                //        newItem["TriggerCreateWorkflow"] = "No";
                //    }

                //    newItem.Update();
                //    _context.Load(newItem);
                //    _context.ExecuteQuery();
                //    RowId = newItem.Id;

                //}


                //var area = form["areaArr"];
                //if (ScreenId == "New")
                //{
                //    List<IDCFDataAreaSubArea> doors = JsonConvert.DeserializeObject<List<IDCFDataAreaSubArea>>(area);
                //    List DoorsList = web.Lists.GetByTitle("NewIdCardAreaSubAreaMapping");
                //    foreach (var door in doors.ToList())
                //    {
                //        if (door.SubAreas.Count > 0)
                //        {
                //            ListItemCreationInformation DoorsListitemCreateInfo = new ListItemCreationInformation();
                //            ListItem DoorsListnewItem = DoorsList.AddItem(DoorsListitemCreateInfo);
                //            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                //            DoorsListnewItem["Area"] = door.Area;
                //            foreach (string str in door.SubAreas)
                //            {
                //                if (sb.Length > 0)
                //                {
                //                    sb.Append(", ");
                //                }
                //                sb.Append(str);
                //            }
                //            DoorsListnewItem["SubAreas"] = sb;
                //            DoorsListnewItem["LocationId"] = form["drpLocationName"];
                //            DoorsListnewItem["FormId"] = formId;
                //            DoorsListnewItem["RowId"] = RowId;
                //            DoorsListnewItem["FormParentId"] = 5;
                //            DoorsListnewItem.Update();
                //            _context.ExecuteQuery();
                //        }
                //    }
                //}
                //else if (ScreenId == "Reissue")
                //{
                //    List<RIDCFDataAreaSubArea> doors = JsonConvert.DeserializeObject<List<RIDCFDataAreaSubArea>>(area);
                //    List DoorsList = web.Lists.GetByTitle("ReissueIdCardAreaSubAreaMapping");
                //    foreach (var door in doors.ToList())
                //    {
                //        if (door.SubAreas.Count > 0)
                //        {
                //            ListItemCreationInformation DoorsListitemCreateInfo = new ListItemCreationInformation();
                //            ListItem DoorsListnewItem = DoorsList.AddItem(DoorsListitemCreateInfo);
                //            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                //            DoorsListnewItem["Area"] = door.Area;
                //            foreach (string str in door.SubAreas)
                //            {
                //                if (sb.Length > 0)
                //                {
                //                    sb.Append(", ");
                //                }
                //                sb.Append(str);
                //            }
                //            DoorsListnewItem["SubAreas"] = sb;
                //            DoorsListnewItem["LocationId"] = form["drpLocationName"];
                //            DoorsListnewItem["FormId"] = formId;
                //            DoorsListnewItem["RowId"] = RowId;
                //            DoorsListnewItem["FormParentId"] = 8;
                //            DoorsListnewItem.Update();
                //            _context.ExecuteQuery();
                //        }
                //    }
                //}
                //else if (ScreenId == "Door Access")
                //{
                //    List<DARFDataAreaSubArea> doors = JsonConvert.DeserializeObject<List<DARFDataAreaSubArea>>(area);
                //    List DoorsList = web.Lists.GetByTitle("DoorAccessRequestAreaSubAreaMapping");
                //    foreach (var door in doors.ToList())
                //    {
                //        if (door.SubAreas.Count > 0)
                //        {
                //            ListItemCreationInformation DoorsListitemCreateInfo = new ListItemCreationInformation();
                //            ListItem DoorsListnewItem = DoorsList.AddItem(DoorsListitemCreateInfo);
                //            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                //            DoorsListnewItem["Area"] = door.Area;
                //            foreach (string str in door.SubAreas)
                //            {
                //                if (sb.Length > 0)
                //                {
                //                    sb.Append(", ");
                //                }
                //                sb.Append(str);
                //            }
                //            DoorsListnewItem["SubAreas"] = sb;
                //            DoorsListnewItem["LocationId"] = form["drpLocationName"];
                //            DoorsListnewItem["FormId"] = formId;
                //            DoorsListnewItem["RowId"] = RowId;
                //            DoorsListnewItem["FormParentId"] = 12;
                //            DoorsListnewItem["ParallelApprovalTrigger"] = 2;
                //            DoorsListnewItem.Update();
                //            _context.ExecuteQuery();
                //        }
                //    }
                //}
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
                    approvalMasteritem["ApplicantName"] = ExternalEmployeeName;

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

                    //approvalMasteritem["ApproverId"] = approverIdList[i].UserId;

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

        /// <summary>
        /// IDCF-It is used to get the approval list for ID Card form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetIDCFApprovalList()
        {
            IDCFResults idcfData = new IDCFResults();
            dynamic result = idcfData;
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ApproverInformationMaster')/items?$select=Department,SubDepartment,Location,ApproverEmployeeCode,ApproverEmailId,ID&$filter=(IsActive eq '1')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var IDCFResult = JsonConvert.DeserializeObject<IDCFModel>(responseText);
                    idcfData = IDCFResult.idcfflist;
                }
                result = idcfData.idcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }
        public async Task<int> IDCardValidityUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            string listName = string.Empty;
            //int RowId = 0;
            Web web = _context.Web;
            var ScreenId = form["ScreenId"];
            if (ScreenId == "New")
            {
                listName = GlobalClass.ListNames.ContainsKey("IDCF") ? GlobalClass.ListNames["IDCF"] : "";
            }
            else if (ScreenId == "Reissue")
            {
                listName = GlobalClass.ListNames.ContainsKey("RIDCF") ? GlobalClass.ListNames["RIDCF"] : "";
            }
            else
            {
                return 0;
            }
            if (listName == "")
            {
                return 0;
            }
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                if (ScreenId == "New")
                {
                    List list = _context.Web.Lists.GetByTitle(listName);
                    ListItem newItem = list.GetItemById(formId);

                    newItem["DateofIssue"] = form["txtDateofIssue"];
                    newItem["ActiveFrom"] = form["txtActiveFrom"];
                    newItem["IDCardNumber"] = form["txtIDCardNumber"];
                    newItem["Barcode"] = form["txtBarcode"];
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

        public async Task<dynamic> ViewIDCFData(int rowId, int formId)
        {
            dynamic IDCFDataList = new ExpandoObject();//constructor called, always expandoobject for dynamic dataType
            string locationid = string.Empty;
            List<IDCFData> AreaList = new List<IDCFData>();
            List<IDCFData> AreaTexList = new List<IDCFData>();
            List<IDCFData> MainList = new List<IDCFData>();
            List<IDCFData> SubAreaTextList = new List<IDCFData>();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = await client.GetAsync("_api/web/lists/GetByTitle('IDCardForm')/items?$select=*" +
                    "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=AttachmentFiles");//

                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                if (!string.IsNullOrEmpty(responseText))
                {
                    var IDCFResult = JsonConvert.DeserializeObject<IDCFModel>(responseText, settings);
                    MainList = IDCFResult.idcfflist.idcfData;
                }

                //Area & Sub Area ID
                //var areasubAreaidresponse = await client.GetAsync("_api/web/lists/GetByTitle('NewIdCardAreaSubAreaMapping')/items?$select=ID,LocationId,Area,SubAreas,Modified" +
                //  "&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')");

                //var areasubareaidresponseText = await areasubAreaidresponse.Content.ReadAsStringAsync();
                //if (!string.IsNullOrEmpty(areasubareaidresponseText))
                //{
                //    var IDCFResult1 = JsonConvert.DeserializeObject<IDCFModel>(areasubareaidresponseText);
                //    AreaList = IDCFResult1.idcfflist.idcfData;
                //    locationid = IDCFResult1.idcfflist.idcfData.Any() ? IDCFResult1.idcfflist.idcfData.FirstOrDefault().LocationId : "0";
                //}

                ////Area text
                //var areatextresponse = await client.GetAsync("_api/web/lists/GetByTitle('AreaMaster')/items?$select=ID,AreaName,Modified&$filter=(LocationId eq '" + locationid + "' and IsActive eq '1')");

                //var areatextresponseText = await areatextresponse.Content.ReadAsStringAsync();
                //if (!string.IsNullOrEmpty(areatextresponseText))
                //{
                //    var IDCFResult2 = JsonConvert.DeserializeObject<IDCFModel>(areatextresponseText);
                //    AreaTexList = IDCFResult2.idcfflist.idcfData;
                //}

                ////Sub Area Text
                //var subareatextresponse = await client.GetAsync("_api/web/lists/GetByTitle('SubAreaMaster')/items?$select=ID,AreaId,SubAreaName,Modified&$filter=(LocationId eq '" + locationid + "'  and IsActive eq '1')");

                //var subarearesponseText = await subareatextresponse.Content.ReadAsStringAsync();
                //if (!string.IsNullOrEmpty(subarearesponseText))
                //{
                //    var IDCFResult3 = JsonConvert.DeserializeObject<IDCFModel>(subarearesponseText);
                //    SubAreaTextList = IDCFResult3.idcfflist.idcfData;

                //}

                var mainModel = MainList.FirstOrDefault();
                mainModel.AreaDetails = new List<IDFCArea>();
                foreach (var currentArea in AreaList)
                {
                    var areaModel = new IDFCArea();
                    areaModel.AreaId = Convert.ToInt32(currentArea.Area);
                    areaModel.Area = AreaTexList.Where(x => x.Id == areaModel.AreaId).FirstOrDefault().AreaName;
                    if (currentArea.SubAreas != null)
                    {
                        var subAreaIds = currentArea.SubAreas.Split(',').Select(Int32.Parse).ToList();
                        areaModel.SubAreas = subAreaIds.Select(x => new IDFCSubArea() { SubAreaId = x, SubArea = SubAreaTextList.Where(y => y.Id == x && y.AreaId == Convert.ToString(areaModel.AreaId)).FirstOrDefault()?.SubAreaName }).ToList();
                    }
                    mainModel.AreaDetails.Add(areaModel);
                }

                IDCFDataList.one = MainList;

                //Approval Master For Sequential Signature

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseApprovalMaster = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,ApproverName,ApproverUserName,Level,Logic,TimeStamp,Designation,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApprovalMaster, settings);

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
                items = items.OrderBy(x => x.ApproverUserName).ToList();
                items = items.OrderBy(x => x.UserLevel).ToList();

                if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                {
                    dynamic data2 = Json.Decode(responseTextApprovalMaster);
                    IDCFDataList.two = data2.d.results;
                    IDCFDataList.three = items;
                }

                //Approval Master For Parallel Signature
                var Parallelsettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var clientParallel = new HttpClient(handler);
                clientParallel.BaseAddress = new Uri(conString);
                clientParallel.DefaultRequestHeaders.Accept.Clear();
                clientParallel.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var responseParallelApprovalMaster = await client.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,ApproverStatus,RowId,Modified,Author/Title,FormId/FormName," +
                //              "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(FormId eq '" + formId + "' and ApproverId eq '" + user.UserId + "')&$expand=FormId,Author");

                var responseParallelApprovalMaster = await client.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,ApprovalType,Comment,ApproverStatus,RowId,Modified,Author/Title,FormId/FormName," +
                         "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseTextParallelApprovalMaster = await responseParallelApprovalMaster.Content.ReadAsStringAsync();
                var modelDataParallel = JsonConvert.DeserializeObject<ParallelApprovalMasterModel>(responseTextParallelApprovalMaster, Parallelsettings);
                dynamic modelDataParallelData = System.Web.Helpers.Json.Decode(responseTextParallelApprovalMaster);
                var Parallelitems = new List<ParallelApprovalDataModel>();
                if (modelDataParallelData.d.results.Length != 0)
                {
                    var Parallelclient1 = new HttpClient(handler);
                    Parallelclient1.BaseAddress = new Uri(conString);
                    Parallelclient1.DefaultRequestHeaders.Accept.Clear();
                    Parallelclient1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    Parallelitems = modelDataParallel.ParallelNode.Data;
                    var Parallelnames = new List<string>();
                    var ParalleidString = "";
                    var ParallelresponseText3 = "";
                    for (int i = 0; i < Parallelitems.Count; i++)
                    {
                        var id = Parallelitems[i];
                        //ParalleidString += $"Id eq '{id.ApproverId}' {(i != Parallelitems.Count - 1 ? "or " : "")}";
                        ParalleidString = $"Id eq '{id.ApproverId}'";
                        Parallelitems[i].UserLevel = i + 1;//
                        var Parallelresponse3 = await Parallelclient1.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + ParalleidString + ")");
                        ParallelresponseText3 = await Parallelresponse3.Content.ReadAsStringAsync();
                        dynamic Paralleldata4 = Json.Decode(ParallelresponseText3);

                        if (Paralleldata4.Count != 0)
                        {
                            foreach (var name in Paralleldata4.d.results)
                            {
                                Parallelnames.Add(name.Title as string);
                            }
                        }
                    }

                    //Parallelitems = Parallelitems.OrderBy(x => x.ApproverId).ToList();
                    if (Parallelitems.Count == Parallelnames.Count)
                    {
                        for (int k = 0; k < Parallelitems.Count; k++)
                        {
                            Parallelitems[k].UserName = Parallelnames[k];
                        }
                    }
                    Parallelitems = Parallelitems.OrderBy(x => x.SubAreaId).ToList();

                    if (!string.IsNullOrEmpty(responseTextParallelApprovalMaster))
                    {
                        dynamic Paralleldata2 = Json.Decode(responseTextParallelApprovalMaster);
                        IDCFDataList.four = Paralleldata2.d.results;
                        IDCFDataList.five = Parallelitems;
                    }

                }
                else
                {
                    IDCFDataList.four = 0;
                    IDCFDataList.five = 0;
                }

                var userList = Parallelitems.ToList();
                var AreaDet = MainList.ToList();
                foreach (var area in mainModel.AreaDetails)
                {
                    foreach (var subarea in area.SubAreas)
                    {
                        var currentUser = userList.Where(x => x.SubAreaId == subarea.SubAreaId).FirstOrDefault();
                        if (currentUser != null)
                        {
                            subarea.UserName = currentUser.UserName;
                            subarea.Modified = currentUser.Modified;
                            subarea.ApproverStatus = currentUser.ApproverStatus;
                            subarea.Comment = currentUser.Comment;
                        }
                    }
                }

                return IDCFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }

        public async Task<List<ApprovalMatrix>> GetApprovalIDCF(string locationId, string ScreenId, string itSecurityEmail, string securityEmail, string onBehalfEmail, string selfOnBehalf = "")
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_IdCardApproval", con);
                cmd.Parameters.Add(new SqlParameter("@ScreenName", ScreenId));
                if (selfOnBehalf == "OnBehalf")
                {
                    cmd.Parameters.Add(new SqlParameter("@emailId", onBehalfEmail));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@emailId", user.Email));
                }
                //cmd.Parameters.Add(new SqlParameter("@locationId", locationId));
                //cmd.Parameters.Add(new SqlParameter("@itSecurityEmailId", itSecurityEmail));
                //cmd.Parameters.Add(new SqlParameter("@securityEmailId", securityEmail));
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
                        appList.Add(app);
                    }
                }

                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                var emailString = "";
                var count = appList.Count;
                for (int i = 0; i < count; i++)
                {
                    var email = appList[i];
                    emailString += $"EMail eq '{email.EmailId}' {(i != count - 1 ? "or " : "")}";
                }
                var response = await client.GetAsync($"_api/web/SiteUserInfoList/items?$select=Id,Title,EMail&$filter=({emailString})");
                var responseText = await response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseText.ToString());
                var title = doc.GetElementsByTagName("d:Title");
                var id = doc.GetElementsByTagName("d:Id");
                var emails = doc.GetElementsByTagName("d:EMail");

                for (int i = 0; i < id.Count; i++)
                {
                    var currentEmail = emails[i].InnerXml;
                    var currentId = Convert.ToInt32(id[i].InnerXml);
                    var matchingUser = appList.Where(x => x.EmailId == currentEmail).FirstOrDefault();
                    if (matchingUser != null)
                        matchingUser.ApproverUserName = Convert.ToString(currentId);
                }

                return appList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }

        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalOfIDCF(long empNum, long ccNum, bool isPKI, bool isInternalEmp, string ExternalEmpOrgName, int LocationID)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_IdCardApproval", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@empNum", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccNum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@isPKI", isPKI));
                cmd.Parameters.Add(new SqlParameter("@isInternalEmp", isInternalEmp));
                cmd.Parameters.Add(new SqlParameter("@ExternalEmployeeOrgName", ExternalEmpOrgName));
                cmd.Parameters.Add(new SqlParameter("@LocationID", LocationID));
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
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }

        public List<UserData> GetIDCFEmployeeDetails(string empCode)
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

        /// <summary>
        /// ID Card-It is used to get the Area Master Dropdown data.
        /// </summary>
        /// <returns></returns>

        public async Task<dynamic> GetAreaMaster(string Locationid)
        {
            RIDCFResults RIDCFData = new RIDCFResults();
            dynamic result = RIDCFData;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML            

                var response = await client.GetAsync("_api/web/lists/GetByTitle('AreaMaster')/items?$select=ID,LocationId,AreaName&$filter=(LocationId eq '" + Locationid + "' and IsActive eq '1')");

                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<RIDCFModel>(responseText);
                    RIDCFData = locResult.ridcfflist;
                }
                result = RIDCFData.ridcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// NewId Card-It is used to get the Sub Area Master Dropdown data.
        /// </summary>
        /// <returns></returns>

        public async Task<dynamic> GetSubAreaMaster(string Locationid, string AreaID)
        {
            RIDCFResults RIDCFData = new RIDCFResults();
            dynamic result = RIDCFData;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML            

                var response = await client.GetAsync("_api/web/lists/GetByTitle('SubAreaMaster')/items?$select=ID,AreaId,LocationId,SubAreaName&$filter=(LocationId eq '" + Locationid + "' and AreaId eq '" + AreaID + "' and IsActive eq '1')");

                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<RIDCFModel>(responseText);
                    RIDCFData = locResult.ridcfflist;
                }
                result = RIDCFData.ridcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// Reissue id Card-It is used to View RIDCF Data
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> ViewRIDCFData(int rowId, int formId)
        {
            dynamic RIDCFDataList = new ExpandoObject();//constructor called, always expandoobject for dynamic dataType
            string locationid = string.Empty;
            List<RIDCFData> AreaList = new List<RIDCFData>();
            List<RIDCFData> AreaTexList = new List<RIDCFData>();
            List<RIDCFData> MainList = new List<RIDCFData>();
            List<RIDCFData> SubAreaTextList = new List<RIDCFData>();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = await client.GetAsync("_api/web/lists/GetByTitle('ReissueIDCardForm')/items?$select=ID,SelfOnBehalf,SelfEmailID,OnBehalfSurname,OnBehalfEmployeeIDNo,OnBehalfTelephone,OnBehalfMobile,OnBehalfEmailID,OnBehalfCostCenter,OnBehalfDepartment,OnBehalfFirstName,SelfMobile,SelfTelephone,LocationId,ReasonforReissue,EmployeeType,Surname,Name,MumbaiProfile,PuneProfile,ActiveFrom,EndDate,"
                    + "DateofJoining,EmployeeNumber,CostCentre,Department,DateofIssue,IDCardNumber,LocationName," +
                    "AurangabadProfile" +
                    "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')");

                var responseText = await response.Content.ReadAsStringAsync();


                if (!string.IsNullOrEmpty(responseText))
                {
                    var RIDCFResult = JsonConvert.DeserializeObject<RIDCFModel>(responseText);
                    MainList = RIDCFResult.ridcfflist.ridcfData;
                }

                //Area & Sub Area ID
                var areasubAreaidresponse = await client.GetAsync("_api/web/lists/GetByTitle('ReissueIdCardAreaSubAreaMapping')/items?$select=ID,LocationId,Area,SubAreas" +
                  "&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')");

                var areasubareaidresponseText = await areasubAreaidresponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(areasubareaidresponseText))
                {
                    var RIDCFResult1 = JsonConvert.DeserializeObject<RIDCFModel>(areasubareaidresponseText);
                    AreaList = RIDCFResult1.ridcfflist.ridcfData;
                    locationid = RIDCFResult1.ridcfflist.ridcfData.Any() ? RIDCFResult1.ridcfflist.ridcfData.FirstOrDefault().LocationId : "0";
                }

                //Area text
                var areatextresponse = await client.GetAsync("_api/web/lists/GetByTitle('AreaMaster')/items?$select=ID,AreaName&$filter=(LocationId eq '" + locationid + "' and IsActive eq '1')");

                var areatextresponseText = await areatextresponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(areatextresponseText))
                {
                    var RIDCFResult2 = JsonConvert.DeserializeObject<RIDCFModel>(areatextresponseText);
                    AreaTexList = RIDCFResult2.ridcfflist.ridcfData;
                }

                //Sub Area Text
                var subareatextresponse = await client.GetAsync("_api/web/lists/GetByTitle('SubAreaMaster')/items?$select=ID,AreaId,SubAreaName&$filter=(LocationId eq '" + locationid + "'  and IsActive eq '1')");

                var subarearesponseText = await subareatextresponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(subarearesponseText))
                {
                    var RIDCFResult3 = JsonConvert.DeserializeObject<RIDCFModel>(subarearesponseText);
                    SubAreaTextList = RIDCFResult3.ridcfflist.ridcfData;

                }

                var mainModel = MainList.FirstOrDefault();
                mainModel.AreaDetails = new List<RIDFCArea>();
                foreach (var currentArea in AreaList)
                {
                    var areaModel = new RIDFCArea();
                    areaModel.AreaId = Convert.ToInt32(currentArea.Area);
                    areaModel.Area = AreaTexList.Where(x => x.Id == areaModel.AreaId).FirstOrDefault().AreaName;
                    if (currentArea.SubAreas != null)
                    {
                        var subAreaIds = currentArea.SubAreas.Split(',').Select(Int32.Parse).ToList();
                        areaModel.SubAreas = subAreaIds.Select(x => new RIDFCSubArea() { SubAreaId = x, SubArea = SubAreaTextList.Where(y => y.Id == x && y.AreaId == Convert.ToString(areaModel.AreaId)).FirstOrDefault()?.SubAreaName }).ToList();
                    }
                    mainModel.AreaDetails.Add(areaModel);
                }

                RIDCFDataList.one = MainList;


                #region Approval Master For Signature
                //Approval Master For Sequential Signature
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseApprovalMaster = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverUserName,NextApproverId,"
                + "FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApprovalMaster, settings);

                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var items = modelData.Node.Data;
                var idString = "";
                for (int i = 0; i < items.Count; i++)
                {
                    var id = items[i];//
                    idString += $"Id eq '{id.ApproverUserName}' {(i != items.Count - 1 ? "or " : "")}";
                    items[i].UserLevel = i + 1;//
                }
                var response3 = await client3.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + idString + ")");
                var responseText3 = await response3.Content.ReadAsStringAsync();

                dynamic data4 = Json.Decode(responseText3);
                var names = new List<string>();
                foreach (var name in data4.d.results)
                {
                    names.Add(name.Title as string);
                }
                items = items.OrderBy(x => x.ApproverUserName).ToList();
                if (items.Count == names.Count)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        items[i].UserName = names[i];
                    }
                }
                items = items.OrderBy(x => x.UserLevel).ToList();

                if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                {
                    dynamic data2 = Json.Decode(responseTextApprovalMaster);
                    RIDCFDataList.two = data2.d.results;
                    RIDCFDataList.three = items;
                }

                #endregion

                #region Parallel Approval Master For Door Access Signature

                //Parallel Approval Master For Signature
                var Parallelsettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var clientParallel = new HttpClient(handler);
                clientParallel.BaseAddress = new Uri(conString);
                clientParallel.DefaultRequestHeaders.Accept.Clear();
                clientParallel.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseParallelApprovalMaster = await client.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,Comment,ApproverStatus,RowId,Modified,Author/Title,FormId/FormName," +
                         "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseTextParallelApprovalMaster = await responseParallelApprovalMaster.Content.ReadAsStringAsync();
                var modelDataParallel = JsonConvert.DeserializeObject<ParallelApprovalMasterModel>(responseTextParallelApprovalMaster, Parallelsettings);
                dynamic modelDataParallelData = System.Web.Helpers.Json.Decode(responseTextParallelApprovalMaster);
                var Parallelitems = new List<ParallelApprovalDataModel>();
                if (modelDataParallelData.d.results.Length != 0)
                {
                    var Parallelclient1 = new HttpClient(handler);
                    Parallelclient1.BaseAddress = new Uri(conString);
                    Parallelclient1.DefaultRequestHeaders.Accept.Clear();
                    Parallelclient1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    Parallelitems = modelDataParallel.ParallelNode.Data;
                    var Parallelnames = new List<string>();
                    var ParalleidString = "";
                    var ParallelresponseText3 = "";
                    for (int i = 0; i < Parallelitems.Count; i++)
                    {
                        var id = Parallelitems[i];
                        //ParalleidString += $"Id eq '{id.ApproverId}' {(i != Parallelitems.Count - 1 ? "or " : "")}";
                        ParalleidString = $"Id eq '{id.ApproverId}'";
                        Parallelitems[i].UserLevel = i + 1;//
                        var Parallelresponse3 = await Parallelclient1.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + ParalleidString + ")");
                        ParallelresponseText3 = await Parallelresponse3.Content.ReadAsStringAsync();
                        dynamic Paralleldata4 = Json.Decode(ParallelresponseText3);

                        if (Paralleldata4.Count != 0)
                        {
                            foreach (var name in Paralleldata4.d.results)
                            {
                                Parallelnames.Add(name.Title as string);
                            }
                        }
                    }

                    //Parallelitems = Parallelitems.OrderBy(x => x.ApproverId).ToList();
                    if (Parallelitems.Count == Parallelnames.Count)
                    {
                        for (int k = 0; k < Parallelitems.Count; k++)
                        {
                            Parallelitems[k].UserName = Parallelnames[k];
                        }
                    }
                    Parallelitems = Parallelitems.OrderBy(x => x.UserLevel).ToList();



                    if (!string.IsNullOrEmpty(responseTextParallelApprovalMaster))
                    {
                        dynamic Paralleldata2 = Json.Decode(responseTextParallelApprovalMaster);
                        RIDCFDataList.four = Paralleldata2.d.results;
                        RIDCFDataList.five = Parallelitems;
                    }
                }
                else
                {
                    RIDCFDataList.four = null;
                    RIDCFDataList.five = null;
                }
                #endregion


                var userList = Parallelitems.ToList();
                var AreaDet = MainList.ToList();
                foreach (var area in mainModel.AreaDetails)
                {
                    foreach (var subarea in area.SubAreas)
                    {
                        var currentUser = userList.Where(x => x.SubAreaId == subarea.SubAreaId).FirstOrDefault();
                        if (currentUser != null)
                        {
                            subarea.UserName = currentUser.UserName;
                            subarea.Modified = currentUser.Modified;
                            subarea.ApproverStatus = currentUser.ApproverStatus;
                            subarea.Comment = currentUser.Comment;
                        }
                    }
                }


                return RIDCFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }


        /// <summary>
        /// RIDCF-It is used to get the approval list for Reissue ID Card form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetRIDCFApprovalList()
        {
            RIDCFResults ridcfData = new RIDCFResults();
            dynamic result = ridcfData;
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ApproverInformationMaster')/items?$select=Department,SubDepartment,Location,ApproverEmployeeCode,ApproverEmailId,ID&$filter=(IsActive eq '1')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var RIDCFResult = JsonConvert.DeserializeObject<RIDCFModel>(responseText);
                    ridcfData = RIDCFResult.ridcfflist;
                }
                result = ridcfData.ridcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// DARF-It is used to get the approval list for Door Access Form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetDARFApprovalList()
        {
            DARFResults darfData = new DARFResults();
            dynamic result = darfData;
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ApproverInformationMaster')/items?$select=Department,SubDepartment,Location,ApproverEmployeeCode,ApproverEmailId,ID&$filter=(IsActive eq '1')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var DARFResult = JsonConvert.DeserializeObject<DARFModel>(responseText);
                    darfData = DARFResult.darflist;
                }
                result = darfData.darfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// New id Card-It is used to View IDCF Data
        /// </summary>
        /// <returns></returns


        public async Task<dynamic> ViewDARFData(int rowId, int formId)
        {
            dynamic DARFDataList = new ExpandoObject();//constructor called, always expandoobject for dynamic dataType
            string locationid = string.Empty;
            List<DARFData> AreaList = new List<DARFData>();
            List<DARFData> AreaTexList = new List<DARFData>();
            List<DARFData> MainList = new List<DARFData>();
            List<DARFData> SubAreaTextList = new List<DARFData>();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = await client.GetAsync("_api/web/lists/GetByTitle('DoorAccessRequestForm')/items?$select=ID,LocationId,SelfOnBehalf,EmployeeType,Surname,EmployeeName,MumbaiProfile,PuneProfile,"
                    + "EmployeeNumber,CostCentre,Department,LocationName," +
                    "AurangabadProfile,SelfMobile,SelfTelephone,SelfTelephone,SelfEmailID,OnBehalfFirstName,OnBehalfSurname,OnBehalfEmployeeIDNo,OnBehalfDepartment,OnBehalfMobile,OnBehalfTelephone,OnBehalfEmailID,OnBehalfCostCenter" +
                    "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')");

                var responseText = await response.Content.ReadAsStringAsync();


                if (!string.IsNullOrEmpty(responseText))
                {
                    var DARFResult = JsonConvert.DeserializeObject<DARFModel>(responseText);
                    MainList = DARFResult.darflist.darfData;
                }

                //Area & Sub Area ID
                var areasubAreaidresponse = await client.GetAsync("_api/web/lists/GetByTitle('DoorAccessRequestAreaSubAreaMapping')/items?$select=ID,LocationId,Area,SubAreas" +
                  "&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')");

                var areasubareaidresponseText = await areasubAreaidresponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(areasubareaidresponseText))
                {
                    var DARFResult1 = JsonConvert.DeserializeObject<DARFModel>(areasubareaidresponseText);
                    AreaList = DARFResult1.darflist.darfData;
                    locationid = DARFResult1.darflist.darfData.Any() ? DARFResult1.darflist.darfData.FirstOrDefault().LocationId : "0";
                }

                //Area text
                var areatextresponse = await client.GetAsync("_api/web/lists/GetByTitle('AreaMaster')/items?$select=ID,AreaName&$filter=(LocationId eq '" + locationid + "' and IsActive eq '1')");

                var areatextresponseText = await areatextresponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(areatextresponseText))
                {
                    var DARFResult2 = JsonConvert.DeserializeObject<DARFModel>(areatextresponseText);
                    AreaTexList = DARFResult2.darflist.darfData;
                }

                //Sub Area Text
                var subareatextresponse = await client.GetAsync("_api/web/lists/GetByTitle('SubAreaMaster')/items?$select=ID,AreaId,SubAreaName&$filter=(LocationId eq '" + locationid + "'  and IsActive eq '1')");

                var subarearesponseText = await subareatextresponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(subarearesponseText))
                {
                    var DARFResult3 = JsonConvert.DeserializeObject<DARFModel>(subarearesponseText);
                    SubAreaTextList = DARFResult3.darflist.darfData;

                }

                var mainModel = MainList.FirstOrDefault();
                mainModel.AreaDetails = new List<DARFArea>();
                foreach (var currentArea in AreaList)
                {
                    var areaModel = new DARFArea();
                    areaModel.AreaId = Convert.ToInt32(currentArea.Area);
                    areaModel.Area = AreaTexList.Where(x => x.Id == areaModel.AreaId).FirstOrDefault().AreaName;
                    if (currentArea.SubAreas != null)
                    {
                        var subAreaIds = currentArea.SubAreas.Split(',').Select(Int32.Parse).ToList();
                        areaModel.SubAreas = subAreaIds.Select(x => new IDFCSubArea() { SubAreaId = x, SubArea = SubAreaTextList.Where(y => y.Id == x && y.AreaId == Convert.ToString(areaModel.AreaId)).FirstOrDefault()?.SubAreaName }).ToList();
                    }
                    mainModel.AreaDetails.Add(areaModel);
                }

                DARFDataList.one = MainList;

                //Approval Master For Sequential Signature
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseApprovalMaster = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,ApproverUserName,Comment,NextApproverId,"
                + "FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApprovalMaster, settings);

                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var items = modelData.Node.Data;
                var idString = "";
                for (int i = 0; i < items.Count; i++)
                {
                    var id = items[i];//
                    idString += $"Id eq '{id.ApproverUserName}' {(i != items.Count - 1 ? "or " : "")}";
                    items[i].UserLevel = i + 1;//
                }
                var response3 = await client3.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + idString + ")");
                var responseText3 = await response3.Content.ReadAsStringAsync();

                dynamic data4 = Json.Decode(responseText3);
                var names = new List<string>();
                foreach (var name in data4.d.results)
                {
                    names.Add(name.Title as string);
                }
                items = items.OrderBy(x => x.ApproverUserName).ToList();
                if (items.Count == names.Count)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        items[i].UserName = names[i];
                    }
                }
                items = items.OrderBy(x => x.UserLevel).ToList();

                if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                {
                    dynamic data2 = Json.Decode(responseTextApprovalMaster);
                    DARFDataList.two = data2.d.results;
                    DARFDataList.three = items;
                }

                //Approval Master For Parallel Signature
                var Parallelsettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var clientParallel = new HttpClient(handler);
                clientParallel.BaseAddress = new Uri(conString);
                clientParallel.DefaultRequestHeaders.Accept.Clear();
                clientParallel.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var responseParallelApprovalMaster = await client.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,ApproverStatus,RowId,Modified,Author/Title,FormId/FormName," +
                //              "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(FormId eq '" + formId + "' and ApproverId eq '" + user.UserId + "')&$expand=FormId,Author");

                var responseParallelApprovalMaster = await client.GetAsync("_api/web/lists/GetByTitle('ParallelApprovalMaster')/items?$select=ApproverId,ApprovalType,Comment,ApproverStatus,RowId,Modified,Author/Title,FormId/FormName," +
                         "FormId/Created,AreaId,SubAreaId,FormId/UniqueFormName&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseTextParallelApprovalMaster = await responseParallelApprovalMaster.Content.ReadAsStringAsync();
                var modelDataParallel = JsonConvert.DeserializeObject<ParallelApprovalMasterModel>(responseTextParallelApprovalMaster, Parallelsettings);
                dynamic modelDataParallelData = System.Web.Helpers.Json.Decode(responseTextParallelApprovalMaster);
                var Parallelitems = new List<ParallelApprovalDataModel>();
                if (modelDataParallelData.d.results.Length != 0)
                {
                    var Parallelclient1 = new HttpClient(handler);
                    Parallelclient1.BaseAddress = new Uri(conString);
                    Parallelclient1.DefaultRequestHeaders.Accept.Clear();
                    Parallelclient1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    Parallelitems = modelDataParallel.ParallelNode.Data;
                    var Parallelnames = new List<string>();
                    var ParalleidString = "";
                    var ParallelresponseText3 = "";
                    for (int i = 0; i < Parallelitems.Count; i++)
                    {
                        var id = Parallelitems[i];
                        //ParalleidString += $"Id eq '{id.ApproverId}' {(i != Parallelitems.Count - 1 ? "or " : "")}";
                        ParalleidString = $"Id eq '{id.ApproverId}'";
                        Parallelitems[i].UserLevel = i + 1;//
                        var Parallelresponse3 = await Parallelclient1.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + ParalleidString + ")");
                        ParallelresponseText3 = await Parallelresponse3.Content.ReadAsStringAsync();
                        dynamic Paralleldata4 = Json.Decode(ParallelresponseText3);

                        if (Paralleldata4.Count != 0)
                        {
                            foreach (var name in Paralleldata4.d.results)
                            {
                                Parallelnames.Add(name.Title as string);
                            }
                        }
                    }

                    //Parallelitems = Parallelitems.OrderBy(x => x.ApproverId).ToList();
                    if (Parallelitems.Count == Parallelnames.Count)
                    {
                        for (int k = 0; k < Parallelitems.Count; k++)
                        {
                            Parallelitems[k].UserName = Parallelnames[k];
                        }
                    }
                    Parallelitems = Parallelitems.OrderBy(x => x.UserLevel).ToList();

                    if (!string.IsNullOrEmpty(responseTextParallelApprovalMaster))
                    {
                        dynamic Paralleldata2 = Json.Decode(responseTextParallelApprovalMaster);
                        DARFDataList.four = Paralleldata2.d.results;
                        DARFDataList.five = Parallelitems;
                    }

                }
                else
                {
                    DARFDataList.four = 0;
                    DARFDataList.five = 0;
                }

                var userList = Parallelitems.ToList();
                var AreaDet = MainList.ToList();
                foreach (var area in mainModel.AreaDetails)
                {
                    foreach (var subarea in area.SubAreas)
                    {
                        var currentUser = userList.Where(x => x.SubAreaId == subarea.SubAreaId).FirstOrDefault();
                        if (currentUser != null)
                        {
                            subarea.UserName = currentUser.UserName;
                            subarea.Modified = currentUser.Modified;
                            subarea.ApproverStatus = currentUser.ApproverStatus;
                            subarea.Comment = currentUser.Comment;
                        }
                    }
                }

                return DARFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }

        public bool SaveApproverResponse(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("IDCF") ? GlobalClass.ListNames["IDCF"] : "";
            if (listName == "")
                return false;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);

                newItem["DateofIssue"] = form["txtDateofIssue"];
                newItem["Chargable"] = form["ddChargable"];
                newItem["IDCardNumber"] = form["txtIdCardNo"];
                newItem["VendorCode"] = form["txtVendorCode"];

                newItem.Update();
                _context.Load(newItem);
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
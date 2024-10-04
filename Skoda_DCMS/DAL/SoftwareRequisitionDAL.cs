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
    public class SoftwareRequisitionDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;


        /// <summary>
        /// SoftwareRequisition-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
    
        public async Task<ResponseModel<object>> CreateSoftwareRequisitionRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
         
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "SRF";
            string formName = "Software Requisition Form";
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
                var loc = requestSubmissionFor == "Self" ? form["ddEmpLocation"]
                     : (otherEmpType == "SAVWIPLEmployee" ? form["ddOtherEmpLocation"]
                         : (otherEmpType == "Others" ? form["ddOtherNewEmpLocation"] : ""));

                var count = Convert.ToInt32(form["totalrows"]);

                string pattern = ",";

                var softwareName = string.Empty;
                var softwareVersion = string.Empty;
                var softwareType = string.Empty;
                for (var i = 1; i < count + 1; i++)
                {
                    softwareName += form["txtSoftwareName_" + i + ""] + ",";
                    softwareVersion += form["txtSoftwareVersion_" + i + ""] + ",";
                    softwareType += form["txtSoftwareType_" + i + ""] + ",";
                }

                var softwareNames = softwareName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                softwareNames = softwareNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var softwareVersions = softwareVersion.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                softwareVersions = softwareVersions.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var softwareTypes = softwareType.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                softwareTypes = softwareTypes.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var otherSoftwareName = form["txtOtherSoftwareName"];
                var otherSoftwareVersion = form["txtOtherSoftwareVersion"];
                var otherSoftwareType = form["drpOtherSoftwareType"];


                var otherSoftwareNames = otherSoftwareName != null ? otherSoftwareName.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                otherSoftwareNames = otherSoftwareNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var otherSoftwareVersions = otherSoftwareVersion != null ? otherSoftwareVersion.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                otherSoftwareVersions = otherSoftwareVersions.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var otherSoftwareTypes = otherSoftwareType != null ? otherSoftwareType.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                otherSoftwareTypes = otherSoftwareTypes.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var x = 0;
                var y = 0;
                foreach (var i in softwareTypes)
                {
                    if (i == "Controlled/Standard Software")
                    {
                        x++;
                    }
                }
                foreach (var i in softwareTypes)
                {
                    if (i == "Non-Standard Software")
                    {
                        y++;
                    }
                }

                foreach (var i in otherSoftwareTypes)
                {
                    if (i == "Non-Standard Software")
                    {
                        y++;
                    }
                }

                var isControlled = x > 0 ? "Yes" : "No";

                var isNonControlled = y > 0 ? "Yes" : "No";


                var response = await GetApprovalForSoftwareRequisition(empNum, ccNum, loc, isControlled, isNonControlled);
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
                    item["FormName"] = "Software Requisition Form";
                    item["UniqueFormName"] = "SRF";
                    item["FormParentId"] = 10;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SoftwareRequisition";
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
                    List flist = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = flist.GetItemById(FormId);
                    item["FormName"] = "Software Requisition Form";
                    item["UniqueFormName"] = "SRF";
                    item["FormParentId"] = 10;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SoftwareRequisition";
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
                        ListItem alistItem = listApprovalMaster.GetItemById(AppRowId);
                        alistItem["ApproverStatus"] = "Resubmitted";
                        alistItem["IsActive"] = 0;
                        alistItem.Update();
                        _context.Load(alistItem);
                        _context.ExecuteQuery();
                    }
                }
                List SoftwareList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                ListItem newRow = SoftwareList.AddItem(itemInfo);
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

                newRow["EmployeeRequestType"] = form["chkRequestType"];
                newRow["TempFrom"] = form["txtTempFrom"] == "" ? null : form["txtTempFrom"];
                newRow["TempTo"] = form["txtTempTo"] == "" ? null : form["txtTempTo"];
                newRow["BusinessNeed"] = form["txtBusinessNeed"];

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                result.Status = 200;
                result.Message = formId.ToString();
               
                int SoftwareId = newRow.Id;

                List SoftwareDetailsList = web.Lists.GetByTitle("SoftwareDetails");

               

                List list = _context.Web.Lists.GetByTitle("SoftwareRequisition");

                ListItem listItem = list.GetItemById(SoftwareId);
                listItem["IsStandard"] = x > 0 ? true : false;
                listItem["IsNonStandard"] = y > 0 ? true : false;
                listItem.Update();
                _context.ExecuteQuery();

                for (int i = 0; i < softwareNames.Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newItem = SoftwareDetailsList.AddItem(itemCreate);
                    newItem["SoftwareRequisitionID"] = SoftwareId;
                    newItem["SoftwareName"] = softwareNames[i] ?? "";
                    newItem["SoftwareVersion"] = softwareVersions.Count != 0 ? softwareVersions[i] : null;
                    newItem["SoftwareType"] = softwareTypes[i] ?? "";
                    newItem["FormID"] = formId;
                    newItem["IsOtherSoftware"] = "No";
                    newItem.Update();
                    _context.ExecuteQuery();
                }

                for (int i = 0; i < otherSoftwareNames.Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newItem = SoftwareDetailsList.AddItem(itemCreate);
                    newItem["SoftwareRequisitionID"] = SoftwareId;
                    newItem["SoftwareName"] = otherSoftwareNames[i] ?? "";
                    newItem["SoftwareVersion"] = otherSoftwareVersions.Count != 0 ? otherSoftwareVersions[i] : null;
                    newItem["SoftwareType"] = otherSoftwareTypes[i] ?? "";
                    newItem["FormID"] = formId;
                    newItem["IsOtherSoftware"] = "Yes";
                    newItem.Update();
                    _context.ExecuteQuery();
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
                    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
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
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                //result.one = 0;
                //result.two = 0;
                return result;
            }

        }
        /// <summary>
        /// SoftwareRequisition-It is used for viewing software requisition form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetSoftwareRequisitionDetails(int rowId, int formId)
        {
            dynamic software = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('SoftwareRequisition')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,EmployeeRequestType,TempFrom,TempTo," +
                    "BusinessNeed,IsNonStandard,IsStandard,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,OtherExternalOrganizationName,OtherExternalOtherOrgName,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<SoftwareRequisitionModel>(responseText, settings);
                    software.one = result.List.SoftwareList;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('SoftwareDetails')/items?$select=SoftwareName,SoftwareVersion,SoftwareType,IsOtherSoftware&$filter=(SoftwareRequisitionID eq '" + rowId + "')");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                //dynamic data2 = Json.Decode(responseText2);
                //software.two = data2.d.results;
                var result1 = JsonConvert.DeserializeObject<SelectedSoftwareModel>(responseText2);
                software.two = result1.List.AVLSoftwareList;

                var data = result1.List.AVLSoftwareList;
                var softwares = data.Where(x => x.IsOtherSoftware == "No").ToList();

                software.Softwares = softwares;

                var otherData = result1.List.AVLSoftwareList;
                var otherSoftwares = otherData.Where(x => x.IsOtherSoftware == "Yes").ToList();

                software.OtherSoftwares = otherSoftwares;

                //approval start
                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response3 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverName,ApproverUserName,NextApproverId,Level,Logic,TimeStamp,Designation,"
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



                    // items = items.OrderBy(x => x.ApproverId).ToList();
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
                        software.three = data3.d.results;
                        software.four = items;
                    }

                }
                else
                {
                    software.two = new List<string>();
                    software.three = new List<string>();
                    software.four = new List<string>();
                }

                //approval end

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return software;
        }
        /// <summary>
        /// SoftwareRequisition-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForSoftwareRequisition(long empNum, long ccNum, string loc, string isControlled, string isNonControlled)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SoftwareRequisitionApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empLoc", loc));
                cmd.Parameters.Add(new SqlParameter("@isControlled", isControlled));
                cmd.Parameters.Add(new SqlParameter("@isNonControlled", isNonControlled));
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

                //return new List<ApprovalMatrix>();
            }

        }

        public List<SoftwareModel> GetAvailableSoftwares(string softName)
        {
            List<SoftwareModel> softwares = new List<SoftwareModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetAvailableSoftwares", con);
                cmd.Parameters.Add(new SqlParameter("@SoftName", softName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        SoftwareModel software = new SoftwareModel();
                        software.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["SoftwareID"]);
                        software.Name = ds.Tables[0].Rows[i]["Name"].ToString();

                        softwares.Add(software);

                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<SoftwareModel>();
            }
            return softwares;
        }
        public List<SoftwareModel> GetAvailableSoftwareDetails(string softwareName)
        {
            List<SoftwareModel> softwares = new List<SoftwareModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetAvailableSoftwares", con);
                cmd.Parameters.Add(new SqlParameter("@SoftName", softwareName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        SoftwareModel software = new SoftwareModel();
                        software.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["SoftwareID"]);
                        software.Version = ds.Tables[0].Rows[i]["Version"].ToString();
                        software.Name = ds.Tables[0].Rows[i]["Name"].ToString();
                        var type = ds.Tables[0].Rows[i]["Classification"].ToString();
                        if (type == "Commercial" || type == "Component")
                        {
                            software.Classification = "Controlled/Standard Software";
                        }
                        else
                        {
                            software.Classification = "Non-Standard Software";
                        }

                        softwares.Add(software);

                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<SoftwareModel>();
            }
            return softwares;


        }

    }
}
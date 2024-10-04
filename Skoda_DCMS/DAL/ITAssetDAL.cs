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
    public class ITAssetDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;


        /// <summary>
        /// ITAsset Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        //public async Task<dynamic> CreateITAssetRequisitionRequest(System.Web.Mvc.FormCollection form, UserData user)
        public async Task<ResponseModel<object>> CreateITAssetRequisitionRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "ITARF";
            string formName = "IT Asset Requisition Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
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
                long empNum = requestSubmissionFor == "Self" ? user.EmpNumber : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                long ccNum = requestSubmissionFor == "Self" ? user.CostCenter : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                string requesterDesg = requestSubmissionFor == "Self" ? form["TempDesignation"] : form["TempOtherDesignation"];

                var loc = requestSubmissionFor == "Self" ? form["ddEmpLocation"]
                     : (otherEmpType == "SAVWIPLEmployee" ? form["ddOtherEmpLocation"]
                         : (otherEmpType == "Others" ? form["ddOtherNewEmpLocation"] : ""));

                int isWorkstationDesktop = form["chkWorkstationDesktop"] == null ? 0 : 1;
                int isWorkstationLaptop = form["chkWorkstationLaptop"] == null ? 0 : 1;
                int isDesktop = form["chkDesktop"] == null ? 0 : 1;
                int isLaptop = form["chkLaptop"] == null ? 0 : 1;
                int isRSAToken = form["chkRSAToken"] == null ? 0 : 1;
                int isSIMAndData = form["chkSIMAndData"] == null ? 0 : 1;
                int isLandline = form["chkLandline"] == null ? 0 : 1;
                int isLANCableAndPort = form["chkLANCableAndPort"] == null ? 0 : 1;
                int isJabraSpeaker = form["chkJabraSpeaker"] == null ? 0 : 1;
                int isAdditionalOfficeMonitor = form["chkAdditionalOfficeMonitor"] == null ? 0 : 1;
                int isiPad = form["chkiPad"] == null ? 0 : 1;
                int isCabinsScreen = form["chkCabinsScreen"] == null ? 0 : 1;
                int isNewMeetingRoomSetup = form["chkNewMeetingRoomSetup"] == null ? 0 : 1;

                string requesterEmployeeType = requestSubmissionFor == "Self" ? form["chkEmployeeType"] : (otherEmpType == "SAVWIPLEmployee" ? form["chkOtherEmployeeType"] : form["chkOtherNewEmployeeType"]);
                int isInternal = requesterEmployeeType == "Internal" ? 1 : 0;

                var response = await GetApprovalForITAssetRequisition(empNum, ccNum, loc, requesterDesg, isInternal, isWorkstationLaptop, isLaptop, isSIMAndData, isLANCableAndPort, isJabraSpeaker, isAdditionalOfficeMonitor, isiPad, isCabinsScreen, isNewMeetingRoomSetup);
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
                    item["FormName"] = "IT Asset Requisition Form";
                    item["UniqueFormName"] = "ITARF";
                    item["FormParentId"] = 21;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ITAsset";
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
                    item["FormName"] = "IT Asset Requisition Form";
                    item["UniqueFormName"] = "ITARF";
                    item["FormParentId"] = 21;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ITAsset";
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
                newRow["EmployeeType"] = form["chkEmployeeType"] ?? "";
                newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newRow["PartnerOrganizationName"] = form["txtPartnerOrganizationName"] ?? "";
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
                        newRow["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? "";
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
                newRow["WorkstationDesktop"] = form["chkWorkstationDesktop"] == null ? "No" : "Yes";
                newRow["WorkstationLaptop"] = form["chkWorkstationLaptop"] == null ? "No" : "Yes";
                newRow["Desktop"] = form["chkDesktop"] == null ? "No" : "Yes";
                newRow["Laptop"] = form["chkLaptop"] == null ? "No" : "Yes";
                newRow["RSAToken"] = form["chkRSAToken"] == null ? "No" : "Yes";
                newRow["SIMAndData"] = form["chkSIMAndData"] == null ? "No" : "Yes";
                newRow["Landline"] = form["chkLandline"] == null ? "No" : "Yes";
                newRow["LANCableAndPort"] = form["chkLANCableAndPort"] == null ? "No" : "Yes";
                newRow["JabraSpeaker"] = form["chkJabraSpeaker"] == null ? "No" : "Yes";
                newRow["AdditionalOfficeMonitor"] = form["chkAdditionalOfficeMonitor"] == null ? "No" : "Yes";
                newRow["iPad"] = form["chkiPad"] == null ? "No" : "Yes";
                newRow["CabinsScreen"] = form["chkCabinsScreen"] == null ? "No" : "Yes";
                newRow["NewMeetingRoomSetup"] = form["chkNewMeetingRoomSetup"] == null ? "No" : "Yes";

                var Designation = "";
                if (requestSubmissionFor == "OnBehalf")
                {
                    Designation = form["TempOtherDesignation"] ?? "";
                }
                else
                {
                    Designation = form["TempDesignation"] ?? "";
                }
                newRow["UsageType"] = form["chkUsageType"] ?? "";
                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

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
                    approvalMasteritem["Designation"] = approverIdList[i].Designation;
                    approvalMasteritem["Logic"] = approverIdList[i].Logic;
                    approvalMasteritem["Level"] = approverIdList[i].ApprovalLevel;

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
        /// IT Asset Requisition Form-It is used for viewing software requisition form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetITAssetRequisitionDetails(int rowId, int formId)
        {
            dynamic ITAsset = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ITAssetRequisition')/items?$select=ID,EmployeeType,Created,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,BusinessNeed,EmployeeLocation,EmployeeDesignation," +
                    "WorkstationDesktop,WorkstationLaptop,RSAToken,SIMAndData,Landline,LANCableAndPort,JabraSpeaker,AdditionalOfficeMonitor,AdditionalOfficeMonitor,iPad,PartnerOrganizationName,WorkflowType,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,OtherPartnerOrganizationName,UsageType," +
                    "FormID/ID,FormID/Created,Desktop,Laptop,CabinsScreen,NewMeetingRoomSetup&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<ITAssetRequisitionModel>(responseText, settings);
                    ITAsset.one = result.List.ITAssetList;
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
                        ITAsset.two = data2.d.results;
                        ITAsset.three = items;
                    }
                }
                return ITAsset;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

        }
        /// <summary>
        /// IT Asset Requisition Form-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForITAssetRequisition(long empNum, long ccNum, string empLoc, string requesterDesg, int isInternal, int isWorkstationLaptop, int isLaptop,
                                                                                 int isSIMAndData, int isLANCableAndPort, int isJabraSpeaker, int isAdditionalOfficeMonitor, int isiPad, int isCabinsScreen, int isNewMeetingRoomSetup)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ITAssetApproval", con);
                cmd.Parameters.Add(new SqlParameter("@empNum", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccNum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empLoc", empLoc));
                cmd.Parameters.Add(new SqlParameter("@desg", requesterDesg));
                cmd.Parameters.Add(new SqlParameter("@isInternal", isInternal));
                cmd.Parameters.Add(new SqlParameter("@isWorkstationLaptop", isWorkstationLaptop));
                cmd.Parameters.Add(new SqlParameter("@isLaptop", isLaptop));
                cmd.Parameters.Add(new SqlParameter("@isSIMAndData", isSIMAndData));
                cmd.Parameters.Add(new SqlParameter("@isLANCableAndPort", isLANCableAndPort));
                cmd.Parameters.Add(new SqlParameter("@isJabraSpeaker", isJabraSpeaker));
                cmd.Parameters.Add(new SqlParameter("@isAdditionalOfficeMonitor", isAdditionalOfficeMonitor));
                cmd.Parameters.Add(new SqlParameter("@isiPad", isiPad));
                cmd.Parameters.Add(new SqlParameter("@isTV", isCabinsScreen));
                cmd.Parameters.Add(new SqlParameter("@isMeetingRoom", isNewMeetingRoomSetup));
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
                        app.ApprovalLevel = Convert.ToInt32(ds.Tables[0].Rows[i]["displevel"]);
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
                appList = common.AddMDAssistantToList(appList);
                appList = common.ChangeDelegateApprover(appList);

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


        //public int ITARFApprovalUpdate(System.Web.Mvc.FormCollection form, UserData user)
        //{

        //    ClientContext _context = new ClientContext(new Uri(conString));
        //    _context.Credentials = new NetworkCredential(user.UserName, user.Password);
        //    Web web = _context.Web;

        //    string listName = GlobalClass.ListNames.ContainsKey("ITARF") ? GlobalClass.ListNames["ITARF"] : "";

        //    if (listName == "")
        //    {
        //        return 0;
        //    }

        //    int rowId = Convert.ToInt32(form["FormSrId"]);
        //    try
        //    {
        //        List list = _context.Web.Lists.GetByTitle(listName);
        //        ListItem newItem = list.GetItemById(rowId);
        //        newItem["CabinsScreenAppByCCO"] = form["chkCabinsScreenForCCO"];
        //        newItem["NewMeetingRoomSetupAppByCCO"] = form["chkNewMeetingRoomSetupForCCO"];
        //        newItem["CabinsScreenAppByHOD"] = form["chkCabinsScreenForHOD"];
        //        newItem["NewMeetingRoomSetupAppByHOD"] = form["chkNewMeetingRoomSetupForHOD"];
        //        newItem["CabinsScreenAppByED"] = form["chkCabinsScreenForED"];
        //        newItem["NewMeetingRoomSetupAppByED"] = form["chkNewMeetingRoomSetupForED"];
        //        newItem.Update();
        //        _context.Load(newItem);
        //        _context.ExecuteQuery();

        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //        return 0;
        //    }
        //    return 1;
        //}
    }
}
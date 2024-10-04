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
    public class CourierRequestDAL
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
        /// CRF-It is used for Saving data.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> SaveCourierRequestForm(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            string formShortName = "CRF";
            string formName = "Courier Request Form";
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
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherCostcenterCode"]);
                    }
                    else if (otherEmpType == "Others")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherNewEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherNewCostcenterCode"]);
                    }
                }

                var response = await GetApprovalCourierRequestForm(user, requestSubmissionFor, txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo);

                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }

                var approverIdList = response.Model;
                int formId = 0;
                int FormId = Convert.ToInt32(form["FormId"]);
                int AppRowId = Convert.ToInt32(form["AppRowId"]);
                bool IsResubmit = FormId == 0 ? false : true;

                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 7;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "CourierRequest";
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
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 7;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "CourierRequest";
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

                List CourierRequestList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = CourierRequestList.AddItem(itemCreateInfo);

                newItem["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newItem["EmployeeType"] = form["chkEmployeeType"];
                var empType = form["chkEmployeeType"];
                newItem["ExternalOrganizationName"] = form["txtExternalOrganizationName"] ?? "";
                newItem["EmployeeCode"] = form["txtEmployeeCode"];
                if (empType == "External")
                {
                    newItem["EmployeeDesignation"] = "Team Member";
                }
                else
                {
                    newItem["EmployeeDesignation"] = form["ddEmpDesignation"];// DropDown selection
                }
                newItem["EmployeeLocation"] = form["ddEmpLocation"];
                newItem["EmployeeCCCode"] = form["txtCostcenterCode"];
                newItem["EmployeeUserId"] = form["txtUserId"];
                newItem["EmployeeName"] = form["txtEmployeeName"];
                newItem["EmployeeDepartment"] = form["txtDepartment"];
                newItem["EmployeeContactNo"] = form["txtContactNo"];
                newItem["EmployeeEmailId"] = user.Email;
                //Other Employee Details
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        newItem["OnBehalfOption"] = otherEmpType;
                        newItem["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                        newItem["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";
                        var otherEmployeeType = form["chkOtherEmployeeType"];
                        if (otherEmployeeType == "External")
                        {
                            newItem["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newItem["OtherEmployeeDesignation"] = form["ddOtherEmpDesignation"] ?? "";// DropDown selection
                        }
                        newItem["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? ""; //Dropdown selection
                        newItem["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? ""; //
                        newItem["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? ""; //SharePoint user Id
                        newItem["OtherEmployeeDepartment"] = form["txtOtherDepartment"] ?? "";
                        newItem["OtherEmployeeContactNo"] = form["txtOtherContactNo"] ?? "";
                        newItem["OtherEmployeeEmailId"] = form["txtOtherEmailId"] ?? "";
                        newItem["OtherEmployeeType"] = form["chkOtherEmployeeType"] ?? "";
                        newItem["OtherExternalOrganizationName"] = form["txtOtherExternalOrganizationName"] ?? "";
                    }
                    else
                    {
                        newItem["OnBehalfOption"] = otherEmpType;
                        newItem["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                        newItem["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";
                        var otherNewEmployeeType = form["chkOtherNewEmployeeType"];
                        if (otherNewEmployeeType == "External")
                        {
                            newItem["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newItem["OtherEmployeeDesignation"] = form["ddOtherNewEmpDesignation"] ?? "";// DropDown selection
                        }
                        newItem["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? ""; //Dropdown selection
                        newItem["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? ""; //
                        newItem["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? ""; //SharePoint user Id
                        newItem["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                        newItem["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                        newItem["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                        newItem["OtherEmployeeType"] = form["chkOtherNewEmployeeType"] ?? "";
                        newItem["OtherExternalOrganizationName"] = form["txtOtherNewExternalOrganizationName"] ?? "";
                    }
                }

                //Transaction
                newItem["ConsignmentType"] = form["drpConsignmentType"];
                newItem["CourierType"] = form["drpCourierType"];
                newItem["AddressofConsignee"] = form["txtAddressofConsignee"];
                newItem["AddressofReceiver"] = form["txtAddressofReceiver"];
                newItem["WeightDimension"] = form["chkWD"];
                newItem["WeightDimensionIn"] = form["txtWD"];
                newItem["BusinessNeed"] = form["txtBusinessNeed"];

                newItem["FormID"] = formId;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();

                RowId = newItem.Id;
                result.Status = 200;
                result.Message = formId.ToString();

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

                    approvalMasteritem["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                    approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                    approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                    approvalMasteritem.Update();
                    _context.Load(approvalMasteritem);
                    _context.ExecuteQuery();
                }

                //Data Row ID Update in Forms List
                List formslist = _context.Web.Lists.GetByTitle("Forms");
                ListItem newItemRow = formslist.GetItemById(formId);
                newItemRow.RefreshLoad();
                _context.ExecuteQuery();
                newItemRow["DataRowId"] = rowid;
                newItemRow.Update();
                _context.Load(newItemRow);
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
        /// CRF-It is used for calculating the approvers.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalCourierRequestForm(UserData user, string requestSubmissionFor, long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_CourierRequestForm", con);
                if (requestSubmissionFor == "Self")
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostCenterNo));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtOnBehalfEmpId));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtOnBehalfCostCenterNo));
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
                        app.ApprovalLevel = Convert.ToInt32(ds.Tables[0].Rows[i]["approvalLevel"]);
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
        public async Task<dynamic> ViewCourierRequestFormData(int rowId, int formId)
        {
            dynamic CRFDataList = new ExpandoObject();
            //GetAttachmentUTL(rowId);
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

                //        var response = await client.GetAsync("_api/web/lists/GetByTitle('CourierRequestForm')/items?$select=*,FormID/ID"
                //+ "&$filter=(ID eq '" + rowId + "')&$expand=FormID");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('CourierRequestForm')/items?$select=*"
             + "&$filter=(ID eq '" + rowId + "')");

                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var CRFResult = JsonConvert.DeserializeObject<CourierRequestModel>(responseText, settings);
                    CRFDataList.one = CRFResult.crflist.crfData;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,Designation,Level,ApproverName,ApproverUserName,Logic,TimeStamp,IsActive,Comment,NextApproverId,"
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

                    //AD Code
                    ListDAL obj = new ListDAL();
                    for (int i = 0; i < items.Count; i++)
                    {
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
                        dynamic data3 = Json.Decode(responseText2);
                        CRFDataList.two = data3.d.results;
                        CRFDataList.three = items;
                    }

                }
                else
                {
                    CRFDataList.two = new List<string>();
                    CRFDataList.three = new List<string>();
                }
                return CRFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return CRFDataList;
            }
        }

        public async Task<int> WeighingUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("CRF") ? GlobalClass.ListNames["CRF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetCourierInwardNumber", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                long CourierInwardRegisterNo = 0;

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        CourierInwardRegisterNo = Convert.ToInt64(ds.Tables[0].Rows[i]["InwardNumber"]);
                    }
                }
                else
                {
                    return 0;
                }
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                newItem["CourierInwardRegisterNo"] = CourierInwardRegisterNo;

                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }
    }
}
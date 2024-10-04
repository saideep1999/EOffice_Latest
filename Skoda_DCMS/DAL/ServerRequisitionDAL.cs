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
    public class ServerRequisitionDAL
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

        public async Task<ResponseModel<object>> SaveServerRequisition(System.Web.Mvc.FormCollection form, UserData user)
        {
            //dynamic result = new ExpandoObject();
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "SRCF";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List name not found.";
                return result;
            }

            int formId = 0;
            int FormId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            bool IsResubmit = FormId == 0 ? false : true;
            try
            {
                var selfOnBehalf = form["drpRequestSubmissionFor"];
                var onBehalfEmail = form["hiddentxtEmail"];

                var hiddenITServerManagerApprover1 = "";
                var hiddenITServerManagerApprover2 = "";
                var hiddenITServerManagerApprover3 = "";
                var hiddenHeadITInfrastructureApprover = "";
                var hiddenITLicenseManagerApprover1 = "";
                var hiddenITLicenseManagerApprover2 = "";
                string servertype = form["chkServerCreationType"];
                string serverlocation = form["chkServerLocation"];
                long txtEmployeeCode = Convert.ToInt32(form["txtEmployeeCode"]);
                long txtCostCenterNo = Convert.ToInt32(form["txtCostCenterNo"]);
                long txtOnBehalfEmpId = 0;
                long txtOnBehalfCostCenterNo = 0;
                if (selfOnBehalf == "OnBehalf")
                {
                    txtOnBehalfEmpId = Convert.ToInt32(form["txtOnBehalfEmpId"]);
                    txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOnBehalfCostCenterNo"]);
                }
                var response = GetApprovalSRCF(user, selfOnBehalf, onBehalfEmail, hiddenITServerManagerApprover1, hiddenITServerManagerApprover2, hiddenITServerManagerApprover3, hiddenHeadITInfrastructureApprover, hiddenITLicenseManagerApprover1, hiddenITLicenseManagerApprover2, txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo, servertype, serverlocation);
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
                    item["FormName"] = "Server Requisition Form";
                    item["UniqueFormName"] = "SRCF";
                    item["FormParentId"] = 29;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ServerRequisition";
                    item["BusinessNeed"] = form["txtReasonforServer"];
                    if (selfOnBehalf == "OnBehalf")
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
                    ListItem item = list.GetItemById(FormId);
                    item["FormName"] = "Server Requisition Form";
                    item["UniqueFormName"] = "SRCF";
                    item["FormParentId"] = 29;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "ServerRequisition";
                    item["BusinessNeed"] = form["txtReasonforServer"];
                    if (selfOnBehalf == "OnBehalf")
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
                if (FormId == 0)
                {
                    newRow["TriggerCreateWorkflow"] = "No";
                }
                else
                {
                    newRow["TriggerCreateWorkflow"] = "Yes";
                }
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["EmployeeName"] = form["txtEmployeeName"];
                newRow["Department"] = form["txtDepartment"];
                newRow["EmployeeCode"] = form["txtEmployeeCode"];
                newRow["CostCenterNo"] = form["txtCostCenterNo"];
                newRow["UserID"] = form["txtUserID"];
                newRow["TypeofEmployee"] = form["chkEmployeeType"];
                newRow["CompanyName"] = form["txtCompanyName"];
                newRow["Designation"] = form["ddEmpDesignation"];
                newRow["ContactNo"] = form["txtContactNo"];
                newRow["SelfTelephone"] = form["txtTelephoneNumber"];
                newRow["EmployeeEmailId"] = form["txtEmail"];
                newRow["Location"] = form["ddEmpLocation"];
                if (selfOnBehalf == "OnBehalf")
                {
                    newRow["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                    newRow["OnBehalfEmployeeNumber"] = form["txtOnBehalfEmpId"];
                    newRow["OnBehlafDepartment"] = form["txtOnBehalfDepartment"];
                    newRow["OnBehalfCostCenterNo"] = form["txtOnBehalfCostCenterNo"];
                    newRow["OnBehalfUserID"] = form["txtOnBehalfUserID"];
                    newRow["OnBehalfdesignation"] = form["txtOnBehalfdesignation"];
                    newRow["OnBehalfMobile"] = form["txtOnBehalfMobile"];
                    newRow["OnBehalfTelephone"] = form["txtOnBehalfTelephone"];
                    newRow["OtherEmployeeEmailId"] = form["txtOnBehalfEmail"];
                    newRow["OnBehalfLocation"] = form["ddOnBehalfLocation"];
                    newRow["OnBehalfTypeofEmployee"] = form["chkOnBehalfEmployeeType"];
                    newRow["OnBehalfCompanyName"] = form["txtOnBehalfCompanyName"];
                }

                newRow["ServerCreationType"] = form["chkServerCreationType"];
                newRow["ServerOwnerName"] = form["txtServerOwnerName"];
                newRow["AdminAccount"] = form["txtAdminAccount"];
                newRow["ServerRole"] = form["txtServerRole"];
                newRow["ServerEnvironment"] = form["chkServerEnvironment"];
                newRow["ServerHardware"] = form["chkServerHardware"];
                newRow["RAM"] = form["txtRAM"];
                newRow["NoofCPU"] = form["txtNoOfCPU"];
                newRow["NoofNetworkPorts"] = form["txtNoOfNetworkPorts"];
                newRow["StorageSize"] = form["txtSize"];
                newRow["TwoRoom"] = form["chkRoom"];
                newRow["OperatingSystem"] = form["chkOperatingSystem"];
                if (form["chkOperatingSystem"] == "Others OS")
                {
                    newRow["OSName"] = form["txtOSNameOther"];
                }
                else if (form["chkOperatingSystem"] == "Windows")
                {
                    newRow["OSName"] = form["txtOSName"];
                }
                else if (form["chkOperatingSystem"] == "Linux")
                {
                    newRow["OSName"] = form["txtOSLinuxName"];
                }

                newRow["OSEdition"] = form["txtOSEdition"];
                newRow["Architecture"] = form["txtArchitecture"];
                newRow["DBName"] = form["txtDBName"];
                newRow["DBEdition"] = form["txtDBEdition"];
                newRow["ServerCriticality"] = form["txtServerCriticality"];
                var chkServerType = form["chkServerType"].Replace(",", "");
                if (chkServerType == "Temporary")
                {
                    newRow["ServerType"] = chkServerType;
                    newRow["Temporaryfrom"] = form["txtTemporaryfrom"];
                    newRow["TemporaryTo"] = form["txtTemporaryTo"];
                }
                else
                {
                    newRow["ServerType"] = chkServerType;
                }

                if (form["chkServerBackup"] == "Yes")
                {
                    newRow["BackupRequired"] = "Yes";
                }
                else if (form["chkServerBackup"] == "No")
                {
                    newRow["BackupRequired"] = "No";
                }
                else
                {
                    newRow["BackupRequired"] = "No";
                }

                newRow["WeekNo"] = form["txtWoM"];
                newRow["Day"] = form["txtDoW"];
                newRow["TimeFrame"] = form["txtTimeFrame"];
                newRow["ReasonForServerRequisition"] = form["txtReasonforServer"];

                newRow["ServerLocation"] = form["chkServerLocation"];
                newRow["HostName"] = form["txtHostName"];
                newRow["IPAddress"] = form["txtIPAddress"];
                newRow["ServerCpu"] = form["chkExServerCpuN"];
                newRow["ServerMemory"] = form["chkExServerMemoryN"];
                newRow["ServerDisk"] = form["chkExServerDiskN"];
                newRow["ServerLan"] = form["chkExServerLANN"];
                newRow["ServerOwn"] = form["chkExServerOwnerChangeN"];
                newRow["CurrentCpu"] = form["txtCurCpu"];
                newRow["IncrementCpu"] = form["txtIncCpu"];
                newRow["TotalCpu"] = form["txtTotalCpu"];
                newRow["CurrentMemory"] = form["txtCurMemory"];
                newRow["IncrementMemory"] = form["txtIncMemory"];
                newRow["TotalMemory"] = form["txtTotalMemory"];
                newRow["CurrentDisk"] = form["txtCurDisk"];
                newRow["IncrementDisk"] = form["txtIncDisk"];
                newRow["TotalDisk"] = form["txtTotalDisk"];
                newRow["CurrentLan"] = form["txtCurLan"];
                newRow["IncrementLan"] = form["txtIncLan"];
                newRow["TotalLan"] = form["txtTotalLan"];
                newRow["CurrentOwner"] = form["txtCurOwner"];
                newRow["NewOwner"] = form["txtNewOwner"];

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                result.Status = 200;
                result.Message = formId.ToString();

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

                    approvalMasteritem["Level"] = approverIdList[i].ApprovalLevel;
                    approvalMasteritem["Logic"] = approverIdList[i].Logic;
                    approvalMasteritem["Designation"] = approverIdList[i].Designation;

                    approvalMasteritem["ApproverStatus"] = "Pending";

                    approvalMasteritem["RunWorkflow"] = "No";

                    approvalMasteritem["BusinessNeed"] = form["txtReasonforServer"];

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
                string completeFormName = await listDal.GetFormNameByUniqueName(formShortName);

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    FormName = completeFormName,
                    Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault()
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);

                ;
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

        public async Task<dynamic> ViewApprovalSRCF(int rowId, int formId)
        {
            dynamic SRCFDataList = new ExpandoObject();

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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ServerRequisitionForm')/items?$select=ID,RequestSubmissionFor,EmployeeName,Department,EmployeeCode,CostCenterNo,UserID,TypeofEmployee,CompanyName,Designation,ContactNo,EmployeeEmailId,SelfTelephone,OtherEmployeeName,OnBehalfEmployeeNumber,OnBehalfTypeofEmployee,OnBehalfCompanyName,OnBehlafDepartment,OnBehalfCostCenterNo,OnBehalfUserID,OnBehalfdesignation,OnBehalfMobile,OnBehalfTelephone,OtherEmployeeEmailId,ServerOwnerName,AdminAccount,ServerRole,ServerEnvironment,"
                + "ServerHardware,RAM,NoofCPU,NoofNetworkPorts,StorageSize,TwoRoom,NonTwoRoom,OperatingSystem,OSName,OSEdition,Architecture,DBName,DBEdition,ServerCriticality,WeekNo,Day,TimeFrame,"
                + "ServerType,Temporaryfrom,TemporaryTo,BackupRequired,ReasonForServerRequisition,ServerCreationType,Location,OnBehalfLocation,ServerLocation,HostName,IPAddress,ServerCpu,ServerMemory,ServerDisk,ServerLan,ServerOwn,CurrentCpu,IncrementCpu,TotalCpu,CurrentMemory,IncrementMemory,TotalMemory,CurrentDisk,IncrementDisk,TotalDisk,CurrentLan,IncrementLan,TotalLan,CurrentOwner,NewOwner"
                + "&$filter=(ID eq '" + rowId + "')");
                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var SRCFResult = JsonConvert.DeserializeObject<ServerRequisitionModel>(responseText, settings);
                    SRCFDataList.one = SRCFResult.srfflist.srfData;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,Designation,Level,Logic,IsActive,ApproverName,ApproverUserName,Comment,NextApproverId,TimeStamp,Designation,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var items = modelData.Node.Data;
                var names = new List<string>();
                var idString = "";
                var responseText3 = "";


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
                    SRCFDataList.two = data2.d.results;
                    SRCFDataList.three = items;
                }

                return SRCFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return SRCFDataList;
            }
        }

        public ResponseModel<List<ApprovalMatrix>> GetApprovalSRCF(UserData user, string selfOnBehalf, string onBehalfEmail, string hiddenITServerManagerApprover1, string hiddenITServerManagerApprover2, string hiddenITServerManagerApprover3, string hiddenHeadITInfrastructureApprover, string hiddenITLicenseManagerApprover1, string hiddenITLicenseManagerApprover2, long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo, string servertype,string serverlocation)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ServerRequisitionFormApproval", con);
                if (selfOnBehalf == "OnBehalf")
                {
                    string OtherEmployeeEmailId = onBehalfEmail.TrimEnd(',');
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtOnBehalfEmpId));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtOnBehalfCostCenterNo));
                    cmd.Parameters.Add(new SqlParameter("@ITServerManagerHead1", hiddenITServerManagerApprover1));
                    cmd.Parameters.Add(new SqlParameter("@ITServerManagerHead2", hiddenITServerManagerApprover2));
                    cmd.Parameters.Add(new SqlParameter("@ITServerManagerHead3", hiddenITServerManagerApprover3));
                    cmd.Parameters.Add(new SqlParameter("@ITInfrastructure", hiddenHeadITInfrastructureApprover));
                    cmd.Parameters.Add(new SqlParameter("@ITLicenseManager1", hiddenITLicenseManagerApprover1));
                    cmd.Parameters.Add(new SqlParameter("@ITLicenseManager2", hiddenITLicenseManagerApprover2));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostCenterNo));
                    cmd.Parameters.Add(new SqlParameter("@ITServerManagerHead1", hiddenITServerManagerApprover1));
                    cmd.Parameters.Add(new SqlParameter("@ITServerManagerHead2", hiddenITServerManagerApprover2));
                    cmd.Parameters.Add(new SqlParameter("@ITServerManagerHead3", hiddenITServerManagerApprover3));
                    cmd.Parameters.Add(new SqlParameter("@ITInfrastructure", hiddenHeadITInfrastructureApprover));
                    cmd.Parameters.Add(new SqlParameter("@ITLicenseManager1", hiddenITLicenseManagerApprover1));
                    cmd.Parameters.Add(new SqlParameter("@ITLicenseManager2", hiddenITLicenseManagerApprover2));
                }
                cmd.Parameters.Add(new SqlParameter("@servertype", servertype));
                cmd.Parameters.Add(new SqlParameter("@serverlocation", serverlocation));

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

                bool appLevelCheck = common.ApprovalLevlCheck(appList);

                if (appLevelCheck == false)
                {
                    return new ResponseModel<List<ApprovalMatrix>>
                    { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Data missing for level 1 approver." }; ;
                }

                appList = common.CallAssistantAndDelegateFunc(appList);

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.Timeout = TimeSpan.FromSeconds(10);
                var count = appList.Count;

                if (adCode.ToLower() == "yes")
                {
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
                }

                if (appList.CheckApproverUserName() == 0)
                {
                    return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Approver User Id is missing." }; ;
                }

                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Status = 200, Model = null, Message = "Error while fetching appprovers." };
            }

        }


        public async Task<dynamic> GetOSName()
        {

            SRFResults srfData = new SRFResults();
            dynamic result = srfData;
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('OSNameMaster')/items?$select=ID,OSId,OSystemName,IsActive"
                  + "&$filter=(IsActive eq '" + 1 + "')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<ServerRequisitionModel>(responseText);
                    srfData = locResult.srfflist;
                }
                result = srfData.srfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        public async Task<dynamic> GetDBName()
        {

            SRFResults srfData = new SRFResults();
            dynamic result = srfData;
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('DatabaseNameMaster')/items?$select=ID,DBId,DataBaseName,IsActive"
                  + "&$filter=(IsActive eq '" + 1 + "')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<ServerRequisitionModel>(responseText);
                    srfData = locResult.srfflist;
                }
                result = srfData.srfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        public async Task<dynamic> GetLinuxOSName()
        {

            SRFResults srfData = new SRFResults();
            dynamic result = srfData;
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('OSLinuxMaster')/items?$select=ID,OSLinuxId,OSLinuxName,IsActive"
                  + "&$filter=(IsActive eq '" + 1 + "')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<ServerRequisitionModel>(responseText);
                    srfData = locResult.srfflist;
                }
                result = srfData.srfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }
    }
}
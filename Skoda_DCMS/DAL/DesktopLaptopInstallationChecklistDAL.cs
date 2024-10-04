using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class DesktopLaptopInstallationChecklistDAL: CommonDAL
    {
        public async Task<ResponseModel<object>> SaveData(DesktopLaptopInstallationChecklistModel data)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            try
            {
                ClientContext _context = new ClientContext(new Uri(conString));
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                Web web = _context.Web;
                int rowId = 0; //User to store newly inserted data id
                string formShortName = "DLIC";
                string formName = "Desktop Laptop Installation Checklist Form";
                string listName = GlobalClass.ListNames.ContainsKey("DLIC") ? GlobalClass.ListNames["DLIC"] : "";
                if (listName == "")
                {
                    result.Status = 500;
                    result.Message = "List not found.";
                    return result;
                }
                int formId = Convert.ToInt32(data.FormId);
                bool IsResubmit = formId == 0 ? false : true;
                int AppRowId = Convert.ToInt32(data.AppRowId);
                var requestSubmissionFor = data.RequestSubmissionFor;
                var otherEmpType = data.OnBehalfOption ?? "";
                bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(data.OtherEmployeeCode) : Convert.ToInt64(data.OtherNewEmployeeCode));
                string empName = isSelf ? user.EmployeeName : (isSAVWIPL ? Convert.ToString(data.OtherEmployeeName) : Convert.ToString(data.OtherNewEmployeeName));
                var empLocName = isSelf ? Convert.ToString(data.EmployeeLocation) : (isSAVWIPL ? Convert.ToString(data.OtherEmployeeLocation) : Convert.ToString(data.OtherNewEmpLocation));
                var locations = await new ListDAL().GetLocations();
                if (locations == null && locations.Count <= 0)
                {
                    result.Status = 500;
                    result.Message = "There were some issue fetching Location data.";
                    return result;
                }
                var locObj = locations.Find(x => x.LocationName == empLocName);
                if (locObj == null)
                {
                    result.Status = 500;
                    result.Message = "Could not found location data.";
                    return result;
                }
                long empLoc = locObj.LocationId == 1 || locObj.LocationId == 3 ? locObj.LocationId : 2;
                long T_empNum = Convert.ToInt64(data.T_EmployeeCode);
                string T_empName = Convert.ToString(data.T_EmployeeName);
                var response = await GetDLICApprovers(empNum, T_empNum, empLoc);
                if (response.Status != 200 && response.Model.Count < 2)
                {
                    result.Status = 500;
                    result.Message = "Approver "
                        + (response.Model.FirstOrDefault(x => x.EmpNumber == empNum) != null ? T_empName : empName)
                        + " data not found."; //response.Message;//(response.Model.Any(x => x.EmpNumber == empNum) ? T_empName : empName) + 
                    return result;
                }
                else if (response.Status != 200)
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approvers = response.Model;
                if (formId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Desktop Laptop Installation Checklist Form";
                    item["UniqueFormName"] = "DLIC";
                    item["FormParentId"] = 31;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "DesktopLaptopInstallationChecklist";
                    item["BusinessNeed"] = data.BusinessNeed ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = data.EmployeeLocation;
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = data.OtherEmployeeLocation;
                        }
                        else
                        {
                            item["Location"] = data.OtherNewEmpLocation;
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
                    ListItem item = list.GetItemById(formId);
                    item["FormName"] = "Desktop Laptop Installation Checklist Form";
                    item["UniqueFormName"] = "DLIC";
                    item["FormParentId"] = 31;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "DesktopLaptopInstallationChecklist";
                    item["BusinessNeed"] = data.BusinessNeed ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = data.EmployeeLocation;
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = data.OtherEmployeeLocation;
                        }
                        else
                        {
                            item["Location"] = data.OtherNewEmpLocation;
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

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(web, data, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                var newRow = userDetailsResponse.Model;
                newRow["FormID"] = formId;
                newRow["T_EmployeeName"] = data.T_EmployeeName;
                newRow["T_EmployeeCode"] = data.T_EmployeeCode;
                newRow["T_UserId"] = data.T_UserId;
                newRow["T_CostCenter"] = data.T_CostCenter;
                newRow["TicketNum"] = data.TicketNum;
                newRow["Make"] = data.Make;
                newRow["Model"] = data.Modal;
                newRow["SerialNumber"] = data.SerialNumber;
                newRow["HostName"] = data.HostName;
                newRow["IsIDoCompleted"] = data.IsIDoCompleted;
                newRow["IsBitLockerCompleted"] = data.IsBitLockerCompleted;
                newRow["IsAntivirusUpdated"] = data.IsAntivirusUpdated;
                newRow["IsProxyConfig"] = data.IsProxyConfig;
                newRow["IsUSBBluetoothDisabled"] = data.IsUSBBluetoothDisabled;
                newRow["IsUserIdConfigured"] = data.IsUserIdConfigured;
                newRow["IsOutLookConfiguration"] = data.IsOutLookConfiguration;
                newRow["IsFirEyeAgent"] = data.IsFirEyeAgent;
                newRow["IsEncryptedEmailConfiguration"] = data.IsEncryptedEmailConfiguration;
                newRow["IsPKIDigitSignCert"] = data.IsPKIDigitSignCert;
                newRow["IsPrinterConfiguration"] = data.IsPrinterConfiguration;
                newRow["IsVPNConfigurationDone"] = data.IsVPNConfigurationDone;
                newRow["IsSharedFolderAccessDone"] = data.IsSharedFolderAccessDone;
                newRow["IsDataRestored"] = data.IsDataRestored;
                newRow["IsNessusAgent"] = data.IsNessusAgent;
                newRow["IsClassificationAddInForOffice"] = data.IsClassificationAddInForOffice;
                newRow["IsUsedMachineToBeClean"] = data.IsUsedMachineToBeClean;
                newRow["IsOneDriveConfiguration"] = data.IsOneDriveConfiguration;
                newRow["IsLocalApps"] = data.IsLocalApps;
                newRow["IsOthers"] = data.IsOthers;
                newRow["IsVirtualSmartCard"] = data.IsVirtualSmartCard;
                newRow["IsAgreeAppInstallation"] = data.IsAgreeAppInstallation;
                newRow["OthersText"] = data.OthersText;
                newRow["BusinessNeed"] = data.BusinessNeed;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                int RowId = newRow.Id;
                result.Status = 200;
                result.Message = formId.ToString();


                var approvalResponse = await SaveApprovalMasterData(approvers, data.BusinessNeed ?? "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }

                //Data Row ID Update in Forms List
                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, RowId);
                if (userList == null || userList.Count == 0)
                {
                    result.Status = 500;
                    result.Message = "There were some issue fetching Submitter Details.";
                    return result;
                }
                foreach (var approver in approvers)
                {
                    var userData = new UserData()
                    {
                        EmployeeName = approver.FName + " " + approver.LName,
                        Email = approver.EmailId,
                        ApprovalLevel = approver.ApprovalLevel,
                        IsApprover = true
                    };
                    userList.Add(userData);
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

        private async Task<ResponseModel<List<ApprovalMatrix>>> GetDLICApprovers(long empNum, long filledForEmpNum, long empLoc)
        {
            List<ApprovalMatrix> list = new List<ApprovalMatrix>();
            try
            {
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                SqlConnection con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetDLICApprovers", con);
                sqlCommand.Parameters.Add(new SqlParameter("@empNum", empNum));
                sqlCommand.Parameters.Add(new SqlParameter("@filledForEmpNum", filledForEmpNum));
                sqlCommand.Parameters.Add(new SqlParameter("@locationId", empLoc));
                sqlCommand.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = sqlCommand;
                con.Open();
                adapter.Fill(data);
                con.Close();

                if (data.Tables[0].Rows.Count > 0 && data.Tables[0] != null)
                {
                    for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                    {
                        ApprovalMatrix app = new ApprovalMatrix();
                        app.EmpNumber = Convert.ToInt32(data.Tables[0].Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(data.Tables[0].Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(data.Tables[0].Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(data.Tables[0].Rows[i]["EmailId"]);
                        app.Designation = Convert.ToString(data.Tables[0].Rows[i]["desg"]);
                        app.ApprovalLevel = (int)data.Tables[0].Rows[i]["approvalLevel"];
                        app.Logic = Convert.ToString(data.Tables[0].Rows[i]["logic"]);
                        if (data.Tables[0].Columns.Contains("Contents"))
                            app.ExtraDetails = Convert.ToString(data.Tables[0].Rows[i]["Contents"]);
                        if (data.Tables[0].Columns.Contains("LogicId"))
                            app.LogicId = Convert.ToInt64(data.Tables[0].Rows[i]["LogicId"]);
                        if (data.Tables[0].Columns.Contains("LogicWith"))
                            app.LogicWith = Convert.ToInt64(data.Tables[0].Rows[i]["LogicWith"]);
                        if (data.Tables[0].Columns.Contains("RelationId"))
                            app.RelationId = Convert.ToInt64(data.Tables[0].Rows[i]["RelationId"]);
                        if (data.Tables[0].Columns.Contains("RelationWith"))
                            app.RelationWith = Convert.ToInt64(data.Tables[0].Rows[i]["RelationWith"]);
                        appList.Add(app);
                    }
                }
                if (data.Tables[0].Rows.Count < 2)
                {
                    return new ResponseModel<List<ApprovalMatrix>> {
                        Model = appList,
                        Status = 500,
                        Message = $"Employee Number {(appList.Any(x => x.EmpNumber == empNum) ? filledForEmpNum : empNum)} approver data not found."
                    };
                }
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }

        public async Task<dynamic> GetDLICDetails(int rowId, int formId)
        {
            dynamic DLICData = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client1 = new HttpClient(handler);
                client1.BaseAddress = new Uri(conString);
                client1.DefaultRequestHeaders.Accept.Clear();
                client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('DesktopLaptopInstallationCheckListForm')/items?$select=*" +
                    "&$filter=(ID eq '" + rowId + "')");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var data = JsonConvert.DeserializeObject<DLICModel>(responseText1, settings);
                    DLICData.one = data.data.list;
                }
                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                DLICData.two = r1.Model;
                DLICData.three = r2.Model;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return DLICData;
        }
    }
}
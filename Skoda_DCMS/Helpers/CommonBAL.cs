using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.DAL;
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
using System.Web.Helpers;

namespace Skoda_DCMS.Helpers
{
    public class CommonBAL
    {

        //public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        private JsonConverter[] settings;

        public async Task<int> FormStatus(int DataRowId, int formID)
        {
            Task.Delay(5000).Wait();
            var formData = new List<FormData>();

            var handler = new HttpClientHandler();
            GlobalClass gc = new GlobalClass();
            var currentUser = gc.GetCurrentUser();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            //ClientContext _context = new ClientContext(new Uri(conString));
            ////_context.Credentials = new NetworkCredential(spUsername, spPass);
            ////_context.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
            //_context.Credentials = new NetworkCredential(currentUser.UserName, currentUser.Password);

            try
            {
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,FormName,Status&$filter=(DataRowId eq '" + DataRowId + "' and ID eq '" + formID + "')")).Result;
                var responseText2 = await response2.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText2))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText2);
                    formData = modelResult.Data.Forms;
                }

                if (formData[0].Status == "Approved")
                {
                    // EmailServiceController obj = new EmailServiceController();
                    // var result = obj.PrepareAndSendFinalEmailToHelpDesk(DataRowId, formData[0].Id, formData[0].FormName, null, currentUser);

                }
                return 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }

        public async Task<string> GetEmailBodyForITHelpDesk(int rowId, int FormID, string FormName, UserData user)
        {
            string body = string.Empty;
            string employeeLocation = string.Empty;
            string applicationURL = ConfigurationManager.AppSettings["AppUrl"];
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + "Team" + ",</span> <br/>";

            body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";


            //Get request Details
            switch (FormName)
            {

                case "Smart Phone Requisition Form":
                    body = await CreateEmailBodyForSmartphoneForm(rowId, FormID, FormName, body);
                    break;
                case "Software Requisition Form":
                    body = await CreateEmailBodyForSoftwareRequisitionForm(rowId, FormID, FormName, body);
                    break;
                case "IT Asset Requisition Form":
                    body = await CreateEmailBodyForITAssetForm(rowId, FormID, FormName, body);
                    break;
                case "IT Clearance Form":
                    body = await CreateEmailBodyForITClearance(rowId, FormID, FormName, body);
                    break;
                case "Ganesh User Id Creation Form":
                    body = await CreateEmailBodyForGaneshForm(rowId, FormID, FormName, body);
                    break;
                case "Data Backup Restore Form":
                    body = await CreateEmailBodyForDataBackupRestoreForm(rowId, FormID, FormName, body);
                    break;
                case "KSRM-User Id Creation Form":
                    body = await CreateEmailBodyForKSRMForm(rowId, FormID, FormName, body);
                    break;
                case "Internet Access Form":
                    body = await CreateEmailBodyForInternetAccessForm(rowId, FormID, FormName, body);
                    break;
                case "Shared Folder Form":
                    body = await CreateEmailBodyForSharedFolderForm(rowId, FormID, FormName, body);
                    break;
                case "Conflict Of Interest Form":
                    body = await CreateEmailBodyForConflictOfInterestForm(rowId, FormID, FormName, body);
                    break;

                default:
                    Console.WriteLine("Nothing");
                    break;
            }

            var approver = await GetApprovalDetails_SQL(rowId, FormID, user);

            if (approver != null && approver.Count > 0)
            {
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";

                for (int i = 0; i < approver.Count; i++)
                {
                    if (approver[i].ApproverStatus == "Approved")
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver[i].UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver[i].Modified + "</td></tr>";
                        //body = body + "<tr><td>" + "Approver Role: " + "Approver Role" + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver[i].Comment + "</td></tr>";
                    }
                    else if (approver[i].ApproverStatus == "Rejected")
                    {

                        body = body + "<tr><td>" + "Rejected By: " + approver[i].UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Rejected On: " + approver[i].Modified + "</td></tr>";
                        //body = body + "<tr><td>" + "Role: " + "Approver Role" + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver[i].Comment + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }


            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fullfillment Task Details</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: " + "VWIPLP - IT Service Desk" + "</td></tr>";
            body = body + "<tr><td>" + "Assigned To: " + "itsupport@skoda-vw.co.in" + "</td></tr>";
            body = body + "<tr><td>" + "Comments: " + "" + "</td></tr>";
            body = body + "</table><br>";

            return body;
        }
        public async Task<string> GetEmployeeLocationFromRequest(int rowId, int FormID, string FormName)
        {
            string result = string.Empty;
            var formData = new List<FormData>();

            var handler = new HttpClientHandler();
            GlobalClass gc = new GlobalClass();
            var currentUser = gc.GetCurrentUser();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            //ClientContext _context = new ClientContext(new Uri(conString));
            ////_context.Credentials = new NetworkCredential(spUsername, spPass);
            ////_context.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
            //_context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            try
            {
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,Location,FormName,Status&$filter=(DataRowId eq '" + rowId + "' and ID eq '" + FormID + "')")).Result;

                var responseText2 = await response2.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText2))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText2);
                    formData = modelResult.Data.Forms;
                    result = formData[0].Location;
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }

        }

        //public async Task<List<ITServiceDesk>> GetApprovalEmailDetails(int rowId, int FormId)
        //{
        //    try
        //    {

        //        List<ITServiceDesk> appList = new List<ITServiceDesk>();
        //        GlobalClass gc = new GlobalClass();
        //        var user = gc.GetCurrentUser();
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client2 = new HttpClient(handler);
        //        client2.BaseAddress = new Uri(conString);
        //        client2.DefaultRequestHeaders.Accept.Clear();
        //        client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

        //        var responseApproval = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,"
        //        + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + FormId + "')&$expand=FormId,Author")).Result;
        //        var responseTextApproval = await responseApproval.Content.ReadAsStringAsync();
        //        var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApproval, settings);

        //        if (modelData.Node.Data.Count > 0)
        //        {
        //            var client3 = new HttpClient(handler);
        //            client3.BaseAddress = new Uri(conString);
        //            client3.DefaultRequestHeaders.Accept.Clear();
        //            client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
        //            var items = modelData.Node.Data;
        //            var idString = "";
        //            for (int i = 0; i < items.Count; i++)
        //            {
        //                var id = items[i];//
        //                idString += $"Id eq '{id.ApproverId}' {(i != items.Count - 1 ? "or " : "")}";
        //                items[i].UserLevel = i + 1;//
        //            }

        //            var response = Task.Run(() => client2.GetAsync($"_api/web/SiteUserInfoList/items?$select=Id,Title,EMail&$filter=({idString})")).Result;
        //            var responseText = await response.Content.ReadAsStringAsync();


        //            if (!string.IsNullOrEmpty(responseText))
        //            {
        //                var ITServiceDeskContactResults = JsonConvert.DeserializeObject<ITServiceDeskContactModel>(responseText, settings);
        //                var appList1 = ITServiceDeskContactResults.itServiceDesklist.ITServiceDeskContactData;

        //                for (int i = 0; i < appList1.Count; i++)
        //                {
        //                    ITServiceDesk app = new ITServiceDesk();
        //                    app.id = Convert.ToInt32(appList1[i].id);
        //                    app.Title = Convert.ToString((appList1[i].Title));
        //                    app.EMail = Convert.ToString((appList1[i].EMail));
        //                    appList.Add(app);
        //                }
        //            }
        //        }
        //        return appList;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //        return null;
        //    }

        //}

        public async Task<List<ApprovalDataModel>> GetApprovalDetails(int rowId, int FormId, UserData currentUser)
        {
            try
            {
                List<ApprovalDataModel> appList = new List<ApprovalDataModel>();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseApproval = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverUserName,NextApproverId,TimeStamp,Designation,Level,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + FormId + "')&$expand=FormId,Author")).Result;
                var responseTextApproval = await responseApproval.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApproval, settings);

                if (modelData.Node.Data.Count > 0)
                {
                    var client3 = new HttpClient(handler);
                    client3.BaseAddress = new Uri(conString);
                    client3.DefaultRequestHeaders.Accept.Clear();
                    client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var items = modelData.Node.Data;
                    var idString = "";
                    var userList = new List<UserData>();

                    if (adCode.ToLower() == "yes")
                    {
                        //AD Code
                        ListDAL obj = new ListDAL();
                        for (int i = 0; i < items.Count; i++)
                        {
                            //string objectSid = currentUser.ObjectSid;
                            string approverId = items[i].ApproverUserName;
                            var userData = obj.GetApproverDetailsFromAD(approverId, currentUser);
                            //string appName = userData.EmployeeName;
                            if (userData != null)
                                userList.Add(userData);
                        }
                        //AD Code
                    }
                    //else
                    //{
                    //    //Local Code:- Sharepoint Code
                    //    for (int i = 0; i < items.Count; i++)
                    //    {
                    //        var id = items[i];
                    //        idString = $"Id eq '{id.ApproverId}'";
                    //        items[i].UserLevel = i + 1;//
                    //        var response = await client2.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + idString + ")");
                    //        var responseText = await response.Content.ReadAsStringAsync();

                    //        dynamic data4 = Json.Decode(responseText);

                    //        if (data4.Count != 0)
                    //        {
                    //            //names = new List<string>();
                    //            foreach (var name in data4.d.results)
                    //            {
                    //                names.Add(name.Title as string);
                    //            }
                    //        }
                    //    }
                    //    //Local Code:- Sharepoint Code

                    //}

                    if (items.Count == userList.Count)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i].UserName = userList[i].EmployeeName;
                            items[i].EmailId = userList[i].Email;
                        }
                    }

                    items = items.OrderBy(x => x.UserLevel).ToList();

                    if (!string.IsNullOrEmpty(responseTextApproval))
                    {
                        var approvalMasterModelResults = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApproval, settings);
                        var output = approvalMasterModelResults.Node.Data;

                        for (int i = 0; i < output.Count; i++)
                        {
                            ApprovalDataModel app = new ApprovalDataModel();
                            app.UserName = items[i].UserName;
                            app.Modified = output[i].Modified;
                            app.ApproverStatus = output[i].ApproverStatus;
                            app.Comment = output[i].Comment;
                            app.Designation = output[i].Designation;
                            app.TimeStamp = output[i].TimeStamp;
                            app.Level = output[i].Level;
                            app.EmailId = items[i].EmailId;

                            appList.Add(app);
                        }
                    }
                }
                return appList.OrderBy(x => x.Level).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }

        }

        public async Task<List<ApprovalDataModel>> GetApprovalDetails_SQL(int rowId, int FormId, UserData currentUser)
        {
            try
            {
                List<ApprovalDataModel> appList = new List<ApprovalDataModel>();
                #region Comment
                //GlobalClass gc = new GlobalClass();
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var responseApproval = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverUserName,NextApproverId,TimeStamp,Designation,Level,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + FormId + "')&$expand=FormId,Author")).Result;
                //var responseTextApproval = await responseApproval.Content.ReadAsStringAsync();
                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApproval, settings);


                //var client3 = new HttpClient(handler);
                //client3.BaseAddress = new Uri(conString);
                //client3.DefaultRequestHeaders.Accept.Clear();
                //client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var items = modelData.Node.Data;
                #endregion
                List<ApprovalDataModel> items = new List<ApprovalDataModel>();

                ApprovalMasterModel approvalMasterModel = new ApprovalMasterModel();
                NodeClass nodeClass = new NodeClass();
                var con = new SqlConnection(sqlConString);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();

                cmd = new SqlCommand("USP_GetApproversData", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                cmd.Parameters.Add(new SqlParameter("@FormID", FormId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(dt, settings);


                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        ApprovalDataModel modelData = new ApprovalDataModel();
                        //modelData.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        FormLookup item = new FormLookup();
                        item.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        modelData.FormId = item;
                        modelData.IsActive = Convert.ToInt32(dt.Rows[i]["IsActive"]);
                        modelData.NextApproverId = dt.Rows[i]["NextApproverId"] != null || dt.Rows[i]["NextApproverId"] != DBNull.Value || dt.Rows[i]["NextApproverId"] != "0" ? Convert.ToInt32(dt.Rows[i]["NextApproverId"]) : 0;
                        modelData.ApproverStatus = dt.Rows[i]["ApproverStatus"] != null || dt.Rows[i]["ApproverStatus"] != DBNull.Value || dt.Rows[i]["ApproverStatus"] != "0" ? Convert.ToString(dt.Rows[i]["ApproverStatus"]) : null;
                        modelData.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
                        modelData.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                        modelData.Designation = Convert.ToString(dt.Rows[i]["Designation"]);
                        modelData.ApproverUserName = Convert.ToString(dt.Rows[i]["ApproverUserName"]);
                        modelData.ApproverName = Convert.ToString(dt.Rows[i]["ApproverName"]);
                        DateTime TimeStamp = Convert.ToDateTime("1/1/0001 00:00 AM");
                        if (dt.Rows[i]["TimeStamp"] != DBNull.Value)
                            TimeStamp = Convert.ToDateTime(dt.Rows[i]["TimeStamp"]);
                        modelData.TimeStamp = TimeStamp;
                        items.Add(modelData);

                    }
                }
                if (items.Count > 0)
                {
                    var idString = "";
                    var userList = new List<UserData>();

                    if (adCode.ToLower() == "yes")
                    {
                        //AD Code
                        ListDAL obj = new ListDAL();
                        for (int i = 0; i < items.Count; i++)
                        {
                            //string objectSid = currentUser.ObjectSid;
                            string approverId = items[i].ApproverUserName;
                            var userData = obj.GetApproverDetailsFromAD(approverId, currentUser);
                            //string appName = userData.EmployeeName;
                            if (userData != null)
                                userList.Add(userData);
                        }
                        //AD Code
                    }

                    if (items.Count == userList.Count)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i].UserName = userList[i].EmployeeName;
                            items[i].EmailId = userList[i].Email;
                        }
                    }

                    items = items.OrderBy(x => x.UserLevel).ToList();

                    if (items.Count > 0)
                    {
                        //var approvalMasterModelResults = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseTextApproval, settings);
                        //var output = approvalMasterModelResults.Node.Data;

                        for (int i = 0; i < items.Count; i++)
                        {
                            ApprovalDataModel app = new ApprovalDataModel();
                            app.UserName = items[i].UserName;
                            app.Modified = items[i].Modified;
                            app.ApproverStatus = items[i].ApproverStatus;
                            app.Comment = items[i].Comment;
                            app.Designation = items[i].Designation;
                            app.TimeStamp = items[i].TimeStamp;
                            app.Level = items[i].Level;
                            app.EmailId = items[i].EmailId;

                            appList.Add(app);
                        }
                    }
                }
                return appList.OrderBy(x => x.Level).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }

        }
        public async Task<string> CreateEmailBodyForSmartphoneForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            SmartPhoneRequisitionData smartphoneRequest = new SmartPhoneRequisitionData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseSmartPhoneRequisition = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SmartPhoneRequisition')/items?$select=ID,EmployeeCode," +
                "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDept,BusinessNeed,EmployeeType," +
                "RequestSubmissionFor,EmployeeEmailId,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName," +
                "OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeLocation,OtherEmployeeDepartment,OtherEmployeeEmailId,OtherEmployeeType,Designation,OnBehalfOption" +
                "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextSmartPhoneRequisition = await responseSmartPhoneRequisition.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextSmartPhoneRequisition))
            {
                var result = JsonConvert.DeserializeObject<SmartPhoneRequisitionModel>(responseTextSmartPhoneRequisition, settings);
                smartphoneRequest = result.List.SmartPhoneList[0];
            }


            if (smartphoneRequest.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + smartphoneRequest.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + smartphoneRequest.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + smartphoneRequest.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + smartphoneRequest.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + smartphoneRequest.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + smartphoneRequest.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + smartphoneRequest.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + smartphoneRequest.OtherEmployeeType + "</td></tr>";
                if (smartphoneRequest.OtherEmployeeType == Constants.EMP_TYPE_EXT)
                {
                    //body = body + "<tr><td>" + "External Orgnization Name: " + smartphoneRequest.OtherEmpExtOrgName + "</td></tr>";
                    //if (Convert.ToString(smartphoneRequest.OtherEmpExtOrgName) == "Other")
                    //{
                    //    body = body + "<tr><td>" + "Other Orgnization Name: " + smartphoneRequest.OtherExtEmpOtherOrgName + "</td></tr>";
                    //}
                }
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + smartphoneRequest.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + smartphoneRequest.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + smartphoneRequest.EmployeeDepartment + "  </td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + smartphoneRequest.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + smartphoneRequest.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + smartphoneRequest.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + smartphoneRequest.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + smartphoneRequest.EmployeeType + "</td></tr>";
                if (smartphoneRequest.EmployeeType == Constants.EMP_TYPE_EXT)
                {
                    //body = body + "<tr><td>" + "External Orgnization Name: " + smartphoneRequest.OtherEmpExtOrgName + "</td></tr>";
                    //if (Convert.ToString(smartphoneRequest.OtherEmpExtOrgName) == "Other")
                    //{
                    //    body = body + "<tr><td>" + "Other Orgnization Name: " + smartphoneRequest.OtherEmpExtOrgName + "</td></tr>";
                    //}
                }
                body = body + "</table><br>";
            }


            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + smartphoneRequest.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + smartphoneRequest.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (smartphoneRequest.IsOnBehalf)
            //{
            //    employeeLocation = Convert.ToString(smartphoneRequest.OtherEmployeeLocation);
            //}
            //else
            //{
            //employeeLocation = smartphoneRequest.EmployeeLocation;
            //}
            return body;

        }
        private async Task<string> CreateEmailBodyForSoftwareRequisitionForm(int rowId, int FormID, string FormName, string body)
        {

            string employeeLocation = "";
            SoftwareRequisitionRequestDto softwareRequsitionRequest = new SoftwareRequisitionRequestDto();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseSoftwareRequisitionForm = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SoftwareRequisition')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDept,EmployeeRequestType,TempFrom,TempTo," +
                    "BusinessNeed,IsNonStandard,IsStandard,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor," +
                    "OtherEmployeeType, OtherEmployeeCode, OtherEmployeeCCCode, OtherEmployeeUserId, OtherEmployeeName, OtherEmployeeContactNo, OtherEmployeeDesignation, OtherEmployeeDepartment, " +
                    "OtherEmployeeLocation,OtherExternalOrganizationName,OtherExternalOtherOrgName,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextSoftwareRequisition = await responseSoftwareRequisitionForm.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextSoftwareRequisition))
            {
                var result = JsonConvert.DeserializeObject<SoftwareRequisitionModel>(responseTextSoftwareRequisition, settings);
                softwareRequsitionRequest = result.List.SoftwareList[0];
            }

            if (softwareRequsitionRequest.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + softwareRequsitionRequest.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + softwareRequsitionRequest.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + softwareRequsitionRequest.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + softwareRequsitionRequest.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + softwareRequsitionRequest.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + softwareRequsitionRequest.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + softwareRequsitionRequest.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + softwareRequsitionRequest.OtherEmployeeType + "</td></tr>";
                if (softwareRequsitionRequest.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + softwareRequsitionRequest.OtherExternalOrganizationName + "</td></tr>";
                    if (softwareRequsitionRequest.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + softwareRequsitionRequest.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + softwareRequsitionRequest.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + softwareRequsitionRequest.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + softwareRequsitionRequest.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + softwareRequsitionRequest.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + softwareRequsitionRequest.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + softwareRequsitionRequest.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + softwareRequsitionRequest.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + softwareRequsitionRequest.EmployeeType + "</td></tr>";
                if (softwareRequsitionRequest.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + softwareRequsitionRequest.ExternalOrganizationName + "</td></tr>";
                    if (softwareRequsitionRequest.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + softwareRequsitionRequest.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }


            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + softwareRequsitionRequest.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Type of Request: " + softwareRequsitionRequest.EmployeeRequestType + "</td></tr>";
            if (softwareRequsitionRequest.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + softwareRequsitionRequest.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + softwareRequsitionRequest.TempTo + "</td></tr>";
            }
            body = body + "<tr><td>" + "Reason for Request: " + softwareRequsitionRequest.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Selected Software Details: </td></tr>";


            //Software Details        
            List<SelectedSoftwareDto> ObjSelectedSoftwareDto = new List<SelectedSoftwareDto>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseSoftwareDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('SoftwareDetails')/items?$select=SoftwareName,SoftwareVersion,SoftwareType,IsOtherSoftware&$filter=(SoftwareRequisitionID eq '" + rowId + "' and FormID eq '" + FormID + "')")).Result;
            var responseTextSoftwareDetails = await responseSoftwareDetails.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseTextSoftwareDetails))
            {
                var SoftwareDetailsresult = JsonConvert.DeserializeObject<SelectedSoftwareModel>(responseTextSoftwareDetails);
                ObjSelectedSoftwareDto = SoftwareDetailsresult.List.AVLSoftwareList;

                foreach (var soft in ObjSelectedSoftwareDto)
                {
                    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "Software Name: " + soft.SoftwareName + "</td></tr>";
                    body = body + "<tr><td>" + "Software Type: " + soft.SoftwareType + "</td></tr>";
                    body = body + "<tr><td>" + "Software Version: " + soft.SoftwareVersion + "</td></tr>";
                }
            }
            body = body + "</table><br>";

            //if (softwareRequsitionRequest.IsOnBehalf)
            //{
            //    employeeLocation = softwareRequsitionRequest.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = softwareRequsitionRequest.EmployeeLocation;
            //}
            return body;
        }

        private async Task<string> CreateEmailBodyForITAssetForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            ITAssetRequisitionRequestDto ITAssetForm = new ITAssetRequisitionRequestDto();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseITAssetForm = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ITAssetRequisition')/items?$select=ID,EmployeeType,Created,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,BusinessNeed,EmployeeLocation,EmployeeDesignation," +
                    "WorkstationDesktop,WorkstationLaptop,RSAToken,SIMAndData,Landline,LANCableAndPort,JabraSpeaker,AdditionalOfficeMonitor,AdditionalOfficeMonitor,iPad,PartnerOrganizationName,WorkflowType,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,OtherPartnerOrganizationName," +
                    "FormID/ID,FormID/Created,Desktop,Laptop&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextITAssetForm = await responseITAssetForm.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextITAssetForm))
            {
                var result = JsonConvert.DeserializeObject<ITAssetRequisitionModel>(responseTextITAssetForm, settings);
                ITAssetForm = result.List.ITAssetList[0];
            }

            if (ITAssetForm.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + ITAssetForm.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITAssetForm.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITAssetForm.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITAssetForm.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITAssetForm.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITAssetForm.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITAssetForm.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITAssetForm.OtherEmployeeType + "</td></tr>";
                if (ITAssetForm.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITAssetForm.OtherExternalOrganizationName + "</td></tr>";
                    if (ITAssetForm.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + ITAssetForm.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                if (ITAssetForm.OtherEmployeeType == "Partner")
                {
                    body = body + "<tr><td>" + "Other Orgnization Name: " + ITAssetForm.OtherPartnerOrganizationName + "</td></tr>";
                }
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + ITAssetForm.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITAssetForm.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITAssetForm.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITAssetForm.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITAssetForm.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITAssetForm.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITAssetForm.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITAssetForm.EmployeeType + "</td></tr>";
                if (ITAssetForm.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITAssetForm.ExternalOtherOrganizationName + "</td></tr>";
                    if (ITAssetForm.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + ITAssetForm.ExternalOrganizationName + "</td></tr>";
                    }
                }

                if (ITAssetForm.EmployeeType == "Partner")
                {
                    body = body + "<tr><td>" + "Other Orgnization Name: " + ITAssetForm.PartnerOrganizationName + "</td></tr>";
                }

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + ITAssetForm.BusinessNeed + "</td></tr>";

            body = body + "<tr><td>" + "Requested Asset Details: </td></tr>";
            //foreach (ITRequiredAsset iTAssetItem in ITAssetForm.ITRequiredAssets)
            //{
            //    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
            //    body = body + "<tr><td>" + "IT Asset Name: " + iTAssetItem.AssetName + "</td></tr>";
            //    if (iTAssetItem.Others == true)
            //    {
            //        body = body + "<tr><td>" + "Other: " + "Yes" + "</td></tr>";
            //    }
            //}
            body = body + "<tr><td>" + "Request Type: " + ITAssetForm.RequestType + "</td></tr>";
            if (ITAssetForm.RequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + ITAssetForm.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + ITAssetForm.TempTo + "</td></tr>";
            }
            body = body + "<tr><td>" + "Reason for Request: " + ITAssetForm.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (ITAssetForm.IsOnBehalf)
            //{
            //    employeeLocation = ITAssetForm.OtherEmployeeLocation;
            //}
            //else
            //{
            employeeLocation = ITAssetForm.EmployeeLocation;
            //}
            return body;
        }

        public void GetEmailData()
        {

        }
        private async Task<string> CreateEmailBodyForITClearance(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            ITClearanceData ITClearance = new ITClearanceData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseITClearance = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ITClearance')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDepartment,EmployeeDesignation,EmployeeLocation,LastWorkingDay,HandedDataToName,HandedDataToEmpNumber," +
                     "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextITClearance = await responseITClearance.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextITClearance))
            {
                var result = JsonConvert.DeserializeObject<ITClearanceModel>(responseTextITClearance, settings);
                ITClearance = result.List.ITClearanceList[0];
            }

            if (ITClearance.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + ITClearance.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITClearance.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITClearance.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITClearance.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITClearance.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITClearance.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITClearance.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITClearance.OtherEmployeeType + "</td></tr>";
                if (ITClearance.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITClearance.OtherExternalOrganizationName + "</td></tr>";
                    if (ITClearance.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + ITClearance.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + ITClearance.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITClearance.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITClearance.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITClearance.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITClearance.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITClearance.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITClearance.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITClearance.EmployeeType + "</td></tr>";
                if (ITClearance.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITClearance.ExternalOrganizationName + "</td></tr>";
                    if (ITClearance.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + ITClearance.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + ITClearance.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + "Last Working Day: " + ITClearance.LastWorkingDay + "</td></tr>";
            body = body + "<tr><td>" + "Data Handed Over To: " + ITClearance.HandedDataToName + "</td></tr>";
            body = body + "</table><br>";

            //if (ITClearance.IsOnBehalf)
            //{
            //    employeeLocation = ITClearance.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = ITClearance.EmployeeLocation;
            //}
            return body;
        }

        private async Task<string> CreateEmailBodyForGaneshForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            GaneshUserIdCreationData GaneshUserIdCreation = new GaneshUserIdCreationData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseGaneshUserIdCreation = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('GaneshUserIdCreation')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,BusinessNeed,SystemType,IsCreationRequest," +
                     "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "IsRoleAuthRequest,EmployeeRequestType,TempFrom,TempTo,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextGaneshUserIdCreation = await responseGaneshUserIdCreation.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextGaneshUserIdCreation))
            {
                var result = JsonConvert.DeserializeObject<GaneshUserIdCreationModel>(responseTextGaneshUserIdCreation, settings);
                GaneshUserIdCreation = result.List.GaneshUserIdList[0];
            }


            if (GaneshUserIdCreation.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + GaneshUserIdCreation.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + GaneshUserIdCreation.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + GaneshUserIdCreation.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + GaneshUserIdCreation.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + GaneshUserIdCreation.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + GaneshUserIdCreation.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + GaneshUserIdCreation.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + GaneshUserIdCreation.OtherEmployeeType + "</td></tr>";
                if (GaneshUserIdCreation.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + GaneshUserIdCreation.OtherExternalOrganizationName + "</td></tr>";
                    if (GaneshUserIdCreation.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + GaneshUserIdCreation.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + GaneshUserIdCreation.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + GaneshUserIdCreation.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + GaneshUserIdCreation.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + GaneshUserIdCreation.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + GaneshUserIdCreation.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + GaneshUserIdCreation.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + GaneshUserIdCreation.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + GaneshUserIdCreation.EmployeeType + "</td></tr>";
                if (GaneshUserIdCreation.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + GaneshUserIdCreation.ExternalOrganizationName + "</td></tr>";
                    if (GaneshUserIdCreation.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + GaneshUserIdCreation.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + GaneshUserIdCreation.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Type of Request: " + GaneshUserIdCreation.EmployeeRequestType + "</td></tr>";
            if (GaneshUserIdCreation.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + GaneshUserIdCreation.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + GaneshUserIdCreation.TempTo + "</td></tr>";
            }

            string requestForUserCreation = "";
            string requestForRoleAuthCreation = "";
            if (GaneshUserIdCreation.IsCreationRequest == true)
            {
                requestForUserCreation = "Yes";
            }
            else
            {
                requestForUserCreation = "No";
            }
            if (GaneshUserIdCreation.IsRoleAuthRequest == true)
            {
                requestForRoleAuthCreation = "Yes";
            }
            else
            {
                requestForRoleAuthCreation = "No";
            }

            body = body + "<tr><td>" + "Details of User Creation/Role Authorization: </td></tr>";
            body = body + "<tr><td>" + "Create User ID: " + requestForUserCreation + "</td></tr>";
            body = body + "<tr><td>" + "Role Authorization: " + requestForRoleAuthCreation + "</td></tr>";
            body = body + "<tr><td>" + "System Type: " + GaneshUserIdCreation.SystemType + "</td></tr>";

            //Ganesh Module Details
            List<GaneshModuleAccessRequestDto> objGaneshModuleAccessRequestDto = new List<GaneshModuleAccessRequestDto>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseGaneshModuleDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('GaneshModuleDetails')/items?$select=ModuleName,Role,Reason&$filter=(GaneshUserReqID eq '" + rowId + "' and FormID eq '" + FormID + "')")).Result;
            var responseTextGaneshModuleDetails = await responseGaneshModuleDetails.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseTextGaneshModuleDetails))
            {
                var resultGaneshModuleDetails = JsonConvert.DeserializeObject<GaneshModuleAccessModel>(responseTextGaneshModuleDetails);
                objGaneshModuleAccessRequestDto = resultGaneshModuleDetails.List.GaneshModuleList;

                body = body + "<tr><td>" + "Details of System Module/Role: </td></tr>";
                foreach (var moduleAccess in objGaneshModuleAccessRequestDto)
                {
                    body = body + "<tr><td>" + "System Module (TAB) Name: " + moduleAccess.ModuleName + "</td></tr>";
                    body = body + "<tr><td>" + "Role: " + moduleAccess.Role + "</td></tr>";
                    body = body + "<tr><td>" + "Reason: " + moduleAccess.Reason + "</td></tr>";
                }
            }
            body = body + "<tr><td>" + "Reason for Request: " + GaneshUserIdCreation.BusinessNeed + "</td></tr>";

            body = body + "</table><br>";

            //if (GaneshUserIdCreation.IsOnBehalf)
            //{
            //    employeeLocation = GaneshUserIdCreation.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = GaneshUserIdCreation.EmployeeLocation;
            //}
            return body;
        }

        private async Task<string> CreateEmailBodyForDataBackupRestoreForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            DataBackupRestore DataBackupRestore = new DataBackupRestore();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseDataBackupRestore = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('DataBackupRestore')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,BusinessNeed,RequirementFor," +
                    "RequestFor,FolderPath,FolderSize,RetentionPeriod,BackupServerName,BackupIpAddress,BackupType,RestoreAt,RestoreServerName,RestoreIpAddress," +
                    "RestoreDate,AlternateFolderPath,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextDataBackupRestore = await responseDataBackupRestore.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextDataBackupRestore))
            {
                var result = JsonConvert.DeserializeObject<DataBackupRestoreModel>(responseTextDataBackupRestore, settings);
                DataBackupRestore = result.List.DataBackupRestoreList[0];
            }


            if (DataBackupRestore.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + DataBackupRestore.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + DataBackupRestore.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + DataBackupRestore.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + DataBackupRestore.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + DataBackupRestore.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + DataBackupRestore.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + DataBackupRestore.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + DataBackupRestore.OtherEmployeeType + "</td></tr>";
                if (DataBackupRestore.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + DataBackupRestore.OtherExternalOrganizationName + "</td></tr>";
                    if (DataBackupRestore.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + DataBackupRestore.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + DataBackupRestore.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + DataBackupRestore.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + DataBackupRestore.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + DataBackupRestore.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + DataBackupRestore.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + DataBackupRestore.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + DataBackupRestore.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + DataBackupRestore.EmployeeType + "</td></tr>";
                if (DataBackupRestore.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + DataBackupRestore.ExternalOrganizationName + "</td></tr>";
                    if (DataBackupRestore.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + DataBackupRestore.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + DataBackupRestore.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Requirement For: " + DataBackupRestore.RequirementFor + "</td></tr>";
            switch (DataBackupRestore.RequirementFor)
            {
                case "Backup":
                    body = CreateBackupDetailsBody(body, DataBackupRestore);
                    break;
                case "Restore":
                    body = CreateRestoreDetailsBody(body, DataBackupRestore);
                    break;
                case "Both":
                    body = CreateBackupDetailsBody(body, DataBackupRestore);
                    body = CreateRestoreDetailsBody(body, DataBackupRestore);
                    break;
                default:
                    break;
            }

            body = body + "<tr><td>" + "Reason for Request: " + DataBackupRestore.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (DataBackupRestore.IsOnBehalf)
            //{
            //    employeeLocation = DataBackupRestore.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = DataBackupRestore.EmployeeLocation;
            //}
            return body;
        }


        private async Task<string> CreateEmailBodyForKSRMForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            KSRMUserIdData KSRMUserIdData = new KSRMUserIdData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseKSRMUserIdData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('KSRMUserIdCreation')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,BusinessNeed,IsKSRMIdAccess," +
                     "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                    "IsKSRMOpReportingAccess,Amount,Role,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextKSRMUserIdData = await responseKSRMUserIdData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextKSRMUserIdData))
            {
                var result = JsonConvert.DeserializeObject<KSRMUserIdModel>(responseTextKSRMUserIdData, settings);
                KSRMUserIdData = result.List.KSRMUserIdList[0];
            }


            if (KSRMUserIdData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + KSRMUserIdData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + KSRMUserIdData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + KSRMUserIdData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + KSRMUserIdData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + KSRMUserIdData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + KSRMUserIdData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + KSRMUserIdData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + KSRMUserIdData.OtherEmployeeType + "</td></tr>";
                if (KSRMUserIdData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + KSRMUserIdData.OtherExternalOrganizationName + "</td></tr>";
                    if (KSRMUserIdData.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + KSRMUserIdData.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + KSRMUserIdData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + KSRMUserIdData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + KSRMUserIdData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + KSRMUserIdData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + KSRMUserIdData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + KSRMUserIdData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + KSRMUserIdData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + KSRMUserIdData.EmployeeType + "</td></tr>";
                if (KSRMUserIdData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + KSRMUserIdData.ExternalOrganizationName + "</td></tr>";
                    if (KSRMUserIdData.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + KSRMUserIdData.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }


                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + KSRMUserIdData.BusinessNeed + "</td></tr>";
            if (Convert.ToBoolean(KSRMUserIdData.IsKSRMOpReportingAccess))
            {
                body = body + "<tr><td>" + "Request For: " + "KBP 100(K-SRM Operational Reporting Access)" + "</td></tr>";
            }
            if (Convert.ToBoolean(KSRMUserIdData.IsKSRMIdAccess))
            {
                body = body + "<tr><td>" + "Request For: " + "KSP 100(K-SRM ID Access)" + "</td></tr>";
            }

            //foreach (KSRMUserRole userRoleItem in KSRMUserIdData.KSRMUserRoles)
            //{
            //    body = body + "<tr><td>" + "Role: " + userRoleItem.Role + "</td></tr>";
            //}
            body = body + "<tr><td>" + "Role: " + KSRMUserIdData.Role + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + KSRMUserIdData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (KSRMUserIdData.IsOnBehalf) 
            //{
            //    employeeLocation = KSRMUserIdData.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = KSRMUserIdData.EmployeeLocation;
            //}
            return body;
        }

        private async Task<string> CreateEmailBodyForResourceAccountDLForm(int rowId, int FormID, string FormName, string body)
        {
            ResourceAccountDLData objResAccDLReq = new ResourceAccountDLData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseResourceAccountDLForm = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ResourceAccountAndDistributionList')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "Level,EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,RequestFor,ActionType,AccountOwner,AccountOwnerEmpNumber,DLResourceAccountName,EmailAddress," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "BusinessNeed,EmployeeLocation,ResourceAccountLocation,EmployeeDesignation,DomainId,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextResourceAccountDLForm = await responseResourceAccountDLForm.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextResourceAccountDLForm))
            {
                var result = JsonConvert.DeserializeObject<ResourceAccountDLRequisitionModel>(responseTextResourceAccountDLForm, settings);
                objResAccDLReq = result.List.ResourceAccountDLList[0];
            }

            if (objResAccDLReq.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + objResAccDLReq.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + objResAccDLReq.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + objResAccDLReq.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + objResAccDLReq.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + objResAccDLReq.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + objResAccDLReq.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + objResAccDLReq.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + objResAccDLReq.OtherEmployeeType + "</td></tr>";
                if (objResAccDLReq.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + objResAccDLReq.OtherExternalOrganizationName + "</td></tr>";
                    if (objResAccDLReq.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + objResAccDLReq.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + objResAccDLReq.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + objResAccDLReq.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + objResAccDLReq.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + objResAccDLReq.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + objResAccDLReq.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + objResAccDLReq.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + objResAccDLReq.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + objResAccDLReq.EmployeeType + "</td></tr>";
                if (objResAccDLReq.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + objResAccDLReq.ExternalOrganizationName + "</td></tr>";
                    if (objResAccDLReq.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + objResAccDLReq.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + objResAccDLReq.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Request For: " + objResAccDLReq.RequestFor + "</td></tr>";
            body = body + "<tr><td>" + "Action Type: " + objResAccDLReq.ActionType + "</td></tr>";
            body = body + "<tr><td>" + "Request Type: " + objResAccDLReq.RequestType + "</td></tr>";
            if (objResAccDLReq.RequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + objResAccDLReq.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + objResAccDLReq.TempTo + "</td></tr>";
            }
            body = body + "<tr><td>" + "Owner of Account: " + objResAccDLReq.AccountOwner + "</td></tr>";
            if (objResAccDLReq.RequestFor == "ResourceAccount")
            {
                body = body + "<tr><td>" + "Resource Account Location: " + objResAccDLReq.ResourceAccountLocation + "</td></tr>";
                body = body + "<tr><td>" + "Display Name: " + objResAccDLReq.DLResourceAccountName + "</td></tr>";
                body = body + "<tr><td>" + "Email Id: " + objResAccDLReq.EmailAddress + "</td></tr>";
            }
            else
            {
                body = body + "<tr><td>" + "Display Name: " + objResAccDLReq.DLResourceAccountName + "</td></tr>";
                body = body + "<tr><td>" + "Domain Id: " + objResAccDLReq.EmailAddress + "</td></tr>";
            }

            //UserIdResourceAccountDL 
            List<UserIdResourceAccountDLData> resourceAccountDL = new List<UserIdResourceAccountDLData>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseUserIdResourceAccountDL = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('UserIdResourceAccountDL')/items?$select=EmployeeUserId,ActionType&$filter=(ResourceAccountRequestId eq '" + rowId + "' and FormID eq '" + FormID + "')")).Result;
            var responseTextUserIdResourceAccountDL = await responseUserIdResourceAccountDL.Content.ReadAsStringAsync();
            var result1 = JsonConvert.DeserializeObject<UserIdResourceAccountDLModel>(responseTextUserIdResourceAccountDL);
            resourceAccountDL = result1.List.UserIdResourceAccountDLList;
            if (!string.IsNullOrEmpty(responseTextUserIdResourceAccountDL))
            {
                foreach (var userID in resourceAccountDL)
                {
                    body = body + "<tr><td>" + "***********************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + userID.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Action Type: " + userID.ActionType + "</td></tr>";
                }
            }

            body = body + "<tr><td>" + "***********************************************" + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request (Business Justification): " + objResAccDLReq.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (objResAccDLReq.IsOnBehalf)
            //{
            //    employeeLocation = objResAccDLReq.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = objResAccDLReq.EmployeeLocation;
            //}
            return body;
        }

        private async Task<string> CreateEmailBodyForInternetAccessForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            InternetAcessData IAData = new InternetAcessData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseInternetAccessData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('InternetAccess')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDept,EmployeeRequestType," +
                     "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                    "IsSpecialRequest,TempFrom,TempTo,BusinessNeed,ExternalOrganizationName,ExternalOtherOrganizationName,MoreInformation,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextInternetAccessData = await responseInternetAccessData.Content.ReadAsStringAsync();
            //var approver = await GetApprovalDetails_SQL(rowId, FormID, user);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextInternetAccessData))
            {
                var result = JsonConvert.DeserializeObject<InternetAcessModel>(responseTextInternetAccessData, settings);
                IAData = result.List.InternetList[0];
            }

            if (IAData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + IAData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + IAData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + IAData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + IAData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + IAData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + IAData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + IAData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + IAData.OtherEmployeeType + "</td></tr>";
                if (IAData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + IAData.OtherExternalOrganizationName + "</td></tr>";
                    if (IAData.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + IAData.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + IAData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + IAData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + IAData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + IAData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + IAData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + IAData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + IAData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + IAData.EmployeeType + "</td></tr>";
                if (IAData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + IAData.ExternalOrganizationName + "</td></tr>";
                    if (IAData.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + IAData.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + IAData.BusinessNeed + "</td></tr>";
            if (IAData.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + IAData.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + IAData.TempTo + "</td></tr>";
            }
            body = body + "<tr><td>" + "Reason for Request: " + IAData.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Is it a special request: " + IAData.IsSpecialRequest + "</td></tr>";
            if (IAData.IsSpecialRequest == "Yes")
            {
                body = body + "<tr><td>" + "Reason for Special Request: " + IAData.MoreInformation + "</td></tr>";
            }
            body = body + "</table><br>";
            // body = body + "<tr><td>" + "Role: " + IAData.Role + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + IAData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";
            //if (IAData.EmployeeType == "External")
            //{
            //    if (approver != null && approver.Count > 0)
            //    {
            //        body = body + "<br><br> <table width=\"100%\">";
            //        body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";

            //        for (int i = 0; i < approver.Count; i++)
            //        {
            //            if (approver[i].ApproverStatus == "Approved")
            //            {
            //                body = body + "<tr><td>" + "Approved By: " + approver[i].UserName + "</td></tr>";
            //                body = body + "<tr><td>" + "Approved On: " + approver[i].Modified + "</td></tr>";
            //                //body = body + "<tr><td>" + "Approver Role: " + "Approver Role" + "</td></tr>";
            //                body = body + "<tr><td>" + "Comments: " + approver[i].Comment + "</td></tr>";
            //            }
            //            else if (approver[i].ApproverStatus == "Rejected")
            //            {

            //                body = body + "<tr><td>" + "Rejected By: " + approver[i].UserName + "</td></tr>";
            //                body = body + "<tr><td>" + "Rejected On: " + approver[i].Modified + "</td></tr>";
            //                //body = body + "<tr><td>" + "Role: " + "Approver Role" + "</td></tr>";
            //                body = body + "<tr><td>" + "Comments: " + approver[i].Comment + "</td></tr>";
            //            }
            //        }
            //        body = body + "</table><br>";
            //    }
            //}

            return body;
        }

        private async Task<string> CreateEmailBodyForSharedFolderForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            SharedFolderData SharedFolderData = new SharedFolderData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseInternetAccessData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SharedFolder')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,RequestFor,ChangeType,ChangeFileServerName,ChangeFolderPath,ChangeSize,FolderOwnerName," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "BusinessNeed,EmployeeLocation,EmployeeDesignation,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextInternetAccessData = await responseInternetAccessData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextInternetAccessData))
            {
                var result = JsonConvert.DeserializeObject<SharedFolderModel>(responseTextInternetAccessData, settings);
                SharedFolderData = result.List.SharedFolderList[0];
            }

            if (SharedFolderData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + SharedFolderData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SharedFolderData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SharedFolderData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SharedFolderData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SharedFolderData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + SharedFolderData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SharedFolderData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SharedFolderData.OtherEmployeeType + "</td></tr>";
                if (SharedFolderData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SharedFolderData.OtherExternalOrganizationName + "</td></tr>";
                    if (SharedFolderData.OtherExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + SharedFolderData.OtherExternalOtherOrganizationName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + SharedFolderData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SharedFolderData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SharedFolderData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SharedFolderData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SharedFolderData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + SharedFolderData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SharedFolderData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SharedFolderData.EmployeeType + "</td></tr>";
                if (SharedFolderData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SharedFolderData.ExternalOrganizationName + "</td></tr>";
                    if (SharedFolderData.ExternalOrganizationName == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + SharedFolderData.ExternalOtherOrganizationName + "</td></tr>";
                    }
                }
                body = body + "</table><br>";
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + SharedFolderData.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Request Type: " + SharedFolderData.RequestType + "</td></tr>";
            if (SharedFolderData.RequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + SharedFolderData.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + SharedFolderData.TempTo + "</td></tr>";
            }
            if (SharedFolderData.RequestFor == "Creation")
            {
                //Folder Creation Details
                List<SharedFolderCreationRequestDto> objSharedFolderCreationRequestDto = new List<SharedFolderCreationRequestDto>();
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseSharedFolderCreationDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('SharedFolderCreationDetails')/items?$select=FileServerName,OwnerName,FolderPath,Size&$filter=(SharedFolderID eq '" + rowId + "')")).Result;
                var responseTextSharedFolderCreationDetails = await responseSharedFolderCreationDetails.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseTextSharedFolderCreationDetails))
                {
                    var resultSharedFolderCreationDetails = JsonConvert.DeserializeObject<SharedFolderCreationModel>(responseTextSharedFolderCreationDetails);
                    objSharedFolderCreationRequestDto = resultSharedFolderCreationDetails.List.CreationList;

                    body = body + "<tr><td>" + "Shared Folder Request For: " + "New Folder Creation" + "</td></tr>";
                    foreach (var FolderCreation in objSharedFolderCreationRequestDto)
                    {
                        body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                        body = body + "<tr><td>" + "File Server Name: " + FolderCreation.FileServerName + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Name: " + FolderCreation.CreationFolderPath + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Size in GB: " + FolderCreation.Size + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Owner Name: " + FolderCreation.CreationOwnerName + "</td></tr>";
                        //body = body + "<tr><td>" + "Permission: " + FolderCreation.Permission + "</td></tr>";
                    }
                }
                // body = body + "<tr><td>" + "Reason for Request: " + SharedFolderData.BusinessNeed + "</td></tr>";

                body = body + "</table><br>";
            }

            if (SharedFolderData.RequestFor == "Change")
            {
                body = body + "<tr><td>" + "Shared Folder Request For: " + "Existing Folder Change" + "</td></tr>";
                if (SharedFolderData.ChangeType == "Both")
                {
                    body = body + "<tr><td>" + "Change In: " + "Folder Size in GB & Folder Owner Name" + "</td></tr>";
                }
                else if (SharedFolderData.ChangeType == "ChangeInFolderOwnerName")
                {
                    body = body + "<tr><td>" + "Change In: " + "Folder Owner Name" + "</td></tr>";
                }
                else
                {
                    body = body + "<tr><td>" + "Change In: " + "Folder Size in GB" + "</td></tr>";
                }

                //foreach (SharedFolderChangeRequest folderChangeItem in sharedFolderRequest.SharedFolderChangeRequests)
                //{
                body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                body = body + "<tr><td>" + "File Server Name: " + SharedFolderData.ChangeFileServerName + "</td></tr>";
                body = body + "<tr><td>" + "Folder Path: " + SharedFolderData.ChangeFolderPath + "</td></tr>";
                if (SharedFolderData.ChangeType == "ChangeInSize")
                {
                    body = body + "<tr><td>" + "Folder Size in GB: " + SharedFolderData.ChangeSize + "</td></tr>";
                }
                if (SharedFolderData.ChangeType == "ChangeInFolderOwnerName")
                {
                    body = body + "<tr><td>" + "Folder Owner Name: " + SharedFolderData.FolderOwnerName + "</td></tr>";
                }
                // }

                //strEmployeeFileServerLocation = sharedFolderRequest.SharedFolderChangeRequests.FirstOrDefault().FolderServer;
            }
            if (SharedFolderData.RequestFor == "AddRemoveMembers")
            {
                //Folder AddRemoveMembers Details
                body = body + "<tr><td>" + "Shared Folder Request For: " + "Access Change - Add/Remove Users" + "</td></tr>";
                List<SharedFolderAddRemoveUserRequestDto> objSharedFolderAddRemoveUserRequestDto = new List<SharedFolderAddRemoveUserRequestDto>();
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseSharedFolderAddRemoveMembersDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('SharedFolderCreationDetails')/items?$select=FileServerName,OwnerName,FolderPath,Size&$filter=(SharedFolderID eq '" + rowId + "')")).Result;
                var responseTextSharedFolderAddRemoveMembersDetails = await responseSharedFolderAddRemoveMembersDetails.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseTextSharedFolderAddRemoveMembersDetails))
                {
                    var resultSharedFolderAddRemoveMembersDetails = JsonConvert.DeserializeObject<SharedFolderAddRemoveUserModel>(responseTextSharedFolderAddRemoveMembersDetails);
                    objSharedFolderAddRemoveUserRequestDto = resultSharedFolderAddRemoveMembersDetails.AddRemoveList.AddRemoveUsersList;

                    foreach (var AddRemoveMembers in objSharedFolderAddRemoveUserRequestDto)
                    {

                        body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                        body = body + "<tr><td>" + "File Server Name: " + AddRemoveMembers.FileServerName + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Path: " + AddRemoveMembers.FolderPath + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Owner Name: " + AddRemoveMembers.FolderOwnerName + "</td></tr>";
                        body = body + "<tr><td>" + "User ID: " + AddRemoveMembers.UserId + "</td></tr>";
                        body = body + "<tr><td>" + "Access Details: </td></tr>";
                        if (Convert.ToBoolean(AddRemoveMembers.Read))
                        {
                            body = body + "<tr><td>" + "Read Access: " + "Yes" + "</td></tr>";
                        }
                        if (Convert.ToBoolean(AddRemoveMembers.ReadWrite))
                        {
                            body = body + "<tr><td>" + "Read Write Access: " + "Yes" + "</td></tr>";
                        }
                        if (Convert.ToBoolean(AddRemoveMembers.Remove))
                        {
                            body = body + "<tr><td>" + "Remove Access: " + "Yes" + "</td></tr>";
                        }

                    }
                }
            }
            // strEmployeeFileServerLocation = sharedFolderRequest.SharedFolderAddRemoveUs_erRequest.FirstOrDefault().FolderServer;

            body = body + "<tr><td>" + "Reason for Request: " + SharedFolderData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (sharedFolderRequest.IsOnBehalf)
            //{
            //    employeeLocation = sharedFolderRequest.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = sharedFolderRequest.EmployeeLocation;
            //}
            //employeeLocation = strEmployeeFileServerLocation;
            return body;

        }

        private async Task<string> CreateEmailBodyForConflictOfInterestForm(int rowId, int FormID, string FormName, string body)
        {
            string employeeLocation = "";
            ConflictOfInterestData COIData = new ConflictOfInterestData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseCOIData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ConflictOfInterest')/items?$select=ID,EmployeeCode,EmployeeType," +
                     "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment," +
                   "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                     "IsQ1Yes,IsQ2Yes,IsQ3Yes,Elaboration1,Elaboration2,Elaboration3,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + FormID + "')&$expand=FormID")).Result;

            var responseTextCOIData = await responseCOIData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextCOIData))
            {
                var result = JsonConvert.DeserializeObject<ConflictOfInterestModel>(responseTextCOIData, settings);
                COIData = result.List.COIList[0];
            }

            if (COIData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + COIData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + COIData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + COIData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + COIData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + COIData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + COIData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + COIData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + COIData.OtherEmployeeType + "</td></tr>";
                //if (COIData.OtherEmployeeType == "External")
                //{
                //    body = body + "<tr><td>" + "External Orgnization Name: " + COIData.OtherExternalOrganizationName + "</td></tr>";
                //if (COIData.OtherExternalOrganizationName == "Other")
                //{
                //    body = body + "<tr><td>" + "Other Orgnization Name: " + COIData.OtherExternalOtherOrganizationName + "</td></tr>";
                //}

                //}

                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + COIData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + COIData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + COIData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + COIData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + COIData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + COIData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + COIData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + COIData.EmployeeType + "</td></tr>";

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + FormID + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + COIData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //if (ITClearance.IsOnBehalf)
            //{
            //    employeeLocation = ITClearance.OtherEmployeeLocation;
            //}
            //else
            //{
            //    employeeLocation = ITClearance.EmployeeLocation;
            //}
            return body;
        }

        public async Task<int> GetRowID(int FormID, UserData currentUser)
        {
            int dataRowID = 0;
            var formData = new List<FormData>();

            var handler = new HttpClientHandler();
            GlobalClass gc = new GlobalClass();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,FormName,Status&$filter=(ID eq '" + FormID + "')")).Result;
            var responseText2 = await response2.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseText2))
            {
                var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText2);
                formData = modelResult.Data.Forms;
                dataRowID = formData.ElementAtOrDefault(0) != null ? (formData.ElementAtOrDefault(0).DataRowId ?? 0) : 0;
            }

            return dataRowID;
        }
        public async Task<int> GetRowID_SQL(int FormID, UserData currentUser)
        {
            int dataRowID = 0;
            var formData = new List<FormData>();

            SqlCommand cmd1 = new SqlCommand();
            SqlDataAdapter adapter1 = new SqlDataAdapter();
            DataTable ds1 = new DataTable();
            con = new SqlConnection(sqlConString);
            cmd1 = new SqlCommand("GetFormDataByFormId", con);
            cmd1.Parameters.Add(new SqlParameter("@FormID", FormID));
            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
            cmd1.CommandType = CommandType.StoredProcedure;
            adapter1.SelectCommand = cmd1;
            con.Open();
            adapter1.Fill(ds1);
            con.Close();
            if (ds1.Rows.Count > 0)
            {
                for (int i = 0; i < ds1.Rows.Count; i++)
                {
                    dataRowID = ds1.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Rows[i]["DataRowId"]);
                }
            }
            return dataRowID;
        }
        //Get IT Clearance Form and Approval Data FAM- Final Approval Mail
        public async Task<EmailDataModel> GetITClearanceFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "IT Clearance Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            ITClearanceData ITClearance = new ITClearanceData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseITClearance = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ITClearance')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDepartment,EmployeeDesignation,EmployeeLocation,LastWorkingDay,HandedDataToName,HandedDataToEmpNumber,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName,BusinessNeed," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextITClearance = await responseITClearance.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextITClearance))
            {
                var result = JsonConvert.DeserializeObject<ITClearanceModel>(responseTextITClearance, settings);
                ITClearance = result.List.ITClearanceList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (ITClearance.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + ITClearance.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITClearance.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + ITClearance.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITClearance.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITClearance.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITClearance.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITClearance.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITClearance.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITClearance.OtherEmployeeType + "</td></tr>";
                if (ITClearance.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITClearance.OtherExternalOtherOrganizationName + "</td></tr>";
                }
                employeeLocation = ITClearance.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + ITClearance.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITClearance.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + ITClearance.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITClearance.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITClearance.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITClearance.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITClearance.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITClearance.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITClearance.EmployeeType + "</td></tr>";
                if (ITClearance.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITClearance.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = ITClearance.EmployeeLocation;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + ITClearance.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Last Working Day: " + ITClearance.LastWorkingDay + "</td></tr>";
            body = body + "<tr><td>" + "Data Handed Over To: " + ITClearance.HandedDataToName + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.OrderBy(x => x.UserLevel).Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };

        }

        //Get Conflict Of Interest Form and Approval Data
        public async Task<EmailDataModel> GetConflictOfInterestFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();

                string FormName = "Conflict Of Interest Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                ConflictOfInterestData COIData = new ConflictOfInterestData();
                GlobalClass gc = new GlobalClass();
                //var user = gc.GetCurrentUser();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseCOIData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ConflictOfInterest')/items?$select=ID,EmployeeCode,EmployeeType," +
                      "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,BusinessNeed," +
                     "IsQ1Yes,IsQ2Yes,IsQ3Yes,Elaboration1,Elaboration2,Elaboration3,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

                var responseTextCOIData = await responseCOIData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextCOIData))
                {
                    var result = JsonConvert.DeserializeObject<ConflictOfInterestModel>(responseTextCOIData, settings);
                    COIData = result.List.COIList[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

                if (COIData.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + COIData.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + COIData.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + COIData.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + COIData.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + COIData.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + COIData.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + COIData.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + COIData.OtherEmployeeType + "</td></tr>";

                    employeeLocation = COIData.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + COIData.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + COIData.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + COIData.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + COIData.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + COIData.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + COIData.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + COIData.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + COIData.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + COIData.EmployeeType + "</td></tr>";

                    employeeLocation = COIData.EmployeeLocation;
                    body = body + "</table><br>";
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "</table><br>";

                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    //body = body + "<tr><td>" + "Approver Role: " + approver. + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                //body = body + "<br><br> <table width=\"100%\">";
                //body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                //"</b></th></tr>";
                //body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                //body = body + "<tr><td>" + "Assigned To: ServiceDeskIndia@skoda-vw.co.in; servicedesk.manager@skoda-vw.co.in </td></tr>";
                //body = body + "<tr><td>" + "Comments: </td></tr>";
                //body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        //Get KSRM Form and Approval Data FAM- Final Approval Mail
        public async Task<EmailDataModel> GetKSRMUserIdCreationFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "K-SRM User Id Creation Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            KSRMUserIdData KSRMUserIdData = new KSRMUserIdData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseKSRMUserIdData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('KSRMUserIdCreation')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,BusinessNeed,IsKSRMIdAccess," +
                     "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                    "IsKSRMOpReportingAccess,Amount,Role,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextKSRMUserIdData = await responseKSRMUserIdData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextKSRMUserIdData))
            {
                var result = JsonConvert.DeserializeObject<KSRMUserIdModel>(responseTextKSRMUserIdData, settings);
                KSRMUserIdData = result.List.KSRMUserIdList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (KSRMUserIdData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + KSRMUserIdData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + KSRMUserIdData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + KSRMUserIdData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + KSRMUserIdData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + KSRMUserIdData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + KSRMUserIdData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + KSRMUserIdData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + KSRMUserIdData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + KSRMUserIdData.OtherEmployeeType + "</td></tr>";
                if (KSRMUserIdData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + KSRMUserIdData.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = KSRMUserIdData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + KSRMUserIdData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + KSRMUserIdData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + KSRMUserIdData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + KSRMUserIdData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + KSRMUserIdData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + KSRMUserIdData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + KSRMUserIdData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + KSRMUserIdData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + KSRMUserIdData.EmployeeType + "</td></tr>";
                if (KSRMUserIdData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + KSRMUserIdData.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = KSRMUserIdData.EmployeeLocation;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            if (Convert.ToBoolean(KSRMUserIdData.IsKSRMOpReportingAccess))
            {
                body = body + "<tr><td>" + "Request For: " + "KBP 100(K-SRM Operational Reporting Access)" + "</td></tr>";
            }
            if (Convert.ToBoolean(KSRMUserIdData.IsKSRMIdAccess))
            {
                body = body + "<tr><td>" + "Request For: " + "KSP 100(K-SRM ID Access)" + "</td></tr>";
            }

            //foreach (KSRMUserRole userRoleItem in KSRMUserIdData.KSRMUserRoles)
            //{
            //    body = body + "<tr><td>" + "Role: " + userRoleItem.Role + "</td></tr>";
            //}
            body = body + "<tr><td>" + "Role: " + KSRMUserIdData.Role + "</td></tr>";
            body = body + "<tr><td>" + "Euro(&#8364;): " + KSRMUserIdData.Amount + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + KSRMUserIdData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";


            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        //Get Resource Account Form and Approval Data FAM- Final Approval Mail
        public async Task<EmailDataModel> GetResourceAccountDLFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Resource Account And Distribution List Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            ResourceAccountDLData ResourceAccountDLData = new ResourceAccountDLData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseResourceAccountDLData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ResourceAccountAndDistributionList')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,RequestFor,ActionType,AccountOwner,AccountOwnerEmpNumber,DLResourceAccountName,EmailAddress," +
                    "BusinessNeed,EmployeeLocation,ResourceAccountLocation,EmployeeDesignation,DomainId,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextResourceAccountDLData = await responseResourceAccountDLData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextResourceAccountDLData))
            {
                var result = JsonConvert.DeserializeObject<ResourceAccountDLRequisitionModel>(responseTextResourceAccountDLData, settings);
                ResourceAccountDLData = result.List.ResourceAccountDLList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (ResourceAccountDLData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + ResourceAccountDLData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ResourceAccountDLData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + ResourceAccountDLData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ResourceAccountDLData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ResourceAccountDLData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ResourceAccountDLData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ResourceAccountDLData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ResourceAccountDLData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ResourceAccountDLData.OtherEmployeeType + "</td></tr>";
                if (ResourceAccountDLData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ResourceAccountDLData.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = ResourceAccountDLData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + ResourceAccountDLData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ResourceAccountDLData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + ResourceAccountDLData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ResourceAccountDLData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ResourceAccountDLData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ResourceAccountDLData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ResourceAccountDLData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ResourceAccountDLData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ResourceAccountDLData.EmployeeType + "</td></tr>";
                if (ResourceAccountDLData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ResourceAccountDLData.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = ResourceAccountDLData.EmployeeLocation;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Request For: " + ResourceAccountDLData.RequestFor + "</td></tr>";
            body = body + "<tr><td>" + "Action Type: " + ResourceAccountDLData.ActionType + "</td></tr>";
            body = body + "<tr><td>" + "Request Type: " + ResourceAccountDLData.RequestType + "</td></tr>";
            if (ResourceAccountDLData.RequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + ResourceAccountDLData.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + ResourceAccountDLData.TempTo + "</td></tr>";
            }
            body = body + "<tr><td>" + "Owner of Account: " + ResourceAccountDLData.AccountOwner + "</td></tr>";
            if (ResourceAccountDLData.RequestFor == "ResourceAccount")
            {
                body = body + "<tr><td>" + "Resource Account Location: " + ResourceAccountDLData.ResourceAccountLocation + "</td></tr>";
                body = body + "<tr><td>" + "Display Name: " + ResourceAccountDLData.DLResourceAccountName + "</td></tr>";
                body = body + "<tr><td>" + "Email Id: " + ResourceAccountDLData.EmailAddress + "</td></tr>";
            }
            else
            {
                body = body + "<tr><td>" + "Display Name: " + ResourceAccountDLData.DLResourceAccountName + "</td></tr>";
                body = body + "<tr><td>" + "Domain Id: " + ResourceAccountDLData.EmailAddress + "</td></tr>";
            }

            //UserIdResourceAccountDL 
            List<UserIdResourceAccountDLData> resourceAccountDL = new List<UserIdResourceAccountDLData>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseUserIdResourceAccountDL = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('UserIdResourceAccountDL')/items?$select=EmployeeUserId,ActionType&$filter=(ResourceAccountRequestId eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            var responseTextUserIdResourceAccountDL = await responseUserIdResourceAccountDL.Content.ReadAsStringAsync();
            var result1 = JsonConvert.DeserializeObject<UserIdResourceAccountDLModel>(responseTextUserIdResourceAccountDL);
            resourceAccountDL = result1.List.UserIdResourceAccountDLList;
            if (!string.IsNullOrEmpty(responseTextUserIdResourceAccountDL))
            {
                foreach (var userID in resourceAccountDL)
                {
                    body = body + "<tr><td>" + "***********************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + userID.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Action Type: " + userID.ActionType + "</td></tr>";
                }
            }

            body = body + "<tr><td>" + "***********************************************" + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request (Business Justification): " + ResourceAccountDLData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To:  {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };

        }

        //Get Server Requisition Form and Approval Data FAM- Final Approval Mail
        public async Task<EmailDataModel> GetSRCFFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Server Requisition Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            SRFData SRFData = new SRFData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseSCF = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ServerRequisitionForm')/items?$select=ID,RequestSubmissionFor,EmployeeName,Department,EmployeeCode,CostCenterNo,UserID,TypeofEmployee,CompanyName,Level,ContactNo,EmployeeEmailId,SelfTelephone,OtherEmployeeName,OnBehalfEmployeeNumber,OnBehalfTypeofEmployee,OnBehalfCompanyName,OnBehlafDepartment,OnBehalfCostCenterNo,OnBehalfUserID,OnBehalfLevel,OnBehalfMobile,OnBehalfTelephone,OtherEmployeeEmailId,ServerOwnerName,AdminAccount,ServerRole,ServerEnvironment,"
            + "ServerHardware,RAM,NoofCPU,NoofNetworkPorts,StorageSize,TwoRoom,NonTwoRoom,OperatingSystem,OSName,OSEdition,Architecture,DBName,DBEdition,ServerCriticality,ServerType,Temporaryfrom,TemporaryTo,BackupRequired,WeekNo,Day,TimeFrame,ReasonForServerRequisition,ServerCreationType,Location,OnBehalfLocation,"
            + "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextSCF = await responseSCF.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextSCF))
            {
                var result = JsonConvert.DeserializeObject<ServerRequisitionModel>(responseTextSCF, settings);
                SRFData = result.srfflist.srfData[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (SRFData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + SRFData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SRFData.OnBehalfEmployeeNumber + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SRFData.OnBehlafDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SRFData.OnBehalfCostCenterNo + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SRFData.OnBehalfMobile + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SRFData.OnBehalfLocation + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + SRFData.OnBehalfUserID + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SRFData.OnBehalfTypeofEmployee + "</td></tr>";
                if (SRFData.OnBehalfTypeofEmployee == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SRFData.OnBehalfCompanyName + "</td></tr>";
                    if (SRFData.OnBehalfTypeofEmployee == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + SRFData.OnBehalfCompanyName + "</td></tr>";
                    }
                }
                employeeLocation = SRFData.OnBehalfLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + SRFData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SRFData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SRFData.Department + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SRFData.CostCenterNo + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SRFData.ContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SRFData.Location + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + SRFData.UserID + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SRFData.TypeofEmployee + "</td></tr>";
                if (SRFData.TypeofEmployee == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SRFData.CompanyName + "</td></tr>";
                    if (SRFData.TypeofEmployee == "Other")
                    {
                        body = body + "<tr><td>" + "Other Orgnization Name: " + SRFData.CompanyName + "</td></tr>";
                    }
                }
                employeeLocation = SRFData.Location;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Requirement: " + SRFData.ServerCreationType + "</td></tr>";
            body = body + "<tr><td>" + "Server Location: " + SRFData.ServerLocation + "</td></tr>";
            body = body + "<tr><td>" + "Server Owner Name: " + SRFData.ServerOwnerName + "</td></tr>";
            body = body + "<tr><td>" + "Admin Account: " + SRFData.AdminAccount + "</td></tr>";
            body = body + "<tr><td>" + "Server Role / Apps Name: " + SRFData.ServerRole + "</td></tr>";
            if (SRFData.ServerCreationType == "Existing Server Modification")
            {
                body = body + "<tr><td>" + "Host Name : " + SRFData.HostName + "</td></tr>";
                body = body + "<tr><td>" + "IP Address : " + SRFData.IPAddress + "</td></tr>";
                if (SRFData.ServerCpu == "Cpu")
                {
                    body = body + "<tr><td>" + "Current Cpu : " + SRFData.CurrentCpu + "</td></tr>";
                    body = body + "<tr><td>" + "Increment Cpu : " + SRFData.IncrementCpu + "</td></tr>";
                    body = body + "<tr><td>" + "Total Cpu : " + SRFData.TotalCpu + "</td></tr>";
                }
                if (SRFData.ServerMemory == "Memory")
                {
                    body = body + "<tr><td>" + "Current Memory : " + SRFData.CurrentMemory + "</td></tr>";
                    body = body + "<tr><td>" + "Increment Memory : " + SRFData.IncrementMemory + "</td></tr>";
                    body = body + "<tr><td>" + "Total Memory : " + SRFData.TotalMemory + "</td></tr>";
                }
                if (SRFData.ServerDisk == "Disk")
                {
                    body = body + "<tr><td>" + "Current Disk : " + SRFData.CurrentDisk + "</td></tr>";
                    body = body + "<tr><td>" + "Increment Disk : " + SRFData.IncrementDisk + "</td></tr>";
                    body = body + "<tr><td>" + "Total Disk : " + SRFData.TotalDisk + "</td></tr>";
                }
                if (SRFData.ServerLan == "LAN")
                {
                    body = body + "<tr><td>" + "Current Lan : " + SRFData.CurrentLan + "</td></tr>";
                    body = body + "<tr><td>" + "Increment Lan : " + SRFData.IncrementLan + "</td></tr>";
                    body = body + "<tr><td>" + "Total Lan : " + SRFData.TotalLan + "</td></tr>";
                }
                if (SRFData.ServerOwn == "OwnerChange")
                {
                    body = body + "<tr><td>" + "Current Owner : " + SRFData.CurrentOwner + "</td></tr>";
                    body = body + "<tr><td>" + "New Owner : " + SRFData.NewOwner + "</td></tr>";
                }
            }
            if (SRFData.ServerCreationType == "New Server Creation")
            {
                body = body + "<tr><td>" + "Server Environment: " + SRFData.ServerEnvironment + "</td></tr>";
                body = body + "<tr><td>" + "Server Hardware: " + SRFData.ServerHardware + "</td></tr>";
                body = body + "<tr><td>" + "RAM in GB: " + SRFData.RAM + "</td></tr>";
                body = body + "<tr><td>" + "CPU: No of CPU / Cores: " + SRFData.NoofCPU + "</td></tr>";
                body = body + "<tr><td>" + "No of Network Ports: " + SRFData.NoofNetworkPorts + "</td></tr>";
                body = body + "<tr><td>" + "Storage Size (GB/TB) : " + SRFData.StorageSize + "</td></tr>";
                body = body + "<tr><td>" + "Rooms : " + SRFData.TwoRoom + "</td></tr>";
                body = body + "<tr><td>" + "Operating System Details: : " + SRFData.OperatingSystem + "</td></tr>";
                body = body + "<tr><td>" + "OS Name : " + SRFData.OSName + "</td></tr>";
                body = body + "<tr><td>" + "OS Edition : " + SRFData.OSEdition + "</td></tr>";
                body = body + "<tr><td>" + "Architecture (32/64 Bit) : " + SRFData.Architecture + "</td></tr>";
                body = body + "<tr><td>" + "Database Details : " + "**********" + "</td></tr>";
                body = body + "<tr><td>" + "DB Name : " + SRFData.DBName + "</td></tr>";
                body = body + "<tr><td>" + "DB Edition : " + SRFData.DBEdition + "</td></tr>";
                body = body + "<tr><td>" + "Server Criticality : " + SRFData.ServerCriticality + "</td></tr>";
                body = body + "<tr><td>" + "Server Type : " + SRFData.ServerType + "</td></tr>";
                if (SRFData.ServerType == "Temporary")
                {
                    body = body + "<tr><td>" + "Temporary from : " + SRFData.Temporaryfrom.ToShortDateString() + "</td></tr>";
                    body = body + "<tr><td>" + "Temporary to : " + SRFData.TemporaryTo.ToShortDateString() + "</td></tr>";
                }
                body = body + "<tr><td>" + "Server Backup : " + "**********" + "</td></tr>";
                body = body + "<tr><td>" + "Backup Required : " + SRFData.BackupRequired + "</td></tr>";
                body = body + "<tr><td>" + "Windows Patching Schedule : " + "**********" + "</td></tr>";
            }

            body = body + "<tr><td>" + "Week No : " + SRFData.WeekNo + "</td></tr>";
            body = body + "<tr><td>" + "Day : " + SRFData.Day + "</td></tr>";
            body = body + "<tr><td>" + "Time Frame : " + SRFData.TimeFrame + "</td></tr>";

            body = body + "<tr><td>" + "Reason for Request: " + SRFData.ReasonForServerRequisition + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };

        }

        //Get Ganesh User Id Creation Form and Approval Data FAM- Final Approval Mail  
        public async Task<EmailDataModel> GetGaneshIdFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Ganesh User Id Creation Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            GaneshUserIdCreationData GaneshUserIdCreation = new GaneshUserIdCreationData();
            GlobalClass gc = new GlobalClass();
            var user = gc.GetCurrentUser();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseGaneshUserIdCreation = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('GaneshUserIdCreation')/items?$select=ID,EmployeeType,EmployeeCode," +
                  "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,BusinessNeed,SystemType,IsCreationRequest," +
                  "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                  "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                  "IsRoleAuthRequest,EmployeeRequestType,TempFrom,TempTo,EmployeeLocation,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextGaneshUserIdCreation = await responseGaneshUserIdCreation.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextGaneshUserIdCreation))
            {
                var result = JsonConvert.DeserializeObject<GaneshUserIdCreationModel>(responseTextGaneshUserIdCreation, settings);
                GaneshUserIdCreation = result.List.GaneshUserIdList[0];
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (GaneshUserIdCreation.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + GaneshUserIdCreation.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + GaneshUserIdCreation.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + GaneshUserIdCreation.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + GaneshUserIdCreation.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + GaneshUserIdCreation.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + GaneshUserIdCreation.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + GaneshUserIdCreation.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + GaneshUserIdCreation.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + GaneshUserIdCreation.OtherEmployeeType + "</td></tr>";
                if (GaneshUserIdCreation.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + GaneshUserIdCreation.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = GaneshUserIdCreation.OtherEmployeeLocation;
                body = body + "</table><br>";

            }
            else
            {
                body = body + "<tr><td>" + "Name: " + GaneshUserIdCreation.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + GaneshUserIdCreation.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + GaneshUserIdCreation.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + GaneshUserIdCreation.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + GaneshUserIdCreation.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + GaneshUserIdCreation.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + GaneshUserIdCreation.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + GaneshUserIdCreation.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + GaneshUserIdCreation.EmployeeType + "</td></tr>";
                if (GaneshUserIdCreation.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + GaneshUserIdCreation.ExternalOtherOrganizationName + "</td></tr>";
                }
                employeeLocation = GaneshUserIdCreation.EmployeeLocation;
                body = body + "</table><br>";

            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Type of Request: " + GaneshUserIdCreation.EmployeeRequestType + "</td></tr>";
            if (GaneshUserIdCreation.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + GaneshUserIdCreation.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + GaneshUserIdCreation.TempTo + "</td></tr>";
            }

            string requestForUserCreation = "";
            string requestForRoleAuthCreation = "";
            if (GaneshUserIdCreation.IsCreationRequest == true)
            {
                requestForUserCreation = "Yes";
            }
            else
            {
                requestForUserCreation = "No";
            }
            if (GaneshUserIdCreation.IsRoleAuthRequest == true)
            {
                requestForRoleAuthCreation = "Yes";
            }
            else
            {
                requestForRoleAuthCreation = "No";
            }

            body = body + "<tr><td>" + "Details of User Creation/Role Authorization: </td></tr>";
            body = body + "<tr><td>" + "Create User ID: " + requestForUserCreation + "</td></tr>";
            body = body + "<tr><td>" + "Role Authorization: " + requestForRoleAuthCreation + "</td></tr>";
            body = body + "<tr><td>" + "System Type: " + GaneshUserIdCreation.SystemType + "</td></tr>";

            //Ganesh Module Details
            List<GaneshModuleAccessRequestDto> objGaneshModuleAccessRequestDto = new List<GaneshModuleAccessRequestDto>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseGaneshModuleDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('GaneshModuleDetails')/items?$select=ModuleName,Role,Reason&$filter=(GaneshUserReqID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            var responseTextGaneshModuleDetails = await responseGaneshModuleDetails.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseTextGaneshModuleDetails))
            {
                var resultGaneshModuleDetails = JsonConvert.DeserializeObject<GaneshModuleAccessModel>(responseTextGaneshModuleDetails);
                objGaneshModuleAccessRequestDto = resultGaneshModuleDetails.List.GaneshModuleList;

                body = body + "<tr><td>" + "Details of System Module/Role: </td></tr>";
                foreach (var moduleAccess in objGaneshModuleAccessRequestDto)
                {
                    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "System Module (TAB) Name: " + moduleAccess.ModuleName + "</td></tr>";
                    body = body + "<tr><td>" + "Role: " + moduleAccess.Role + "</td></tr>";
                    body = body + "<tr><td>" + "Reason: " + moduleAccess.Reason + "</td></tr>";
                }
            }
            body = body + "<tr><td>" + "Reason for Request: " + GaneshUserIdCreation.BusinessNeed + "</td></tr>";

            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };

        }
        //Get SmartPhone Requisition Form and Approval Data FAM- Final Approval Mail  
        public async Task<EmailDataModel> GetSmartPhoneFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "SmartPhone Requisition Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            SmartPhoneRequisitionData SmartPhone = new SmartPhoneRequisitionData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseSmartPhone = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SmartPhoneRequisition')/items?$select=ID,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,BusinessNeed," +
                    "EmployeeType,RequestSubmissionFor,EmployeeEmailId,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName," +
                    "OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeLocation,OtherEmployeeDepartment,OtherEmployeeEmailId,OtherEmployeeType,Designation,OnBehalfOption,ExternalOrganizationName,ExternalOtherOrganizationName" +
                    ",FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextSmartPhone = await responseSmartPhone.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextSmartPhone))
            {
                var result = JsonConvert.DeserializeObject<SmartPhoneRequisitionModel>(responseTextSmartPhone, settings);
                SmartPhone = result.List.SmartPhoneList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (SmartPhone.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + SmartPhone.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SmartPhone.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + SmartPhone.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SmartPhone.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SmartPhone.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SmartPhone.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + SmartPhone.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SmartPhone.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SmartPhone.OtherEmployeeType + "</td></tr>";
                if (SmartPhone.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SmartPhone.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = SmartPhone.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + SmartPhone.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SmartPhone.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + SmartPhone.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SmartPhone.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SmartPhone.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SmartPhone.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + SmartPhone.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SmartPhone.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SmartPhone.EmployeeType + "</td></tr>";
                if (SmartPhone.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SmartPhone.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = SmartPhone.EmployeeLocation;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + SmartPhone.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            if (approverList != null && approverList.Count > 0)
            {
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }
            }
            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        //Get Data Backup Restore Form and Approval Data FAM- Final Approval Mail  
        public async Task<EmailDataModel> GetDataBackupRestoreFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Data Backup Restore Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            DataBackupRestore DataBackupRestore = new DataBackupRestore();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseDataBackupRestore = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('DataBackupRestore')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,BusinessNeed,RequirementFor," +
                      "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                    "RequestFor,FolderPath,FolderSize,RetentionPeriod,BackupServerName,BackupIpAddress,BackupType,RestoreAt,RestoreServerName,RestoreIpAddress," +
                    "RestoreDate,AlternateFolderPath,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextDataBackupRestore = await responseDataBackupRestore.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextDataBackupRestore))
            {
                var result = JsonConvert.DeserializeObject<DataBackupRestoreModel>(responseTextDataBackupRestore, settings);
                DataBackupRestore = result.List.DataBackupRestoreList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (DataBackupRestore.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + DataBackupRestore.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + DataBackupRestore.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + DataBackupRestore.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + DataBackupRestore.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + DataBackupRestore.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + DataBackupRestore.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + DataBackupRestore.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + DataBackupRestore.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + DataBackupRestore.OtherEmployeeType + "</td></tr>";
                if (DataBackupRestore.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + DataBackupRestore.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = DataBackupRestore.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + DataBackupRestore.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + DataBackupRestore.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + DataBackupRestore.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + DataBackupRestore.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + DataBackupRestore.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + DataBackupRestore.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + DataBackupRestore.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + DataBackupRestore.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + DataBackupRestore.EmployeeType + "</td></tr>";
                if (DataBackupRestore.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + DataBackupRestore.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = DataBackupRestore.EmployeeLocation;

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Requirement For: " + DataBackupRestore.RequirementFor + "</td></tr>";

            switch (DataBackupRestore.RequirementFor)
            {
                case "Backup":
                    body = CreateBackupDetailsBody(body, DataBackupRestore);
                    break;
                case "Restore":
                    body = CreateRestoreDetailsBody(body, DataBackupRestore);
                    break;
                case "Both":
                    body = CreateBackupDetailsBody(body, DataBackupRestore);
                    body = CreateRestoreDetailsBody(body, DataBackupRestore);
                    break;
                default:
                    break;
            }

            body = body + "<tr><td>" + "Reason for Request: " + DataBackupRestore.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";


            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                    approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                    : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        private static string CreateBackupDetailsBody(string body, DataBackupRestore backupRestoreRequest)
        {
            body = body + "<tr><td>" + "Request For: " + backupRestoreRequest.RequestFor + "</td></tr>";
            if (backupRestoreRequest.RequestFor == "FileFolderBackup")
            {
                body = body + "<tr><td>" + "Folder Path: " + backupRestoreRequest.FolderPath + "</td></tr>";
                body = body + "<tr><td>" + "Size: " + backupRestoreRequest.FolderSize + "</td></tr>";
                body = body + "<tr><td>" + "Retension Period: " + backupRestoreRequest.RetentionPeriod + "</td></tr>";
            }
            else
            {
                body = body + "<tr><td>" + "Server Name: " + backupRestoreRequest.BackupServerName + "</td></tr>";
                body = body + "<tr><td>" + "IP Address: " + backupRestoreRequest.BackupIpAddress + "</td></tr>";
            }

            body = body + "<tr><td>" + "Backup Type: " + backupRestoreRequest.BackupType + "</td></tr>";
            return body;
        }

        private static string CreateRestoreDetailsBody(string body, DataBackupRestore backupRestoreRequest)
        {
            body = body + "<tr><td>" + "Restore At: " + backupRestoreRequest.RestoreAt + "</td></tr>";
            if (backupRestoreRequest.RestoreAt == "AlternatePath")
            {
                body = body + "<tr><td>" + "Alternate Folder Path: " + backupRestoreRequest.AlternateFolderPath + "</td></tr>";
            }
            body = body + "<tr><td>" + "Server Name: " + backupRestoreRequest.RestoreServerName + "</td></tr>";
            body = body + "<tr><td>" + "IP Address: " + backupRestoreRequest.RestoreIpAddress + "</td></tr>";
            body = body + "<tr><td>" + "Restore Date: " + backupRestoreRequest.RestoreDate.ToString().Split(new char[] { ' ' })[0] + "</td></tr>";
            return body;
        }

        //Get Internet Access Form and Approval Data FAM- Final Approval Mail
        public async Task<EmailDataModel> GetInternetAccessFAM(int formId, UserData currentUser)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Internet Access Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            //var approver = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            InternetAcessData InternetAccess = new InternetAcessData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseInternetAccess = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('InternetAccess')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,EmployeeRequestType," +
                    "IsSpecialRequest,TempFrom,TempTo,BusinessNeed,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor,MoreInformation,EmailId," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextInternetAccess = await responseInternetAccess.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextInternetAccess))
            {
                var result = JsonConvert.DeserializeObject<InternetAcessModel>(responseTextInternetAccess, settings);
                InternetAccess = result.List.InternetList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (InternetAccess.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + InternetAccess.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + InternetAccess.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + InternetAccess.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + InternetAccess.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + InternetAccess.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + InternetAccess.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + InternetAccess.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + InternetAccess.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + InternetAccess.OtherEmployeeType + "</td></tr>";
                if (InternetAccess.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + InternetAccess.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = InternetAccess.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + InternetAccess.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + InternetAccess.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + InternetAccess.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + InternetAccess.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + InternetAccess.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + InternetAccess.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + InternetAccess.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + InternetAccess.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + InternetAccess.EmployeeType + "</td></tr>";
                if (InternetAccess.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + InternetAccess.ExternalOtherOrganizationName + "</td></tr>";

                }



                employeeLocation = InternetAccess.EmployeeLocation;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            if (InternetAccess.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + InternetAccess.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + InternetAccess.TempTo + "</td></tr>";
            }
            // body = body + "<tr><td>" + "Reason for Request: " + InternetAccess.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Is it a special request: " + InternetAccess.IsSpecialRequest + "</td></tr>";
            if (InternetAccess.IsSpecialRequest == "Yes")
            {
                body = body + "<tr><td>" + "Reason for Special Request: " + InternetAccess.MoreInformation + "</td></tr>";
            }
            body = body + "</table><br>";
            // body = body + "<tr><td>" + "Role: " + IAData.Role + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + InternetAccess.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            //if (InternetAccess.EmployeeType == "External")
            if (InternetAccess.OtherEmployeeType == "External")
            {
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                        approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                        : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";
            }


            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }
        public async Task<EmailDataModel> GetInternetAccessFAM_SQL(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Internet Access Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            #region Comment
            //var approver = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            //GlobalClass gc = new GlobalClass();
            //var handler = new HttpClientHandler();
            //handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            //var client = new HttpClient(handler);
            //client.BaseAddress = new Uri(conString);
            //client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            //var responseInternetAccess = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('InternetAccess')/items?$select=ID,EmployeeType,EmployeeCode," +
            //        "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,EmployeeRequestType," +
            //        "IsSpecialRequest,TempFrom,TempTo,BusinessNeed,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor,MoreInformation,EmailId," +
            //        "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
            //        "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
            //        "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            //var responseTextInternetAccess = await responseInternetAccess.Content.ReadAsStringAsync();

            //var settings = new JsonSerializerSettings
            //{
            //    NullValueHandling = NullValueHandling.Ignore
            //};
            //if (!string.IsNullOrEmpty(responseTextInternetAccess))
            //{
            //    var result = JsonConvert.DeserializeObject<InternetAcessModel>(responseTextInternetAccess, settings);
            //    InternetAccess = result.List.InternetList[0];
            //}
            #endregion
            InternetAcessData InternetAccess = new InternetAcessData();
            List<InternetAcessData> item = new List<InternetAcessData>();
            InternetAcessData model = new InternetAcessData();
            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataTable dt = new DataTable();
            con = new SqlConnection(sqlConString);
            cmd = new SqlCommand("ViewIAFDetails", con);
            cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
            cmd.CommandType = CommandType.StoredProcedure;
            adapter.SelectCommand = cmd;
            con.Open();
            adapter.Fill(dt);
            con.Close();
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    //int emp;
                    model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                    FormLookup item1 = new FormLookup();
                    item1.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                    if (dt.Rows[i]["Created"] != DBNull.Value)
                        item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                    model.FormID = item1;
                    model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                    model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                    model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                    model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                    model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                    model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                    model.EmployeeContactNo = Convert.ToString(dt.Rows[0]["EmployeeContactNo"]);
                    model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                    model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                    model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                    model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                    model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                    model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                    model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                    model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                    //model.OtherEmployeeContactNo = Convert.ToInt64(dt.Rows[0]["OtherEmployeeContactNo"]);
                    model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                    model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                    model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                    model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                    model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                    model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                    model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                    model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                    model.EmployeeRequestType = Convert.ToString(dt.Rows[0]["EmployeeRequestType"]);
                    model.BusinessNeed = Convert.ToString(dt.Rows[0]["BusinessNeed"]);
                    model.IsSpecialRequest = Convert.ToString(dt.Rows[0]["IsSpecialRequest"]);
                    model.MoreInformation = Convert.ToString(dt.Rows[0]["MoreInformation"]);
                    model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                    DateTime? TempFrom = null;
                    DateTime? TempTo = null;
                    if (dt.Rows[0]["TempFrom"] != DBNull.Value)
                        TempFrom = Convert.ToDateTime(dt.Rows[0]["TempFrom"]);
                    model.TempFrom = TempFrom;
                    if (dt.Rows[0]["TempTo"] != DBNull.Value)
                        TempTo = Convert.ToDateTime(dt.Rows[0]["TempTo"]);
                    model.TempTo = TempTo;
                    item.Add(model);
                }
            }

            if (item.Count > 0)
            {
                InternetAccess = item[0];
            }
            if (Status != 1)
            {
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

                if (InternetAccess.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + InternetAccess.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + InternetAccess.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + InternetAccess.OtherEmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + InternetAccess.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + InternetAccess.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + InternetAccess.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + InternetAccess.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + InternetAccess.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + InternetAccess.OtherEmployeeType + "</td></tr>";
                    if (InternetAccess.OtherEmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + InternetAccess.OtherExternalOtherOrganizationName + "</td></tr>";

                    }
                    employeeLocation = InternetAccess.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + InternetAccess.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + InternetAccess.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + InternetAccess.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + InternetAccess.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + InternetAccess.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + InternetAccess.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + InternetAccess.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + InternetAccess.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + InternetAccess.EmployeeType + "</td></tr>";
                    if (InternetAccess.EmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + InternetAccess.ExternalOtherOrganizationName + "</td></tr>";

                    }



                    employeeLocation = InternetAccess.EmployeeLocation;
                    body = body + "</table><br>";
                }
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            if (InternetAccess.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + InternetAccess.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + InternetAccess.TempTo + "</td></tr>";
            }
            // body = body + "<tr><td>" + "Reason for Request: " + InternetAccess.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Is it a special request: " + InternetAccess.IsSpecialRequest + "</td></tr>";
            if (InternetAccess.IsSpecialRequest == "Yes")
            {
                body = body + "<tr><td>" + "Reason for Special Request: " + InternetAccess.MoreInformation + "</td></tr>";
            }
            body = body + "</table><br>";
            // body = body + "<tr><td>" + "Role: " + IAData.Role + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Request: " + InternetAccess.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            if (Status != 1)
            {
                //approvers
                //if (InternetAccess.EmployeeType == "External")
                if (InternetAccess.OtherEmployeeType == "External")
                {
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                            approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                            : "") + "</td></tr>";
                        body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";
                }

                //task fulfilment details
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                    "</b></th></tr>";
                body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                body = body + "<tr><td>" + "Comments: </td></tr>";
                body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

            }

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        public async Task<EmailDataModel> GetSoftwareRequisitionFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Software Requisition Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            SoftwareRequisitionRequestDto SoftwareData = new SoftwareRequisitionRequestDto();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseSoftwareData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SoftwareRequisition')/items?$select=ID,EmployeeType,EmployeeCode," +
                            "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment,EmployeeRequestType,TempFrom,TempTo," +
                            "BusinessNeed,IsNonStandard,IsStandard,EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor," +
                            "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                            "OtherEmployeeLocation,OtherExternalOrganizationName,OtherExternalOtherOrgName,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextSoftwareData = await responseSoftwareData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextSoftwareData))
            {
                var result = JsonConvert.DeserializeObject<SoftwareRequisitionModel>(responseTextSoftwareData, settings);
                SoftwareData = result.List.SoftwareList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (SoftwareData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + SoftwareData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SoftwareData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + SoftwareData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SoftwareData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SoftwareData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SoftwareData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + SoftwareData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SoftwareData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SoftwareData.OtherEmployeeType + "</td></tr>";
                if (SoftwareData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SoftwareData.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = SoftwareData.OtherEmployeeLocation;


                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + SoftwareData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + SoftwareData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + SoftwareData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + SoftwareData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + SoftwareData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + SoftwareData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + SoftwareData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + SoftwareData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + SoftwareData.EmployeeType + "</td></tr>";
                if (SoftwareData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + SoftwareData.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = SoftwareData.EmployeeLocation;

                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Type of Request: " + SoftwareData.EmployeeRequestType + "</td></tr>";
            if (SoftwareData.EmployeeRequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + SoftwareData.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + SoftwareData.TempTo + "</td></tr>";
            }
            body = body + "<tr><td>" + "Reason for Request: " + SoftwareData.BusinessNeed + "</td></tr>";
            body = body + "<tr><td>" + "Selected Software Details: </td></tr>";


            //Software Details        
            List<SelectedSoftwareDto> ObjSelectedSoftwareDto = new List<SelectedSoftwareDto>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseSoftwareDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('SoftwareDetails')/items?$select=SoftwareName,SoftwareVersion,SoftwareType,IsOtherSoftware&$filter=(SoftwareRequisitionID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            var responseTextSoftwareDetails = await responseSoftwareDetails.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseTextSoftwareDetails))
            {
                var SoftwareDetailsresult = JsonConvert.DeserializeObject<SelectedSoftwareModel>(responseTextSoftwareDetails);
                ObjSelectedSoftwareDto = SoftwareDetailsresult.List.AVLSoftwareList;

                foreach (var soft in ObjSelectedSoftwareDto)
                {
                    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "Software Name: " + soft.SoftwareName + "</td></tr>";
                    body = body + "<tr><td>" + "Software Type: " + soft.SoftwareType + "</td></tr>";
                    body = body + "<tr><td>" + "Software Version: " + soft.SoftwareVersion + "</td></tr>";
                }
            }
            body = body + "</table><br>";


            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                  approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                  : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        public async Task<EmailDataModel> GetSharedFolderFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Shared Folder Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            SharedFolderData sharedFolderData = new SharedFolderData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responsesharedFolderData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SharedFolder')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,RequestFor,ChangeType,ChangeFileServerName,ChangeFolderPath,ChangeSize,FolderOwnerName," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "BusinessNeed,EmployeeLocation,EmployeeDesignation,RequestSubmissionFor,ProposedFolderOwnerName,ProposedFolderOwnerEmpNum,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextsharedFolderData = await responsesharedFolderData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextsharedFolderData))
            {
                var result = JsonConvert.DeserializeObject<SharedFolderModel>(responseTextsharedFolderData, settings);
                sharedFolderData = result.List.SharedFolderList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (sharedFolderData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + sharedFolderData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + sharedFolderData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + sharedFolderData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + sharedFolderData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + sharedFolderData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + sharedFolderData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + sharedFolderData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + sharedFolderData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + sharedFolderData.OtherEmployeeType + "</td></tr>";
                if (sharedFolderData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + sharedFolderData.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = sharedFolderData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + sharedFolderData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + sharedFolderData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + sharedFolderData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + sharedFolderData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + sharedFolderData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + sharedFolderData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + sharedFolderData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + sharedFolderData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + sharedFolderData.EmployeeType + "</td></tr>";
                if (sharedFolderData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + sharedFolderData.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = sharedFolderData.EmployeeLocation;
                body = body + "</table><br>";
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Request Type: " + sharedFolderData.RequestType + "</td></tr>";
            if (sharedFolderData.RequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + sharedFolderData.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + sharedFolderData.TempTo + "</td></tr>";
            }
            if (sharedFolderData.RequestFor == "Creation")
            {
                //Folder Creation Details
                List<SharedFolderCreationRequestDto> objSharedFolderCreationRequestDto = new List<SharedFolderCreationRequestDto>();
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseSharedFolderCreationDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('SharedFolderCreationDetails')/items?$select=FileServerName,OwnerName,FolderPath,Size&$filter=(SharedFolderID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
                var responseTextSharedFolderCreationDetails = await responseSharedFolderCreationDetails.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseTextSharedFolderCreationDetails))
                {
                    var resultSharedFolderCreationDetails = JsonConvert.DeserializeObject<SharedFolderCreationModel>(responseTextSharedFolderCreationDetails);
                    objSharedFolderCreationRequestDto = resultSharedFolderCreationDetails.List.CreationList;

                    body = body + "<tr><td>" + "Shared Folder Request For: " + "Creation(New Share Folder)" + "</td></tr>";
                    foreach (var FolderCreation in objSharedFolderCreationRequestDto)
                    {
                        body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                        body = body + "<tr><td>" + "File Server Name: " + FolderCreation.FileServerName + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Name: " + FolderCreation.CreationFolderPath + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Size in GB: " + FolderCreation.Size + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Owner Name: " + FolderCreation.CreationOwnerName + "</td></tr>";
                    }
                }

                body = body + "</table><br>";
            }

            if (sharedFolderData.RequestFor == "Change")
            {

                body = body + "<tr><td>" + "Shared Folder Request For: " + "Change(Space Required/Folder Owner Name)" + "</td></tr>";
                if (sharedFolderData.ChangeType == "Both")
                {
                    body = body + "<tr><td>" + "Change In: " + "Folder Size in GB & Folder Owner Name" + "</td></tr>";
                }
                else if (sharedFolderData.ChangeType == "ChangeInFolderOwnerName")
                {
                    body = body + "<tr><td>" + "Change In: " + "Folder Owner Name" + "</td></tr>";
                }
                else
                {
                    body = body + "<tr><td>" + "Change In: " + "Folder Size in GB" + "</td></tr>";
                }
                body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                body = body + "<tr><td>" + "File Server Name: " + sharedFolderData.ChangeFileServerName + "</td></tr>";
                body = body + "<tr><td>" + "Folder Path: " + sharedFolderData.ChangeFolderPath + "</td></tr>";


                if (sharedFolderData.ChangeType == "ChangeInFolderOwnerName")
                {
                    body = body + "<tr><td>" + "Current Folder Owner Name: " + sharedFolderData.FolderOwnerName + "</td></tr>";
                    body = body + "<tr><td>" + "Propoasd Folder Owner Name: " + sharedFolderData.ProposedFolderOwnerName + "</td></tr>";
                }
                if (sharedFolderData.ChangeType == "ChangeInSize")
                {
                    body = body + "<tr><td>" + "Folder Size in GB: " + sharedFolderData.ChangeSize + "</td></tr>";
                }
                if (sharedFolderData.ChangeType == "Both")
                {
                    body = body + "<tr><td>" + "Current Folder Owner Name: " + sharedFolderData.FolderOwnerName + "</td></tr>";
                    body = body + "<tr><td>" + "Proposed Folder Owner Name: " + sharedFolderData.ProposedFolderOwnerName + "</td></tr>";
                    body = body + "<tr><td>" + "Folder Size in GB: " + sharedFolderData.ChangeSize + "</td></tr>";
                }
            }
            if (sharedFolderData.RequestFor == "AddRemoveMembers")
            {
                //Folder AddRemoveMembers Details
                body = body + "<tr><td>" + "Shared Folder Request For: " + "Access Change - Add/Remove Users" + "</td></tr>";
                List<SharedFolderAddRemoveUserRequestDto> objSharedFolderAddRemoveUserRequestDto = new List<SharedFolderAddRemoveUserRequestDto>();
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseSharedFolderAddRemoveMembersDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('SharedFolderAddRemoveUserDetails')/items?$select=FileServerName,FolderOwnerName,FolderPath,UserId,Read,ReadWrite,Remove,OwnerEmployeeNumber,Email&$filter=(SharedFolderID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
                var responseTextSharedFolderAddRemoveMembersDetails = await responseSharedFolderAddRemoveMembersDetails.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseTextSharedFolderAddRemoveMembersDetails))
                {
                    var resultSharedFolderAddRemoveMembersDetails = JsonConvert.DeserializeObject<SharedFolderAddRemoveUserModel>(responseTextSharedFolderAddRemoveMembersDetails);
                    objSharedFolderAddRemoveUserRequestDto = resultSharedFolderAddRemoveMembersDetails.AddRemoveList.AddRemoveUsersList;

                    foreach (var AddRemoveMembers in objSharedFolderAddRemoveUserRequestDto)
                    {

                        body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                        body = body + "<tr><td>" + "File Server Name: " + AddRemoveMembers.FileServerName + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Path: " + AddRemoveMembers.FolderPath + "</td></tr>";
                        body = body + "<tr><td>" + "Folder Owner Name: " + AddRemoveMembers.FolderOwnerName + "</td></tr>";
                        body = body + "<tr><td>" + "User ID: " + AddRemoveMembers.UserId + "</td></tr>";
                        body = body + "<tr><td>" + "Access Details: </td></tr>";
                        if (Convert.ToBoolean(AddRemoveMembers.Read == true))
                        {
                            body = body + "<tr><td>" + "Read Access: " + "Yes" + "</td></tr>";
                        }
                        else
                        {
                            body = body + "<tr><td>" + "Read Access: " + "No" + "</td></tr>";
                        }
                        if (Convert.ToBoolean(AddRemoveMembers.ReadWrite == true))
                        {
                            body = body + "<tr><td>" + "Read Write Access: " + "Yes" + "</td></tr>";
                        }
                        else
                        {
                            body = body + "<tr><td>" + "Read Write Access: " + "No" + "</td></tr>";
                        }

                        if (Convert.ToBoolean(AddRemoveMembers.Remove == true))
                        {
                            body = body + "<tr><td>" + "Remove Access: " + "Yes" + "</td></tr>";
                        }
                        else
                        {
                            body = body + "<tr><td>" + "Remove Access: " + "No" + "</td></tr>";

                        }
                    }
                }
            }

            body = body + "<tr><td>" + "Business Functions, Reason & Responsibility for Creation/Change : " + sharedFolderData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }
        public async Task<EmailDataModel> GetITAssetRequisitionFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "IT Asset Requisition Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            ITAssetRequisitionRequestDto ITAssetData = new ITAssetRequisitionRequestDto();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            //var responseITAssetData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ITAssetRequisition')/items?$select=ID,EmployeeType,Created,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
            //        "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeDepartment,EmployeeContactNo,RequestType,TempFrom,TempTo,BusinessNeed,EmployeeLocation,EmployeeDesignation," +
            //        "WorkstationDesktop,WorkstationLaptop,RSAToken,SIMAndData,Landline,LANCableAndPort,JabraSpeaker,AdditionalOfficeMonitor,AdditionalOfficeMonitor,iPad,PartnerOrganizationName,WorkflowType,RequestSubmissionFor," +
            //        "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
            //        "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,OtherPartnerOrganizationName,UsageType," +
            //        "FormID/ID,FormID/Created,Desktop,Laptop&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;         

            var responseITAssetData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ITAssetRequisition')/items?$select=*,FormID/ID"
         + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextITAssetData = await responseITAssetData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextITAssetData))
            {
                var result = JsonConvert.DeserializeObject<ITAssetRequisitionModel>(responseTextITAssetData, settings);
                ITAssetData = result.List.ITAssetList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (ITAssetData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + ITAssetData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITAssetData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + ITAssetData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITAssetData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITAssetData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITAssetData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITAssetData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITAssetData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITAssetData.OtherEmployeeType + "</td></tr>";
                if (ITAssetData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITAssetData.OtherExternalOtherOrganizationName + "</td></tr>";
                }
                employeeLocation = ITAssetData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + ITAssetData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + ITAssetData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + ITAssetData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + ITAssetData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + ITAssetData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + ITAssetData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + ITAssetData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + ITAssetData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + ITAssetData.EmployeeType + "</td></tr>";
                if (ITAssetData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + ITAssetData.ExternalOtherOrganizationName + "</td></tr>";
                }
                employeeLocation = ITAssetData.EmployeeLocation;
                body = body + "</table><br>";
            }


            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

            body = body + "<tr><td>" + "Requested Asset Details: </td></tr>";
            body = body + "<tr><td>" + "*************************************" + "</td></tr>";
            if (ITAssetData.Desktop == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Office Desktop Set (CPU, Monitor, Keyboard & Mouse)" + "</td></tr>";
            }

            if (ITAssetData.Laptop == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Office Laptop + MS-Teams + Skype + VPN" + "</td></tr>";
            }

            if (ITAssetData.WorkstationDesktop == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Workstation Desktop Set (CPU, Monitor, Keyboard & Mouse)" + "</td></tr>";
            }

            if (ITAssetData.WorkstationLaptop == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Workstation Laptop  + MS-Teams + Skype + VPN" + "</td></tr>";
            }

            if (ITAssetData.Landline == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "One landline phone will be shared between two employees sitting next to each other. Exceptions would be given to cabin users, and users placed at corner Desk.)" + "</td></tr>";
            }

            if (ITAssetData.AdditionalOfficeMonitor == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Office Monitor + Keyboard + Mouse" + "</td></tr>";
            }

            if (ITAssetData.LANCableAndPort == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "LAN Cable And Port" + "</td></tr>";
            }

            if (ITAssetData.RSAToken == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "RSA Token" + "</td></tr>";
            }

            if (ITAssetData.SIMAndData == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "SIM + Data" + "</td></tr>";
            }

            if (ITAssetData.JabraSpeaker == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Jabra Speaker" + "</td></tr>";
            }

            if (ITAssetData.iPad == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "iPad Or Tablet" + "</td></tr>";
            }

            if (ITAssetData.CabinsScreen == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "Cabins Screen 55” with MS teams Setup (Budgetary Cost INR 2,00,000.00)" + "</td></tr>";
            }

            if (ITAssetData.NewMeetingRoomSetup == "Yes")
            {
                body = body + "<tr><td>" + "IT Asset Name: " + "New Meeting Room Setup (TV Screen + MS teams Setup) (minimum Budgetary Cost in INR 3,00,000.00)" + "</td></tr>";
            }

            body = body + "<tr><td>" + "*************************************" + "</td></tr>";

            body = body + "<tr><td>" + "Request Type: " + ITAssetData.RequestType + "</td></tr>";
            if (ITAssetData.RequestType == "Temporary")
            {
                body = body + "<tr><td>" + "From Date: " + ITAssetData.TempFrom + "</td></tr>";
                body = body + "<tr><td>" + "To Date: " + ITAssetData.TempTo + "</td></tr>";
            }

            body = body + "<tr><td>" + "Usage of requested IT Asset: " + ITAssetData.UsageType + "</td></tr>";

            body = body + "<tr><td>" + "Reason for Request: " + ITAssetData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        public List<ITServiceDeskContactModel> GetEmailIdByLocation(string location)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ITServiceDeskContactModel> contactList = new List<ITServiceDeskContactModel>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetEmailIdByLocations", con);
                cmd.Parameters.Add(new SqlParameter("@location", location));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        ITServiceDeskContactModel contact = new ITServiceDeskContactModel();
                        contact.ContactId = Convert.ToInt32(ds.Tables[0].Rows[i]["ContactId"]);
                        contact.Email = Convert.ToString(ds.Tables[0].Rows[i]["Email"]);
                        contact.IsManager = Convert.ToInt32(ds.Tables[0].Rows[i]["IsManager"]);
                        contact.LocationId = Convert.ToInt32(ds.Tables[0].Rows[i]["LocationId"]);

                        contactList.Add(contact);
                    }
                }
                return contactList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<ITServiceDeskContactModel>();
            }
        }

        public async Task<EmailDataModel> GetCabBookingFormBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Cab Booking Request Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

            CBRFData CBRFData = new CBRFData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseCBRF = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('CabBookingRequestForm')/items?$select=ID,SelfEmployeeIDNo,SelfMobile,SelfTelephone,OnBehlafDepartment,OnBehalfMobile,OnBehalfEmployeeIDNo,OnBehalfTelephone,OnBehalfCostCenterNumber,OnBehalfDesignation,Designation,Department,CostCenterNumber,ShoppingCartNo,Name,ContactNumber,CarRequiredFromDate,CarRequiredToDate,UserName,UserContactNumber,ReportingPlaceWithAddress,"
               + "ReportingTime,EmployeeName,EmployeeEmailId,RequestSubmissionFor,OtherEmployeeName,OtherEmployeeEmailId,ReasonforBooking,Destination,TypeofCar,TypeofCarOther,VehicleNumber,VehicleNumberOther,NumberofUsers,AirportPickUpDrop,FlightNo,FlightTime,CarRequiredFromTime,ReportingTime,FlightTime,Location,OnBehalfLocation,"
          + "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=FormID")).Result;

            var responseTextCBRF = await responseCBRF.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextCBRF))
            {
                var result = JsonConvert.DeserializeObject<CabBookingRequestModel>(responseTextCBRF, settings);
                CBRFData = result.cbrfflist.cbrfData[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (CBRFData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + CBRFData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + CBRFData.OnBehalfEmployeeIDNo + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + CBRFData.OnBehlafDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + CBRFData.OnBehalfCostCenterNumber + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + CBRFData.OnBehalfDesignation + " </td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + CBRFData.OnBehalfMobile + "</td></tr>";
                body = body + "<tr><td>" + "Email ID: " + CBRFData.OtherEmployeeEmailId + " </td></tr>";
                body = body + "<tr><td>" + "Location: " + CBRFData.OnBehalfLocation + "</td></tr>";

                employeeLocation = CBRFData.OnBehalfLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + CBRFData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + CBRFData.SelfEmployeeIDNo + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + CBRFData.Department + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + CBRFData.CostCenterNumber + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + CBRFData.Designation + " </td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + CBRFData.SelfMobile + "</td></tr>";
                body = body + "<tr><td>" + "Email ID: " + CBRFData.EmployeeEmailId + " </td></tr>";
                body = body + "<tr><td>" + "Location: " + CBRFData.Location + "</td></tr>";

                employeeLocation = CBRFData.Location;
                body = body + "</table><br>";
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

            body = body + "<tr><td>" + "Shopping Cart No: " + CBRFData.ShoppingCartNo + "</td></tr>";
            body = body + "<tr><td>" + "Name (Booked By): " + CBRFData.Name + "</td></tr>";
            body = body + "<tr><td>" + "Contact Number (Booked By): " + CBRFData.ContactNumber + "</td></tr>";
            body = body + "<tr><td>" + "Car Required : " + "**********" + "</td></tr>";
            body = body + "<tr><td>" + "From (DD-MM-YY) & Time: " + CBRFData.CarRequiredFromDate.ToString("dd/MM/yyyy h:mm tt") + "</td></tr>";
            body = body + "<tr><td>" + "To (DD-MM-YY) & Time: " + CBRFData.CarRequiredToDate.ToString("dd/MM/yyyy h:mm tt") + "</td></tr>";


            //body = body + "<tr><td>" + "User Name: " + CBRFData.UserName + "</td></tr>";
            //body = body + "<tr><td>" + "User Contact Number: " + CBRFData.UserContactNumber + "</td></tr>";
            //body = body + "<tr><td>" + "Reporting Place with address: " + CBRFData.ReportingPlaceWithAddress + "</td></tr>";
            //body = body + "<tr><td>" + "Reporting Time: " + CBRFData.ReportingTime.ToString("hh:mm tt") + "</td></tr>";
            //body = body + "<tr><td>" + "Destination : " + CBRFData.Destination + "</td></tr>";
            body = body + "<tr><td>" + "Car : " + "**********" + "</td></tr>";
            body = body + "<tr><td>" + "Number of Users : " + CBRFData.NumberofUsers + " </td></tr>";
            body = body + "<tr><td>" + "Flight No & Time (Incase it is Airport Pickup or Drop) : " + "**********" + "</td></tr>";
            body = body + "<tr><td>" + "Airport Pick Up Drop : " + CBRFData.AirportPickUpDrop + " </td></tr>";
            if (CBRFData.AirportPickUpDrop == "Yes")
            {
                body = body + "<tr><td>" + "Flight No : " + CBRFData.FlightNo + " </td></tr>";
                body = body + "<tr><td>" + "Flight Time : " + CBRFData.FlightTime.ToString("hh:mm tt") + " </td></tr>";
            }

            body = body + "<tr><td>" + "Action : " + "**********" + "</td></tr>";

            if (CBRFData.TypeofCar == "Other")
            {
                body = body + "<tr><td>" + "Type of Car : " + CBRFData.TypeofCarOther + " </td></tr>";
            }
            else
            {
                body = body + "<tr><td>" + "Type of Car : " + CBRFData.TypeofCar + " </td></tr>";
            }
            if (CBRFData.VehicleNumber == "Other")
            {
                body = body + "<tr><td>" + "Vehicle number : " + CBRFData.VehicleNumberOther + " </td></tr>";
            }
            else
            {
                body = body + "<tr><td>" + "Vehicle number : " + CBRFData.VehicleNumber + " </td></tr>";
            }


            //Cab user Details        
            List<CabUsersDto> ObjSelectedSoftwareDto = new List<CabUsersDto>();
            var clientcab = new HttpClient(handler);
            clientcab.BaseAddress = new Uri(conString);
            clientcab.DefaultRequestHeaders.Accept.Clear();
            clientcab.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseCabUser = Task.Run(() => clientcab.GetAsync("_api/web/lists/GetByTitle('CabBookingUserDetails')/items?$select=UserName,UserContactNumber,Destination,ReportingTime,ReportingPlaceWithAddress&$filter=(CabUsersId eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            var responseTextCabUser = await responseCabUser.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseTextCabUser))
            {
                var SoftwareDetailsresult = JsonConvert.DeserializeObject<CabUsersModel>(responseTextCabUser);
                ObjSelectedSoftwareDto = SoftwareDetailsresult.List.CabUsersList;


                body = body + "<tr><td>" + "Booked For : " + "**********" + "</td></tr>";
                foreach (var cab in ObjSelectedSoftwareDto)
                {
                    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "User Name: " + cab.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "User Contact Number: " + cab.UserContactNumber + "</td></tr>";
                    body = body + "<tr><td>" + "Destination: " + cab.Destination + "</td></tr>";
                    body = body + "<tr><td>" + "Reporting Time: " + cab.ReportingTime + "</td></tr>";
                    body = body + "<tr><td>" + "Reporting Place With Address: " + cab.ReportingPlaceWithAddress + "</td></tr>";
                }
            }

            body = body + "<tr><td>" + "Reason for booking: " + CBRFData.ReasonforBooking + "</td></tr>";

            body = body + "</table><br>";


            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };

        }

        public async Task<EmailDataModel> GetSuggestionForOrderFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Suggestion For Order Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            SuggestionForOrderData suggestionForOrderData = new SuggestionForOrderData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseSuggestionForOrderData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('OrderDetails')/items?$select=ID, EmployeeType, EmployeeCode, " +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeLocation,RequestSubmissionFor,Department, Date,ShopCartNumber," +
                    "ConcernSection,Budget,Currency,Description,TechDisqualify,SuggestOrder,ConversionValue,AttachmentFiles,DeviationNoteForm,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles")).Result;

            var responseTextSuggestionForOrderData = await responseSuggestionForOrderData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextSuggestionForOrderData))
            {
                var result = JsonConvert.DeserializeObject<SuggestionForOrderModel>(responseTextSuggestionForOrderData, settings);
                suggestionForOrderData = result.List.SuggestionForOrderList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (suggestionForOrderData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + suggestionForOrderData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + suggestionForOrderData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + suggestionForOrderData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + suggestionForOrderData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + suggestionForOrderData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + suggestionForOrderData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + suggestionForOrderData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + suggestionForOrderData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + suggestionForOrderData.OtherEmployeeType + "</td></tr>";
                if (suggestionForOrderData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + suggestionForOrderData.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = suggestionForOrderData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + suggestionForOrderData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + suggestionForOrderData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + suggestionForOrderData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + suggestionForOrderData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + suggestionForOrderData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + suggestionForOrderData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + suggestionForOrderData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + suggestionForOrderData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + suggestionForOrderData.EmployeeType + "</td></tr>";
                if (suggestionForOrderData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + suggestionForOrderData.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = suggestionForOrderData.EmployeeLocation;
                body = body + "</table><br>";
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

            //  body = body + "<tr><td>" + "Department: " + suggestionForOrderData.Department + "</td></tr>";
            body = body + "<tr><td>" + "Shopping Cart Number: " + suggestionForOrderData.ShopCartNumber + "</td></tr>";
            body = body + "<tr><td>" + "Concerned Section / Facility: " + suggestionForOrderData.ConcernSection + "</td></tr>";
            //  body = body + "<tr><td>" + "Date: " + suggestionForOrderData.Date + "</td></tr>";

            body = body + "<tr><td>" + " Budget: " + suggestionForOrderData.Budget.ToString("n0") + "</td></tr>";

            body = body + "<tr><td>" + "Currency: " + suggestionForOrderData.Currency + "</td></tr>";
            body = body + "<tr><td>" + "Conversion Value: " + suggestionForOrderData.ConversionValue + "</td></tr>";

            //SFO Order Items Data 
            List<SFOOrderItemsData> objSFOOrderItemsDataRequestDto = new List<SFOOrderItemsData>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseSFOOrderItemsDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('OrderItems')/items?$select=SupplierName,Comments,TechAcceptance,OfferPrice,Currency,ConversionRate&$filter=(OrderDetailsID eq '" + rowId + "' and FormId eq '" + formId + "')")).Result;
            var responseTextSFOOrderItemsDetails = await responseSFOOrderItemsDetails.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(responseTextSFOOrderItemsDetails))
            {
                //var resultSharedFolderCreationDetails = JsonConvert.DeserializeObject<SFOOrderItemsModel>(responseTextSFOOrderItemsDetails);

                var resultSharedFolderCreationDetails = JsonConvert.DeserializeObject<SFOOrderItemsModel>(responseTextSFOOrderItemsDetails, settings);

                objSFOOrderItemsDataRequestDto = resultSharedFolderCreationDetails.List.SFOOrderItemsList;

                body = body + "<tr><td>" + "Supplier Details: " + "</td></tr>";
                foreach (var OrderItem in objSFOOrderItemsDataRequestDto)
                {
                    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "Available Offers (Supplier Name): " + OrderItem.SupplierName + " </td></tr>";
                    body = body + "<tr><td>" + "Technically Acceptance: " + OrderItem.TechAcceptance + "</td></tr>";
                    if (OrderItem.Comments != null)
                    {
                        body = body + "<tr><td>" + "Comments: " + OrderItem.Comments + "</td></tr>";
                    }
                    body = body + "<tr><td>" + "Currency: " + OrderItem.Currency + "</td></tr>";
                    if (OrderItem.Currency != "Rupee")
                    {
                        body = body + "<tr><td>" + "Conversion Value: " + OrderItem.ConversionRate + "</td></tr>";
                    }
                    //body = body + "<tr><td>" + "Offer Price: " + OrderItem.OfferPrice.ToString("n0") + "</td></tr>";
                }
            }

            body = body + "</table><br>";

            body = body + "<tr><td>" + "Reasons for Technical Disqualification / Not Ok Supplier(s): " + suggestionForOrderData.TechDisqualify + "</td></tr>";
            body = body + "<tr><td>" + " Additional Comments: " + suggestionForOrderData.SuggestOrder + "</td></tr>";
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group:</td></tr>";
            body = body + "<tr><td>" + "Assigned To:  </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        public async Task<EmailDataModel> GetDeviationNoteFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Deviation Note Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            DNFData dNFData = new DNFData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseDNFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('DeviationNoteForm')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment," +
                    "EmployeeLocation,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,OtherExternalOrganizationName,OtherExternalOtherOrgName,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption," +
                    "Supplier,Description,Currency,Budget,ConversionValue,Department,Brand,Location,OnBehalfLocation,Reason,Reason1,Reason2,Reason3,Reason4,DeviationDate" +
                    "&$filter=(ID eq '" + rowId + "')")).Result;

            var responseTextDNFData = await responseDNFData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextDNFData))
            {
                var result = JsonConvert.DeserializeObject<DeviationNoteModel>(responseTextDNFData, settings);
                dNFData = result.dnflist.dnfData[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (dNFData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + dNFData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + dNFData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + dNFData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + dNFData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + dNFData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + dNFData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + dNFData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + dNFData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + dNFData.OtherEmployeeType + "</td></tr>";
                if (dNFData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + dNFData.OtherExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = dNFData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + dNFData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + dNFData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + dNFData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + dNFData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + dNFData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + dNFData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + dNFData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + dNFData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + dNFData.EmployeeType + "</td></tr>";
                if (dNFData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + dNFData.ExternalOtherOrganizationName + "</td></tr>";

                }
                employeeLocation = dNFData.EmployeeLocation;
                body = body + "</table><br>";
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

            body = body + "<tr><td>" + "Supplier: " + dNFData.Supplier + "</td></tr>";
            body = body + "<tr><td>" + "Description: " + dNFData.Description + "</td></tr>";
            body = body + "<tr><td>" + "Currency: " + dNFData.Currency + "</td></tr>";
            body = body + "<tr><td>" + "Budget/Contract/PO Value: " + dNFData.Budget + "</td></tr>";
            if (dNFData.Currency != "Rupee")
            {
                body = body + "<tr><td>" + "Conversion Value: " + dNFData.ConversionValue + "</td></tr>";
            }
            body = body + "<tr><td>" + "Brand: " + dNFData.Brand + "</td></tr>";

            body = body + "</table><br>";

            body = body + "<tr><td>" + "Reason for Deviation: " + "</td></tr>";
            if (dNFData.Reason1 == "Reason1")
            {
                body = body + "<tr><td>" + "1. Single Sourcing(except the cases referred in “Exception list : Deviation from General purchasing process” dated " + dNFData.DeviationDate + ")" + "</td></tr>";
            }
            if (dNFData.Reason2 == "Reason2")
            {
                body = body + "<tr><td>" + "2.Regularization of Invoices where ever the Procurement was not involved and no negotiation done." + "</td></tr>";
            }
            if (dNFData.Reason3 == "Reason3")
            {
                body = body + "<tr><td>" + "3. Rendering the services or delivery of Goods without PO (rates are pre-negotiated/approved by Procurement)" + "</td></tr>";
            }
            if (dNFData.Reason4 == "Reason4")
            {
                body = body + "<tr><td>" + "4. Late financial /Shopping Cart approval, An emergency, product change, critical shortage,a safety issue or no time for specifications." + "</td></tr>";
            }
            body = body + "</table><br>";
            body = body + "<tr><td>" + "Reason in detail for the deviation: " + dNFData.Reason + "</td></tr>";
            body = body + "</table><br>";
            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: </td></tr>";
            body = body + "<tr><td>" + "Assigned To:  </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        public async Task<EmailDataModel> GetIDCardFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "New ID Card Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                IDCFData IDCFData = new IDCFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseDNFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('IDCardForm')/items?$select=*" +
                        "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=AttachmentFiles")).Result;

                var responseTextDNFData = await responseDNFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextDNFData))
                {
                    var result = JsonConvert.DeserializeObject<IDCFModel>(responseTextDNFData, settings);
                    IDCFData = result.idcfflist.idcfData[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

                if (IDCFData.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + IDCFData.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + IDCFData.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + IDCFData.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + IDCFData.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + IDCFData.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + IDCFData.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + IDCFData.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + IDCFData.OtherEmployeeType + "</td></tr>";
                    if (IDCFData.OtherEmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + IDCFData.OtherExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = IDCFData.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + IDCFData.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + IDCFData.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + IDCFData.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + IDCFData.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + IDCFData.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + IDCFData.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + IDCFData.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + IDCFData.EmployeeType + "</td></tr>";
                    if (IDCFData.EmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + IDCFData.ExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = IDCFData.EmployeeLocation;
                    body = body + "</table><br>";
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Type Of Card: " + IDCFData.TypeOfCard + "</td></tr>";
                body = body + "<tr><td>" + "Date of Joining: " + IDCFData.DateofJoining + "</td></tr>";
                //var sharepointURL = System.Configuration.ConfigurationManager.AppSettings["SharepointSiteURL"];
                //var imgUrl = IDCFData.AttachmentFiles?.Attachments?.First().ServerRelativeUrl ?? "";
                //var imgName = IDCFData.AttachmentFiles?.Attachments[0].FileName;
                //body = body + "<tr><td>" + "Attachment: <a href='" + sharepointURL + imgUrl + "'>" + imgName + "</a></td></tr>";

                List<string> list = new List<string>() { "Apprentice", "Intern", "Graduate Engineer Trainee" };
                if ((IDCFData.RequestSubmissionFor == "OnBehalf"
                    && !string.IsNullOrEmpty(list.Find(x => x == IDCFData.OtherEmployeeDesignation)))
                    || IDCFData.RequestSubmissionFor == "Self" && !string.IsNullOrEmpty(list.Find(x => x == IDCFData.EmployeeDesignation)))
                {
                    body = body + "<tr><td>" + "Validity:(Active From): " + IDCFData.ValidityStartDate + "</td></tr>";
                    body = body + "<tr><td>" + "Validity:(End Date): " + IDCFData.ValidityEndDate + "</td></tr>";
                }
                body = body + "<tr><td>" + "Request Description: " + IDCFData.BusinessNeed + "</td></tr>";

                body = body + "<tr><td>" + "Action : " + "**********" + "</td></tr>";
                body = body + "<tr><td>" + "Date of Issue: " + IDCFData.DateofIssue + "</td></tr>";
                body = body + "<tr><td>" + "Chargable: " + (IDCFData.Chargable == "Yes"
                                                        ? (IDCFData.TypeOfCard == "PKI" ? "Yes (500)" : "Yes (250)")
                                                        : "No") + "</td></tr>";
                body = body + "<tr><td>" + "ID Card Number: " + IDCFData.IDCardNumber + "</td></tr>";
                body = body + "<tr><td>" + "Vendor Code: " + IDCFData.VendorCode + "</td></tr>";

                body = body + "</table><br>";
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                    "</b></th></tr>";
                body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                body = body + "<tr><td>" + "Comments: </td></tr>";
                body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                var ccCode = IDCFData.RequestSubmissionFor.ToLower() == "onbehalf" ? IDCFData.OtherEmployeeCCCode : IDCFData.EmployeeCCCode;
                var empCode = IDCFData.RequestSubmissionFor.ToLower() == "onbehalf" ? IDCFData.OtherEmployeeCode : IDCFData.EmployeeCode;
                var empLoc = IDCFData.RequestSubmissionFor.ToLower() == "onbehalf" ? IDCFData.OtherEmployeeLocation : IDCFData.EmployeeLocation;
                int locId = empLoc.ToLower().Contains("pune") ? 1 : empLoc.ToLower().Contains("aurangabad") ? 3 : 2;
                var ecfDal = new EmployeeClearanceDAL();
                var response = await ecfDal.GetApprovalECF(empCode, ccCode, locId);
                List<string> HRCApproversEmail = new List<string>();
                if (response.Status == 200 && (response.Model != null || response.Model.Count > 0))
                {
                    HRCApproversEmail = response.Model.ToList().Where(x =>
                        x.Designation.ToLower() == "skoda-vw hr"
                    ).Select(x => x.EmailId).ToList();
                }
                string additionalApprover = "";
                if (locId == 2)//For Mumbai and other locations
                    additionalApprover = "extern.shreekumar.pillai@skoda-vw.co.in";
                else if (locId == 3)
                    additionalApprover = "extern.rajendra.ghuge@skoda-vw.co.in";

                var toIds = approverList.Where(x => x.Designation.ToLower() == "id card office").Select(y => y.EmailId).ToList();
                if (!string.IsNullOrEmpty(additionalApprover))
                    toIds.Add(additionalApprover);

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = toIds };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetReissueIDCardFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Reissue ID Card Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            ReissueIDCardData rIDCFData = new ReissueIDCardData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseRIDCFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ReissueIDCardForm')/items?$select=ID,EmployeeType,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment," +
                    "BusinessNeed,EmployeeLocation,ExternalOrganizationName,RequestSubmissionFor,OnBehalfOption,DateofJoining," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,OtherExternalOrganizationName,EmployeeEmailId,OtherEmployeeEmailId,IDCardNumber,DateofIssue,ReasonforReissue,OtherReason,TypeOfCard,DateofIssue,Chargeable,ActiveFrom,EndDate,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

            var responseTextRIDCFData = await responseRIDCFData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextRIDCFData))
            {
                var result = JsonConvert.DeserializeObject<ReissueIDCardModel>(responseTextRIDCFData, settings);
                rIDCFData = result.List.ReissueIDCardList[0];
            }
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

            if (rIDCFData.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + rIDCFData.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + rIDCFData.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + rIDCFData.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + rIDCFData.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + rIDCFData.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + rIDCFData.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + rIDCFData.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + rIDCFData.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + rIDCFData.OtherEmployeeType + "</td></tr>";
                if (rIDCFData.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + rIDCFData.OtherExternalOrganizationName + "</td></tr>";

                }
                employeeLocation = rIDCFData.OtherEmployeeLocation;
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + rIDCFData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + rIDCFData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + rIDCFData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + rIDCFData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + rIDCFData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + rIDCFData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + rIDCFData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + rIDCFData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + rIDCFData.EmployeeType + "</td></tr>";
                if (rIDCFData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + rIDCFData.ExternalOrganizationName + "</td></tr>";

                }
                employeeLocation = rIDCFData.EmployeeLocation;
                body = body + "</table><br>";
            }

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

            body = body + "<tr><td>" + "Date of Joining: " + rIDCFData.DateofJoining.ToShortDateString() + "</td></tr>";
            body = body + "<tr><td>" + "Reason for Reissue: " + rIDCFData.ReasonforReissue + "</td></tr>";
            if (rIDCFData.ReasonforReissue == "Others")
            {
                body = body + "<tr><td>" + "Other Reason: " + rIDCFData.OtherReason + "</td></tr>";
            }
            if (rIDCFData.TypeOfCard == "PKI")
                body = body + "<tr><td>" + "Type Of Card: " + "PKI" + "</td></tr>";
            else
                body = body + "<tr><td>" + "Type Of Card: " + "Non PKI" + "</td></tr>";
            body = body + "<tr><td>" + "Business Function & Responsibility: " + rIDCFData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";
            //Action
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Action Details</b></th></tr>";
            body = body + "<tr><td>" + "Date Of Issue: " + rIDCFData.DateOfIssue.ToShortDateString() + "</td></tr>";
            body = body + "<tr><td>" + "Chargeable: " + rIDCFData.Chargeable + "</td></tr>";
            body = body + "<tr><td>" + "ID Card Number: " + rIDCFData.IDCardNumber + "</td></tr>";
            // body = body + "<tr><td>" + "Validity Start Date: " + rIDCFData.ActiveFrom.ToShortDateString() + "</td></tr>";
            // body = body + "<tr><td>" + "Validity End Date: " + rIDCFData.EndDate.ToShortDateString() + "</td></tr>";

            body = body + "</table><br>";
            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation.ToLower() == "id card office").Select(y => y.EmailId).ToList() };
        }

        public async Task<EmailDataModel> GetInternalJobPostingFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Internal Job Posting Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                InternalJobPostingData IJPFData = new InternalJobPostingData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseDNFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('InternalJobPosting')/items?$select=*,FormID/ID"
             + "&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles")).Result;

                var responseTextDNFData = await responseDNFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextDNFData))
                {
                    var result = JsonConvert.DeserializeObject<InternalJobPostingModel>(responseTextDNFData, settings);
                    IJPFData = result.List.IJPList[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

                body = body + "<tr><td>" + "Name: " + IJPFData.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + IJPFData.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + IJPFData.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "User Id: " + IJPFData.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + IJPFData.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + IJPFData.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + IJPFData.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + IJPFData.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + IJPFData.EmployeeType + "</td></tr>";
                if (IJPFData.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + IJPFData.ExternalOrganizationName + "</td></tr>";

                }
                employeeLocation = IJPFData.EmployeeLocation;
                body = body + "</table><br>";


                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "IJP Reference: " + IJPFData.IJPReference + "</td></tr>";
                body = body + "<tr><td>" + "MPR Reference: " + IJPFData.MPRReference + "</td></tr>";
                body = body + "<tr><td>" + "Date Of Joining: " + IJPFData.DateOfJoining.ToString("dd/MM/yyyy") + "</td></tr>";
                //body = body + "<tr><td>" + "Level: " + IJPFData.Level + "</td></tr>";
                body = body + "<tr><td>" + "Current Role: " + IJPFData.CurrentRole + "</td></tr>";
                body = body + "<tr><td>" + "Current Department Duration: " + IJPFData.CurrentDepartmentDuration + "</td></tr>";
                body = body + "<tr><td>" + "Current Role Duration: " + IJPFData.CurrentRoleDuration + "</td></tr>";
                body = body + "<tr><td>" + "Current Reporting Manager Name: " + IJPFData.CurrentReportingManagerName + "</td></tr>";
                body = body + "<tr><td>" + "Qualification: " + IJPFData.Qualification + "</td></tr>";



                //Employment Details      
                List<IJPFEmploymentDetailsData> ObjIJPFEmploymentDetailsData = new List<IJPFEmploymentDetailsData>();
                var clientcab = new HttpClient(handler);
                clientcab.BaseAddress = new Uri(conString);
                clientcab.DefaultRequestHeaders.Accept.Clear();
                clientcab.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseCabUser = Task.Run(() => clientcab.GetAsync("_api/web/lists/GetByTitle('IJPFEmploymentDetails')/items?$select=ID,Organisation,Designation,FromDate,ToDate,MainResponsibilities"
                + "&$filter=(IJPFID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
                var responseTextCabUser = await responseCabUser.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseTextCabUser))
                {
                    var internalJobPostingResults = JsonConvert.DeserializeObject<IJPFEmploymentDetailsModel>(responseTextCabUser);
                    ObjIJPFEmploymentDetailsData = internalJobPostingResults.List.IJPEDList;


                    body = body + "<tr><td>" + "Employment Details : " + "**********" + "</td></tr>";
                    foreach (var empDet in ObjIJPFEmploymentDetailsData)
                    {
                        body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                        body = body + "<tr><td>" + "Organisation: " + empDet.Organisation + "</td></tr>";
                        body = body + "<tr><td>" + "Designation: " + empDet.Designation + "</td></tr>";
                        body = body + "<tr><td>" + "From Date: " + empDet.FromDate.ToString("dd/MM/yyyy") + "</td></tr>";
                        body = body + "<tr><td>" + "To Date: " + empDet.ToDate.ToString("dd/MM/yyyy") + "</td></tr>";
                        body = body + "<tr><td>" + "Main Responsibilities: " + empDet.MainResponsibilities + "</td></tr>";
                    }
                }


                body = body + "<tr><td>" + "Position And Department Applied For: " + IJPFData.PositionAndDepartmentAppliedFor + "</td></tr>";
                body = body + "<tr><td>" + "Reason For Changing Job Profile: " + IJPFData.ReasonForChangingJobProfile + "</td></tr>";
                //body = body + "<tr><td>" + "Achievements: " + IJPFData.Achievements + "</td></tr>";
                //body = body + "<tr><td>" + "About Role Applied For: " + IJPFData.AboutRoleAppliedFor + "</td></tr>";
                //body = body + "<tr><td>" + "Request Description: " + IJPFData.BusinessNeed + "</td></tr>";


                body = body + "</table><br>";
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }



                body = body + "</table><br>";

                //task fulfilment details
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                    "</b></th></tr>";
                body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                body = body + "<tr><td>" + "Comments: </td></tr>";
                body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation.ToLower() == "hrc").Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetBusTranFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Bus Transportation Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                BTFData BTFData = new BTFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseBTFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('BusTransportationForm')/items?$select=*,FormID/ID"
             + "&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

                var responseTextBTFData = await responseBTFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextBTFData))
                {
                    var result = JsonConvert.DeserializeObject<BusTransportationFormModel>(responseTextBTFData, settings);
                    BTFData = result.btflist.btfData[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";


                if (BTFData.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + BTFData.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + BTFData.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + BTFData.OtherEmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + BTFData.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + BTFData.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + BTFData.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + BTFData.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + BTFData.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + BTFData.OtherEmployeeType + "</td></tr>";
                    if (BTFData.OtherEmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + BTFData.OtherExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = BTFData.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + BTFData.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + BTFData.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + BTFData.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + BTFData.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + BTFData.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + BTFData.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + BTFData.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + BTFData.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + BTFData.EmployeeType + "</td></tr>";
                    if (BTFData.EmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + BTFData.ExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = BTFData.EmployeeLocation;
                    body = body + "</table><br>";
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Transportation Required: " + BTFData.TransportationRequired + "</td></tr>";
                body = body + "<tr><td>" + "Gender: " + BTFData.Gender + "</td></tr>";
                body = body + "<tr><td>" + "Shift: " + BTFData.BusShift + "</td></tr>";
                body = body + "<tr><td>" + "Address: " + BTFData.Address + "</td></tr>";
                body = body + "<tr><td>" + "Distance: " + BTFData.Distance + "</td></tr>";

                body = body + "<tr><td>" + "Bus Route Name: " + BTFData.BusRouteName + "</td></tr>";
                body = body + "<tr><td>" + "Bus Route Number: " + BTFData.BusRouteNumber + "</td></tr>";
                body = body + "<tr><td>" + "Pick Up Point: " + BTFData.PickupPoint + "</td></tr>";
                body = body + "<tr><td>" + "Slab: " + BTFData.Slab + "</td></tr>";
                body = body + "<tr><td>" + "Reason: " + BTFData.BusinessNeed + "</td></tr>";

                body = body + "</table><br>";

                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                //body = body + "<br><br> <table width=\"100%\">";
                //body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                //    "</b></th></tr>";
                //body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                //body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                //body = body + "<tr><td>" + "Comments: </td></tr>";
                //body = body + "</table><br><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation.ToLower() == "Admin").Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetOCRFFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "OCRF Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                OCRFData OCRFData = new OCRFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseBTFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('OCRFForms')/items?$select=*,FormID/ID"
             + "&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles")).Result;

                var responseTextBTFData = await responseBTFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextBTFData))
                {
                    var result = JsonConvert.DeserializeObject<OCRFModel>(responseTextBTFData, settings);
                    OCRFData = result.ocrfflist.ocrfData[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";


                if (OCRFData.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + OCRFData.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + OCRFData.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + OCRFData.OtherEmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + OCRFData.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + OCRFData.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + OCRFData.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + OCRFData.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + OCRFData.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + OCRFData.OtherEmployeeType + "</td></tr>";
                    if (OCRFData.OtherEmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + OCRFData.OtherExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = OCRFData.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + OCRFData.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + OCRFData.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + OCRFData.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + OCRFData.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + OCRFData.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + OCRFData.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + OCRFData.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + OCRFData.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + OCRFData.EmployeeType + "</td></tr>";
                    if (OCRFData.EmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + OCRFData.ExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = OCRFData.EmployeeLocation;
                    body = body + "</table><br>";
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Request Type: " + OCRFData.RequestType + "</td></tr>";

                //Type of Change
                if (OCRFData.ChkEmployeeTransfer == "Employee Transfer")
                {
                    body = body + "<tr><td>" + "Type Of Change(Employee Transfer): " + OCRFData.ChkEmployeeTransfer + "</td></tr>";
                }
                else if (OCRFData.ChkCostCenter == "Cost Center")
                {
                    body = body + "<tr><td>" + "Type Of Change(Cost Center): " + OCRFData.ChkCostCenter + "</td></tr>";
                }
                else if (OCRFData.ChkReportingAuthority == "Reporting Authority")
                {
                    body = body + "<tr><td>" + "Type Of Change(Reporting Authority): " + OCRFData.ChkReportingAuthority + "</td></tr>";
                }

                //Position
                if (OCRFData.ChkPosition == "Empl moves with his/her position")
                {
                    body = body + "<tr><td>" + "Position: " + OCRFData.ChkPosition + "</td></tr>";
                }
                else if (OCRFData.ChkPosition == "Transfer to available position in new dept")
                {
                    body = body + "<tr><td>" + "Position: " + OCRFData.ChkPosition + "</td></tr>";
                }

                body = body + "<tr><td>" + "Reason For Change: " + OCRFData.ReasonforChange + "</td></tr>";

                body = body + "<tr><td>" + "Employee Transfer Details:******************" + "</td></tr>";

                body = body + "<tr><td>" + "Transfer Effective Date: " + OCRFData.TransferEffectiveDate + "</td></tr>";

                if (OCRFData.RequestType == "Organisation Change Request Form")
                {
                    body = body + "<tr><td>" + "Current Role From: " + OCRFData.CurrentRoleFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Current Role To: " + OCRFData.CurrentRoleTo + "</td></tr>";
                    body = body + "<tr><td>" + "Work Contract From: " + OCRFData.WorkContractFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Work Contract To: " + OCRFData.WorkContractTo + "</td></tr>";
                    body = body + "<tr><td>" + "Reporting Manager From: " + OCRFData.ReportingManagerFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Reporting Manager To: " + OCRFData.ReportingManagerTo + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Centre From: " + OCRFData.CostCentreFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Centre To: " + OCRFData.CostCentreTo + "</td></tr>";
                    body = body + "<tr><td>" + "Division From: " + OCRFData.DivisionFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Division To: " + OCRFData.DivisionTo + "</td></tr>";
                    body = body + "<tr><td>" + "Department From: " + OCRFData.DepartmentFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Department To: " + OCRFData.DepartmentTo + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Department From: " + OCRFData.SubDepartmentFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Department To: " + OCRFData.SubDepartmentTo + "</td></tr>";
                    body = body + "<tr><td>" + "Work Location (Physical location) From: " + OCRFData.WorkLocationFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Work Location (Physical location) To: " + OCRFData.WorkLocationTo + "</td></tr>";
                }
                else
                {
                    body = body + "<tr><td>" + "Employee Category From: " + OCRFData.EmployeeCategoryFromTRF + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Category To: " + OCRFData.EmployeeCategoryToTRF + "</td></tr>";
                    body = body + "<tr><td>" + "Reporting Manager From: " + OCRFData.ReportingManagerFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Reporting Manager To: " + OCRFData.ReportingManagerTo + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Centre From: " + OCRFData.CostCentreFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Centre To: " + OCRFData.CostCentreTo + "</td></tr>";
                    body = body + "<tr><td>" + "Division From: " + OCRFData.DivisionFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Division To: " + OCRFData.DivisionTo + "</td></tr>";
                    body = body + "<tr><td>" + "Department From: " + OCRFData.DepartmentFrom + "</td></tr>";
                    body = body + "<tr><td>" + "Department To: " + OCRFData.DepartmentTo + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Department 1 From: " + OCRFData.SubDepartment1FromTRF + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Department 1 To: " + OCRFData.SubDepartment1ToTRF + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Department 2 From: " + OCRFData.SubDepartment2FromTRF + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Department 2 To: " + OCRFData.SubDepartment2ToTRF + "</td></tr>";

                }

                body = body + "</table><br>";

                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                //body = body + "<br><br> <table width=\"100%\">";
                //body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                //    "</b></th></tr>";
                //body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                //body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                //body = body + "<tr><td>" + "Comments: </td></tr>";
                //body = body + "</table><br><br>";
                //body += "<img src=cid:LogoImage alt=\"\"></img>";


                OCRFDAL objOCRF = new OCRFDAL();
                var hrcEmailIds = objOCRF.GetHRCEmailIds(OCRFData.CostCentreFrom, OCRFData.CostCentreTo);

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = hrcEmailIds };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public List<ITAssetLocationModel> GetEmailIdByAssetLocation(string location)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ITAssetLocationModel> itAssetList = new List<ITAssetLocationModel>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetEmailIdByAssetLocation", con);
                cmd.Parameters.Add(new SqlParameter("@location", location));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        ITAssetLocationModel itAsset = new ITAssetLocationModel();
                        itAsset.AssetEmailId = Convert.ToString(ds.Tables[0].Rows[i]["AssetEmailId"]);
                        itAsset.IsActive = Convert.ToInt32(ds.Tables[0].Rows[i]["IsActive"]);
                        itAsset.LocationName = Convert.ToString(ds.Tables[0].Rows[i]["LocationName"]);
                        itAssetList.Add(itAsset);
                    }
                }

                return itAssetList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<ITAssetLocationModel>();
            }
        }

        public async Task<EmailDataModel> GetEmployeeClearanceFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Employee Clearance Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                ECFData Model = new ECFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseDNFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('EmployeeClearanceForm')/items?$select=*," +
                    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

                var responseTextDNFData = await responseDNFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextDNFData))
                {
                    var result = JsonConvert.DeserializeObject<ECFModel>(responseTextDNFData, settings);
                    Model = result.ecflist.ecfData[0];
                }

                body += GetSubmitterAndApplicantHtml(Model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Date Of Joining: " + Model.DateOfJoining + "</td></tr>";
                body = body + "<tr><td>" + "Date of Relieving: " + Model.DateOfRelieving + "</td></tr>";
                body = body + "<tr><td>" + "Resignation given on: " + Model.ResignationGivenDate + "</td></tr>";
                body = body + "<tr><td>" + "Charge To Be Handed Over To: " + Model.ChargeHandOverToEmpName + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + Model.BusinessNeed + "</td></tr>";

                body = body + "<tr><td>" + "Action : " + "**********" + "</td></tr>";
                body = body + "<tr><td>" + "Any disciplinary action taken in the assessment period: " + Model.DisciplinaryAction + "</td></tr>";
                body = body + "<tr><td>" + "Credit card: " + Model.CreditCard + "</td></tr>";
                body = body + "<tr><td>" + "Resignation received on: " + Model.ResignationReceivedDate + "</td></tr>";
                body = body + "<tr><td>" + "Notice Period: " + Model.NoticePeriod + "</td></tr>";
                if (Model.NoticePeriod == "Applicable")
                    body = body + "<tr><td>" + "Applicable Days: " + Model.ApplicableDays + "</td></tr>";
                body = body + "<tr><td>" + "Eligible for Gratuity: " + Model.Gratuity + "</td></tr>";

                body = body + "</table><br>";
                //approvers
                body += GetApproverDetailsHtml(approverList);


                body = body + "</table><br>";

                //task fulfilment details
                //body = body + "<br><br> <table width=\"100%\">";
                //body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                //    "</b></th></tr>";
                //body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                //body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                //body = body + "<tr><td>" + "Comments: </td></tr>";
                //body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                var ccCode = Model.RequestSubmissionFor.ToLower() == "onbehalf" ? Model.OtherEmployeeCCCode : Model.EmployeeCCCode;
                var empCode = Model.RequestSubmissionFor.ToLower() == "onbehalf" ? Model.OtherEmployeeCode : Model.EmployeeCode;
                var empLoc = Model.RequestSubmissionFor.ToLower() == "onbehalf" ? Model.OtherEmployeeLocation : Model.EmployeeLocation;
                //var listDal = new ListDAL();
                int locId = empLoc.ToLower().Contains("pune") ? 1 : empLoc.ToLower().Contains("aurangabad") ? 3 : 2;
                var ecfDal = new EmployeeClearanceDAL();
                var response = await ecfDal.GetApprovalECF(empCode, ccCode, locId);
                List<string> HRCApproversEmail = new List<string>();
                if (response.Status == 200 && (response.Model != null || response.Model.Count > 0))
                {
                    HRCApproversEmail = response.Model.ToList().Where(x =>
                        x.ApprovalLevel == 1
                        || x.Designation.ToLower() == "hr & admin dept"
                        || x.Designation.ToLower() == "hrc"
                    ).Select(x => x.EmailId).ToList();
                }
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = HRCApproversEmail };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetCourierRequestFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Courier Request Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                CRFData CRFData = new CRFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseDNFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('CourierRequestForm')/items?$select=*,FormID/ID"
             + "&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

                var responseTextDNFData = await responseDNFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextDNFData))
                {
                    var result = JsonConvert.DeserializeObject<CourierRequestModel>(responseTextDNFData, settings);
                    CRFData = result.crflist.crfData[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";


                if (CRFData.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + CRFData.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + CRFData.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + CRFData.OtherEmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + CRFData.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + CRFData.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + CRFData.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + CRFData.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + CRFData.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + CRFData.OtherEmployeeType + "</td></tr>";
                    if (CRFData.OtherEmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + CRFData.OtherExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = CRFData.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + CRFData.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + CRFData.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + CRFData.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + CRFData.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + CRFData.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + CRFData.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + CRFData.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + CRFData.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + CRFData.EmployeeType + "</td></tr>";
                    if (CRFData.EmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + CRFData.ExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = CRFData.EmployeeLocation;
                    body = body + "</table><br>";
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Type of Consignment: " + CRFData.ConsignmentType + "</td></tr>";
                body = body + "<tr><td>" + "Courier Type: " + CRFData.CourierType + "</td></tr>";

                body = body + "<tr><td>" + "Name & Address of Sender: " + CRFData.AddressofConsignee + "</td></tr>";
                body = body + "<tr><td>" + "Name & Address of Receiver: " + CRFData.AddressofReceiver + "</td></tr>";

                if (CRFData.WeightDimension == "Weight")
                {
                    body = body + "<tr><td>" + CRFData.WeightDimension + ": " + CRFData.WeightDimensionIn + " </td></tr>";
                }
                else if (CRFData.WeightDimension == "Dimension")
                {
                    body = body + "<tr><td>" + CRFData.WeightDimension + ": " + CRFData.WeightDimensionIn + " </td></tr>";
                }

                body = body + "<tr><td>" + "Business Function & Responsibility (Max. 500): " + CRFData.BusinessNeed + "</td></tr>";

                body = body + "<tr><td>" + "Action********: " + "</td></tr>";
                body = body + "<tr><td>" + "Courier Inward Register Sl. No: " + CRFData.CourierInwardRegisterNo + "</td></tr>";

                body = body + "</table><br>";
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                    "</b></th></tr>";
                body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                body = body + "<tr><td>" + "Comments: </td></tr>";
                body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetPAFFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Photography Authorization Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                PPFData PPFData = new PPFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('PhotographyAuthorizationForm')/items?$select=*,FormID/ID"
           + "&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

                var responseTextPPFData = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextPPFData))
                {
                    var result = JsonConvert.DeserializeObject<PPFModel>(responseTextPPFData, settings);
                    PPFData = result.ppflist.ppfData[0];
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";

                if (PPFData.RequestSubmissionFor == "OnBehalf")
                {
                    body = body + "<tr><td>" + "Name: " + PPFData.OtherEmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + PPFData.OtherEmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + PPFData.OtherEmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + PPFData.OtherEmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + PPFData.OtherEmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + PPFData.OtherEmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + PPFData.OtherEmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + PPFData.OtherEmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + PPFData.OtherEmployeeType + "</td></tr>";
                    if (PPFData.OtherEmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + PPFData.OtherExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = PPFData.OtherEmployeeLocation;
                    body = body + "</table><br>";
                }
                else
                {
                    body = body + "<tr><td>" + "Name: " + PPFData.EmployeeName + "</td></tr>";
                    body = body + "<tr><td>" + "Employee Number: " + PPFData.EmployeeCode + "</td></tr>";
                    body = body + "<tr><td>" + "User ID: " + PPFData.EmployeeUserId + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + PPFData.EmployeeDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Cost Center: " + PPFData.EmployeeCCCode + "</td></tr>";
                    body = body + "<tr><td>" + "Phone Number: " + PPFData.EmployeeContactNo + "</td></tr>";
                    body = body + "<tr><td>" + "Designation: " + PPFData.EmployeeDesignation + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + PPFData.EmployeeLocation + "</td></tr>";
                    body = body + "<tr><td>" + "Type of Employee: " + PPFData.EmployeeType + "</td></tr>";
                    if (PPFData.EmployeeType == "External")
                    {
                        body = body + "<tr><td>" + "External Orgnization Name: " + PPFData.ExternalOrganizationName + "</td></tr>";

                    }
                    employeeLocation = PPFData.EmployeeLocation;
                    body = body + "</table><br>";
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "3rd Party Photographer: " + PPFData.ThirdPartyPhotographer + "</td></tr>";

                if (PPFData.VideoCameraDevice == "VideoCamera")
                {
                    body = body + "<tr><td>" + "Device: " + "Video Camera" + "</td></tr>";
                    body = body + "<tr><td>" + "Make: " + PPFData.VideoCameraMake + "</td></tr>";
                    body = body + "<tr><td>" + "Model: " + PPFData.VideoCameraModel + "</td></tr>";
                    body = body + "<tr><td>" + "Serial/IMEI No: " + PPFData.VideoCameraSerialIMEINo + "</td></tr>";
                    body = body + "<tr><td>" + "SAVWIPL Owned: " + PPFData.VideoCameraSAVWIPLOwned + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Voice/Sound: " + PPFData.VideoCameraCaptureVoiceSound + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Video: " + PPFData.VideoCameraCaptureVideo + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Images: " + PPFData.VideoCameraCaptureImages + "</td></tr>";
                    body = body + "<tr><td>" + "Bluetooth/Wireless: " + PPFData.VideoCameraBluetoothWireless + "</td></tr>";
                    body = body + "<tr><td>" + "Other: " + PPFData.VideoCameraOther + "</td></tr>";
                }
                if (PPFData.CameraDevice == "Camera")
                {
                    body = body + "<tr><td>" + "Device: " + "Camera" + "</td></tr>";
                    body = body + "<tr><td>" + "Make: " + PPFData.CameraMake + "</td></tr>";
                    body = body + "<tr><td>" + "Model: " + PPFData.CameraModel + "</td></tr>";
                    body = body + "<tr><td>" + "Serial/IMEI No: " + PPFData.CameraSerialIMEINo + "</td></tr>";
                    body = body + "<tr><td>" + "SAVWIPL Owned: " + PPFData.CameraSAVWIPLOwned + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Voice/Sound: " + PPFData.CameraCaptureVoiceSound + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Video: " + PPFData.CameraCapture + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Images: " + PPFData.CameraCaptureImages + "</td></tr>";
                    body = body + "<tr><td>" + "Bluetooth/Wireless: " + PPFData.CameraBluetoothWireless + "</td></tr>";
                    body = body + "<tr><td>" + "Other: " + PPFData.CameraOther + "</td></tr>";
                }
                if (PPFData.MobileDevice == "Mobile")
                {
                    body = body + "<tr><td>" + "Device: " + "Mobile" + "</td></tr>";
                    body = body + "<tr><td>" + "Make: " + PPFData.MobileMake + "</td></tr>";
                    body = body + "<tr><td>" + "Model: " + PPFData.MobileModel + "</td></tr>";
                    body = body + "<tr><td>" + "Serial/IMEI No: " + PPFData.MobileSerialIMEINo + "</td></tr>";
                    body = body + "<tr><td>" + "SAVWIPL Owned: " + PPFData.MobileSAVWIPLOwned + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Voice/Sound: " + PPFData.MobileCaptureVoiceSound + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Video: " + PPFData.MobileCapture + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Images: " + PPFData.MobileCaptureImages + "</td></tr>";
                    body = body + "<tr><td>" + "Bluetooth/Wireless: " + PPFData.MobileBluetoothWireless + "</td></tr>";
                    body = body + "<tr><td>" + "Other: " + PPFData.MobileOther + "</td></tr>";
                }
                if (PPFData.TabletDevice == "Tablet")
                {
                    body = body + "<tr><td>" + "Device: " + "Tablet" + "</td></tr>";
                    body = body + "<tr><td>" + "Make: " + PPFData.TabletMake + "</td></tr>";
                    body = body + "<tr><td>" + "Model: " + PPFData.TabletModel + "</td></tr>";
                    body = body + "<tr><td>" + "Serial/IMEI No: " + PPFData.TabletSerialIMEINo + "</td></tr>";
                    body = body + "<tr><td>" + "SAVWIPL Owned: " + PPFData.TabletSAVWIPLOwned + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Voice/Sound: " + PPFData.TabletCaptureVoiceSound + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Video: " + PPFData.TabletCapture + "</td></tr>";
                    body = body + "<tr><td>" + "Capture Images: " + PPFData.TabletCaptureImages + "</td></tr>";
                    body = body + "<tr><td>" + "Bluetooth/Wireless: " + PPFData.TabletBluetoothWireless + "</td></tr>";
                    body = body + "<tr><td>" + "Other: " + PPFData.TabletOther + "</td></tr>";
                }

                body = body + "<tr><td>" + "Location: " + PPFData.PAFLocation + "</td></tr>";
                body = body + "<tr><td>" + "Zone: " + PPFData.Zone + "</td></tr>";
                body = body + "<tr><td>" + "Pre-series Car / Part: " + PPFData.PreSeriesCarOrPart + "</td></tr>";
                body = body + "<tr><td>" + "Exceptional Photo: " + PPFData.ExceptionalPhoto + "</td></tr>";
                body = body + "<tr><td>" + "Purpose: " + PPFData.Purpose + "</td></tr>";

                body = body + "<tr><td>" + "Valid From: " + PPFData.ValidFrom.ToString("dd/MM/yyyy") + "</td></tr>";
                body = body + "<tr><td>" + "Valid To: " + PPFData.ValidTo.ToString("dd/MM/yyyy") + "</td></tr>";

                body = body + "</table><br>";
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                    "</b></th></tr>";
                body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                body = body + "<tr><td>" + "Comments: </td></tr>";
                body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                var keyValPair = new KeyValuePair<string, Object>("IsExceptionalPhoto", PPFData.ExceptionalPhoto.ToLower() == "yes" ? true : false);
                var pafLocation = PPFData.PAFLocation;
                return new EmailDataModel() { Body = body, PAFLocation = pafLocation, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation.ToLower() == "security").Select(y => y.EmailId).ToList(), ExtraFormData = new List<KeyValuePair<string, object>>() { keyValPair } };

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetDrivingAuthorizationFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Driving Authorization Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                DAFData Model = new DAFData();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('DrivingAuthorizationForm')/items?$select=*,"
                    + "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles")).Result;
                var responseTextData = await responseData.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    var result = JsonConvert.DeserializeObject<DrivingAuthorizationFormModel>(responseTextData, settings);
                    Model = result.daflist.dafData[0];
                }

                body += GetSubmitterAndApplicantHtml(Model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Sub Department: " + Model.SubDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Date of Birth: " + Model.DateOfBirth + "</td></tr>";

                var PhotoImg = Model?.AttachmentFiles?.Attachments?.Count > 0 ? Model.AttachmentFiles.Attachments.Find(x => Path.GetFileNameWithoutExtension(x.FileName) == "Photo") : null;
                var LicenseImg = Model?.AttachmentFiles?.Attachments?.Count > 0 ? Model.AttachmentFiles.Attachments.Find(x => Path.GetFileNameWithoutExtension(x.FileName) == "LicensePhotoCopy") : null;
                body = body + "<tr><td>" + "Photo: <a href='" + (PhotoImg != null
                                                     ? System.Configuration.ConfigurationManager.AppSettings["SharepointSiteURL"] + PhotoImg.ServerRelativeUrl
                                                     : "") + "'>Photo</a></td></tr>";
                body = body + "<tr><td>" + "Driving License Details: <a href='" + (LicenseImg != null
                                                     ? System.Configuration.ConfigurationManager.AppSettings["SharepointSiteURL"] + LicenseImg.ServerRelativeUrl
                                                     : "") + "'>LicensePhotoCopy</a></td></tr>";
                body = body + "<tr><td>" + "License Number: " + Model.LicenseNumber + "</td></tr>";
                body = body + "<tr><td>" + "Valid From: " + Model.ValidFrom + "</td></tr>";
                body = body + "<tr><td>" + "Valid Till: " + Model.ValidTill + "</td></tr>";
                body = body + "<tr><td>" + "Vehicles Driven: " + Model.VehiclesDriven + "</td></tr>";
                body = body + "<tr><td>" + "Driving Experience: " + Model.DrivingExperience + " years</td></tr>";
                body = body + "<tr><td>" + "Address: " + Model.Address + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + Model.BusinessNeed + "</td></tr>";
                body = body + "<tr><td>" + "Applicant is in need of a Driving Authorization for official purpose. Please issue him the authorization for: "
                    + (Model.AuthorizationForInternal == "Yes" ? "Internal, " : "")
                    + (Model.AuthorizationForExternal == "Yes" ? "External, " : "")
                    + (Model.AuthorizationForTestTrack == "Yes" ? "Test Track, " : "")
                    + (Model.AuthorizationForMaterialHandling == "Yes" ? "Material Handling, " : "") + "</td></tr>";

                body = body + "<tr><td>" + "Action : " + "**********" + "</td></tr>";
                body = body + "<tr><td>" + "Blood Group: " + Model.BloodGroup + "</td></tr>";
                body = body + "<tr><td>" + "Eye Sight: " + Model.EyeSight + "</td></tr>";
                body = body + "<tr><td>" + "LT: " + Model.LT + "</td></tr>";
                body = body + "<tr><td>" + "RT: " + Model.RT + "</td></tr>";
                body = body + "<tr><td>" + "History of Epilepsy: " + Model.HistoryofEpilepsy + "</td></tr>";
                body = body + "<tr><td>" + "Certification: " + Model.Certification + "</td></tr>";
                body = body + "<tr><td>" + "Remarks: " + Model.Remarks + "</td></tr>";

                body = body + "</table><br>";
                //approvers
                body += GetApproverDetailsHtml(approverList);


                body = body + "</table><br>";

                //task fulfilment details
                //body = body + "<br><br> <table width=\"100%\">";
                //body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                //    "</b></th></tr>";
                //body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                //body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                //body = body + "<tr><td>" + "Comments: </td></tr>";
                //body = body + "</table><br><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        private string GetSubmitterAndApplicantHtml(ApplicantDataModel model)
        {
            string body = "";
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Requester Details</b></th></tr>";
            if (model.RequestSubmissionFor == "OnBehalf")
            {
                body = body + "<tr><td>" + "Name: " + model.OtherEmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + model.OtherEmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + model.OtherEmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + model.OtherEmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + model.OtherEmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + model.OtherEmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + model.OtherEmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + model.OtherEmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + model.OtherEmployeeType + "</td></tr>";
                if (model.OtherEmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + model.OtherExternalOrganizationName + "</td></tr>";

                }
                body = body + "</table><br>";
            }
            else
            {
                body = body + "<tr><td>" + "Name: " + model.EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Number: " + model.EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "User ID: " + model.EmployeeUserId + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + model.EmployeeDepartment + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + model.EmployeeCCCode + "</td></tr>";
                body = body + "<tr><td>" + "Phone Number: " + model.EmployeeContactNo + "</td></tr>";
                body = body + "<tr><td>" + "Designation: " + model.EmployeeDesignation + "</td></tr>";
                body = body + "<tr><td>" + "Location: " + model.EmployeeLocation + "</td></tr>";
                body = body + "<tr><td>" + "Type of Employee: " + model.EmployeeType + "</td></tr>";
                if (model.EmployeeType == "External")
                {
                    body = body + "<tr><td>" + "External Orgnization Name: " + model.ExternalOrganizationName + "</td></tr>";

                }
                //employeeLocation = model.EmployeeLocation;
                body = body + "</table><br>";
            }
            return body;
        }

        private string GetApproverDetailsHtml(List<ApprovalDataModel> approverList)
        {
            string body = "";
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }
            body = body + "</table><br>";
            return body;
        }

        public async Task<EmailDataModel> GetDoorAccessFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Door Access Request Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            DoorAccessRequestData doorAccessData = new DoorAccessRequestData();
            GlobalClass gc = new GlobalClass();

            #region ListData
            List<DoorAccessRequestData> item = new List<DoorAccessRequestData>();
            DoorAccessRequestData model = new DoorAccessRequestData();

            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataTable dt = new DataTable();
            con = new SqlConnection(sqlConString);
            cmd = new SqlCommand("USP_ViewDARFDetails", con);
            cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
            cmd.CommandType = CommandType.StoredProcedure;
            adapter.SelectCommand = cmd;
            con.Open();
            adapter.Fill(dt);
            con.Close();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    //int emp;
                    FormLookup item1 = new FormLookup();
                    item1.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                    if (dt.Rows[i]["Created"] != DBNull.Value)
                        item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                    model.FormID = item1;
                    model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                    model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                    model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                    model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                    model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                    model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                    model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                    model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                    model.EmployeeContactNo = dt.Rows[0]["EmployeeContactNo"] != DBNull.Value || dt.Rows[0]["EmployeeContactNo"] != "0" ? Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]) : 0;
                    model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                    model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                    model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                    model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                    model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                    model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                    model.OtherEmployeeCode = dt.Rows[0]["OtherEmployeeCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCode"] != "0" && dt.Rows[0]["OtherEmployeeCode"] != "" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]) : 0;
                    model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null && dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                    //model.OtherEmployeeContactNo = Convert.ToInt64(dt.Rows[0]["OtherEmployeeContactNo"]);
                    model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);


                    model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                    model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                    model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                    model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                    model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                    model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                    model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                    model.BusinessNeed = Convert.ToString(dt.Rows[0]["BusinessNeed"]);
                    model.IDCardNumber = Convert.ToString(dt.Rows[0]["IDCardNumber"]);
                    item.Add(model);
                }
            }
            #endregion
            doorAccessData = item[0];
            if (Status != 1)
                body += GetSubmitterAndApplicantHtml(doorAccessData);

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "ID Card Number: " + doorAccessData.IDCardNumber + "</td></tr>";
            body = body + "<tr><td>" + "Business Function & Responsibility: " + doorAccessData.BusinessNeed + "</td></tr>";
            //body = body + "</table><br>";

            //Door Access Details        
            List<SelectedAcsessDoorDto> ObjSelectedDoorAccessDto = new List<SelectedAcsessDoorDto>();

            //var client2 = new HttpClient(handler);
            //client2.BaseAddress = new Uri(conString);
            //client2.DefaultRequestHeaders.Accept.Clear();
            //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            //var responseSoftwareDetails = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('AccessDoorDetails')/items?$select=*&$filter=(DoorAccessReqId eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            //var responseTextSoftwareDetails = await responseSoftwareDetails.Content.ReadAsStringAsync();

            List<string> doorIDList = new List<string>();

            #region DataList
            List<SelectedAcsessDoorDto> OtherList = new List<SelectedAcsessDoorDto>();
            SqlCommand cmd1 = new SqlCommand();
            SqlDataAdapter adapter1 = new SqlDataAdapter();
            DataTable ds1 = new DataTable();
            con = new SqlConnection(sqlConString);
            cmd = new SqlCommand("USP_ViewDARFormDataList", con);
            cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
            cmd.CommandType = CommandType.StoredProcedure;
            adapter.SelectCommand = cmd;
            con.Open();
            adapter.Fill(ds1);
            con.Close();
            if (ds1.Rows.Count > 0)
            {

                for (int i = 0; i < ds1.Rows.Count; i++)
                {
                    SelectedAcsessDoorDto model1 = new SelectedAcsessDoorDto();
                    FormLookup item1 = new FormLookup();
                    item1.Id = Convert.ToInt32(ds1.Rows[i]["FormID"]);
                    model1.FormID = item1;
                    model1.ID = Convert.ToInt64(ds1.Rows[i]["Id"]);
                    model1.DoorAccessReqId = Convert.ToString(ds1.Rows[i]["DoorAccessReqId"]);
                    model1.Location = Convert.ToString(ds1.Rows[i]["Location"]);
                    model1.DoorDepartment = Convert.ToString(ds1.Rows[i]["DoorDepartment"]);
                    model1.DoorName = Convert.ToString(ds1.Rows[i]["DoorName"]);
                    model1.DoorID = ds1.Rows[0]["DoorID"] != DBNull.Value && ds1.Rows[0]["DoorID"] != "0" ? Convert.ToInt32(ds1.Rows[i]["DoorID"]) : 0;
                    OtherList.Add(model1);

                }

            }
            #endregion
            if (OtherList.Count > 0)
            {
                //var DoorAccessDetailsResult = JsonConvert.DeserializeObject<SelectedAcsessDoorModel>(responseTextSoftwareDetails);
                ObjSelectedDoorAccessDto = OtherList;
                body = body + "<tr><td>" + "Supplier Details: " + "</td></tr>";
                foreach (var door in ObjSelectedDoorAccessDto)
                {
                    doorIDList.Add(door.DoorID.ToString());
                    body = body + "<tr><td>" + "***************************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "Location: " + door.Location + "</td></tr>";
                    body = body + "<tr><td>" + "Department: " + door.DoorDepartment + "</td></tr>";
                    body = body + "<tr><td>" + "Access Door: " + door.DoorName + "</td></tr>";
                }
            }
            body = body + "</table><br>";

            if (Status != 1)
            {
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                       : "") + "</td></tr>";
                    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                //task fulfilment details
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                    "</b></th></tr>";
                body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
                body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
                body = body + "<tr><td>" + "Comments: </td></tr>";
                body = body + "</table><br><br>";


                body += "<img src=cid:LogoImage alt=\"\"></img>";
            }
            return new EmailDataModel() { Body = body, Location = employeeLocation, IDList = doorIDList };
        }

        public async Task<EmailDataModel> GetMaterialRequestFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "Material Request Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            MaterialRequestData materialRequestData = new MaterialRequestData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseMaterialRequestData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('MaterialRequestForm')/items?$select=*,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

            var responseTextMaterialRequestData = await responseMaterialRequestData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextMaterialRequestData))
            {
                var result = JsonConvert.DeserializeObject<MaterialRequestModel>(responseTextMaterialRequestData, settings);
                materialRequestData = result.List.MaterialRequestList[0];
            }
            body += GetSubmitterAndApplicantHtml(materialRequestData);

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "Request Number: " + materialRequestData.RequestNumber + "</td></tr>";
            body = body + "<tr><td>" + "Request To: " + materialRequestData.RequestFrom + "</td></tr>";
            body = body + "<tr><td>" + "Request From: " + materialRequestData.RequestTo + "</td></tr>";
            body = body + "<tr><td>" + "Business Function & Responsibility: " + materialRequestData.BusinessNeed + "</td></tr>";
            //body = body + "</table><br>";

            //Door Access Details        
            List<MaterialDetailsData> materialDetailsData = new List<MaterialDetailsData>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseMaterialDetailsData = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('MaterialDetails')/items?$select=*&$filter=(MaterialRequestID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            var responseTextMaterialDetailsData = await responseMaterialDetailsData.Content.ReadAsStringAsync();
            //List<string> doorIDList = new List<string>();

            if (!string.IsNullOrEmpty(responseTextMaterialDetailsData))
            {
                var materialDetailsResult = JsonConvert.DeserializeObject<MaterialDetailsModel>(responseTextMaterialDetailsData);
                materialDetailsData = materialDetailsResult.List.MaterialDetailsList;
                body = body + "<tr><td>" + "Material Details: " + "</td></tr>";
                foreach (var material in materialDetailsData)
                {
                    body = body + "<tr><td>" + "***************************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "Part Number: " + material.PartNumber + "</td></tr>";
                    body = body + "<tr><td>" + "Part Description: " + material.PartDescription + "</td></tr>";
                    body = body + "<tr><td>" + "Quantity: " + material.Quantity + "</td></tr>";
                    body = body + "<tr><td>" + "Remarks: " + material.Remarks + "</td></tr>";
                }
            }
            body = body + "</table><br>";

            //approvers
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            {
                body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
                   approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
                   : "") + "</td></tr>";
                body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
                body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            }

            body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }
        public async Task<EmailDataModel> GetBEIFAM(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            string employeeLocation = "";
            var returnModel = new EmailDataModel();
            string FormName = "BEI Form";
            var rowId = await GetRowID_SQL(formId, currentUser);
            var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
            BeiData beiData = new BeiData();
            GlobalClass gc = new GlobalClass();
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

            var responseBeiData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('BEIForm')/items?$select=*,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID")).Result;

            var responseTextBeiData = await responseBeiData.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            if (!string.IsNullOrEmpty(responseTextBeiData))
            {
                var result = JsonConvert.DeserializeObject<BeiModel>(responseTextBeiData, settings);
                beiData = result.list.beiData[0];
            }
            body += GetSubmitterAndApplicantHtml(beiData);

            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
            body = body + "<tr><td>" + "VIN: " + beiData.Vin + "</td></tr>";
            body = body + "<tr><td>" + "Business Function & Responsibility: " + beiData.BusinessNeed + "</td></tr>";
            body = body + "</table><br>";

            //BEI Part Details        
            List<BeiPartData> beiPartData = new List<BeiPartData>();
            var client2 = new HttpClient(handler);
            client2.BaseAddress = new Uri(conString);
            client2.DefaultRequestHeaders.Accept.Clear();
            client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var responseBeiPartData = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('BeiData')/items?$select=*&$filter=(BeiRowID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
            var responseTextBeiPartData = await responseBeiPartData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseTextBeiPartData))
            {
                var BeiPartDataResult = JsonConvert.DeserializeObject<BeiDataModel>(responseTextBeiPartData);
                beiPartData = BeiPartDataResult.beiDatalist.beiPartData;
                foreach (var part in beiPartData)
                {
                    body = body + "<tr><td>" + "*************************************" + "</td></tr>";
                    body = body + "<tr><td>" + "Part Description: " + part.PartDesc + "</td></tr>";
                    body = body + "<tr><td>" + "Quantity: " + part.Quantity + "</td></tr>";
                    body = body + "<tr><td>" + "Availability: " + (part.Availability == "true" ? "Yes" : "No") + "</td></tr>";
                    if (part.Remark != null)
                        body = body + "<tr><td>" + "Remark: " + part.Remark + "</td></tr>";
                }
            }
            body = body + "</table><br>";

            //approvers
            //body = body + "<br><br> <table width=\"100%\">";
            //body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
            //foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
            //{
            //    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
            //    body = body + "<tr><td>" + "Approved On: " + (approver.ApproverStatus != "Pending" ?
            //       approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString()
            //       : "") + "</td></tr>";
            //    body = body + "<tr><td>" + "Approver Role: " + approver.Designation + "</td></tr>";
            //    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
            //}

            // body = body + "</table><br>";

            //task fulfilment details
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Fulfilment Task Details" +
                "</b></th></tr>";
            body = body + "<tr><td>" + "Assigned Group: VWIPLP - IT Service Desk </td></tr>";
            body = body + "<tr><td>" + "Assigned To: {assignedToSection} </td></tr>";
            body = body + "<tr><td>" + "Comments: </td></tr>";
            body = body + "</table><br><br>";
            body += "<img src=cid:LogoImage alt=\"\"></img>";

            return new EmailDataModel() { Body = body, Location = employeeLocation };
        }

        public List<string> GetIDCardOfficeEmailId(string doorIds)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<string> emailIds = new List<string>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetIDCardOfficeApprovers", con);
                cmd.Parameters.Add(new SqlParameter("@doorIds", doorIds));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        emailIds.Add(Convert.ToString(ds.Tables[0].Rows[i]["EmailId"]));
                    }
                }
                return emailIds;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<string>();
            }
        }

        public async Task<EmailDataModel> GetSUCFFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "SAP UserId Creation Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                SAPUserIdCreationModel Model = new SAPUserIdCreationModel();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SAPUserIDForm')/items?$select=*" +
                    "&$filter=(ID eq '" + rowId + "')")).Result;

                var responseData = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseData))
                {
                    var result = JsonConvert.DeserializeObject<SUCFModel>(responseData, settings);
                    Model = result.list.data[0];
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response1 = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('SAPUserDataList')/items?$select=*,ListItemId/ID" +
                        "&$filter=(ListItemId/ID eq '" + rowId + "')&$expand=ListItemId")).Result;
                    var responseText = await response1.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var SUCFUserResult = JsonConvert.DeserializeObject<SUCFUserDataModel>(responseText, settings);
                        Model.UserData = SUCFUserResult.list.data;
                    }
                }

                body += GetSubmitterAndApplicantHtml(Model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                foreach (var item in Model.UserData)
                {
                    body = body + "<tr><td>" + "System: " + item.System + "</td></tr>";
                    body = body + "<tr><td>" + "Client: " + item.Client + "</td></tr>";
                    body = body + "<tr><td>" + "Type Of UserId: " + item.Type + "</td></tr>";
                    body = body + "<tr><td>" + "Reason/Details/Justification: " + item.Reason + "</td></tr>";
                    body = body + "<tr><td>" + "Module: " + item.Module + "</td></tr>";
                    if (item.Module.ToLower() == "others")
                        body = body + "<tr><td>" + "Module Description: " + item.ModuleDescription + "</td></tr>";
                    body = body + "<tr><td>" + "Sub-Module: " + item.SubModule + "</td></tr>";
                    body = body + "<tr><td>" + "Request Type: " + item.RequestType + "</td></tr>";
                    if (item.RequestType.ToLower() == "temporary")
                    {
                        body = body + "<tr><td>" + "Temp From: " + item.TempFrom + "</td></tr>";
                        body = body + "<tr><td>" + "Temp To: " + item.TempTo + "</td></tr>";
                    }
                    body = body + "******************";
                }
                body = body + "<tr><td>" + "Request Description: " + Model.BusinessNeed + "</td></tr>";


                body = body + "</table><br>";
                //approvers
                body += GetApproverDetailsHtml(approverList);

                employeeLocation = Model.RequestSubmissionFor.ToLower() == "onbehalf" ? Model.OtherEmployeeLocation : Model.EmployeeLocation;
                body = body + "</table><br>";
                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = new List<string>() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }

        public async Task<EmailDataModel> GetDLICFormFAM(int formId, UserData currentUser, int Status = 0)
        {
            try
            {
                string body = "";
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Desktop Laptop Installation Checklist";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);
                var Model = new DesktopLaptopInstallationChecklistModel();
                GlobalClass gc = new GlobalClass();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseDNFData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('DesktopLaptopInstallationCheckListForm')/items?$select=*" +
                    "&$filter=(ID eq '" + rowId + "')")).Result;

                var responseTextDNFData = await responseDNFData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextDNFData))
                {
                    var result = JsonConvert.DeserializeObject<DLICModel>(responseTextDNFData, settings);
                    Model = result.data.list[0];
                }

                body += GetSubmitterAndApplicantHtml(Model);
                employeeLocation = Model.RequestSubmissionFor.ToLower() == "onbehalf" ? Model.OtherEmployeeLocation : Model.EmployeeLocation;

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Ticket Number: " + Model.TicketNum + "</td></tr>";
                body = body + "<tr><td>" + "Employee Name: " + Model.T_EmployeeName + "</td></tr>";
                body = body + "<tr><td>" + "Employee Code: " + Model.T_EmployeeCode + "</td></tr>";
                body = body + "<tr><td>" + "Userid: " + Model.T_UserId + "</td></tr>";
                body = body + "<tr><td>" + "Cost Center: " + Model.T_CostCenter + "</td></tr>";
                body = body + "<tr><td>" + "Make: " + Model.Make + "</td></tr>";
                body = body + "<tr><td>" + "Model: " + Model.Modal + "</td></tr>";
                body = body + "<tr><td>" + "Serial Number: " + Model.SerialNumber + "</td></tr>";
                //body = body + "<tr><td>" + "Host Name: " + Model.HostName + "</td></tr>";
                //body = body + "<tr><td>" + "i.Do Completed: " + (Model.IsIDoCompleted ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Bitlocker: " + (Model.IsBitLockerCompleted ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Antivirus Updates Checked: " + (Model.IsAntivirusUpdated ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Zscaler/Proxy Configuration: " + (Model.IsProxyConfig ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "USB/Bluetooth Disabled: " + (Model.IsUSBBluetoothDisabled ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "User ID Configured: " + (Model.IsUserIdConfigured ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Outlook Configuration: " + (Model.IsOutLookConfiguration ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "FireEye: " + (Model.IsFirEyeAgent ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Encrypted email Configurations: " + (Model.IsEncryptedEmailConfiguration ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "PKI Card & Digital Signature Setting: " + (Model.IsPKIDigitSignCert ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Printer Configuration: " + (Model.IsPrinterConfiguration ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "VPN Configuration Done(Laptop Only): " + (Model.IsVPNConfigurationDone ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Shared Folder Access Done: " + (Model.IsSharedFolderAccessDone ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Data Restored (for Replacement/re-i.do): " + (Model.IsDataRestored ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Nessus Agent: " + (Model.IsNessusAgent ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Classification add-in for  Office: " + (Model.IsClassificationAddInForOffice ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Used machine to be cleaned: " + (Model.IsUsedMachineToBeClean ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "One Drive Configuration: " + (Model.IsOneDriveConfiguration ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Local apps 1.Smart Sign: " + (Model.IsLocalApps ? "Yes" : "No") + "</td></tr>";
                //body = body + "<tr><td>" + "Others: " + (Model.IsOthers ? "Yes" : "No") + "</td></tr>";
                if (Model.IsOthers)
                    body = body + "<tr><td>" + "Others Value: " + Model.OthersText + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + Model.BusinessNeed + "</td></tr>";
                body = body + "</table><br>";
                //approvers
                body += GetApproverDetailsHtml(approverList);

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = new List<string>() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel();
            }
        }


        public async Task<EmailDataModel> GetGiftsInvitationBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Gifts, Invitation and Compliance Consultation Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                GiftsInvitationData model = new GiftsInvitationData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseCOIData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('GiftsInvitationForm')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')&$expand=AttachmentFiles")).Result;

                var responseTextCOIData = await responseCOIData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                if (!string.IsNullOrEmpty(responseTextCOIData))
                {
                    var result = JsonConvert.DeserializeObject<GAIFModel>(responseTextCOIData, settings);
                    model = result.list.data[0];
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('GiftsInvitationFormQuestionList')/items?$select=*"
           + "&$filter=(QuestionId eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var GAIFResult = JsonConvert.DeserializeObject<QuestionModel>(responseText, settings);
                        model.QuestionData = GAIFResult.QuestionList.data;
                    }
                }

                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Request Type: " + model.RequestType + "</td></tr>";

                body = body + "<tr><td>" + "Transaction: " + model.Transaction + "</td></tr>";

                body = body + "<tr><td>" + "Is Gift/Invitation given to public official?: " + model.IsGiftOrInviteToPublicOfficial + "</td></tr>";

                body = body + "<tr><td>" + "Name of Business partner/ Other individual providing the gift/invitation ?: " + model.NameRelationOtherDet + "</td></tr>";

                body = body + "<tr><td>" + "Frequency of receipt of gifts/invitation from the same business partner in a year?: " + model.FrequencyOfGiftsOrInvitationfrm + "</td></tr>";

                body = body + "<tr><td>" + "Approximate value of gifts/invitation (How is the value estimated) ?: " + model.ApproxValueOfGiftsInvt + "</td></tr>";

                body = body + "<tr><td>" + "Reason for gifting/invitation ?: " + model.ReasonForGiftingInvitation + "</td></tr>";

                body = body + "<tr><td>" + "Gift is accepted/ Refused ?: " + model.GiftIsAcceptedRefused + "</td></tr>";

                body = body + "<tr><td>" + "Reason of acceptance/ refusal of gift/ Invitation: " + model.ReasonGiftIsAcceptedRefused + "</td></tr>";

                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Question Details</b></th></tr>";

                for (int i = 0; i < model.QuestionData.Count; i++)
                {
                    body = body + "<tr><td>" + "Question: " + model.QuestionData[i].Question + "</td></tr>";
                }

                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Action</b></th></tr>";

                if (model.RequestType == "Consultation")
                {
                    body = body + "<tr><td>" + "Answers: " + model.Answers + "</td></tr>";
                }
                else
                {
                    body = body + "<tr><td>" + "Gift to be Deposited with GRC?*: " + model.GiftTobeDepoWithGRC + "</td></tr>";
                }
                body = body + "<tr><td>" + "Business justification(Max. 500): " + model.BusinessNeed + "</td></tr>";



                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetNewGlobalCodeBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "New Global GL code form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                //NewGlobalCodeData model = new NewGlobalCodeData();
                //GlobalClass gc = new GlobalClass();

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('NewGlobalCodeForm')/items?$select=*"
                //+ "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;

                //var responseTextData = await responseData.Content.ReadAsStringAsync();

                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //if (!string.IsNullOrEmpty(responseTextData))
                //{
                //    var result = JsonConvert.DeserializeObject<NewGlobalCodeModel>(responseTextData, settings);
                //    model = result.NGCFResults.NewGlobalCodeData[0];
                //}
                NewGlobalCodeData model = new NewGlobalCodeData();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<NewGlobalCodeData> appList = new List<NewGlobalCodeData>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetNewGlobalCodeFormById", con);
                cmd.Parameters.Add(new SqlParameter("@AppRowId", rowId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        NewGlobalCodeData app = new NewGlobalCodeData();
                        app.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["FormID"]);
                        item1.CreatedDate = ds.Tables[0].Rows[i]["Created"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds.Tables[0].Rows[i]["Created"]);
                        app.FormIDNGCF = item1;
                        app.EmployeeType = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeType"]);
                        app.EmployeeCode = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeCode"]);
                        app.EmployeeCCCode = Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeCCCode"]);
                        app.EmployeeUserId = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeUserId"]);
                        app.EmployeeName = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeName"]);
                        app.EmployeeContactNo = ds.Tables[0].Rows[i]["EmployeeContactNo"] == DBNull.Value ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["EmployeeContactNo"]);
                        app.ExternalOrganizationName = ds.Tables[0].Rows[i]["ExternalOrganizationName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["ExternalOrganizationName"]);
                        app.OtherExternalOrganizationName = ds.Tables[0].Rows[i]["ExternalOtherOrganizationName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["ExternalOtherOrganizationName"]);
                        app.EmployeeLocation = ds.Tables[0].Rows[i]["EmployeeLocation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeLocation"]);
                        app.EmployeeDepartment = ds.Tables[0].Rows[i]["EmployeeDepartment"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeDepartment"]);
                        app.EmployeeDesignation = ds.Tables[0].Rows[i]["EmployeeDesignation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeDesignation"]);
                        app.RequestSubmissionFor = ds.Tables[0].Rows[i]["RequestSubmissionFor"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["RequestSubmissionFor"]);
                        app.OnBehalfOption = ds.Tables[0].Rows[i]["OnBehalfOption"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OnBehalfOption"]);
                        //app.EmployeeEmailId = ds.Tables[0].Rows[i]["EmployeeEmailId"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["EmployeeEmailId"]);
                        app.OtherEmployeeType = ds.Tables[0].Rows[i]["OtherEmployeeType"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeType"]);
                        app.OtherEmployeeCode = ds.Tables[0].Rows[i]["OtherEmployeeCode"] == DBNull.Value ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["OtherEmployeeCode"]);
                        app.OtherEmployeeCCCode = ds.Tables[0].Rows[i]["OtherEmployeeCCCode"] == DBNull.Value ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["OtherEmployeeCCCode"]);
                        app.OtherEmployeeContactNo = ds.Tables[0].Rows[i]["OtherEmployeeContactNo"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeContactNo"]);
                        app.OtherEmployeeUserId = ds.Tables[0].Rows[i]["OtherEmployeeUserId"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeUserId"]);
                        app.OtherEmployeeName = ds.Tables[0].Rows[i]["OtherEmployeeName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeName"]);
                        app.OtherEmployeeDepartment = ds.Tables[0].Rows[i]["OtherEmployeeDepartment"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeDepartment"]);
                        app.OtherEmployeeLocation = ds.Tables[0].Rows[i]["OtherEmployeeLocation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeLocation"]);
                        app.OtherEmployeeDesignation = ds.Tables[0].Rows[i]["OtherEmployeeDesignation"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeDesignation"]);
                        app.OtherExternalOrganizationName = ds.Tables[0].Rows[i]["OtherExternalOrganizationName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherExternalOrganizationName"]);
                        app.OtherEmployeeEmailId = ds.Tables[0].Rows[i]["OtherEmployeeEmailId"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["OtherEmployeeEmailId"]);
                        app.BusinessNeed = ds.Tables[0].Rows[i]["BusinessNeed"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["BusinessNeed"]);
                        app.RequestType = ds.Tables[0].Rows[i]["RequestType"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["RequestType"]);
                        app.NameOfGLToOpen = ds.Tables[0].Rows[i]["NameOfGLToOpen"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["NameOfGLToOpen"]);
                        app.NatureOfTranInGL = ds.Tables[0].Rows[i]["NatureOfTranInGL"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["NatureOfTranInGL"]);
                        app.Purpose = ds.Tables[0].Rows[i]["Purpose"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["Purpose"]);
                        app.DateToOpenNewGL = ds.Tables[0].Rows[i]["DateToOpenNewGL"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds.Tables[0].Rows[i]["DateToOpenNewGL"]);
                        app.GLCode = ds.Tables[0].Rows[i]["GLCode"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["GLCode"]);
                        app.GLName = ds.Tables[0].Rows[i]["GLName"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["GLName"]);
                        app.GLSeries = ds.Tables[0].Rows[i]["GLSeries"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["GLSeries"]);
                        app.NewGLNo = ds.Tables[0].Rows[i]["NewGLNo"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["NewGLNo"]);
                        app.CommitmentItem = ds.Tables[0].Rows[i]["CommitmentItem"] == DBNull.Value ? null : Convert.ToString(ds.Tables[0].Rows[i]["CommitmentItem"]);
                        appList.Add(app);
                    }
                    model = appList[0];
                }

                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Request Type: " + model.RequestType + "</td></tr>";

                body = body + "<tr><td>" + "Name Of GL To Open: " + model.NameOfGLToOpen + "</td></tr>";

                body = body + "<tr><td>" + "Nature of transaction to be captured in GL: " + model.NatureOfTranInGL + "</td></tr>";

                body = body + "<tr><td>" + "Purpose: " + model.Purpose + "</td></tr>";

                body = body + "<tr><td>" + "Date To Open New GL: " + model.DateToOpenNewGL + "</td></tr>";

                body = body + "<tr><td>" + "GL Code:" + model.GLCode + "</td></tr>";

                body = body + "<tr><td>" + "GL Name: " + model.GLName + "</td></tr>";

                body = body + "<tr><td>" + "GL Series: " + model.GLSeries + "</td></tr>";

                body = body + "<tr><td>" + "Business justification(Max. 500): " + model.BusinessNeed + "</td></tr>";

                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Action</b></th></tr>";


                body = body + "<tr><td>" + "New GL No.: " + model.NewGLNo + "</td></tr>";
                body = body + "<tr><td>" + "Commitment Item: " + model.CommitmentItem + "</td></tr>";


                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Select(x => x.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetUserRequestBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "User Request Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                UserRequestData model = new UserRequestData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('UserRequestForm')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;

                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    var result = JsonConvert.DeserializeObject<UserRequestModel>(responseTextData, settings);
                    model = result.URCFResults.UserRequestData[0];
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('UserRequestApplicationList')/items?$select=*"
           + "&$filter=(AppId eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var SUCFUserResult = JsonConvert.DeserializeObject<ApplicationCategoryModel>(responseText, settings);
                        model.ApplicationCategoryData = SUCFUserResult.ApplicationCategoryResults.data;
                    }

                }

                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Brand: " + model.Brand + "</td></tr>";

                body = body + "<tr><td>" + "Service Type: " + model.ServiceType + "</td></tr>";

                body = body + "<tr><td>" + "Type of Request: " + model.TypeofRequest + "</td></tr>";

                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";

                for (int i = 0; i < model.ApplicationCategoryData.Count; i++)
                {
                    body = body + "<tr><td>" + "Service Category: " + model.ApplicationCategoryData[i].ServiceCategory + "</td></tr>";
                    body = body + "<tr><td>" + "Service Sub Category: " + model.ApplicationCategoryData[i].ServiceSubCategory + "</td></tr>";
                    body = body + "<tr><td>" + "Role: " + model.ApplicationCategoryData[i].Role + "</td></tr>";
                    body = body + "<tr><td>" + "Access Type: " + model.ApplicationCategoryData[i].AccessType + "</td></tr>";
                    body = body + "<tr><td>" + "Brand: " + model.ApplicationCategoryData[i].BrandApp + "</td></tr>";
                    body = body + "<tr><td>" + "Application User ID: " + model.ApplicationCategoryData[i].ApplicationUserID + "</td></tr>";
                }

                body = body + "<tr><td>" + "Business justification(Max. 500): " + model.BusinessNeed + "</td></tr>";

                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "IT Manager(Dealer Connect Team)").Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }


        public async Task<EmailDataModel> GetISLSBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                string GPNo = "";
                var returnModel = new EmailDataModel();
                string FormName = "ISC LST Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                #region ViewData
                ISCLSData model = new ISCLSData();
                GlobalClass gc = new GlobalClass();
                List<ISCLSData> item = new List<ISCLSData>();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("ViewISCLSCFDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        //int emp;
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        if (dt.Rows[i]["Created"] != DBNull.Value)
                        {
                            item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            item1.Created = Convert.ToDateTime(dt.Rows[i]["Created"]);
                        }
                        model.FormIDISLS = item1;        // For PDF 
                        //Author AI = new Author();
                        //AI.Title = Convert.ToString(dt.Rows[i]["SubmitterUserName"]);
                        //model.Author = AI;
                        model.FormSrId = Convert.ToString(dt.Rows[0]["FormID"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        //model.OtherEmployeeContactNo = Convert.ToInt64(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.BusinessNeed = Convert.ToString(dt.Rows[0]["BusinessNeed"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.ActionType = Convert.ToString(dt.Rows[0]["ActionType"]);
                        model.BuyerName = Convert.ToString(dt.Rows[0]["BuyerName"]);
                        model.Team = Convert.ToString(dt.Rows[0]["Team"]);
                        model.GlobalProcessNumber = Convert.ToString(dt.Rows[0]["GlobalProcessNumber"]);
                        model.Description = Convert.ToString(dt.Rows[0]["Description"]);
                        model.InitialBudget = Convert.ToString(dt.Rows[0]["InitialBudget"]);
                        model.Status = Convert.ToString(dt.Rows[0]["Status"]);
                        model.BestBidOffer = Convert.ToString(dt.Rows[0]["BestBidOffer"]);
                        model.OrderVolume = Convert.ToString(dt.Rows[0]["OrderVolume"]);
                        model.TransactionVolume = Convert.ToString(dt.Rows[0]["TransactionVolume"]);
                        model.DiffBudgetAmount = Convert.ToString(dt.Rows[0]["DiffBudgetAmount"]);
                        model.Status1 = Convert.ToString(dt.Rows[0]["Status1"]);
                        model.Status2 = Convert.ToString(dt.Rows[0]["Status2"]);
                        model.AttachmentPath = Convert.ToString(dt.Rows[0]["AttachmentPath"]);
                        DateTime? BidderApprovalDate = null;
                        DateTime? RFQReceiptDate = null;
                        DateTime? RFQSentDate = null;
                        DateTime? OfferReceiptDate = null;
                        DateTime? SFODate = null;
                        DateTime? TargetClouseDate = null;
                        if (dt.Rows[0]["BidderApprovalDate"] != DBNull.Value)
                            BidderApprovalDate = Convert.ToDateTime(dt.Rows[0]["BidderApprovalDate"]);
                        model.BidderApprovalDate = BidderApprovalDate;
                        if (dt.Rows[0]["RFQReceiptDate"] != DBNull.Value)
                            RFQReceiptDate = Convert.ToDateTime(dt.Rows[0]["RFQReceiptDate"]);
                        if (dt.Rows[0]["RFQSentDate"] != DBNull.Value)
                            RFQSentDate = Convert.ToDateTime(dt.Rows[0]["RFQSentDate"]);
                        if (dt.Rows[0]["OfferReceiptDate"] != DBNull.Value)
                            OfferReceiptDate = Convert.ToDateTime(dt.Rows[0]["OfferReceiptDate"]);
                        if (dt.Rows[0]["SFODate"] != DBNull.Value)
                            SFODate = Convert.ToDateTime(dt.Rows[0]["SFODate"]);
                        if (dt.Rows[0]["TargetClouseDate"] != DBNull.Value)
                            TargetClouseDate = Convert.ToDateTime(dt.Rows[0]["TargetClouseDate"]);

                        model.TargetClouseDate = TargetClouseDate;
                        model.RFQReceiptDate = RFQReceiptDate;
                        model.RFQSentDate = RFQSentDate;
                        model.OfferReceiptDate = OfferReceiptDate;
                        model.SFODate = SFODate;
                        item.Add(model);
                    }
                }
                model = item.FirstOrDefault();
                #endregion

                GPNo = model.GlobalProcessNumber;
                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Buyer Name: " + model.BuyerName + "</td></tr>";

                body = body + "<tr><td>" + "Team: " + model.Team + "</td></tr>";

                body = body + "<tr><td>" + "Global Process Number: " + model.GlobalProcessNumber + "</td></tr>";

                body = body + "<tr><td>" + "Description: " + model.Description + "</td></tr>";

                body = body + "<tr><td>" + "Budget: " + model.InitialBudget + "</td></tr>";

                body = body + "<tr><td>" + "Status1: " + model.Status1 + "</td></tr>";

                body = body + "<tr><td>" + "Bidder Approval Date: " + model.BidderApprovalDate + "</td></tr>";

                body = body + "<tr><td>" + "RFQ Receipt Date: " + model.RFQReceiptDate + "</td></tr>";

                body = body + "<tr><td>" + "Offer Receipt Date: " + model.OfferReceiptDate + "</td></tr>";

                body = body + "<tr><td>" + "SFO Date: " + model.SFODate + "</td></tr>";

                body = body + "<tr><td>" + "Best Bid Offer: " + model.BestBidOffer + "</td></tr>";

                body = body + "<tr><td>" + "Order Volume: " + model.OrderVolume + "</td></tr>";

                body = body + "<tr><td>" + "Transaction Volume: " + model.TransactionVolume + "</td></tr>";

                body = body + "<tr><td>" + "Difference Budget Amount: " + model.DiffBudgetAmount + "</td></tr>";

                body = body + "<tr><td>" + "Target Clouse Date: " + model.TargetClouseDate + "</td></tr>";

                body = body + "<tr><td>" + "Status2: " + model.Status2 + "</td></tr>";



                body = body + "<tr><td>" + "Business justification(Max. 500): " + model.BusinessNeed + "</td></tr>";

                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                return new EmailDataModel() { Body = body, Location = employeeLocation, Comment = GPNo };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetAPFPBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "Analysis Parts Form Presentation Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<AnalysisPartsFormPresentationData> item = new List<AnalysisPartsFormPresentationData>();
                List<AnalysisPartsFormPresentationData> APFPTableDataList = new List<AnalysisPartsFormPresentationData>();
                AnalysisPartsFormPresentationData model = new AnalysisPartsFormPresentationData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('AnalysisPartsFormPresentationList')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;


                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    //var result = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationData>(responseTextData, settings);
                    //model = result;

                    //var result = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationData>(responseTextData, settings);
                    //model = result;

                    var SUCFUserResult = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationModel>(responseTextData, settings);
                    item = SUCFUserResult.AnalysisPartsFormPresentationResults.AnalysisPartsFormPresentationData;
                    model = item[0];


                    var handler1 = new HttpClientHandler();
                    handler1.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);
                    var client1 = new HttpClient(handler1);
                    client1.BaseAddress = new Uri(conString);
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('AnalysisPartsFormDataList')/items?$select=*"
           + "&$filter=(ListItemId eq '" + rowId + "')")).Result;
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var ListResult = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationModel>(responseText, settings);
                        APFPTableDataList = ListResult.AnalysisPartsFormPresentationResults.AnalysisPartsFormPresentationData;
                    }

                }

                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "Week No: " + model.WeekNo + "</td></tr>";
                body = body + "<tr><td>" + "Topic: " + model.Topic + "</td></tr>";
                body = body + "<tr><td>" + "Department: " + model.Department + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";

                for (int i = 0; i < APFPTableDataList.Count; i++)
                {
                    body = body + "<tr><td>" + "Project : " + APFPTableDataList[i].Project + "</td></tr>";
                    body = body + "<tr><td>" + "Parts : " + APFPTableDataList[i].Parts + "</td></tr>";
                    body = body + "<tr><td>" + "Quantity : " + APFPTableDataList[i].Quantity + "</td></tr>";
                    body = body + "<tr><td>" + "Reason : " + APFPTableDataList[i].Reason + "</td></tr>";
                }


                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";

                approverList.Add(nileshid);
                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }





        public async Task<EmailDataModel> GetIMACBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "IMAC Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<IMACFormModel> item = new List<IMACFormModel>();
                List<IMACFormModel> IMACTableDataList = new List<IMACFormModel>();
                IMACFormModel model = new IMACFormModel();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('IMACForm')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' )")).Result;


                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    //var result = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationData>(responseTextData, settings);
                    //model = result;

                    //var result = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationData>(responseTextData, settings);
                    //model = result;

                    var SUCFUserResult = JsonConvert.DeserializeObject<IMACModel>(responseTextData, settings);
                    item = SUCFUserResult.IMACResults.IMACFormModel;
                    model = item[0];


                    var handler1 = new HttpClientHandler();
                    handler1.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);
                    var client1 = new HttpClient(handler1);
                    client1.BaseAddress = new Uri(conString);
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    //         var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('IMACDataList')/items?$select=*"
                    //+ "&$filter=(FormID eq '" + formId + "' and RowId eq "+rowId+"')")).Result;

                    //         var response = await client1.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=*,"
                    //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");




                    //      var response = Task.Run(() => client1.GetAsync("_api/web/lists/GetByTitle('IMACDataList')/items?$select=*,"
                    //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author")).Result;
                    //      var responseText = await response.Content.ReadAsStringAsync();
                    //      if (!string.IsNullOrEmpty(responseText))
                    //      {
                    //          var ListResult = JsonConvert.DeserializeObject<IMACModel>(responseText, settings);
                    //          IMACTableDataList = ListResult.IMACResults.IMACFormModel;
                    //      }


                    var response2 = Task.Run(() => client1.GetAsync("_api/web/lists/GetByTitle('IMACDataList')/items?$select=*" +
                 "&$filter=(FormID eq '" + formId + "' and RowId eq '" + rowId + "')")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {

                        var ListResult = JsonConvert.DeserializeObject<IMACModel>(responseText2, settings);
                        IMACTableDataList = ListResult.IMACResults.IMACFormModel;
                    }

                }

                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "IMAC: " + model.IMACtype + "</td></tr>";
                body = body + "<tr><td>" + "Business Justification: " + model.BusinessJustification + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";

                for (int i = 0; i < IMACTableDataList.Count; i++)
                {
                    body = body + "<tr><td>" + "Assset Name : " + IMACTableDataList[i].AssetName + "</td></tr>";
                    body = body + "<tr><td>" + "Sub Category : " + IMACTableDataList[i].SubAssetName + "</td></tr>";
                    body = body + "<tr><td>" + "Make : " + IMACTableDataList[i].Make + "</td></tr>";
                    body = body + "<tr><td>" + "Model : " + IMACTableDataList[i].Modal + "</td></tr>";
                    body = body + "<tr><td>" + "Asset Type : " + IMACTableDataList[i].AssetType + "</td></tr>";

                    body = body + "<tr><td>" + "Acknowledgement : " + IMACTableDataList[i].Acknowledgement + "</td></tr>";
                    body = body + "<tr><td>" + "Serial No : " + IMACTableDataList[i].SerialNo + "</td></tr>";
                    body = body + "<tr><td>" + "Host Name : " + IMACTableDataList[i].HostName + "</td></tr>";
                    body = body + "<tr><td>" + "Location : " + IMACTableDataList[i].Location + "</td></tr>";
                    body = body + "<tr><td>" + "Type : " + IMACTableDataList[i].AssignType + "</td></tr>";
                    body = body + "<tr><td>" + "From : " + IMACTableDataList[i].FromDate.ToString("dd MMM yyyy") + "</td></tr>";
                    body = body + "<tr><td>" + "To : " + IMACTableDataList[i].ToDate.ToString("dd MMM yyyy") + "</td></tr>";

                }


                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + currentUser.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                //nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);
                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }
        public async Task<EmailDataModel> GetQMCRBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                string GPNo = "";
                var returnModel = new EmailDataModel();
                string FormName = "Quality Meisterbock Cubing Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                QualityMeisterbockCubingData model = new QualityMeisterbockCubingData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('QualityMeisterbockCubingList')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;

                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    var result = JsonConvert.DeserializeObject<QualityMeisterbockCubingModel>(responseTextData, settings);
                    model = result.QualityMeisterbockCubingResults.QualityMeisterbockCubingData[0];

                }
                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Form Type: " + model.FormType + "</td></tr>";

                body = body + "<tr><td>" + "Model: " + model.ModelQCM + "</td></tr>";

                body = body + "<tr><td>" + "Series: " + model.Series + "</td></tr>";

                body = body + "<tr><td>" + "Part Name: " + model.PartName + "</td></tr>";

                body = body + "<tr><td>" + "Part Quantity: " + model.PartQuantity + "</td></tr>";

                body = body + "<tr><td>" + "Problem Reported: " + model.ProblemReported + "</td></tr>";


                body = body + "<tr><td>" + "Details (Reason for past trial): " + model.Details + "</td></tr>";

                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, Comment = GPNo };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetMMRFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                string GPNo = "";
                var returnModel = new EmailDataModel();
                string FormName = "MMR Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                MMRData model = new MMRData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('MMRForm')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;

                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    var result = JsonConvert.DeserializeObject<MMRModel>(responseTextData, settings);
                    model = result.MMRResults.MMRData[0];

                }
                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Existing Department: " + model.ExistingDepartment + "</td></tr>";

                body = body + "<tr><td>" + "New Department: " + model.NewDepartment + "</td></tr>";

                body = body + "<tr><td>" + "Future Owner: " + model.FutureOwner + "</td></tr>";

                body = body + "<tr><td>" + "MMR Description: " + model.MMRDescription + "</td></tr>";

                body = body + "<tr><td>" + "MMR Identification: " + model.MMRIdentification + "</td></tr>";

                body = body + "<tr><td>" + "Transfer / Handover Date: " + model.HandoverDate?.ToString("dd MMM yyyy") + "</td></tr>";

                body = body + "<tr><td>" + "Transfer / Handover Type: " + model.TransferType + "</td></tr>";
                if (model.TransferType == "Temporary")
                {
                    body = body + "<tr><td>" + "Transfer From Date: " + model.TransferFromDate?.ToString("dd MMM yyyy") + "</td></tr>";
                    body = body + "<tr><td>" + "Transfer TO Date: " + model.TransferToDate?.ToString("dd MMM yyyy") + "</td></tr>";
                }
                body = body + "<tr><td>" + "MMR removed by existing user from EPUS: " + model.MMREpus.ToString("dd MMM yyyy") + "</td></tr>";

                body = body + "<tr><td>" + "Details: " + model.Details + "</td></tr>";

                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, Comment = GPNo };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetQFRFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                string GPNo = "";
                var returnModel = new EmailDataModel();
                string FormName = "Fixture Requisition Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                FixtureRequisitionData model = new FixtureRequisitionData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('FixtureRequisitionForm')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' and FormID eq '" + formId + "')")).Result;

                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {
                    var result = JsonConvert.DeserializeObject<FixtureRequisitionModel>(responseTextData, settings);
                    model = result.FixtureRequisitionResults.FixtureRequisitionData[0];

                }
                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";

                body = body + "<tr><td>" + "Fixture Name: " + model.FixtureName + "</td></tr>";

                body = body + "<tr><td>" + "Fixture No: " + model.FixtureNo + "</td></tr>";

                body = body + "<tr><td>" + "Project Name: " + model.ProjectName + "</td></tr>";

                body = body + "<tr><td>" + "From Date: " + model.FromDate + "</td></tr>";

                body = body + "<tr><td>" + "To Date: " + model.ToDate + "</td></tr>";

                body = body + "<tr><td>" + "Reason: " + model.Reason + "</td></tr>";
                // Measurement
                body = body + "<br><br> <tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Taking from Measurement Room</b></th></tr>";
                body = body + "<br> <table width=\"100%\">";

                body = body + "<tr> <th style=\"width: 10%;text-align: left;\">Sr. No.</th> <th style=\"width:25%;text-align: left;\">Check point</th>  <th style=\"width:25%;text-align: left;\">Status</th> <th style=\"width:10%;text-align: left;\">Remarks</th></tr>" +
                    "";
                body = body + "<tr><td>1</td> <td><label>RPS Pins</label></td><td class=\"text-center\"> " + model.RpsPin + " </td><td class=\"text-center\"> " + model.RpsPinRemark + " </td></tr>";

                body = body + "<tr><td>2</td> <td><label>Clamps</label></td><td class=\"text-center\">" + model.Clamps + "</td><td class=\"text-center\"> " + model.ClampsRemark + " </td></tr>";

                body = body + "<tr><td>3</td> <td><label>Wheels</label></td><td class=\"text-center\">" + model.Wheels + "</td><td class=\"text-center\"> " + model.WheelsRemark + " </td></tr>";

                body = body + "<tr><td>4</td> <td><label>Rps Stick</label></td><td class=\"text-center\">" + model.RpsStick + "</td><td class=\"text-center\"> " + model.RpsStickRemark + " </td></tr>";

                body = body + "<tr><td>5</td> <td><label>Lose Element</label></td><td class=\"text-center\"> " + model.LoseElement + "</td><td class=\"text-center\"> " + model.LoseRemark + " </td></tr>";

                body = body + "<tr><td>6</td> <td><label>Mylers</label></td><td class=\"text-center\"> " + model.Mylers + "</td><td class=\"text-center\"> " + model.MylerRemark + " </td></tr>";

                body = body + "<tr><td>7</td> <td><label>Pin Threads</label></td><td class=\"text-center\">" + model.PinThreads + "</td><td class=\"text-center\"> " + model.PinRemark + " </td></tr>";

                body = body + "<tr><td>8</td> <td><label>Resting Pads</label></td><td class=\"text-center\"> " + model.RestingPads + "</td><td class=\"text-center\"> " + model.PadsRemark + " </td></tr>";

                body = body + "<tr><td>9</td> <td><label>Sliders</label></td><td class=\"text-center\"> " + model.Sliders + "</td><td class=\"text-center\"> " + model.SlidersRemark + " </td></tr>";

                body = body + "<tr><td>10</td> <td><label>Kugel</label></td><td class=\"text-center\"> " + model.Kugel + "</td><td class=\"text-center\"> " + model.KugelRemark + " </td></tr>";

                body = body + "</table><br>";
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.TimeStamp.ToShortDateString() + " " + Convert.ToDateTime(approver.TimeStamp).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                return new EmailDataModel() { Body = body, Location = employeeLocation, Comment = GPNo };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetIPAFFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "IPAF Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<IPAFData> item = new List<IPAFData>();
                List<IPAFData> IPAFFormDataList = new List<IPAFData>();
                IPAFData model = new IPAFData();
                #region Comment
                //GlobalClass gc = new GlobalClass();

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('IPAFForm')/items?$select=*"
                //+ "&$filter=(ID eq '" + rowId + "' )")).Result;


                //var responseTextData = await responseData.Content.ReadAsStringAsync();

                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //if (!string.IsNullOrEmpty(responseTextData))
                //{


                //    var Result = JsonConvert.DeserializeObject<IPAFModel>(responseTextData, settings);
                //    item = Result.IPAFResults.IPAFData;
                //    model = item[0];


                //    var handler1 = new HttpClientHandler();
                //    handler1.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);
                //    var client1 = new HttpClient(handler1);
                //    client1.BaseAddress = new Uri(conString);
                //    client1.DefaultRequestHeaders.Accept.Clear();
                //    client1.DefaultRequestHeaders.Accept.Clear();
                //    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //    var response2 = Task.Run(() => client1.GetAsync("_api/web/lists/GetByTitle('IPAFFormDataList')/items?$select=*" +
                // "&$filter=(FormId eq '" + rowId + "')")).Result;
                //    var responseText2 = await response2.Content.ReadAsStringAsync();
                //    if (!string.IsNullOrEmpty(responseText2))
                //    {

                //        var ListResult = JsonConvert.DeserializeObject<IPAFModel>(responseText2, settings);
                //        IPAFFormDataList = ListResult.IPAFResults.IPAFData;
                //    }

                //}
                #endregion

                #region SQL COMMENT TEST
                //SqlCommand cmd = new SqlCommand();
                //SqlDataAdapter adapter = new SqlDataAdapter();
                //DataTable dt = new DataTable();
                //con = new SqlConnection(sqlConString);
                //cmd = new SqlCommand("USP_ViewIPAFDetails", con);
                //cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                //// cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                //cmd.CommandType = CommandType.StoredProcedure;
                //adapter.SelectCommand = cmd;
                //con.Open();
                //adapter.Fill(dt);
                //con.Close();
                //if (dt.Rows.Count > 0)
                //{
                //    for (int i = 0; i < dt.Rows.Count; i++)
                //    {
                //        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                //        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                //        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                //        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                //        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                //        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                //        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                //        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                //        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                //        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                //        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                //        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                //        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                //        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                //        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                //        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                //        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                //        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                //        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                //        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                //        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                //        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                //        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                //        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                //        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                //        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                //        model.BusinessJustification = Convert.ToString(dt.Rows[0]["BusinessJustification"]);
                //        model.RequestType = Convert.ToString(dt.Rows[0]["RequestType"]);
                //        model.RequestFromDate = Convert.ToDateTime(dt.Rows[0]["RequestFromDate"]);
                //        model.RequestToDate = Convert.ToDateTime(dt.Rows[0]["RequestToDate"]);
                //        model.CreatedDate = Convert.ToDateTime(dt.Rows[0]["Created"]);
                //        item.Add(model);
                //    }

                //}
                //model = item[0];

                //SqlCommand cmd1 = new SqlCommand();
                //SqlDataAdapter adapter1 = new SqlDataAdapter();
                //DataTable ds1 = new DataTable();
                //con = new SqlConnection(sqlConString);
                //cmd = new SqlCommand("USP_ViewIPAFFormDataList", con);
                //cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                //// cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                //cmd.CommandType = CommandType.StoredProcedure;
                //adapter.SelectCommand = cmd;
                //con.Open();
                //adapter.Fill(ds1);
                //con.Close();
                //if (ds1.Rows.Count > 0)
                //{
                //    for (int i = 0; i < ds1.Rows.Count; i++)
                //    {
                //        IPAFData model1 = new IPAFData();
                //        model1.FormId = Convert.ToInt32(ds1.Rows[i]["FormId"]);
                //        model1.Applicationname = Convert.ToString(ds1.Rows[i]["Applicationname"]);
                //        model1.Applicationurl = Convert.ToString(ds1.Rows[i]["Applicationurl"]);
                //        model1.Applicationaccess = Convert.ToString(ds1.Rows[i]["Applicationaccess"]);
                //        model1.Accessgroup = Convert.ToString(ds1.Rows[i]["Accessgroup"]);
                //        IPAFFormDataList.Add(model1);
                //    }
                //}
                #endregion

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewIPAFDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.BusinessJustification = Convert.ToString(dt.Rows[0]["BusinessJustification"]);
                        model.RequestType = Convert.ToString(dt.Rows[0]["RequestType"]);
                        model.RequestFromDate = Convert.ToDateTime(dt.Rows[0]["RequestFromDate"]);
                        model.RequestToDate = Convert.ToDateTime(dt.Rows[0]["RequestToDate"]);
                        model.CreatedDate = Convert.ToDateTime(dt.Rows[0]["Created"]);
                        item.Add(model);
                    }

                }
                model = item[0];
                //dynamic URCFData = new ExpandoObject();
                //IPAFFormDAL iPAFFormDAL = new IPAFFormDAL();
                //URCFData = iPAFFormDAL.ViewIPAFFormData(rowId, formId);
                //model = URCFData.Result;
                //IPAFFormDataList = URCFData.Four;

                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "Request Type: " + model.RequestType + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";


                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewIPAFFormDataList", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {
                    for (int i = 0; i < ds1.Rows.Count; i++)
                    {
                        IPAFData model1 = new IPAFData();
                        model1.FormId = Convert.ToInt32(ds1.Rows[i]["FormId"]);
                        model1.Applicationname = Convert.ToString(ds1.Rows[i]["Applicationname"]);
                        model1.Applicationurl = Convert.ToString(ds1.Rows[i]["Applicationurl"]);
                        model1.Applicationaccess = Convert.ToString(ds1.Rows[i]["Applicationaccess"]);
                        model1.Accessgroup = Convert.ToString(ds1.Rows[i]["Accessgroup"]);
                        IPAFFormDataList.Add(model1);
                    }
                }

                for (int i = 0; i < IPAFFormDataList.Count; i++)
                {
                    body = body + "<tr><td>" + "Application Name : " + IPAFFormDataList[i].Applicationname + "</td></tr>";
                    body = body + "<tr><td>" + "Application Url : " + IPAFFormDataList[i].Applicationurl + "</td></tr>";
                    body = body + "<tr><td>" + "Application Access : " + IPAFFormDataList[i].Applicationaccess + "</td></tr>";
                    body = body + "<tr><td>" + "Access Group : " + IPAFFormDataList[i].Accessgroup + "</td></tr>";


                }
                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                //nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetTSFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "TSF Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<TSFModel> item = new List<TSFModel>();
                List<DailyHour> TSFormDataList = new List<DailyHour>();
                TSFModel model = new TSFModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewTSFormDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);

                        model.EmployeeID = Convert.ToString(dt.Rows[0]["EmployeeID"]);
                        model.WeekEndingDate = Convert.ToDateTime(dt.Rows[0]["WeekEndingDate"]);
                        model.TotalHoursWorked = Convert.ToDecimal(dt.Rows[0]["TotalHoursWorked"]);
                        if (dt.Rows[0]["LeaveTaken"] != DBNull.Value)
                            model.LeaveTaken = Convert.ToDecimal(dt.Rows[0]["LeaveTaken"]);
                        else
                            model.LeaveTaken = 0;
                        item.Add(model);
                    }

                }
                model = item[0];
                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "EmployeeID: " + model.EmployeeID + "</td></tr>";
                body = body + "<tr><td>" + "WeekEndingDate: " + model.WeekEndingDate + "</td></tr>";
                body = body + "<tr><td>" + "Total Hours Worked: " + model.TotalHoursWorked + "</td></tr>";
                body = body + "<tr><td>" + "LeaveTaken: " + model.LeaveTaken + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";


                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewTSFormDataList", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {
                    for (int i = 0; i < ds1.Rows.Count; i++)
                    {
                        DailyHour model1 = new DailyHour();
                        model1.DailyHourId = Convert.ToInt32(ds1.Rows[i]["Id"]);
                        model1.TSFId = Convert.ToInt32(ds1.Rows[i]["TSFId"]);
                        model1.CostCode = Convert.ToString(ds1.Rows[i]["CostCode"]);
                        model1.MondayHours = Convert.ToDecimal(ds1.Rows[i]["MondayHours"]);
                        model1.TuesdayHours = Convert.ToDecimal(ds1.Rows[i]["TuesdayHours"]);
                        model1.WednesdayHours = Convert.ToDecimal(ds1.Rows[i]["WednesdayHours"]);
                        model1.ThursdayHours = Convert.ToDecimal(ds1.Rows[i]["ThursdayHours"]);
                        model1.FridayHours = Convert.ToDecimal(ds1.Rows[i]["FridayHours"]);
                        if (ds1.Rows[i]["SaturdayHours"] != DBNull.Value)
                            model1.SaturdayHours = Convert.ToDecimal(ds1.Rows[i]["SaturdayHours"]);
                        else
                            model1.SaturdayHours = null;
                        if (ds1.Rows[i]["SundayHours"] != DBNull.Value)
                            model1.SundayHours = Convert.ToDecimal(ds1.Rows[i]["SundayHours"]);
                        else
                            model1.SundayHours = null;
                        TSFormDataList.Add(model1);
                    }
                }

                for (int i = 0; i < TSFormDataList.Count; i++)
                {
                    body = body + "<tr><td>" + "CostCode : " + TSFormDataList[i].CostCode + "</td></tr>";
                    body = body + "<tr><td>" + "MondayHours : " + TSFormDataList[i].MondayHours + "</td></tr>";
                    body = body + "<tr><td>" + "Tuesday Hours : " + TSFormDataList[i].TuesdayHours + "</td></tr>";
                    body = body + "<tr><td>" + "Wednesday Hours : " + TSFormDataList[i].WednesdayHours + "</td></tr>";
                    body = body + "<tr><td>" + "Thursday Hours : " + TSFormDataList[i].ThursdayHours + "</td></tr>";
                    body = body + "<tr><td>" + "Friday Hours : " + TSFormDataList[i].FridayHours + "</td></tr>";
                    body = body + "<tr><td>" + "Saturday Hours : " + TSFormDataList[i].SaturdayHours + "</td></tr>";
                    body = body + "<tr><td>" + "Sunday Hours : " + TSFormDataList[i].SundayHours + "</td></tr>";


                }
                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                //nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetDCAFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "DCAF Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<DCAFModel> item = new List<DCAFModel>();
                List<DailyHour> DCAFormDataList = new List<DailyHour>();
                DCAFModel model = new DCAFModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewDCAFormDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);

                        model.DCAFEmployeeID = Convert.ToString(dt.Rows[0]["DCAFEmployeeID"]);
                        model.DateOfIncident = (DateTime)(dt.Rows[0]["DateOfIncident"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["DateOfIncident"]) : (DateTime?)null);
                        model.DescriptionOfIncident = Convert.ToString(dt.Rows[0]["DescriptionOfIncident"]);
                        model.Witnesses = Convert.ToString(dt.Rows[0]["Witnesses"]);
                        model.DisciplinaryActionType = Convert.ToString(dt.Rows[0]["DisciplinaryActionType"]);
                        model.ActionTakenBy = Convert.ToString(dt.Rows[0]["ActionTakenBy"]);
                        model.DateOfAction = Convert.ToDateTime(dt.Rows[0]["DateOfAction"]); // Mandatory field, so no null check
                        model.EmployeeComments = Convert.ToString(dt.Rows[0]["EmployeeComments"]);
                        item.Add(model);
                    }

                }
                model = item[0];
                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "EmployeeID: " + model.DCAFEmployeeID + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Employee ID:</td><td>" + model.DCAFEmployeeID + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Date of Incident:</td><td>" + (model.DateOfIncident.HasValue ? model.DateOfIncident.Value.ToString("yyyy-MM-dd") : "N/A") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Description of Incident:</td><td>" + (model.DescriptionOfIncident ?? "N/A") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Witnesses:</td><td>" + (model.Witnesses ?? "N/A") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Disciplinary Action Type:</td><td>" + (model.DisciplinaryActionType ?? "N/A") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Action Taken By:</td><td>" + model.ActionTakenBy + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Date of Action:</td><td>" + model.DateOfAction.Value.ToString("yyyy-MM-dd") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Employee Comments:</td><td>" + (model.EmployeeComments ?? "N/A") + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";


                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                //nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }
        
        public async Task<EmailDataModel> GetERFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "ERF Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<ERFModel> item = new List<ERFModel>();
                ERFModel model = new ERFModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewERFormDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);

                        model.ERFEmployeeID = Convert.ToString(dt.Rows[0]["ERFEmployeeID"]); model.ERFEmployeeID = Convert.ToString(dt.Rows[0]["ERFEmployeeID"]);
                        model.ExpenseType = Convert.ToString(dt.Rows[0]["ExpenseType"]);
                        model.ExpenseDate = Convert.ToDateTime(dt.Rows[0]["ExpenseDate"]);
                        model.ExpenseAmount = Convert.ToDecimal(dt.Rows[0]["ExpenseAmount"]);
                        model.ReceiptAttached = Convert.ToBoolean(dt.Rows[0]["ReceiptAttached"]);
                        model.CostCode = Convert.ToString(dt.Rows[0]["CostCode"]);
                        item.Add(model);
                    }

                }
                model = item[0];
                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "EmployeeID: " + model.ERFEmployeeID + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Expense Type:</td><td>" + model.ExpenseType + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Expense Date:</td><td>" + model.ExpenseDate.Value.ToString("yyyy-MM-dd") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Expense Amount:</td><td>" + model.ExpenseAmount.ToString("F2") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Receipt Attached:</td><td>" + (model.ReceiptAttached ? "Yes" : "No") + "</td></tr>";
                body += "<tr><td style='font-weight: bold;'>Cost Code:</td><td>" + model.CostCode + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";


                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                //nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetNEIFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "NEIF Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<NEIFModel> item = new List<NEIFModel>();
                List<NEIFModel> NEIFormDataList = new List<NEIFModel>();
                NEIFModel model = new NEIFModel();
                #region Comment
                //GlobalClass gc = new GlobalClass();

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('NEIForm')/items?$select=*"
                //+ "&$filter=(ID eq '" + rowId + "' )")).Result;


                //var responseTextData = await responseData.Content.ReadAsStringAsync();

                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //if (!string.IsNullOrEmpty(responseTextData))
                //{


                //    var Result = JsonConvert.DeserializeObject<NEIFModel>(responseTextData, settings);
                //    item = Result.NEIFResults.NEIFData;
                //    model = item[0];


                //    var handler1 = new HttpClientHandler();
                //    handler1.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);
                //    var client1 = new HttpClient(handler1);
                //    client1.BaseAddress = new Uri(conString);
                //    client1.DefaultRequestHeaders.Accept.Clear();
                //    client1.DefaultRequestHeaders.Accept.Clear();
                //    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //    var response2 = Task.Run(() => client1.GetAsync("_api/web/lists/GetByTitle('NEIFormDataList')/items?$select=*" +
                // "&$filter=(FormId eq '" + rowId + "')")).Result;
                //    var responseText2 = await response2.Content.ReadAsStringAsync();
                //    if (!string.IsNullOrEmpty(responseText2))
                //    {

                //        var ListResult = JsonConvert.DeserializeObject<NEIFModel>(responseText2, settings);
                //        NEIFormDataList = ListResult.NEIFResults.NEIFData;
                //    }

                //}
                #endregion

                #region SQL COMMENT TEST
                //SqlCommand cmd = new SqlCommand();
                //SqlDataAdapter adapter = new SqlDataAdapter();
                //DataTable dt = new DataTable();
                //con = new SqlConnection(sqlConString);
                //cmd = new SqlCommand("USP_ViewNEIFDetails", con);
                //cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                //// cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                //cmd.CommandType = CommandType.StoredProcedure;
                //adapter.SelectCommand = cmd;
                //con.Open();
                //adapter.Fill(dt);
                //con.Close();
                //if (dt.Rows.Count > 0)
                //{
                //    for (int i = 0; i < dt.Rows.Count; i++)
                //    {
                //        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                //        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                //        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                //        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                //        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                //        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                //        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                //        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                //        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                //        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                //        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                //        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                //        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                //        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                //        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                //        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                //        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                //        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                //        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                //        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                //        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                //        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                //        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                //        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                //        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                //        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                //        model.BusinessJustification = Convert.ToString(dt.Rows[0]["BusinessJustification"]);
                //        model.RequestType = Convert.ToString(dt.Rows[0]["RequestType"]);
                //        model.RequestFromDate = Convert.ToDateTime(dt.Rows[0]["RequestFromDate"]);
                //        model.RequestToDate = Convert.ToDateTime(dt.Rows[0]["RequestToDate"]);
                //        model.CreatedDate = Convert.ToDateTime(dt.Rows[0]["Created"]);
                //        item.Add(model);
                //    }

                //}
                //model = item[0];

                //SqlCommand cmd1 = new SqlCommand();
                //SqlDataAdapter adapter1 = new SqlDataAdapter();
                //DataTable ds1 = new DataTable();
                //con = new SqlConnection(sqlConString);
                //cmd = new SqlCommand("USP_ViewNEIFormDataList", con);
                //cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                //// cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                //cmd.CommandType = CommandType.StoredProcedure;
                //adapter.SelectCommand = cmd;
                //con.Open();
                //adapter.Fill(ds1);
                //con.Close();
                //if (ds1.Rows.Count > 0)
                //{
                //    for (int i = 0; i < ds1.Rows.Count; i++)
                //    {
                //        NEIFData model1 = new NEIFData();
                //        model1.FormId = Convert.ToInt32(ds1.Rows[i]["FormId"]);
                //        model1.Applicationname = Convert.ToString(ds1.Rows[i]["Applicationname"]);
                //        model1.Applicationurl = Convert.ToString(ds1.Rows[i]["Applicationurl"]);
                //        model1.Applicationaccess = Convert.ToString(ds1.Rows[i]["Applicationaccess"]);
                //        model1.Accessgroup = Convert.ToString(ds1.Rows[i]["Accessgroup"]);
                //        NEIFormDataList.Add(model1);
                //    }
                //}
                #endregion

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewNEIFDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.FormId = Convert.ToInt32(dt.Rows[i]["FormId"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]);
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]);
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null || dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value || dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);

                        model.FullName = Convert.ToString(dt.Rows[0]["FullName"]);
                        model.PreferredName = Convert.ToString(dt.Rows[0]["PreferredName"]);
                        model.Gender = Convert.ToString(dt.Rows[0]["Gender"]);
                        model.MaritalStatus = Convert.ToString(dt.Rows[0]["MaritalStatus"]);
                        model.Nationality = Convert.ToString(dt.Rows[0]["Nationality"]);
                        model.PersonalEmail = Convert.ToString(dt.Rows[0]["PersonalEmail"]);
                        model.MobilePhoneNumber = Convert.ToString(dt.Rows[0]["MobilePhoneNumber"]);
                        model.EmergencyContactName = Convert.ToString(dt.Rows[0]["EmergencyContactName"]);
                        model.EmergencyContactRelationship = Convert.ToString(dt.Rows[0]["EmergencyContactRelationship"]);
                        model.EmergencyContactPhoneNumber = Convert.ToString(dt.Rows[0]["EmergencyContactPhoneNumber"]);
                        model.CurrentAddress = Convert.ToString(dt.Rows[0]["CurrentAddress"]);
                        model.PermanentAddress = Convert.ToString(dt.Rows[0]["PermanentAddress"]);
                        model.PostalCode = Convert.ToString(dt.Rows[0]["PostalCode"]);
                        model.JobTitle = Convert.ToString(dt.Rows[0]["JobTitle"]);
                        model.Department = Convert.ToString(dt.Rows[0]["Department"]);
                        model.EmploymentType = Convert.ToString(dt.Rows[0]["EmploymentType"]);
                        model.ManagerName = Convert.ToString(dt.Rows[0]["ManagerName"]);
                        model.BankAccountNumber = Convert.ToString(dt.Rows[0]["BankAccountNumber"]);
                        model.BankName = Convert.ToString(dt.Rows[0]["BankName"]);
                        model.IFSCCode = Convert.ToString(dt.Rows[0]["IFSCCode"]);
                        model.TaxIDNumber = Convert.ToString(dt.Rows[0]["TaxIDNumber"]);
                        model.PAN = Convert.ToString(dt.Rows[0]["PAN"]);
                        model.AADHAR = Convert.ToString(dt.Rows[0]["AADHAR"]);
                        model.DrivingLicense = Convert.ToString(dt.Rows[0]["DrivingLicense"]);
                        model.Passport = Convert.ToString(dt.Rows[0]["Passport"]);
                        model.ValidVisa = Convert.ToString(dt.Rows[0]["ValidVisa"]);
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);
                        model.DateOfBirth = null;
                        if (dt.Rows[0]["DateOfBirth"] != DBNull.Value)
                            model.DateOfBirth = Convert.ToDateTime(dt.Rows[0]["DateOfBirth"]);
                        model.DateOfJoining = null;
                        if (dt.Rows[0]["DateOfJoining"] != DBNull.Value)
                            model.DateOfJoining = Convert.ToDateTime(dt.Rows[0]["DateOfJoining"]);
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);

                        item.Add(model);
                    }

                }
                model = item[0];
                //dynamic URCFData = new ExpandoObject();
                //NEIFormDAL NEIFormDAL = new NEIFormDAL();
                //URCFData = NEIFormDAL.ViewNEIFormData(rowId, formId);
                //model = URCFData.Result;
                //NEIFormDataList = URCFData.Four;

                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body += "<tr><td>" + "Full Name: " + model.FullName + "</td></tr>";
                body += "<tr><td>" + "Preferred Name: " + model.PreferredName + "</td></tr>";
                body += "<tr><td>" + "Date of Birth: " + model.DateOfBirth?.ToString("dd/MM/yyyy") + "</td></tr>";
                body += "<tr><td>" + "Gender: " + model.Gender + "</td></tr>";
                body += "<tr><td>" + "Marital Status: " + model.MaritalStatus + "</td></tr>";
                body += "<tr><td>" + "Nationality: " + model.Nationality + "</td></tr>";
                body += "<tr><td>" + "Personal Email: " + model.PersonalEmail + "</td></tr>";
                body += "<tr><td>" + "Mobile Phone Number: " + model.MobilePhoneNumber + "</td></tr>";
                body += "<tr><td>" + "Emergency Contact Name: " + model.EmergencyContactName + "</td></tr>";
                body += "<tr><td>" + "Emergency Contact Relationship: " + model.EmergencyContactRelationship + "</td></tr>";
                body += "<tr><td>" + "Emergency Contact Phone Number: " + model.EmergencyContactPhoneNumber + "</td></tr>";
                body += "<tr><td>" + "Current Address: " + model.CurrentAddress + "</td></tr>";
                body += "<tr><td>" + "Permanent Address: " + model.PermanentAddress + "</td></tr>";
                body += "<tr><td>" + "Postal Code: " + model.PostalCode + "</td></tr>";
                body += "<tr><td>" + "Job Title: " + model.JobTitle + "</td></tr>";
                body += "<tr><td>" + "Department: " + model.Department + "</td></tr>";
                body += "<tr><td>" + "Date of Joining: " + model.DateOfJoining?.ToString("dd/MM/yyyy") + "</td></tr>";
                body += "<tr><td>" + "Employment Type: " + model.EmploymentType + "</td></tr>";
                body += "<tr><td>" + "Manager/Supervisor Name: " + model.ManagerName + "</td></tr>";
                body += "<tr><td>" + "Bank Account Number: " + model.BankAccountNumber + "</td></tr>";
                body += "<tr><td>" + "Bank Name: " + model.BankName + "</td></tr>";
                body += "<tr><td>" + "IFSC Code: " + model.IFSCCode + "</td></tr>";
                body += "<tr><td>" + "Tax ID Number: " + model.TaxIDNumber + "</td></tr>";
                body += "<tr><td>" + "PAN: " + model.PAN + "</td></tr>";
                body += "<tr><td>" + "AADHAR: " + model.AADHAR + "</td></tr>";
                body += "<tr><td>" + "Driving License: " + model.DrivingLicense + "</td></tr>";
                body += "<tr><td>" + "Passport: " + model.Passport + "</td></tr>";
                body += "<tr><td>" + "Valid Visa: " + model.ValidVisa + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";

                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                //nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }

        public async Task<EmailDataModel> GetPOCRFBody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "POCR Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                #region ViewPOCRMFDetails
                List<POCRFormModel> item = new List<POCRFormModel>();
                POCRFormModel model = new POCRFormModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("ViewPOCRMFDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int j = 0; j < dt.Rows.Count; j++)
                    {
                        model.AppRowId = dt.Rows[0]["AutoID"] != DBNull.Value || dt.Rows[0]["AutoID"] != "0" ? Convert.ToInt32(dt.Rows[j]["AutoID"]) : 0;
                        model.EmployeeType = dt.Rows[0]["EmployeeType"] != DBNull.Value || dt.Rows[0]["EmployeeType"] != "0" ? Convert.ToString(dt.Rows[0]["EmployeeType"]) : "";
                        model.EmployeeCode = dt.Rows[0]["EmployeeCode"] != DBNull.Value || dt.Rows[0]["EmployeeCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["EmployeeCode"]) : 0;
                        model.EmployeeCCCode = dt.Rows[0]["EmployeeCCCode"] != DBNull.Value || dt.Rows[0]["EmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]) : 0;
                        model.EmployeeUserId = dt.Rows[0]["EmployeeUserId"] != DBNull.Value || dt.Rows[0]["EmployeeUserId"] != "0" ? Convert.ToString(dt.Rows[0]["EmployeeUserId"]) : "";
                        model.EmployeeName = dt.Rows[0]["EmployeeName"] != DBNull.Value || dt.Rows[0]["EmployeeName"] != "0" ? Convert.ToString(dt.Rows[0]["EmployeeName"]) : "";
                        model.EmployeeDepartment = dt.Rows[0]["EmployeeDepartment"] != DBNull.Value || dt.Rows[0]["EmployeeDepartment"] != "0" ? Convert.ToString(dt.Rows[0]["EmployeeDepartment"]) : "";
                        model.EmployeeContactNo = dt.Rows[0]["EmployeeContactNo"] != DBNull.Value || dt.Rows[0]["EmployeeContactNo"] != "0" ? Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]) : 0;
                        model.ExternalOrganizationName = dt.Rows[0]["ExternalOrganizationName"] != DBNull.Value || dt.Rows[0]["ExternalOrganizationName"] != "0" ? Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]) : "";
                        model.EmployeeLocation = dt.Rows[0]["EmployeeLocation"] != DBNull.Value || dt.Rows[0]["EmployeeLocation"] != "0" ? Convert.ToString(dt.Rows[0]["EmployeeLocation"]) : "";
                        model.EmployeeDesignation = dt.Rows[0]["EmployeeDesignation"] != DBNull.Value || dt.Rows[0]["EmployeeDesignation"] != "0" ? Convert.ToString(dt.Rows[0]["EmployeeDesignation"]) : "";
                        model.RequestSubmissionFor = dt.Rows[0]["RequestSubmissionFor"] != DBNull.Value || dt.Rows[0]["RequestSubmissionFor"] != "0" ? Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]) : "";
                        model.OnBehalfOption = dt.Rows[0]["OnBehalfOption"] != DBNull.Value || dt.Rows[0]["OnBehalfOption"] != "0" ? Convert.ToString(dt.Rows[0]["OnBehalfOption"]) : "";
                        model.OtherEmployeeType = dt.Rows[0]["OtherEmployeeType"] != DBNull.Value || dt.Rows[0]["OtherEmployeeType"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeType"]) : "";
                        model.OtherEmployeeCode = dt.Rows[0]["OtherEmployeeCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCode"] != "0" && dt.Rows[0]["OtherEmployeeCode"] != "" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]) : 0;
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null && dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = dt.Rows[0]["OtherEmployeeContactNo"] != DBNull.Value || dt.Rows[0]["OtherEmployeeContactNo"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]) : "";
                        model.OtherEmployeeUserId = dt.Rows[0]["OtherEmployeeUserId"] != DBNull.Value || dt.Rows[0]["OtherEmployeeUserId"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]) : "";
                        model.OtherEmployeeName = dt.Rows[0]["OtherEmployeeName"] != DBNull.Value || dt.Rows[0]["OtherEmployeeName"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeName"]) : "";
                        model.OtherEmployeeDepartment = dt.Rows[0]["OtherEmployeeDepartment"] != DBNull.Value || dt.Rows[0]["OtherEmployeeDepartment"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]) : "";
                        model.OtherEmployeeLocation = dt.Rows[0]["OtherEmployeeLocation"] != DBNull.Value || dt.Rows[0]["OtherEmployeeLocation"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]) : "";
                        model.OtherEmployeeDesignation = dt.Rows[0]["OtherEmployeeDesignation"] != DBNull.Value || dt.Rows[0]["OtherEmployeeDesignation"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]) : "";
                        model.OtherExternalOrganizationName = dt.Rows[0]["OtherExternalOrganizationName"] != DBNull.Value || dt.Rows[0]["OtherExternalOrganizationName"] != "0" ? Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]) : "";
                        model.OtherEmployeeEmailId = dt.Rows[0]["OtherEmployeeEmailId"] != DBNull.Value || dt.Rows[0]["OtherEmployeeEmailId"] != "0" ? Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]) : "";
                        model.BusinessNeed = dt.Rows[0]["BusinessNeed"] != DBNull.Value || dt.Rows[0]["BusinessNeed"] != "0" ? Convert.ToString(dt.Rows[0]["BusinessNeed"]) : "";
                        model.ID = dt.Rows[0]["AutoID"] != DBNull.Value || dt.Rows[0]["AutoID"] != "0" ? Convert.ToInt32(dt.Rows[0]["AutoID"]) : 0;
                        model.POCRID = dt.Rows[0]["POCRID"] != DBNull.Value || dt.Rows[0]["POCRID"] != "0" ? Convert.ToInt32(dt.Rows[0]["POCRID"]) : 0;
                        model.POCRNo = dt.Rows[0]["POCRNo"] != DBNull.Value || dt.Rows[0]["POCRNo"] != "0" ? Convert.ToString(dt.Rows[0]["POCRNo"]) : "";
                        model.TSEName = dt.Rows[0]["TSEName"] != DBNull.Value || dt.Rows[0]["TSEName"] != "0" ? Convert.ToString(dt.Rows[0]["TSEName"]) : "";
                        model.ZSMName = dt.Rows[0]["ZSMName"] != DBNull.Value || dt.Rows[0]["ZSMName"] != "0" ? Convert.ToString(dt.Rows[0]["ZSMName"]) : "";
                        model.ASMName = dt.Rows[0]["ASMName"] != DBNull.Value || dt.Rows[0]["ASMName"] != "0" ? Convert.ToString(dt.Rows[0]["ASMName"]) : "";
                        model.State = dt.Rows[0]["StateID"] != DBNull.Value || dt.Rows[0]["StateID"] != "0" ? Convert.ToString(dt.Rows[0]["StateID"]) : "";
                        model.DealerCode = dt.Rows[0]["DealerCode"] != DBNull.Value || dt.Rows[0]["DealerCode"] != "0" ? Convert.ToString(dt.Rows[0]["DealerCode"]) : "";
                        model.DealerName = dt.Rows[0]["DealerName"] != DBNull.Value || dt.Rows[0]["DealerName"] != "0" ? Convert.ToString(dt.Rows[0]["DealerName"]) : "";
                        model.DealerLocation = dt.Rows[0]["DealerLocation"] != DBNull.Value || dt.Rows[0]["DealerLocation"] != "0" ? Convert.ToString(dt.Rows[0]["DealerLocation"]) : "";
                        model.FILDealership = dt.Rows[0]["FILDealership"] != DBNull.Value || dt.Rows[0]["FILDealership"] != "0" ? Convert.ToString(dt.Rows[0]["FILDealership"]) : "";
                        model.DealerSalesLastYr = dt.Rows[0]["DealerSalesLY"] != DBNull.Value || dt.Rows[0]["DealerSalesLY"] != "0" ? Convert.ToString(dt.Rows[0]["DealerSalesLY"]) : "";
                        model.DealerSalesTill = dt.Rows[0]["DealerSalesTD"] != DBNull.Value || dt.Rows[0]["DealerSalesTD"] != "0" ? Convert.ToString(dt.Rows[0]["DealerSalesTD"]) : "";
                        model.Status = dt.Rows[0]["ExclusiveStatus"] != DBNull.Value || dt.Rows[0]["ExclusiveStatus"] != "0" ? Convert.ToString(dt.Rows[0]["ExclusiveStatus"]) : "";
                        model.BuilderName = dt.Rows[0]["BuilderName"] != DBNull.Value || dt.Rows[0]["BuilderName"] != "0" ? Convert.ToString(dt.Rows[0]["BuilderName"]) : "";
                        //  model.CustomerName = dt.Rows[0]["CustomerName"] != DBNull.Value || dt.Rows[0]["CustomerName"] != "0" ?Convert.ToString(dt.Rows[0]["CustomerName"]) : "";
                        // model.LocationName = dt.Rows[0]["LocationName"] != DBNull.Value || dt.Rows[0]["LocationName"] != "0" ?Convert.ToString(dt.Rows[0]["LocationName"]) : "";
                        model.ProjectName = dt.Rows[0]["ProjectName"] != DBNull.Value || dt.Rows[0]["ProjectName"] != "0" ? Convert.ToString(dt.Rows[0]["ProjectName"]) : "";
                        model.SiteName = dt.Rows[0]["SiteNameLoc"] != DBNull.Value || dt.Rows[0]["SiteNameLoc"] != "0" ? Convert.ToString(dt.Rows[0]["SiteNameLoc"]) : "";
                        model.RERANumber = dt.Rows[0]["ReraNo"] != DBNull.Value || dt.Rows[0]["ReraNo"] != "0" ? Convert.ToString(dt.Rows[0]["ReraNo"]) : "";
                        model.CustomerReference = dt.Rows[0]["ReferenceCustomer"] != DBNull.Value || dt.Rows[0]["ReferenceCustomer"] != "0" ? Convert.ToString(dt.Rows[0]["ReferenceCustomer"]) : "";
                        //   model.CustFrequency = dt.Rows[0]["CustFrequency"] != DBNull.Value || dt.Rows[0]["CustFrequency"] != "0" ?Convert.ToString(dt.Rows[0]["CustFrequency"]) : "";
                        DateTime? DateofEnquiry = null;
                        if (dt.Rows[0]["EnquiryDate"] != DBNull.Value)
                            DateofEnquiry = Convert.ToDateTime(dt.Rows[0]["EnquiryDate"]);
                        DateTime? DeliveryFrom = null;
                        if (dt.Rows[0]["DeliveryFrom"] != DBNull.Value)
                            DeliveryFrom = Convert.ToDateTime(dt.Rows[0]["DeliveryFrom"]);
                        DateTime? DeliveryTo = null;
                        if (dt.Rows[0]["DeliveryTo"] != DBNull.Value)
                            DeliveryTo = Convert.ToDateTime(dt.Rows[0]["DeliveryTo"]);
                        DateTime? LiftingDate = null;
                        if (dt.Rows[0]["FirstLiftingD"] != DBNull.Value)
                            LiftingDate = Convert.ToDateTime(dt.Rows[0]["FirstLiftingD"]);
                        DateTime? ProDisValid = null;
                        if (dt.Rows[0]["ProjectDiscountValid"] != DBNull.Value)
                            ProDisValid = Convert.ToDateTime(dt.Rows[0]["ProjectDiscountValid"]);

                        model.DateofEnquiry = DateofEnquiry;

                        model.OrderValue = dt.Rows[0]["ApproxOrderValue"] != DBNull.Value || dt.Rows[0]["ApproxOrderValue"] != "0" ? Convert.ToString(dt.Rows[0]["ApproxOrderValue"]) : "";
                        model.DateFrom = DeliveryFrom;
                        model.DateTo = DeliveryTo;
                        model.PreferredPlant = dt.Rows[0]["PreferredPlant"] != DBNull.Value || dt.Rows[0]["PreferredPlant"] != "0" ? Convert.ToString(dt.Rows[0]["PreferredPlant"]) : "";
                        model.FirstLifting = dt.Rows[0]["FirstLiftingV"] != DBNull.Value || dt.Rows[0]["FirstLiftingV"] != "0" ? Convert.ToString(dt.Rows[0]["FirstLiftingV"]) : "";
                        model.LiftingDate = LiftingDate;
                        model.POCRRequest = dt.Rows[0]["POCRRequest"] != DBNull.Value || dt.Rows[0]["POCRRequest"] != "0" ? Convert.ToString(dt.Rows[0]["POCRRequest"]) : "";
                        model.AddDisMaterial = dt.Rows[0]["AddDiscountPerMaterial"] != DBNull.Value || dt.Rows[0]["AddDiscountPerMaterial"] != "0" ? Convert.ToString(dt.Rows[0]["AddDiscountPerMaterial"]) : "";
                        model.ProDisValid = ProDisValid;
                        model.CreditReq = dt.Rows[0]["CreditRequired"] != DBNull.Value || dt.Rows[0]["CreditRequired"] != "0" ? Convert.ToString(dt.Rows[0]["CreditRequired"]) : "";
                        model.CreditPer = dt.Rows[0]["CreditPeriod"] != DBNull.Value || dt.Rows[0]["CreditPeriod"] != "0" ? Convert.ToString(dt.Rows[0]["CreditPeriod"]) : "";
                        model.InterstCost = dt.Rows[0]["InterestCost"] != DBNull.Value || dt.Rows[0]["InterestCost"] != "0" ? Convert.ToString(dt.Rows[0]["InterestCost"]) : "";
                        model.CreditLimit = dt.Rows[0]["CreditLimit"] != DBNull.Value || dt.Rows[0]["CreditLimit"] != "0" ? Convert.ToString(dt.Rows[0]["CreditLimit"]) : "";
                        model.CustOverview = dt.Rows[0]["BriefCustomer"] != DBNull.Value || dt.Rows[0]["BriefCustomer"] != "0" ? Convert.ToString(dt.Rows[0]["BriefCustomer"]) : "";
                        model.WhyProject = dt.Rows[0]["WhyProject"] != DBNull.Value || dt.Rows[0]["WhyProject"] != "0" ? Convert.ToString(dt.Rows[0]["WhyProject"]) : "";
                        model.CompName = dt.Rows[0]["CompetitorName"] != DBNull.Value || dt.Rows[0]["CompetitorName"] != "0" ? Convert.ToString(dt.Rows[0]["CompetitorName"]) : "";
                        model.Invoice = dt.Rows[0]["Quotation"] != DBNull.Value || dt.Rows[0]["Quotation"] != "0" ? Convert.ToString(dt.Rows[0]["Quotation"]) : "";
                        model.Freight = dt.Rows[0]["FrieghtValue"] != DBNull.Value || dt.Rows[0]["FrieghtValue"] != "0" ? Convert.ToString(dt.Rows[0]["FrieghtValue"]) : "";
                        model.TruckVol = dt.Rows[0]["TruckVolume"] != DBNull.Value || dt.Rows[0]["TruckVolume"] != "0" ? Convert.ToString(dt.Rows[0]["TruckVolume"]) : "";
                        model.Place = dt.Rows[0]["Place"] != DBNull.Value || dt.Rows[0]["Place"] != "0" ? Convert.ToString(dt.Rows[0]["Place"]) : "";
                        model.VehValue = dt.Rows[0]["Vehiclevalue"] != DBNull.Value || dt.Rows[0]["Vehiclevalue"] != "0" ? Convert.ToString(dt.Rows[0]["Vehiclevalue"]) : "";
                        model.ID = dt.Rows[0]["ID"] != DBNull.Value || dt.Rows[0]["ID"] != "0" ? Convert.ToInt32(dt.Rows[0]["ID"]) : 0;
                        model.FormSrId = dt.Rows[0]["FormID"] != DBNull.Value || dt.Rows[0]["FormID"] != "0" ? Convert.ToString(dt.Rows[0]["FormID"]) : "";
                        model.TSELocation = dt.Rows[0]["TSELoction"] != DBNull.Value || dt.Rows[0]["TSELoction"] != "0" ? Convert.ToString(dt.Rows[0]["TSELoction"]) : "";
                        model.ProjectBrochure = dt.Rows[0]["TSELoction"] != DBNull.Value || dt.Rows[0]["TSELoction"] != "0" ? Convert.ToString(dt.Rows[0]["TSELoction"]) : "";

                        item.Add(model);
                    }
                }
                #endregion
                model = item[0];


                if (Status != 1)
                {
                    body += GetSubmitterAndApplicantHtml(model);
                }
                #region Enail Body
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td><b>FIL Person Details</b></td></tr>";
                body = body + "<tr><td>" + "Name of TSE: " + model.TSEName + "</td></tr>";
                body = body + "<tr><td>" + "Name of ZSM/RSM: " + model.ZSMName + "</td></tr>";
                body = body + "<tr><td>" + "Name of ASM: " + model.ASMName + "</td></tr>";
                body = body + "<tr><td>" + "Location of TSE: " + model.TSELocation + "</td></tr>";
                body = body + "<tr><td>" + "State: " + model.State + "</td></tr>";
                body = body + "<tr><td>" + "POCR No: " + model.POCRNo + "</td></tr>";

                body = body + "<tr><td><b>Dealers Details</b></td></tr>";
                body = body + "<tr><td>" + "Dealer Code: " + model.DealerCode + "</td></tr>";
                body = body + "<tr><td>" + "Name of the Dealer: " + model.DealerName + "</td></tr>";
                body = body + "<tr><td>" + "Dealer Location: " + model.DealerLocation + "</td></tr>";
                body = body + "<tr><td>" + "FIL Dealership Since: " + model.FILDealership + "</td></tr>";
                body = body + "<tr><td>" + "Dealer Sales – Last Year (Rs. Lakhs): " + model.DealerSalesLastYr + "</td></tr>";
                body = body + "<tr><td>" + "Dealer Sales – Till Date (Rs. Lakhs): " + model.DealerSalesTill + "</td></tr>";
                body = body + "<tr><td>" + "Exclusive/Non-exclusive Status: " + model.Status + "</td></tr>";

                body = body + "<tr><td><b>FIL Person Details</b></td></tr>";
                body = body + "<tr><td>" + "Name of Builder/Customer & Location: " + model.BuilderName + "</td></tr>";
                body = body + "<tr><td>" + "Project Name: " + model.ProjectName + "</td></tr>";
                body = body + "<tr><td>" + "Site Name/Location: " + model.SiteName + "</td></tr>";
                body = body + "<tr><td>" + "RERA Number: " + model.RERANumber + "</td></tr>";
                body = body + "<tr><td>" + "Project Brochure: " + model.ProjectBrochure + "</td></tr>";
                body = body + "<tr><td>" + "Project Website: " + model.ProjectWebsite + "</td></tr>";
                body = body + "<tr><td>" + "Reference From/Customer: " + model.CustomerReference + "</td></tr>";
                body = body + "<tr><td>" + "Frequency of Customer: " + model.CustomerReference + "</td></tr>";
                body = body + "<tr><td>" + "Date of Enquiry: " + model.DateofEnquiry + "</td></tr>";
                body = body + "<tr><td>" + "Approx.Order Value in Rs Lakhs: " + model.OrderValue + "</td></tr>";
                body = body + "<tr><td>" + "Preferred Plant: " + model.PreferredPlant + "</td></tr>";
                body = body + "<tr><td>" + "Delivery Schedule (From): " + model.DateFrom + "</td></tr>";
                body = body + "<tr><td>" + "Delivery Schedule (To): " + model.DateTo + "</td></tr>";
                body = body + "<tr><td>" + "Preferred Plant: " + model.PreferredPlant + "</td></tr>";
                body = body + "<tr><td>" + "First Lifting- Value (Rs. in Lakhs): " + model.FirstLifting + "</td></tr>";
                body = body + "<tr><td>" + "First Lifting- Date: " + model.LiftingDate + "</td></tr>";
                body = body + "<tr><td>" + "POCR Request For: " + model.POCRRequest + "</td></tr>";

                body = body + "<tr><td><b>Material Details</b></td></tr>";
                body = body + "<tr><td>" + "%age additional discount per material group: " + model.AddDisMaterial + "</td></tr>";

                body = body + "<tr><td><b>Validity Details</b></td></tr>";
                body = body + "<tr><td>" + "Credit Required: " + model.CreditReq + "</td></tr>";
                body = body + "<tr><td>" + "Credit Period (Days): " + model.CreditPer + "</td></tr>";
                body = body + "<tr><td>" + "Interest Cost: " + model.InterstCost + "</td></tr>";
                body = body + "<tr><td>" + "Credit Limit (Rs. in Lakhs): " + model.CreditLimit + "</td></tr>";
                body = body + "<tr><td>" + "Project Discount Validity: " + model.ProDisValid + "</td></tr>";

                body = body + "<tr><td><b>Customer Brief</b></td></tr>";
                body = body + "<tr><td>" + "Brief Overview of Customer: " + model.CustOverview + "</td></tr>";
                body = body + "<tr><td>" + "Why we should go for the Project: " + model.WhyProject + "</td></tr>";

                body = body + "<tr><td><b>Customer Brief</b></td></tr>";
                body = body + "<tr><td>" + "Competitor Names 1: " + model.CompName + "</td></tr>";
                body = body + "<tr><td>" + "Competitor Names 2: " + model.CompName2 + "</td></tr>";
                body = body + "<tr><td>" + "Quotation/Invoice: " + model.Invoice + "</td></tr>";

                body = body + "<tr><td><b>Freight Details</b></td></tr>";
                body = body + "<tr><td>" + "Freight Value (From): " + model.Freight + "</td></tr>";
                body = body + "<tr><td>" + "Freight Value (To): " + model.Freight + "</td></tr>";
                body = body + "<tr><td>" + "Freight Value (Avg): " + model.Freight + "</td></tr>";
                body = body + "<tr><td>" + "Volume of Truck (in MT) (From): " + model.TruckVol + "</td></tr>";
                body = body + "<tr><td>" + "Volume of Truck (in MT) (To): " + model.TruckVol + "</td></tr>";
                body = body + "<tr><td>" + "Place (From): " + model.Place + "</td></tr>";
                body = body + "<tr><td>" + "Place (To): " + model.Place + "</td></tr>";
                body = body + "<tr><td>" + "Value of Vehicle (Rs. in Lakhs) (From): " + model.VehValue + "</td></tr>";
                body = body + "<tr><td>" + "Value of Vehicle (Rs. in Lakhs) (To): " + model.VehValue + "</td></tr>";

                #endregion
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>PO Review Sheet</b></th></tr>";


                List<POCRList> OtherList = new List<POCRList>();
                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetPOCRDDetails", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {

                    for (int j = 0; j < ds1.Rows.Count; j++)
                    {
                        POCRList model1 = new POCRList();
                        model1.ProductID = ds1.Rows[j]["ProductID"] != DBNull.Value || ds1.Rows[j]["ProductID"] != "0" ? Convert.ToInt32(ds1.Rows[j]["ProductID"]) : 0;
                        model1.Value = ds1.Rows[j]["Value"] != DBNull.Value || ds1.Rows[j]["Value"] != "0" ? Convert.ToInt32(ds1.Rows[j]["Value"]) : 0;
                        model1.MaterialDes = ds1.Rows[j]["MaterialDes"] != DBNull.Value || ds1.Rows[j]["MaterialDes"] != "0" ? Convert.ToString(ds1.Rows[j]["MaterialDes"]) : "";
                        model1.ListPrice = ds1.Rows[j]["ListPrice"] != DBNull.Value || ds1.Rows[j]["ListPrice"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ListPrice"]) : 0;
                        model1.LessStdDis = ds1.Rows[j]["LessStdDis"] != DBNull.Value || ds1.Rows[j]["LessStdDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["LessStdDis"]) : 0;
                        model1.LessPriceAftStdDis = ds1.Rows[j]["LessPriceAftStdDis"] != DBNull.Value || ds1.Rows[j]["LessPriceAftStdDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["LessPriceAftStdDis"]) : 0;
                        model1.LessAddDis = ds1.Rows[j]["LessAddDis"] != DBNull.Value || ds1.Rows[j]["LessAddDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["LessAddDis"]) : 0;
                        model1.PriceAftAddDis = ds1.Rows[j]["PriceAftAddDis"] != DBNull.Value || ds1.Rows[j]["PriceAftAddDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftAddDis"]) : 0;
                        model1.OTLSchemeDis = ds1.Rows[j]["OTLSchemeDis"] != DBNull.Value || ds1.Rows[j]["OTLSchemeDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["OTLSchemeDis"]) : 0;
                        model1.PriceAftOTLSDis = ds1.Rows[j]["PriceAftOTLSDis"] != DBNull.Value || ds1.Rows[j]["PriceAftOTLSDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftOTLSDis"]) : 0;
                        model1.TruckScheme = ds1.Rows[j]["TruckScheme"] != DBNull.Value || ds1.Rows[j]["TruckScheme"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["TruckScheme"]) : 0;
                        model1.PriceAftTruckSch = ds1.Rows[j]["PriceAftTruckSch"] != DBNull.Value || ds1.Rows[j]["PriceAftTruckSch"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftTruckSch"]) : 0;
                        model1.DealerSpecific = ds1.Rows[j]["DealerSpecific"] != DBNull.Value || ds1.Rows[j]["DealerSpecific"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DealerSpecific"]) : 0;
                        model1.PriceDealerSpecific = ds1.Rows[j]["PriceDealerSpecific"] != DBNull.Value || ds1.Rows[j]["PriceDealerSpecific"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceDealerSpecific"]) : 0;
                        model1.GST = ds1.Rows[j]["GST"] != DBNull.Value || ds1.Rows[j]["GST"] != "0" ? Convert.ToInt32(ds1.Rows[j]["GST"]) : 0;
                        model1.GSTPrice = ds1.Rows[j]["GSTPrice"] != DBNull.Value || ds1.Rows[j]["GSTPrice"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["GSTPrice"]) : 0;
                        model1.PrimaryFreight = ds1.Rows[j]["PrimaryFreight"] != DBNull.Value || ds1.Rows[j]["PrimaryFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PrimaryFreight"]) : 0;
                        model1.PricePriFreight = ds1.Rows[j]["PricePriFreight"] != DBNull.Value || ds1.Rows[j]["PricePriFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PricePriFreight"]) : 0;
                        model1.SecondaryFreight = ds1.Rows[j]["SecondaryFreight"] != DBNull.Value || ds1.Rows[j]["SecondaryFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["SecondaryFreight"]) : 0;
                        model1.PriceSecFreight = ds1.Rows[j]["PriceSecFreight"] != DBNull.Value || ds1.Rows[j]["PriceSecFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceSecFreight"]) : 0;
                        model1.COIForCredit = ds1.Rows[j]["COIForCredit"] != DBNull.Value || ds1.Rows[j]["COIForCredit"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["COIForCredit"]) : 0;
                        model1.PriceCOIForCredit = ds1.Rows[j]["PriceCOIForCredit"] != DBNull.Value || ds1.Rows[j]["PriceCOIForCredit"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceCOIForCredit"]) : 0;
                        model1.DealerLanding = ds1.Rows[j]["DealerLanding"] != DBNull.Value || ds1.Rows[j]["DealerLanding"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DealerLanding"]) : 0;
                        model1.DealerMargin = ds1.Rows[j]["DealerMargin"] != DBNull.Value || ds1.Rows[j]["DealerMargin"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DealerMargin"]) : 0;
                        model1.BillRateBuild = ds1.Rows[j]["BillRateBuild"] != DBNull.Value || ds1.Rows[j]["BillRateBuild"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["BillRateBuild"]) : 0;
                        model1.DiffBtnBRBAndDL = ds1.Rows[j]["DiffBtnBRBAndDL"] != DBNull.Value || ds1.Rows[j]["DiffBtnBRBAndDL"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DiffBtnBRBAndDL"]) : 0;
                        model1.ProjectDis = ds1.Rows[j]["ProjectDis"] != DBNull.Value || ds1.Rows[j]["ProjectDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ProjectDis"]) : 0;
                        model1.ActDiscount = ds1.Rows[j]["ActDiscount"] != DBNull.Value || ds1.Rows[j]["ActDiscount"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ActDiscount"]) : 0;
                        model1.ActMargin = ds1.Rows[j]["ActMargin"] != DBNull.Value || ds1.Rows[j]["ActMargin"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ActMargin"]) : 0;
                        model1.PriceAftQuotDis = ds1.Rows[j]["PriceAftQuotDis"] != DBNull.Value || ds1.Rows[j]["PriceAftQuotDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftQuotDis"]) : 0;
                        model1.PriceAftSchDis = ds1.Rows[j]["PriceAftSchDis"] != DBNull.Value || ds1.Rows[j]["PriceAftSchDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftSchDis"]) : 0;
                        model1.PriceAftDealerBillAnnDis = ds1.Rows[j]["PriceAftDealerBillAnnDis"] != DBNull.Value || ds1.Rows[j]["PriceAftDealerBillAnnDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftDealerBillAnnDis"]) : 0;
                        model1.ExeGSTPrice = ds1.Rows[j]["ExeGSTPrice"] != DBNull.Value || ds1.Rows[j]["ExeGSTPrice"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeGSTPrice"]) : 0;
                        model1.ExePricePriFreight = ds1.Rows[j]["ExePricePriFreight"] != DBNull.Value || ds1.Rows[j]["ExePricePriFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExePricePriFreight"]) : 0;
                        model1.ExePriceSecFreight = ds1.Rows[j]["ExePriceSecFreight"] != DBNull.Value || ds1.Rows[j]["ExePriceSecFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExePriceSecFreight"]) : 0;
                        model1.ExePriceCOIForCredit = ds1.Rows[j]["ExePriceCOIForCredit"] != DBNull.Value || ds1.Rows[j]["ExePriceCOIForCredit"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExePriceCOIForCredit"]) : 0;
                        model1.ExeDealerLanding = ds1.Rows[j]["ExeDealerLanding"] != DBNull.Value || ds1.Rows[j]["ExeDealerLanding"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeDealerLanding"]) : 0;
                        model1.ExeBillRateBuild = ds1.Rows[j]["ExeBillRateBuild"] != DBNull.Value || ds1.Rows[j]["ExeBillRateBuild"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeBillRateBuild"]) : 0;
                        model1.BillRateBuildInclDealMargin = ds1.Rows[j]["BillRateBuildInclDealMargin"] != DBNull.Value || ds1.Rows[j]["BillRateBuildInclDealMargin"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["BillRateBuildInclDealMargin"]) : 0;
                        model1.CompetitorLP = ds1.Rows[j]["CompetitorLP"] != DBNull.Value || ds1.Rows[j]["CompetitorLP"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["CompetitorLP"]) : 0;
                        model1.CompetitorQuote = ds1.Rows[j]["CompetitorQuote"] != DBNull.Value || ds1.Rows[j]["CompetitorQuote"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["CompetitorQuote"]) : 0;
                        model1.ExeDiffBtnBRBAndDL = ds1.Rows[j]["ExeDiffBtnBRBAndDL"] != DBNull.Value || ds1.Rows[j]["ExeDiffBtnBRBAndDL"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeDiffBtnBRBAndDL"]) : 0;
                        model1.DiffBtnBRBFVC = ds1.Rows[j]["DiffBtnBRBFVC"] != DBNull.Value || ds1.Rows[j]["DiffBtnBRBFVC"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DiffBtnBRBFVC"]) : 0;
                        model1.DiffWOPOCR = ds1.Rows[j]["DiffWOPOCR"] != DBNull.Value || ds1.Rows[j]["DiffWOPOCR"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DiffWOPOCR"]) : 0;
                        model1.WeightAvgWOPOCR = ds1.Rows[j]["WeightAvgWOPOCR"] != DBNull.Value || ds1.Rows[j]["WeightAvgWOPOCR"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["WeightAvgWOPOCR"]) : 0;
                        model1.WeightAvgWPOCR = ds1.Rows[j]["WeightAvgWPOCR"] != DBNull.Value || ds1.Rows[j]["WeightAvgWPOCR"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["WeightAvgWPOCR"]) : 0;
                        model1.SrNo = ds1.Rows[j]["SrNo"] != DBNull.Value || ds1.Rows[j]["SrNo"] != "0" ? Convert.ToString(ds1.Rows[j]["SrNo"]) : "";
                        model1.ProductName = ds1.Rows[j]["ProductName"] != DBNull.Value || ds1.Rows[j]["ProductName"] != "0" ? Convert.ToString(ds1.Rows[j]["ProductName"]) : "";

                        OtherList.Add(model1);

                    }

                }
                int k = 0;
                for (int i = 0; i < OtherList.Count; i++)
                {
                    k++;
                    body = body + "<tr><td>" + "Sr. No. : " + k + "</td></tr>";
                    body = body + "<tr><td>" + "Product : " + OtherList[i].ProductName + "</td></tr>";
                    body = body + "<tr><td>" + "Value : " + OtherList[i].Value + "</td></tr>";
                    body = body + "<tr><td>" + "Description  : " + OtherList[i].MaterialDes + "</td></tr>";
                    body = body + "<tr><td>" + "List Price  : " + OtherList[i].LessStdDis + "</td></tr>";
                    body = body + "<tr><td>" + "Less Standards Discount  : " + OtherList[i].LessStdDis + "</td></tr>";
                    body = body + "<tr><td>" + "Less Price After Standard Discount  : " + OtherList[i].LessPriceAftStdDis + "</td></tr>";
                    body = body + "<tr><td>" + "Less Additional Discount  : " + OtherList[i].LessAddDis + "</td></tr>";
                    body = body + "<tr><td>" + "Price after additional discount  : " + OtherList[i].PriceAftAddDis + "</td></tr>";
                    body = body + "<tr><td>" + "In Bill OTL scheme discount	  : " + OtherList[i].OTLSchemeDis + "</td></tr>";
                    body = body + "<tr><td>" + "Price after in-bill OTL discount  : " + OtherList[i].PriceAftOTLSDis + "</td></tr>";
                    body = body + "<tr><td>" + "In bill scheme/Truck scheme (slab wise)  : " + OtherList[i].TruckScheme + "</td></tr>";
                    body = body + "<tr><td>" + "Price after truck scheme (in bill slab wise)  : " + OtherList[i].PriceAftTruckSch + "</td></tr>";
                    body = body + "<tr><td>" + "Dealer specific in bill annual  : " + OtherList[i].DealerSpecific + "</td></tr>";
                    body = body + "<tr><td>" + "Price after dealer specific in bill annual  : " + OtherList[i].PriceDealerSpecific + "</td></tr>";
                    body = body + "<tr><td>" + "Add GST  : " + OtherList[i].GST + "</td></tr>";
                    body = body + "<tr><td>" + "Actual GST price  : " + OtherList[i].GSTPrice + "</td></tr>";
                    body = body + "<tr><td>" + "Add primary proportionate freight  : " + OtherList[i].PrimaryFreight + "</td></tr>";
                    body = body + "<tr><td>" + "Price after proportionate primary freight  : " + OtherList[i].PricePriFreight + "</td></tr>";
                    body = body + "<tr><td>" + "Add secondary freight  : " + OtherList[i].SecondaryFreight + "</td></tr>";
                    body = body + "<tr><td>" + "Price after adding secondary freight  : " + OtherList[i].PriceSecFreight + "</td></tr>";
                    body = body + "<tr><td>" + "Add the cost of interest %age for credit if any  : " + OtherList[i].COIForCredit + "</td></tr>";
                    body = body + "<tr><td>" + "Price after the cost of interest %age for credit  : " + OtherList[i].PriceCOIForCredit + "</td></tr>";
                    body = body + "<tr><td>" + "Dealer landing (without project discount)  : " + OtherList[i].DealerLanding + "</td></tr>";
                    body = body + "<tr><td>" + "%age Dealer margin  : " + OtherList[i].DealerMargin + "</td></tr>";
                    body = body + "<tr><td>" + "Bill rate to the builder (without project discount)  : " + OtherList[i].BillRateBuild + "</td></tr>";
                    body = body + "<tr><td>" + "Diff between billing rate to the builder & dealer landing (without project discount)  : " + OtherList[i].DiffBtnBRBAndDL + "</td></tr>";
                    body = body + "<tr><td>" + "Overall project discount  : " + OtherList[i].ProjectDis + "</td></tr>";
                    body = body + "<tr><td>" + "Actual discount (hierarchy-wise)  : " + OtherList[i].ActDiscount + "</td></tr>";
                    body = body + "<tr><td>" + "Actual Margin (POCR discount request)  : " + OtherList[i].ActMargin + "</td></tr>";
                    body = body + "<tr><td>" + "Price after quotation discount  : " + OtherList[i].PriceAftQuotDis + "</td></tr>";
                    body = body + "<tr><td>" + "Price after schemes or additional extra discount  : " + OtherList[i].PriceAftSchDis + "</td></tr>";
                    body = body + "<tr><td>" + "Price after dealer specific in bill annual discount  : " + OtherList[i].PriceAftDealerBillAnnDis + "</td></tr>";
                    body = body + "<tr><td>" + "Price after project discount  : " + OtherList[i].PriceAftProjDis + "</td></tr>";
                    body = body + "<tr><td>" + "Actual GST price  : " + OtherList[i].ExeGSTPrice + "</td></tr>";
                    body = body + "<tr><td>" + "Price after proportionate primary freight  : " + OtherList[i].ExePricePriFreight + "</td></tr>";
                    body = body + "<tr><td>" + "Price after adding secondary freight  : " + OtherList[i].ExePriceSecFreight + "</td></tr>";
                    body = body + "<tr><td>" + "Price after the cost of interest %age for credit  : " + OtherList[i].ExePriceCOIForCredit + "</td></tr>";
                    body = body + "<tr><td>" + "Dealer landing (without project discount)  : " + OtherList[i].ExeDealerLanding + "</td></tr>";
                    body = body + "<tr><td>" + "Billing rate to the builder without project discount  : " + OtherList[i].BillRateWOProjDis + "</td></tr>";
                    body = body + "<tr><td>" + "Billing rate to the builder including dealer margin (with project discount)  : " + OtherList[i].BillRateBuildInclDealMargin + "</td></tr>";
                    body = body + "<tr><td>" + "Competitor LP  : " + OtherList[i].CompetitorLP + "</td></tr>";
                    body = body + "<tr><td>" + "Competitor quote (including GST)  : " + OtherList[i].CompetitorQuote + "</td></tr>";
                    body = body + "<tr><td>" + "Diff between billing rate to the builder & dealer landing (with project discount)  : " + OtherList[i].ExeDiffBtnBRBAndDL + "</td></tr>";
                    body = body + "<tr><td>" + "Diff between billing rate to the builder (FIL vs Competitor)  : " + OtherList[i].DiffBtnBRBFVC + "</td></tr>";
                    body = body + "<tr><td>" + "Diff without POCR  : " + OtherList[i].DiffWOPOCR + "</td></tr>";
                    body = body + "<tr><td>" + "Weighted avg. group wise without POCR  : " + OtherList[i].WeightAvgWOPOCR + "</td></tr>";
                    body = body + "<tr><td>" + "Weighted avg. group wise with POCR  : " + OtherList[i].WeightAvgWPOCR + "</td></tr>";
                    body = body + "<tr><td><br></td></tr>";
                    body = body + "<tr><td>***************************************************************************</td></tr>";


                }
                if (Status != 1)
                {
                    //approvers
                    body = body + "<br><br> <table width=\"100%\">";
                    body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                    foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                    {
                        body = body + "<tr><td>" + "Approved By: " + approver.UserName + "</td></tr>";
                        body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                        body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                    }

                    body = body + "</table><br>";

                    body += "<img src=cid:LogoImage alt=\"\"></img>";
                }
                ApprovalDataModel nileshid = new ApprovalDataModel();
                nileshid.Level = 0;
                nileshid.EmailId = "";
                //nileshid.EmailId = "giridhar.patil@skoda-vw.co.in";
                //nileshid.EmailId = "prashant.k@mobinexttech.com";
                approverList.Add(nileshid);

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }
        public List<string> GetFinalEmailReceipient(string location, string uniqueFormName)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<string> contactList = new List<string>();
                int locationId = (location.ToLower().Contains("pune") ? 1 : (location.ToLower().Contains("aurangabad") ? 3 : 2));
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetFinalEmailReceipientByLocations", con);
                cmd.Parameters.Add(new SqlParameter("@locationId", locationId));
                cmd.Parameters.Add(new SqlParameter("@uniqueFormName", uniqueFormName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        var contact = Convert.ToString(ds.Tables[0].Rows[i]["Email"]);
                        contactList.Add(contact);
                    }
                }
                return contactList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<string>();
            }
        }

        public async Task<EmailDataModel> GetEQSABody(int formId, UserData currentUser, int Status = 0)
        {
            string body = "";
            try
            {
                string employeeLocation = "";
                var returnModel = new EmailDataModel();
                string FormName = "EQSA Form";
                var rowId = await GetRowID_SQL(formId, currentUser);
                var approverList = await GetApprovalDetails_SQL(rowId, formId, currentUser);

                List<EQSAccessmModelData> item = new List<EQSAccessmModelData>();
                List<EQSAccessmModelData> EQSATableDataList = new List<EQSAccessmModelData>();
                EQSAccessmModelData model = new EQSAccessmModelData();
                GlobalClass gc = new GlobalClass();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var responseData = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('EQSAccess')/items?$select=*"
                + "&$filter=(ID eq '" + rowId + "' )")).Result;


                var responseTextData = await responseData.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseTextData))
                {

                    var SUCFUserResult = JsonConvert.DeserializeObject<EQSAccessmModel>(responseTextData, settings);
                    item = SUCFUserResult.EQSAccessmModelResults.EQSAccessmModelData;
                    model = item[0];


                    var handler1 = new HttpClientHandler();
                    handler1.Credentials = new NetworkCredential($"{currentUser.DomainName}\\{currentUser.UserName}", currentUser.Password);
                    var client1 = new HttpClient(handler1);
                    client1.BaseAddress = new Uri(conString);
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    var response2 = Task.Run(() => client1.GetAsync("_api/web/lists/GetByTitle('EQSAccessTableData')/items?$select=*" +
                 "&$filter=(FormId eq '" + rowId + "' )")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {

                        var ListResult = JsonConvert.DeserializeObject<EQSAccessmModel>(responseText2, settings);
                        EQSATableDataList = ListResult.EQSAccessmModelResults.EQSAccessmModelData;
                    }

                }

                body += GetSubmitterAndApplicantHtml(model);

                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
                body = body + "<tr><td>" + "Request Id: " + formId + "</td></tr>";
                body = body + "<tr><td>" + "Request Description: " + FormName + "</td></tr>";
                body = body + "<tr><td>" + "Request Type: " + model.RequestType + "</td></tr>";
                body = body + "<tr><td>" + "Business Justification: " + model.BusinessJustification + "</td></tr>";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Application Category</b></th></tr>";

                body = body + "<table width=\"100%\">  <thead>\r\n <tr><th style= \"text-align: center;\">Employee Name</th><th style=\"text-align: center;\">Employee ID</th><th style=\" text-align: center;\">Logic Card ID</th><th style=\" text-align: center;\">Station Name</th><th style=\" text-align: center;\">Shop</th><th style=\" text-align: center;\">Access Group</th>\r\n</tr></thead>\r\n<tbody class=\"form-border\">";
                for (int i = 0; i < EQSATableDataList.Count; i++)
                {
                    body = body + "<tr><td style=\" text-align: center;\">" + EQSATableDataList[i].EmployeeName + "</td>";
                    body = body + "<td style=\"text-align: center;\">" + EQSATableDataList[i].EmployeeID + "</td>";
                    body = body + "<td style=\"text-align: center;\">" + EQSATableDataList[i].LogicCardID + "</td>";
                    body = body + "<td style=\"text-align: center;\">" + EQSATableDataList[i].StationName + "</td>";
                    body = body + "<td style=\"text-align: center;\">" + EQSATableDataList[i].Shop + "</td>";
                    body = body + "<td style=\"text-align: center;\">" + EQSATableDataList[i].AccessGroup + "</td></tr>";

                }

                body = body + "</tbody> </table>";
                //approvers
                body = body + "<br><br> <table width=\"100%\">";
                body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Approver Details</b></th></tr>";
                foreach (var approver in approverList.Where(x => x.ApproverStatus != "Pending"))
                {
                    body = body + "<tr><td>" + "Approved By: " + currentUser.UserName + "</td></tr>";
                    body = body + "<tr><td>" + "Approved On: " + approver.Modified.ToShortDateString() + " " + Convert.ToDateTime(approver.Modified).AddHours(5.5).ToShortTimeString() + "</td></tr>";
                    body = body + "<tr><td>" + "Comments: " + approver.Comment + "</td></tr>";
                }

                body = body + "</table><br>";

                body += "<img src=cid:LogoImage alt=\"\"></img>";

                //return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Designation == "Head of Department (Pilot Hall)" ).Select(y => y.EmailId).ToList() };
                return new EmailDataModel() { Body = body, Location = employeeLocation, ToIds = approverList.Where(x => x.Level == 1 || x.Level == 0).Select(y => y.EmailId).ToList() };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new EmailDataModel() { };
            }
        }
    }
}

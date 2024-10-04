using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.WebControls;
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
using DataTable = System.Data.DataTable;

namespace Skoda_DCMS.DAL
{
    public class IMACDAL : CommonDAL
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


        public async Task<dynamic> GetIMACDetails(int rowId, int formId)
        {
            dynamic IMACData = new ExpandoObject();
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
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('IMACForm')/items?$select=*" +
                    "&$filter=(ID eq '" + rowId + "')");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                { 
                    var SUCESSResult = JsonConvert.DeserializeObject<IMACModel>(responseText1, settings);
                    IMACData.one = SUCESSResult.IMACResults.IMACFormModel;

                    var handler2 = new HttpClientHandler();
                    handler2.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var client2 = new HttpClient(handler2);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('IMACDataList')/items?$select=*" +
                        "&$filter=(FormID eq '" + formId + "' and RowId eq '"+rowId+"')")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {

                        var ListResult = JsonConvert.DeserializeObject<IMACModel>(responseText2, settings);
                        IMACData.Four = ListResult.IMACResults.IMACFormModel;
                    }
                }

                var (r1, r2) = await GetApproversData(user, rowId, formId);

                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                IMACData.two = r1.Model;
                IMACData.three = r2.Model;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return IMACData;
        }
        public async Task<ResponseModel<object>> SaveData_Backup(IMACFormModel data, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            try
            {
                ClientContext _context = new ClientContext(new Uri(conString));
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                Web web = _context.Web;
                int rowId = 0; //User to store newly inserted data id
                string formShortName = "IMAC";
                string formName = "IMAC Form";
                string listName = GlobalClass.ListNames.ContainsKey("IMAC") ? GlobalClass.ListNames["IMAC"] : "";
                if (listName == "")
                {
                    result.Status = 500;
                    result.Message = "List not found.";
                    return result;
                }

                int prevItemId = Convert.ToInt32(data.FormSrId);
                DateTime tempDate = new DateTime(1500, 1, 1);
                int formId = 0;
                formId = Convert.ToInt32(data.FormId);
                bool IsResubmit = formId == 0 ? false : true;
                int AppRowId = Convert.ToInt32(data.AppRowId);
                var requestSubmissionFor = data.RequestSubmissionFor;
                var otherEmpType = data.OnBehalfOption ?? "";
                bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                //int formId = Convert.ToInt32(data.FormId);
                //bool IsResubmit = formId == 0 ? false : true;
                //int AppRowId = Convert.ToInt32(data.AppRowId);
                //var requestSubmissionFor = data.RequestSubmissionFor;
                //var otherEmpType = data.OnBehalfOption ?? "";
                //bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                //long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(data.OtherEmployeeCode) : Convert.ToInt64(data.OtherNewEmployeeCode));
                //string empName = isSelf ? user.EmployeeName : (isSAVWIPL ? Convert.ToString(data.OtherEmployeeName) : Convert.ToString(data.OtherNewEmployeeName));
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
                //long T_empNum = Convert.ToInt64(data.EmployeeCode);
                //string T_empName = Convert.ToString(data.T_EmployeeName);
                //var response = await GetDLICApprovers(empNum, T_empNum, empLoc);
                //if (response.Status != 200 && response.Model.Count < 2)
                //{
                //    result.Status = 500;
                //    result.Message = "Approver "
                //        + (response.Model.FirstOrDefault(x => x.EmpNumber == empNum) != null ? T_empName : empName)
                //        + " data not found."; //response.Message;//(response.Model.Any(x => x.EmpNumber == empNum) ? T_empName : empName) + 
                //    return result;
                //}
                //else if (response.Status != 200)
                //{
                //    result.Status = 500;
                //    result.Message = response.Message;
                //    return result;
                //}
                long ccNum = user.CostCenter;
                long empNum = user.EmpNumber;
                // string empDes = data.EmployeeDesignation;
                // long emploc = user.EmployeeLocation;

                var response = await GetIMACApprovers(empNum, ccNum, empLoc);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
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
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 42;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "IMACForm";
                    //item["BusinessNeed"] = data.BusinessNeed ?? "";
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
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 42;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "IMACForm";
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
                // var newRow = userDetailsResponse.Model;

                List List = web.Lists.GetByTitle("IMACForm");
                List.Update();
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newRow = List.AddItem(itemCreateInfo);
                newRow["FormID"] = formId;
                //newRow["EmployeeName"] = data.EmployeeName;
                //newRow["EmployeeUserId"] = data.EmployeeUserId;
                //newRow["EmployeeContactNo"] = data.EmployeeContactNo;
                //newRow["EmployeeDesignation"] = data.EmployeeDesignation;
                //newRow["EmployeeCCCode"] = data.EmployeeCCCode;
                //newRow["EmployeeCode"] = data.EmployeeCode;
                //newRow["EmployeeLocation"] = data.EmployeeLocation;
                //newRow["EmployeeDepartment"] = data.EmployeeDepartment;
                //newRow["TypeofForm"] = data.TypeofForm;
            

                newRow["IMACtype"] = data.IMACtype;
                //newRow["IsOthers"] = data.IsOthers;
                //newRow["OthersText"] = data.OthersText;
                //newRow["SerialNumber"] = data.SerialNumber;
                //newRow["HostName"] = data.HostName;
                //newRow["Quantity"] = data.Quantity;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                int RowId = newRow.Id;
                result.Status = 200;
                result.Message = formId.ToString();
                int SrNo = 1;
                foreach (var item in data.IMACFormDataList)
                {
                    List List1 = web.Lists.GetByTitle("IMACDataList");
                    List1.Update();
                    ListItemCreationInformation itemCreateInfo1 = new ListItemCreationInformation();
                    ListItem newItem = List1.AddItem(itemCreateInfo1);
                    newItem["SrNo"] = SrNo++;
                    newItem["AssetName"] = item.AssetName;
                    newItem["SubAssetName"] = item.SubAssetName;
                    newItem["Make"] = item.Make;
                    newItem["Model"] = item.Modal;
                    newItem["AssetType"] = item.AssetType;
                    newItem["Remarks"] = item.Remarks;
                    newItem["Acknowledgement"] = item.Acknowledgement;
                    newItem["AssignType"] = item.AssignType;
                    //if(item.FromDate.ToString()== "01-01-0001 00:00:00")
                    //{
                    //    newItem["FromDate"] = null;
                    //}
                    //else
                    //{
                    //    newItem["FromDate"] = null;
                    //}
                    //newItem["ToDate"] = item.ToDate.ToString();
                    //newItem["FromDate"] = item.FromDate.ToString(); 
              newItem["ToDate"] = item.ToDate.ToString() == "01-01-0001 00:00:00" ? null : item.ToDate.ToString("yyyy-MM-ddThh:mm:ss");
          newItem["FromDate"] = item.FromDate.ToString() == "01-01-0001 00:00:00" ? null : item.FromDate.ToString("yyyy-MM-ddThh:mm:ss");
                    newItem["FormID"] = formId;
                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();
                }



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


        public async Task<ResponseModel<object>> SaveData(IMACFormModel model, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "IMAC";
            //string formName = "AnalysisPartsFormPresentation";
            string formName = "IMACForm";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }
            int prevItemId = Convert.ToInt32(model.FormSrId);
            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            formId = Convert.ToInt32(model.FormId);
            bool IsResubmit = formId == 0 ? false : true;
            int AppRowId = Convert.ToInt32(model.AppRowId);


            var requestSubmissionFor = model.RequestSubmissionFor;
            var otherEmpType = model.OnBehalfOption ?? "";
            bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
            long ccNum = user.CostCenter;
            long empNum = user.EmpNumber;
            string empDes = model.EmployeeDesignation;
            var empLocName = isSelf ? Convert.ToString(model.EmployeeLocation) : (isSAVWIPL ? Convert.ToString(model.OtherEmployeeLocation) : Convert.ToString(model.OtherNewEmpLocation));
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
            var response = await GetIMACApprovers(empNum, ccNum, empLoc);
            if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
            {
                result.Status = 500;
                result.Message = response.Message;
                return result;
            }

            var approvers = response.Model;

            SqlCommand cmd_form = new SqlCommand();
            SqlDataAdapter adapter_form = new SqlDataAdapter();
            DataSet ds_form = new DataSet();
            try
            {
                if (formId == 0)
                {
                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                    cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                    cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterId", DBNull.Value));
                    if (formId == 0)
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Submitted"));
                    }
                    else
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Resubmitted"));
                    }
                    cmd_form.Parameters.Add(new SqlParameter("@UniqueFormName", formShortName));
                    if (requestSubmissionFor == "Self")
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Location", model.EmployeeLocation));
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", model.OtherEmployeeLocation));
                        }
                        else
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", model.OtherNewEmpLocation));
                        }

                    }

                    cmd_form.Parameters.Add(new SqlParameter("@Modified", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@TimeStamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@Department", user.Department));
                    cmd_form.Parameters.Add(new SqlParameter("@DataRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "IMACForm"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 42));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con_form.Open();
                    adapter_form.Fill(ds_form);
                    con_form.Close();

                    if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                    {
                        for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                        {
                            formId = Convert.ToInt32(ds_form.Tables[0].Rows[i]["FormID"]);
                        }
                    }
                }
                else
                {
                    ListDAL dal = new ListDAL();
                    //var resubmitResult = await dal.ResubmitUpdate(formId);

                    DataTable dt = new DataTable();
                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_updateFlagInApprovalMaster", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formId", formId));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", AppRowId));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con_form.Open();
                    adapter_form.Fill(dt);
                    con_form.Close();

                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            new ResponseModel<object> { Message = Convert.ToString(dt.Rows[i]["message"]), Status = Convert.ToInt32(dt.Rows[i]["Status"]) };
                        }
                    }


                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(web, model, listName, formId);
                if (userDetailsResponse.Status != 200)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                RowId = Convert.ToInt32(userDetailsResponse.RowId);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_UpdateIMACForm", con);
                cmd.Parameters.Add(new SqlParameter("@IMACtype", model.IMACtype));
                cmd.Parameters.Add(new SqlParameter("@BusinessJustification", model.BusinessJustification));
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId", userDetailsResponse.RowId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();

                int SrNo = 1;
                foreach (var item in model.IMACFormDataList)
                {
                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_SaveIMACDataList", con);
                    cmd.Parameters.Add(new SqlParameter("@AssetName", item.AssetName));
                    cmd.Parameters.Add(new SqlParameter("@SubAssetName", item.SubAssetName));
                    cmd.Parameters.Add(new SqlParameter("@Make", item.Make));
                    cmd.Parameters.Add(new SqlParameter("@AssetType", item.AssetType));
                    cmd.Parameters.Add(new SqlParameter("@Remarks", ""));
                    cmd.Parameters.Add(new SqlParameter("@Acknowledgement", item.Acknowledgement));
                    cmd.Parameters.Add(new SqlParameter("@AssignType", item.AssignType));
                    // == "0001-01-01 00:00:00" ? DBNull.Value : item.FromDate.ToString("yyyy-MM-dd HH:mm:ss")
                    if (item.FromDate.ToString("yyyy-MM-dd HH:mm:ss") == "0001-01-01 00:00:00")
                    {
                        cmd.Parameters.Add(new SqlParameter("@FromDate", DBNull.Value));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@FromDate", item.FromDate.ToString("yyyy-MM-dd HH:mm:ss")));
                    }
                    if (item.ToDate.ToString("yyyy-MM-dd HH:mm:ss") == "0001-01-01 00:00:00")
                    {
                        cmd.Parameters.Add(new SqlParameter("@ToDate", DBNull.Value));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@ToDate", item.ToDate.ToString("yyyy-MM-dd HH:mm:ss")));
                    }

                    cmd.Parameters.Add(new SqlParameter("@SrNo", SrNo++));
                    cmd.Parameters.Add(new SqlParameter("@Model", item.Modal));
                    cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd.Parameters.Add(new SqlParameter("@RowId", RowId));
                    cmd.Parameters.Add(new SqlParameter("@SerialNo", item.SerialNo));
                    cmd.Parameters.Add(new SqlParameter("@HostName", item.HostName));
                    cmd.Parameters.Add(new SqlParameter("@Location", item.Location));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(ds);
                    con.Close();

                    if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            result.Status = 200;
                            result.Message = formId.ToString();
                        }
                    }
                }

                var approverIdList = response.Model;

                List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                for (var i = 0; i < approverIdList.Count; i++)
                {
                    SqlCommand cmd_Approver = new SqlCommand();
                    SqlDataAdapter adapter_App = new SqlDataAdapter();
                    con = new SqlConnection(sqlConString);
                    cmd_Approver = new SqlCommand("USP_SaveApproverDetails", con);
                    cmd_Approver.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@RowId", RowId));
                    if (approverIdList[i].ApprovalLevel == 1)
                    {
                        cmd_Approver.Parameters.Add(new SqlParameter("@IsActive", 1));
                    }
                    else
                    {
                        cmd_Approver.Parameters.Add(new SqlParameter("@IsActive", 0));
                    }

                    cmd_Approver.Parameters.Add(new SqlParameter("@NextAppId", DBNull.Value));

                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverStatus", "Approved"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Department", ""));
                    cmd_Approver.Parameters.Add(new SqlParameter("@FormParentId", 42));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ControllerName", "IMAC"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@CreatedBy", approverIdList[i].FName));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Email", approverIdList[i].EmailId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@BusinessNeed", model.BusinessNeed ?? ""));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Level", approverIdList[i].ApprovalLevel));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Logic", approverIdList[i].Logic));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Designation", approverIdList[i].Designation));
                    cmd_Approver.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", RowId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverUserName", approverIdList[i].FName));

                    cmd_Approver.CommandType = CommandType.StoredProcedure;
                    adapter_App.SelectCommand = cmd_Approver;
                    con.Open();
                    adapter_App.Fill(ds);
                    con.Close();
                }

                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, RowId);
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

                //status Approved
                List formslist = _context.Web.Lists.GetByTitle("Forms");
                ListItem newItemApprove = formslist.GetItemById(formId);
                newItemApprove.RefreshLoad();
                _context.ExecuteQuery();
                newItemApprove["Status"] = "Approved";
                newItemApprove.Update();
                _context.Load(newItemApprove);
                _context.ExecuteQuery();

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    //Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    Action = FormStates.FinalApproval,
                    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
                    FormName = formName,
                    CurrentUser = user
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

        public async Task<int> UpdateData(IMACFormModel model, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("IMAC") ? GlobalClass.ListNames["IMAC"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {
                int RowId = 0;

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                //newItem["BidderApprovalDate"] = model.BidderApprovalDate;
                //newItem["RFQReceiptDate"] = model.RFQReceiptDate;
                //newItem["RFQSentDate"] = model.RFQSentDate;
                //newItem["OfferReceiptDate"] = model.OfferReceiptDate;
                //newItem["SFODate"] = model.SFODate;
                //newItem["BestBidOffer"] = model.BestBidOffer;
                //newItem["OrderVolume"] = model.OrderVolume;
                //newItem["TransactionVolume"] = model.TransactionVolume;
                //newItem["DiffBudgetAmount"] = model.DiffBudgetAmount;
                //newItem["TargetClouseDate"] = model.TargetClouseDate;
                //newItem["Status2"] = model.Status2;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
                RowId = newItem.Id;



            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

        private async Task<ResponseModel<List<ApprovalMatrix>>> GetIMACApprovers(long empNum, long filledForEmpNum, long empLoc)
        {
            List<ApprovalMatrix> list = new List<ApprovalMatrix>();
            try
            {
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                SqlConnection con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetIMACApprovers", con);
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
                //if (data.Tables[0].Rows.Count < 2)
                //{
                //    return new ResponseModel<List<ApprovalMatrix>>
                //    {
                //        Model = appList,
                //        Status = 500,
                //        Message = $"Employee Number {(appList.Any(x => x.EmpNumber == empNum) ? filledForEmpNum : empNum)} approver data not found."
                //    };
                //}
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }

        public async Task<dynamic> GetAssets()
        {
            dynamic result = new IMACFormModel();
            try
            {

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<IMACFormModel> IMACModelList = new List<IMACFormModel>();

                SqlConnection con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetAssetCategory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        IMACFormModel IMACData = new IMACFormModel();
                        IMACData.ID = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        IMACData.AssetName = Convert.ToString(ds.Tables[0].Rows[i]["AssetName"]);
                        IMACData.IsActive = Convert.ToString(ds.Tables[0].Rows[i]["IsActive"]);
                        //if (!Convert.IsDBNull(ds.Tables[0].Rows[i]["CostCenter"]))
                        //    PPFData.CostCenterNumberByZone = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        //PPFData.IsActive = Convert.ToString(ds.Tables[0].Rows[i]["IsActive"]);
                        IMACModelList.Add(IMACData);
                    }
                }

                result = IMACModelList.ToList();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }


        public async Task<dynamic> GetSubAssets(string AssetName)
        {
            dynamic result = new IMACFormModel();
            try
            {
                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<IMACFormModel> IMACModelList = new List<IMACFormModel>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SubAssetCategory1", con);
                //cmd.Parameters.Add(new SqlParameter("@AssetID", AssetID));
                cmd.Parameters.Add(new SqlParameter("@AssetName", AssetName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        IMACFormModel IMACData = new IMACFormModel();
                        IMACData.ID = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        IMACData.SubAssetName = Convert.ToString(ds.Tables[0].Rows[i]["SubAssetName"]);
                        IMACData.AssetID = Convert.ToString(ds.Tables[0].Rows[i]["AssetID"]);
                        IMACData.IsActive = Convert.ToString(ds.Tables[0].Rows[i]["IsActive"]);

                        //if (!Convert.IsDBNull(ds.Tables[0].Rows[i]["CostCenter"]))
                        //    PPFData.CostCenterNumberByZone = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        //PPFData.IsActive = Convert.ToString(ds.Tables[0].Rows[i]["IsActive"]);
                        IMACModelList.Add(IMACData);
                    }
                }

                result = IMACModelList.ToList();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }


        public List<UserData> GetIMACEmployeeDetails(string empCode)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlConnection con;
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

        public async Task<int> UpdateBidApprovalDate(IMACFormModel model, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("IMAC") ? GlobalClass.ListNames["IMAC"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                newItem["BidderApprovalDate"] = model.BidderApprovalDate;

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

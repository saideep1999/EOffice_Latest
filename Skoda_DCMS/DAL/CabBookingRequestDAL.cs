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
    public class CabBookingRequestDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        //public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        //public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        //public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;


        //public async Task<dynamic> SaveCBRF(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        public async Task<ResponseModel<object>> SaveCBRF(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            //dynamic result = new ExpandoObject();
            ResponseModel<object> result = new ResponseModel<object>();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            //var web = _context.Web;
            string formShortName = "CBRF";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";

                return result;
            }

            int Id = 0;
            int formId = 0;
            formId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);

            bool IsResubmit = formId == 0 ? false : true;
            try
            {

                var RequestSubmissionFor = form["drpRequestSubType"];
                var onBehalfEmail = form["hiddentxtEmail"];
                long txtEmployeeCode = Convert.ToInt32(form["txtEmpId"]);
                long txtCostCenterNo = Convert.ToInt32(form["txtCostCenterNumber"]);
                string desg = form["ddEmpDesignation"].Trim();

                long txtOnBehalfEmpId = 0;
                long txtOnBehalfCostCenterNo = 0;
                string onBehlafDesg = "";

                if (RequestSubmissionFor == "OnBehalf")
                {
                    txtOnBehalfEmpId = Convert.ToInt32(form["txtOnBehalfEmpId"]);
                    txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOnBehalfCostCenterNumber"]);
                    onBehlafDesg = form["txtOnBehalfdesignation"].Trim();
                }

                var response = await GetApprovalCBRF(txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo, RequestSubmissionFor, desg, onBehlafDesg);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }


                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();

                string formName = "Cab Booking Request Form";

                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 22));
                cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "CabBookingRequest"));
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
                if (RequestSubmissionFor == "OnBehalf")
                {
                    cmd_form.Parameters.Add(new SqlParameter("@Location", form["ddOnBehalfLocation"]));
                }
                else
                {
                    cmd_form.Parameters.Add(new SqlParameter("@Location", form["ddEmpLocation"]));
                }

                cmd_form.Parameters.Add(new SqlParameter("@Modified", ""));
                cmd_form.Parameters.Add(new SqlParameter("@TimeStamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd_form.Parameters.Add(new SqlParameter("@Department", user.Department));
                cmd_form.Parameters.Add(new SqlParameter("@DataRowId", DBNull.Value));
                cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtReasonforBooking"] ?? ""));
                cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
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
                DataTable dt1 = new DataTable();
                var con_form1 = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("SaveCabBookingRequestData", con_form1);
                object SelfEmployeeIDNo = DBNull.Value;
                object SelfMobile = DBNull.Value;
                object SelfTelephone = DBNull.Value;
                object CostCenterNumber = DBNull.Value;
                object ShoppingCartNo = DBNull.Value;
                object NumberofUsers = DBNull.Value;
                object CarRequiredFromDate = DBNull.Value;
                object CarRequiredToDate = DBNull.Value;
                object FlightTime = DBNull.Value;

                if (form["txtEmpId"] != "")
                    SelfEmployeeIDNo = Convert.ToInt64(form["txtEmpId"]);
                if (form["txtMobile"] != "")
                    SelfMobile = Convert.ToInt64(form["txtMobile"]);
                if (form["txtTelephone"] != "")
                    SelfTelephone = Convert.ToInt64(form["txtTelephone"]);
                if (form["txtCostCenterNumber"] != "")
                    CostCenterNumber = Convert.ToInt64(form["txtCostCenterNumber"]);
                if (form["txtShoppingCartNo"] != "")
                    ShoppingCartNo = Convert.ToInt64(form["txtShoppingCartNo"]);
                if (form["txtNumberofUsers"] != "")
                    NumberofUsers = Convert.ToInt64(form["txtNumberofUsers"]);
                if (form["txtCarRequiredFromDate"] != "")
                    CarRequiredFromDate = Convert.ToDateTime(Convert.ToString(form["txtCarRequiredFromDate"]) + " " + Convert.ToString(form["txtCarRequiredFromTime"]));
                if (form["txtCarRequiredToDate"] != "")
                    CarRequiredToDate = Convert.ToDateTime(Convert.ToString(form["txtCarRequiredToDate"]) + " " + Convert.ToString(form["txtCarRequiredToTime"]));
                if (form["txtFlightTime"] != null && form["txtFlightTime"] != "")
                    FlightTime = Convert.ToDateTime(Convert.ToString(DateTime.Now.ToString("yyyy-MM-dd")) + " " + Convert.ToString(form["txtFlightTime"]));
                cmd_form.Parameters.Add(new SqlParameter("@RequestSubmissionFor", Convert.ToString(form["drpRequestSubType"])));
                cmd_form.Parameters.Add(new SqlParameter("@Location", Convert.ToString(form["ddEmpLocation"])));
                cmd_form.Parameters.Add(new SqlParameter("@EmployeeName", Convert.ToString(form["txtEmployeeName"])));
                cmd_form.Parameters.Add(new SqlParameter("@SelfEmployeeIDNo", SelfEmployeeIDNo));
                cmd_form.Parameters.Add(new SqlParameter("@Department", Convert.ToString(form["txtDepartment"])));
                cmd_form.Parameters.Add(new SqlParameter("@Designation", Convert.ToString(form["ddEmpDesignation"].Trim())));
                cmd_form.Parameters.Add(new SqlParameter("@SelfMobile", SelfMobile));
                cmd_form.Parameters.Add(new SqlParameter("@SelfTelephone", SelfTelephone));
                cmd_form.Parameters.Add(new SqlParameter("@EmployeeEmailId", Convert.ToString(form["txtEmail"])));
                cmd_form.Parameters.Add(new SqlParameter("@CostCenterNumber", CostCenterNumber));
                cmd_form.Parameters.Add(new SqlParameter("@ShoppingCartNo", ShoppingCartNo));
                cmd_form.Parameters.Add(new SqlParameter("@Name", Convert.ToString(form["txtName"])));
                cmd_form.Parameters.Add(new SqlParameter("@ContactNumber", Convert.ToString(form["txtContactNumber"])));
                cmd_form.Parameters.Add(new SqlParameter("@CarRequiredFromDate", CarRequiredFromDate));
                cmd_form.Parameters.Add(new SqlParameter("@CarRequiredToDate", CarRequiredToDate));
                cmd_form.Parameters.Add(new SqlParameter("@ReasonforBooking", Convert.ToString(form["txtReasonforBooking"])));
                cmd_form.Parameters.Add(new SqlParameter("@NumberofUsers", NumberofUsers));
                cmd_form.Parameters.Add(new SqlParameter("@AirportPickUpDrop", Convert.ToString(form["drpAirportPickUpDrop"])));
                cmd_form.Parameters.Add(new SqlParameter("@FlightNo", Convert.ToString(form["txtFlightNo"])));
                cmd_form.Parameters.Add(new SqlParameter("@FlightTime", FlightTime));
                cmd_form.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", formId == 0 ? "No" : "Yes"));
                cmd_form.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd_form.Parameters.Add(new SqlParameter("@Id", 0));
                cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                if (RequestSubmissionFor == "OnBehalf")
                {
                    object OnBehalfEmployeeIDNo = DBNull.Value;
                    object OnBehalfMobile = DBNull.Value;
                    object OnBehalfTelephone = DBNull.Value;
                    object OnBehalfCostCenterNumber = DBNull.Value;

                    if (form["txtOnBehalfEmpId"] != "")
                        OnBehalfEmployeeIDNo = Convert.ToInt64(form["txtOnBehalfEmpId"]);
                    if (form["txtOnBehalfMobile"] != "")
                        OnBehalfMobile = Convert.ToInt64(form["txtOnBehalfMobile"]);
                    if (form["txtOnBehalfTelephone"] != "")
                        OnBehalfTelephone = Convert.ToInt64(form["txtOnBehalfTelephone"]);
                    if (form["txtOnBehalfCostCenterNumber"] != "")
                        OnBehalfCostCenterNumber = Convert.ToInt64(form["txtOnBehalfCostCenterNumber"]);

                    cmd_form.Parameters.Add(new SqlParameter("@OtherEmployeeName", Convert.ToString(form["txtOtherEmployeeName"])));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehalfEmployeeIDNo", OnBehalfEmployeeIDNo));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehlafDepartment", Convert.ToString(form["txtOnBehalfDepartment"])));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehalfMobile", OnBehalfMobile));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehalfTelephone", OnBehalfTelephone));
                    cmd_form.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", Convert.ToString(form["txtOnBehalfEmail"])));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehalfCostCenterNumber", OnBehalfCostCenterNumber));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehalfDesignation", Convert.ToString(form["txtOnBehalfdesignation"])));
                    cmd_form.Parameters.Add(new SqlParameter("@OnBehalfLocation", Convert.ToString(form["ddOnBehalfLocation"])));
                }

                cmd_form.CommandType = CommandType.StoredProcedure;
                adapter_form.SelectCommand = cmd_form;
                con_form1.Open();
                adapter_form.Fill(dt1);
                con_form1.Close();

                if (dt1.Rows.Count > 0)
                {
                    for (int i = 0; i < dt1.Rows.Count; i++)
                    {
                        AppRowId = Convert.ToInt32(dt1.Rows[i]["Id"]);
                    }

                }

                result.Status = 200;
                result.Message = formId.ToString();

                var count = Convert.ToInt32(form["totalrows"]);
                int CabUsersId = AppRowId;

                //List CabBookingUserDetailsList = web.Lists.GetByTitle("CabBookingUserDetails");

                string pattern = "||";
                var cbudId = string.Empty;
                var userName = string.Empty;
                var userContactNumber = string.Empty;
                var destination = string.Empty;
                var reportingTime = string.Empty;
                var reportingPlaceWithAddress = string.Empty;

                for (var i = 1; i < count + 1; i++)
                {
                    cbudId += (form["txtCBUId_" + i + ""] == null ? "0" : form["txtCBUId_" + i + ""]) + "||";
                    userName += form["txtUserName_" + i + ""] + "||";
                    userContactNumber += form["txtUserContactNumber_" + i + ""] + "||";
                    destination += form["txtDestination_" + i + ""] + "||";
                    reportingTime += form["txtReportingTime_" + i + ""] + "||";
                    reportingPlaceWithAddress += form["txtReportingPlaceWithAddress_" + i + ""] + "||";
                }

                var cbudIds = cbudId.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                cbudIds = cbudIds.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var userNames = userName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                userNames = userNames.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var userContactNumbers = userContactNumber.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                userContactNumbers = userContactNumbers.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var destinations = destination.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                destinations = destinations.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var reportingTimes = reportingTime.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                reportingTimes = reportingTimes.Where(s => !string.IsNullOrEmpty(s)).ToList();

                var reportingPlaceWithAddresss = reportingPlaceWithAddress.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                reportingPlaceWithAddresss = reportingPlaceWithAddresss.Where(s => !string.IsNullOrEmpty(s)).ToList();


                int SrNo = 1;
                for (int i = 0; i < userNames.Count; i++)
                {
                    DataSet ds = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveCBRFDataList", con);
                    cmd_form.Parameters.Add(new SqlParameter("@Id", cbudIds == null || cbudIds.Count == 0 || cbudIds[i] == "" ? 0 : Convert.ToInt64(cbudIds[i])));
                    cmd_form.Parameters.Add(new SqlParameter("@CabUsersId", CabUsersId));
                    cmd_form.Parameters.Add(new SqlParameter("@UserName", userNames[i] ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@UserContactNumber", userContactNumbers.Count != 0 ? userContactNumbers[i] : null));
                    cmd_form.Parameters.Add(new SqlParameter("@Destination", destinations[i] ?? ""));
                    object reportTime = DBNull.Value;
                    if (reportingTimes[i] != null && reportingTimes[i] != "")
                        reportTime = Convert.ToDateTime(reportingTimes[i]);
                    cmd_form.Parameters.Add(new SqlParameter("@ReportingTime", reportTime));
                    cmd_form.Parameters.Add(new SqlParameter("@ReportingPlaceWithAddress", reportingPlaceWithAddresss[i] ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds);
                    con.Close();

                    if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                    {
                        for (int j = 0; j < ds.Tables[0].Rows.Count; j++)
                        {
                            result.Status = 200;
                            result.Message = formId.ToString();
                        }
                    }
                }

                var approverIdList = response.Model;
                int isactive = 0;
                //List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                for (var i = 0; i < approverIdList.Count; i++)
                {
                    DataSet ds1 = new DataSet();
                    SqlCommand cmd_Approver = new SqlCommand();
                    SqlDataAdapter adapter_App = new SqlDataAdapter();
                    con = new SqlConnection(sqlConString);
                    cmd_Approver = new SqlCommand("USP_SaveApproverDetails", con);
                    cmd_Approver.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd_Approver.Parameters.Add(new SqlParameter("@RowId", AppRowId));
                    if (approverIdList[i].ApprovalLevel == 1)
                    {
                        isactive = 1;
                    }
                    else
                    {
                        isactive = 0;
                    }
                    cmd_Approver.Parameters.Add(new SqlParameter("@IsActive", isactive));
                    cmd_Approver.Parameters.Add(new SqlParameter("@NextAppId", DBNull.Value));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverUserName", approverIdList[i].ApproverUserName));
                    //cmd_Approver.Parameters.Add(new SqlParameter("@NextApproverId", 0));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Level", approverIdList[i].ApprovalLevel));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Logic", approverIdList[i].Logic));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Designation", approverIdList[i].Designation));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverStatus", "Pending"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtReasonforBooking"] ?? ""));
                    cmd_Approver.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", approverIdList[i].DelegatedByEmpNum));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverName", approverIdList[i].ApproverName));

                    cmd_Approver.Parameters.Add(new SqlParameter("@Department", ""));
                    cmd_Approver.Parameters.Add(new SqlParameter("@FormParentId", 22));
                    cmd_Approver.Parameters.Add(new SqlParameter("@ControllerName", "CabBookingRequest"));
                    cmd_Approver.Parameters.Add(new SqlParameter("@CreatedBy", approverIdList[i].FName));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_Approver.Parameters.Add(new SqlParameter("@Email", approverIdList[i].EmailId));

                    cmd_Approver.CommandType = CommandType.StoredProcedure;
                    adapter_App.SelectCommand = cmd_Approver;
                    con.Open();
                    adapter_App.Fill(ds1);
                    con.Close();
                }

                var updateRowResponse = UpdateDataRowIdInFormsList(AppRowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, AppRowId);
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

            return result;
        }

        public async Task<int> TypeOfCarUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {

            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            //Web web = _context.Web;
            //var listName = GlobalClass.ListNames.ContainsKey("CBRF") ? GlobalClass.ListNames["CBRF"] : "";
            //if (listName == "")
            //    return 0;
            long Id = Convert.ToInt64(form["FormSrId"]);
            try
            {
                object ddTypeofCar = DBNull.Value;
                object txtTypeofCarOther = DBNull.Value;
                object ddVehicleNumber = DBNull.Value;
                object txtVehicleNumberOther = DBNull.Value;
                if (form["ddTypeofCar"] != null && form["ddTypeofCar"] != "") {
                    ddTypeofCar = Convert.ToString(form["ddTypeofCar"]); }
                if (form["txtTypeofCarOther"] != null && form["txtTypeofCarOther"] != "") {
                    txtTypeofCarOther = Convert.ToString(form["txtTypeofCarOther"]); }
                if (form["ddVehicleNumber"] != null && form["ddVehicleNumber"] != "") {
                    ddVehicleNumber = Convert.ToString(form["ddVehicleNumber"]); }
                if (form["txtVehicleNumberOther"] != null && form["txtVehicleNumberOther"] != "") {
                    txtVehicleNumberOther = Convert.ToString(form["txtVehicleNumberOther"]); }

                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();
                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_UpdateTypeOfCar", con_form);
                cmd_form.Parameters.Add(new SqlParameter("@Id", Id));
                cmd_form.Parameters.Add(new SqlParameter("@TypeofCar", ddTypeofCar));
                cmd_form.Parameters.Add(new SqlParameter("@TypeofCarOther", txtTypeofCarOther));
                cmd_form.Parameters.Add(new SqlParameter("@VehicleNumber", ddVehicleNumber));
                cmd_form.Parameters.Add(new SqlParameter("@VehicleNumberOther", txtVehicleNumberOther));
                cmd_form.CommandType = CommandType.StoredProcedure;
                adapter_form.SelectCommand = cmd_form;
                con_form.Open();
                adapter_form.Fill(ds_form);
                con_form.Close();

                #region Comment
                //List list = _context.Web.Lists.GetByTitle(listName);
                //ListItem newItem = list.GetItemById(formId);
                //newItem["TypeofCar"] = form["ddTypeofCar"];
                //newItem["TypeofCarOther"] = form["txtTypeofCarOther"];
                //newItem["VehicleNumber"] = form["ddVehicleNumber"];
                //newItem["VehicleNumberOther"] = form["txtVehicleNumberOther"];
                //newItem.Update();
                //_context.Load(newItem);
                //_context.ExecuteQuery();
                #endregion
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalCBRF(long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo, string RequestSubmissionFor, string desg, string onBehlafDesg)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();


                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_CabBookingApproval", con);

                if (RequestSubmissionFor == "OnBehalf")
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtOnBehalfEmpId));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtOnBehalfCostCenterNo));
                    cmd.Parameters.Add(new SqlParameter("@desg", onBehlafDesg));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostCenterNo));
                    cmd.Parameters.Add(new SqlParameter("@desg", desg));
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

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
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
                //return null;
            }

        }

        public async Task<dynamic> GetCBRFApprovalList()
        {
            CBRFResults cbrfData = new CBRFResults();
            dynamic result = cbrfData;
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('CabBookingApproverInformationMaster')/items?$select=Department,SubDepartment,Location,ApproverEmployeeCode,ApproverEmailId,ID&$filter=(IsActive eq '1')");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var CBRFResult = JsonConvert.DeserializeObject<CabBookingRequestModel>(responseText);
                    cbrfData = CBRFResult.cbrfflist;
                }
                result = cbrfData.cbrfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        public async Task<dynamic> ViewCBRFFData(int rowId, int formId)
        {
            dynamic CBRFDataList = new ExpandoObject();
            CabBookingRequestModel cabBookingRequestModel = new CabBookingRequestModel();
            CBRFResults cBRFResults = new CBRFResults();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<CBRFData> item = new List<CBRFData>();
                List<CBRFData> CBRFTableDataList = new List<CBRFData>();
                CBRFData model = new CBRFData();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewCBRFDetails", con);
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
                        //model.FormID = Convert.ToInt32(dt.Rows[i]["FormId"]);
                        model.SelfEmployeeIDNo = dt.Rows[0]["SelfEmployeeIDNo"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["SelfEmployeeIDNo"]);
                        model.SelfMobile = dt.Rows[0]["SelfMobile"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["SelfMobile"]);
                        model.SelfTelephone = dt.Rows[0]["SelfTelephone"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["SelfTelephone"]);
                        model.OnBehlafDepartment = Convert.ToString(dt.Rows[0]["OnBehlafDepartment"]);
                        model.OnBehalfMobile = dt.Rows[0]["OnBehalfMobile"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["OnBehalfMobile"]);
                        model.OnBehalfEmployeeIDNo = dt.Rows[0]["OnBehalfEmployeeIDNo"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["OnBehalfEmployeeIDNo"]);
                        model.OnBehalfTelephone = dt.Rows[0]["OnBehalfTelephone"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["OnBehalfTelephone"]);
                        model.OnBehalfCostCenterNumber = Convert.ToString(dt.Rows[0]["OnBehalfCostCenterNumber"]);
                        model.OnBehalfDesignation = Convert.ToString(dt.Rows[0]["OnBehalfDesignation"]);
                        model.Designation = Convert.ToString(dt.Rows[0]["Designation"]);
                        model.Department = Convert.ToString(dt.Rows[0]["Department"]);
                        model.CostCenterNumber = Convert.ToString(dt.Rows[0]["CostCenterNumber"]);
                        model.ShoppingCartNo = Convert.ToString(dt.Rows[0]["ShoppingCartNo"]);
                        model.Name = Convert.ToString(dt.Rows[0]["Name"]);
                        model.ContactNumber = Convert.ToString(dt.Rows[0]["ContactNumber"]);
                        model.CarRequiredFromDate = dt.Rows[0]["CarRequiredFromDate"] == DBNull.Value ? Convert.ToDateTime("0001-01-01 00:00") : Convert.ToDateTime(dt.Rows[0]["CarRequiredFromDate"]);
                        model.CarRequiredToDate = dt.Rows[0]["CarRequiredToDate"] == DBNull.Value ? Convert.ToDateTime("0001-01-01 00:00") : Convert.ToDateTime(dt.Rows[0]["CarRequiredToDate"]);
                        //model.CarRequiredToDate = dt.Rows[0]["CarRequiredToDate"] != null || dt.Rows[0]["CarRequiredToDate"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["CarRequiredToDate"]) : null;
                        //model.OtherEmployeeContactNo = Convert.ToInt64(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.CarRequiredFromTime = Convert.ToString(dt.Rows[0]["CarRequiredFromTime"]);
                        model.CarRequiredToTime = Convert.ToString(dt.Rows[0]["CarRequiredToTime"]);
                        model.UserName = Convert.ToString(dt.Rows[0]["UserName"]);
                        model.UserContactNumber = dt.Rows[0]["UserContactNumber"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["UserContactNumber"]);
                        model.ReportingPlaceWithAddress = Convert.ToString(dt.Rows[0]["ReportingPlaceWithAddress"]);
                        model.ReportingTime = dt.Rows[0]["ReportingTime"] == DBNull.Value ? Convert.ToDateTime("0001-01-01 00:00") : Convert.ToDateTime(dt.Rows[0]["ReportingTime"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeEmailId = Convert.ToString(dt.Rows[0]["EmployeeEmailId"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.ReasonforBooking = Convert.ToString(dt.Rows[0]["ReasonforBooking"]);
                        model.Destination = Convert.ToString(dt.Rows[0]["Destination"]);
                        model.TypeofCar = Convert.ToString(dt.Rows[0]["TypeofCar"]);
                        model.TypeofCarOther = Convert.ToString(dt.Rows[0]["TypeofCarOther"]);
                        model.VehicleNumber = Convert.ToString(dt.Rows[0]["VehicleNumber"]);
                        model.VehicleNumberOther = Convert.ToString(dt.Rows[0]["VehicleNumberOther"]);
                        model.NumberofUsers = Convert.ToString(dt.Rows[0]["NumberofUsers"]);
                        model.AirportPickUpDrop = Convert.ToString(dt.Rows[0]["AirportPickUpDrop"]);
                        model.FlightNo = Convert.ToString(dt.Rows[0]["FlightNo"]);
                        model.FlightTime = dt.Rows[0]["FlightTime"] == DBNull.Value ? Convert.ToDateTime("0001-01-01 00:00") : Convert.ToDateTime(dt.Rows[0]["FlightTime"]);
                        model.Location = Convert.ToString(dt.Rows[0]["Location"]);
                        model.OnBehalfLocation = Convert.ToString(dt.Rows[0]["OnBehalfLocation"]);
                        item.Add(model);
                    }
                }

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("USP_ViewCBRFFormDataList", con);
                cmd1.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd1;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {

                    for (int i = 0; i < ds1.Rows.Count; i++)
                    {
                        CBRFData model1 = new CBRFData();
                        //IMACFormModel itemDataList = new IMACFormModel();

                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(ds1.Rows[i]["FormID"]);
                        model1.FormID = item1;

                        model1.CBUId = Convert.ToInt32(ds1.Rows[i]["Id"]);
                        //model1.FormID = Convert.ToInt32(ds1.Rows[i]["FormId"]);
                        //model1.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                        model1.UserName = Convert.ToString(ds1.Rows[i]["UserName"]);
                        model1.UserContactNumber = Convert.ToInt64(ds1.Rows[i]["UserContactNumber"]);
                        model1.Destination = Convert.ToString(ds1.Rows[i]["Destination"]);
                        model1.ReportingTime = Convert.ToDateTime(ds1.Rows[i]["ReportingTime"]);
                        model1.ReportingPlaceWithAddress = Convert.ToString(ds1.Rows[i]["ReportingPlaceWithAddress"]);
                        CBRFTableDataList.Add(model1);

                    }

                }
                //CBRFDataList.CabUsersList = CBRFTableDataList;
                CBRFDataList.one = item;
                CBRFDataList.Four = CBRFTableDataList;
                var (r1, r2) = await GetApproversData(user, rowId, formId);

                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                CBRFDataList.two = r1.Model;
                CBRFDataList.three = r2.Model;


            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            //cBRFResults.cbrfData = CBRFDataList;

            //cabBookingRequestModel.cbrfflist = cBRFResults;
            return CBRFDataList;
        }

        public async Task<dynamic> GetTypeofCar()
        {


            CBRFResults cbrfData = new CBRFResults();
            dynamic result = cbrfData;
            try
            {

                List<CBRFData> item = new List<CBRFData>();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_GetTypeofCarDetails", con);
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
                        CBRFData model = new CBRFData();
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.CarID = dt.Rows[i]["CarID"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["CarID"]);
                        model.CarName = dt.Rows[i]["CarName"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[i]["CarName"]);
                        model.IsActive = dt.Rows[i]["IsActive"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[i]["IsActive"]);
                        item.Add(model);
                    }
                }

                #region Comment
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                //var response = await client.GetAsync("_api/web/lists/GetByTitle('TypeofCarDetails')/items?$select=ID,CarID,CarName,IsActive"
                //  + "&$filter=(IsActive eq '" + 1 + "')");
                //var responseText = await response.Content.ReadAsStringAsync();

                //if (responseText.Contains("401 UNAUTHORIZED"))
                //    GlobalClass.IsUserLoggedOut = true;

                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var locResult = JsonConvert.DeserializeObject<CabBookingRequestModel>(responseText);
                //    cbrfData = locResult.cbrfflist;
                //}
                #endregion
                result = item;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        public async Task<dynamic> GetVehicleNumber(string carId)
        {
            CBRFResults cbrfData = new CBRFResults();
            dynamic result = cbrfData;
            try
            {

                List<CBRFData> item = new List<CBRFData>();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_GetVehicleNumberDetails", con);
                cmd.Parameters.Add(new SqlParameter("@CarId", carId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        CBRFData model = new CBRFData();
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.VehicleID = dt.Rows[i]["VehicleID"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["VehicleID"]);
                        model.VehicleNumber = dt.Rows[i]["VehicleNumber"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[i]["VehicleNumber"]);
                        item.Add(model);
                    }
                }
                #region Comment 
                //    var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                //var response = await client.GetAsync("_api/web/lists/GetByTitle('VehicleNumberDetails')/items?$select=ID,VehicleID,VehicleNumber"
                //  + "&$filter=(CarId eq '" + carId + "')");
                //var responseText = await response.Content.ReadAsStringAsync();

                //if (responseText.Contains("401 UNAUTHORIZED"))
                //    GlobalClass.IsUserLoggedOut = true;

                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var locResult = JsonConvert.DeserializeObject<CabBookingRequestModel>(responseText);
                //    cbrfData = locResult.cbrfflist;
                //}
                #endregion
                result = item;
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
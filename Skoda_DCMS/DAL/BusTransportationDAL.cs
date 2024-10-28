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
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Xml;
using static Skoda_DCMS.Helpers.Flags;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace Skoda_DCMS.DAL
{
    public class BusTransportationDAL : CommonDAL
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
        /// BTF-It is used for saving the Bus transportation form.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> SaveBusTransportationForm(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            string formShortName = "BTF";
            string formName = "Bus Transportation Form";
            string listName = string.Empty;

            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            //Web web = _context.Web;

            listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }

            try
            {
                var otherEmpType = "";
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                otherEmpType = form["chkEmployeeType"] ?? "";
                var rdOnBehalfOption = form["rdOnBehalfOption"] ?? "";
                var zoneType = form["txtZone"] ?? "";
                long txtEmployeeCode = Convert.ToInt32(form["txtEmployeeCode"]);
                long txtCostCenterNo = Convert.ToInt32(form["txtCostcenterCode"]);
                string locationName = form["ddEmpLocation"];
                string onBehalfLocationName = "";
                long txtOnBehalfEmpId = 0;
                long txtOnBehalfCostCenterNo = 0;
                long txtCostCenterNumberByZone = Convert.ToInt32(form["txtCostCenterNumberByZone"]);

                if (requestSubmissionFor == "OnBehalf")
                {
                    if (rdOnBehalfOption == "SAVWIPLEmployee")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherCostcenterCode"]);
                        onBehalfLocationName = form["ddOtherEmpLocation"];
                    }
                    else if (rdOnBehalfOption == "Others")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherNewEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherNewCostcenterCode"]);
                        onBehalfLocationName = form["ddOtherNewEmpLocation"];
                    }
                    otherEmpType = form["chkOtherEmployeeType"] ?? "";
                }

                var response = await GetApprovalBusTransportationForm(user, requestSubmissionFor, txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo, locationName, onBehalfLocationName);

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
                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();

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
                        cmd_form.Parameters.Add(new SqlParameter("@Location", form["ddEmpLocation"]));
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", form["ddOtherEmpLocation"]));
                        }
                        else
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", form["ddOtherNewEmpLocation"]));
                        }

                    }

                    cmd_form.Parameters.Add(new SqlParameter("@Modified", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@TimeStamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@Department", user.Department));
                    cmd_form.Parameters.Add(new SqlParameter("@DataRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "BusTransportation"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"] ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 17));
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

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData1(form, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                //var newRow = userDetailsResponse.Model;
                RowId = Convert.ToInt32(userDetailsResponse.RowId);


                //Transaction
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);

                cmd = new SqlCommand("USP_UpdateDataBTF", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@Address", form["txtAddress"]));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@TransportationRequired", form["drpTransportationRequired"]));
                cmd.Parameters.Add(new SqlParameter("@Distance", form["txtDistance"]));
                cmd.Parameters.Add(new SqlParameter("@PickupPoint", form["ddPickUpPoint"]));
                cmd.Parameters.Add(new SqlParameter("@BusRouteName", form["ddBusRouteName"]));
                cmd.Parameters.Add(new SqlParameter("@Gender", form["drpGender"]));
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@BusShift", form["drpShift"]));
                cmd.Parameters.Add(new SqlParameter("@BusRouteNumber", form["ddBusRouteNumber"]));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();

                ////Transaction
                //newRow["Address"] = form["txtAddress"];
                //newRow["TransportationRequired"] = form["drpTransportationRequired"];
                //newRow["BusShift"] = form["drpShift"];
                //newRow["Distance"] = form["txtDistance"];
                //newRow["PickupPoint"] = form["ddPickUpPoint"];
                //newRow["BusRouteName"] = form["ddBusRouteName"];
                //newRow["BusRouteNumber"] = form["ddBusRouteNumber"];
                //newRow["Gender"] = form["drpGender"];
                //newRow["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                //newRow["FormID"] = formId;
                //newRow.Update();
                //_context.Load(newRow);
                //_context.ExecuteQuery();

                //RowId = newRow.Id;
                //result.Status = 200;
                //result.Message = formId.ToString();

                //Approval Tracking
                //var approvalResponse = await SaveApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);
                //if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                //{
                //    return approvalResponse;
                //}


                //var approverIdList1 = response.Model;
                //var approverIdList = response.Model;
                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);
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

                //var emailData = new EmailDataModel()
                //{
                //    FormId = formId.ToString(),
                //    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                //    Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                //    UniqueFormName = formShortName,
                //    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                //    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                //    FormName = formName
                //};

                //var emailService = new EmailService();
                //emailService.SendMail(emailData);

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

        public int BusActionUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;

            string listName = GlobalClass.ListNames.ContainsKey("BTF") ? GlobalClass.ListNames["BTF"] : "";

            if (listName == "")
            {
                return 0;
            }

            int rowId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(rowId);
                //newItem["PickupPoint"] = form["ddPickUpPoint"];
                //newItem["BusRouteName"] = form["ddBusRouteName"];
                //newItem["BusRouteNumber"] = form["ddBusRouteNumber"];
                newItem["Slab"] = form["drpSlab"];
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

        /// <summary>
        /// BTF-It is used for viewing the Bus transportation form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> ViewBusTransportationFormData(int rowId, int formId)
        {
            dynamic BTFDataList = new ExpandoObject();
            try
            {
 
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<BTFData> item = new List<BTFData>();
                BTFData model = new BTFData();
                IPAFResults iPAFResults = new IPAFResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                 var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewBusTransportationFormDetails", conn);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                conn.Open();
                adapter.Fill(dt);
                conn.Close();





                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        //int emp;
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
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
                        model.BusShift = Convert.ToString(dt.Rows[0]["BusShift"]);
                        model.Gender = Convert.ToString(dt.Rows[0]["Gender"]);
                        model.Address = Convert.ToString(dt.Rows[0]["Address"]);
                        model.TransportationRequired = Convert.ToString(dt.Rows[0]["TransportationRequired"]);
                        model.Distance = Convert.ToString(dt.Rows[0]["Distance"]);
                        model.PickupPoint = Convert.ToString(dt.Rows[0]["PickupPoint"]);
                        model.BusRouteName = Convert.ToString(dt.Rows[0]["BusRouteName"]);
                        model.BusRouteNumber = Convert.ToString(dt.Rows[0]["BusRouteNumber"]);
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);
                        item.Add(model);
                    }
                }

                BTFDataList.one = item;


                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                List<ApprovalDataModel> modeldatalist = new List<ApprovalDataModel>();
               
                ApprovalMasterModel approvalMasterModel = new ApprovalMasterModel();
               
                NodeClass nodeClass = new NodeClass();
                var con = new SqlConnection(sqlConString);
                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable dt1 = new DataTable();

                cmd1 = new SqlCommand("USP_GetApproversData", con);
                cmd1.Parameters.Add(new SqlParameter("@rowId", rowId));
                cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter1.SelectCommand = cmd1;
                con.Open();
                adapter1.Fill(dt1);
                con.Close();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(dt, settings);


                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt1.Rows.Count; i++)
                    {
                        ApprovalDataModel modelData = new ApprovalDataModel();
                        //modelData.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(dt1.Rows[i]["FormID"]);
                        modelData.FormId = item1;
                        modelData.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
                        modelData.NextApproverId = dt1.Rows[i]["NextApproverId"] != null || dt1.Rows[i]["NextApproverId"] != DBNull.Value || dt1.Rows[i]["NextApproverId"] != "0" ? Convert.ToInt32(dt1.Rows[i]["NextApproverId"]) : 0;
                        modelData.ApproverStatus = dt1.Rows[i]["ApproverStatus"] != null || dt1.Rows[i]["ApproverStatus"] != DBNull.Value || dt1.Rows[i]["ApproverStatus"] != "0" ? Convert.ToString(dt1.Rows[i]["ApproverStatus"]) : null;
                        modelData.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                        modelData.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                        modelData.Designation = Convert.ToString(dt1.Rows[i]["Designation"]);
                        modelData.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
                        modelData.ApproverName = Convert.ToString(dt1.Rows[i]["ApproverName"]);
                        modelData.TimeStamp = Convert.ToDateTime(dt1.Rows[i]["TimeStamp"]);
                        modeldatalist.Add(modelData);

                    }
                }

                nodeClass.Data = modeldatalist;
                approvalMasterModel.Node = nodeClass;

                if (approvalMasterModel.Node.Data.Count > 0)
                {
                    //var clientApp = new HttpClient(handler);
                    //clientApp.BaseAddress = new Uri(conString);
                    //clientApp.DefaultRequestHeaders.Accept.Clear();
                    //clientApp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var names = new List<string>();

                    var items = approvalMasterModel.Node.Data;

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

                 
                       
                        BTFDataList.two = modeldatalist;
                        BTFDataList.three = items;
                    

                }
                else
                {
                    BTFDataList.two = new List<string>();
                    BTFDataList.three = new List<string>();
                }
                return BTFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return BTFDataList;
            }
        }
        /// <summary>
        /// BTF-It is used for calculating the approvers.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalBusTransportationForm(UserData user, string requestSubmissionFor, long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo, string locationName, string onBehalfLocationName)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_BusTransportApproval", con);
                if (requestSubmissionFor == "OnBehalf")
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtOnBehalfEmpId));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtOnBehalfCostCenterNo));
                    cmd.Parameters.Add(new SqlParameter("@LocationName", onBehalfLocationName));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostCenterNo));
                    cmd.Parameters.Add(new SqlParameter("@LocationName", locationName));
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
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                var count = appList.Count;


                //AD Code
                ListDAL obj = new ListDAL();
                for (int i = 0; i < count; i++)
                {
                    string eml = appList[i].EmailId;
                    string currentId = obj.GetUserIdByEmailId(eml);
                    if (string.IsNullOrEmpty(currentId))
                    {
                        return new ResponseModel<List<ApprovalMatrix>> { Status = 500, Message = "User not found in AD", Model = new List<ApprovalMatrix>() };
                    }
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


        /// <summary>
        /// Bus Transportation Form-It is used to get the Bus Transportation Form Pickup Point Master Dropdown data.
        /// </summary>
        /// <returns></returns>

        public List<BTFData> GetBusRouteName(string routeName, string routeNumber, string shift, string locationName)
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetBusRouteData", con);
                cmd.Parameters.Add(new SqlParameter("@RouteName", routeName));
                cmd.Parameters.Add(new SqlParameter("@RouteNumber", routeNumber));
                cmd.Parameters.Add(new SqlParameter("@Shift", shift));
                cmd.Parameters.Add(new SqlParameter("@LocationName", locationName));
                cmd.Parameters.Add(new SqlParameter("@DrpType", "RouteName"));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.BusRouteName = ds.Tables[0].Rows[i]["RouteName"].ToString();
                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }

        public List<BTFData> GetGenderName()
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetGender", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.Gender = ds.Tables[0].Rows[i]["GenderName"].ToString();
                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }


        public List<BTFData> GetShiftName()
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetShift", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.BusShift = ds.Tables[0].Rows[i]["ShiftName"].ToString();
                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }
        public List<BTFData> GetBusRouteNumber(string routeName, string routeNumber, string shift, string locationName)
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetBusRouteData", con);
                cmd.Parameters.Add(new SqlParameter("@RouteName", routeName));
                cmd.Parameters.Add(new SqlParameter("@RouteNumber", routeNumber));
                cmd.Parameters.Add(new SqlParameter("@Shift", shift));
                cmd.Parameters.Add(new SqlParameter("@LocationName", locationName));
                cmd.Parameters.Add(new SqlParameter("@DrpType", "RouteNumber"));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.BusRouteNumber = ds.Tables[0].Rows[i]["RouteNumber"].ToString();
                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }

        public List<BTFData> GetBusPickUpPoint(string routeName, string routeNumber, string shift, string locationName)
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetBusRouteData", con);
                cmd.Parameters.Add(new SqlParameter("@RouteName", routeName));
                cmd.Parameters.Add(new SqlParameter("@RouteNumber", routeNumber));
                cmd.Parameters.Add(new SqlParameter("@Shift", shift));
                cmd.Parameters.Add(new SqlParameter("@LocationName", locationName));
                cmd.Parameters.Add(new SqlParameter("@DrpType", "PickupPoint"));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.PickupPoint = ds.Tables[0].Rows[i]["PickupPoint"].ToString();
                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }

        public int AddEmployeeData(System.Web.Mvc.FormCollection form)
        {
            int result = 0;
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();

                string[] txtempsearchArry = form["txtempsearch"].Split('|');
                string empumber = txtempsearchArry[1];
                string email = txtempsearchArry[2];

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_BusTransportOldEmp", con);
                cmd.Parameters.Add(new SqlParameter("@empNumber", empumber));
                cmd.Parameters.Add(new SqlParameter("@EmailId", email));
                cmd.Parameters.Add(new SqlParameter("@Flag", "I"));
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                result = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Dispose();
                con.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        public DataTable GetBusTransportOldEmp()
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetBusTransportOldEmp", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    return ds.Tables[0];
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return null;
        }

        public int DeleteEmployeeData(string rowId)
        {
            int result = 0;
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_BusTransportOldEmp", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                cmd.Parameters.Add(new SqlParameter("@Flag", "D"));
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                result = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Dispose();
                con.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        public List<BTFData> GetDeletedEmpNumber()
        {
            List<BTFData> username = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_BusTransportOldEmp", con);
                cmd.Parameters.Add(new SqlParameter("@Flag", "S"));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.EmployeeCode = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        route.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailId"]);
                        username.Add(route);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return username;
        }

        public async Task<List<FormData>> GetBusFormsReport(List<BTFData> userDet)
        {
            ListDAL obj = new ListDAL();
            var resultMain = new List<FormData>();
            var resultNew = new List<FormData>();
            List<FormData> approvalMasterList = new List<FormData>();
            List<FormData> formRelationList = new List<FormData>();
            List<FormData> formRelationListAppList = new List<FormData>();
            List<FormData> formRelationMainList = new List<FormData>();

            try
            {
                for (int i = 0; i < userDet.Count; i++)
                {
                    var result = new List<FormData>();
                    var emailId = userDet[i].EmailId;
                    var userDets = obj.GetEmpDetByEmailId(emailId);
                    var userName = userDets.UserName;


                    var handler = new HttpClientHandler();
                    handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var url = "";
                    var client = new HttpClient(handler);
                    client.BaseAddress = new Uri(conString);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    string uniqueFormName = "BTF";
                    if (!string.IsNullOrEmpty(uniqueFormName))
                    {
                        url = "_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,ControllerName,Status,FormParentId/Id,Created,Modified,Department,Author/Title,BusinessNeed,UniqueFormName,FormName" +
                         "&$filter=SubmitterUserName eq '" + userName + "' " +
                         (!string.IsNullOrEmpty(uniqueFormName) ? (" and UniqueFormName eq '" + uniqueFormName + "'") : "") +
                         "&$expand=FormParentId,Author&$top=5000";

                        var responseApprovalMasterUrl = "_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,BusinessNeed,Level,Logic,RunWorkflow,Department,ApproverStatus,RowId,Comment,NextApproverId,Modified,Author/Title,FormId/FormName," +
                         "FormId/Id,FormId/Created,FormId/DataRowId,FormId/Location,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/SubmitterId,FormID/Status" +
                          "&$filter=ApproverUserName eq '" + userName + "' " +
                        (!string.IsNullOrEmpty(uniqueFormName) ? (" and FormId/UniqueFormName eq '" + uniqueFormName + "'") : "")
                         + "and " + "(" +
                            (" ApproverStatus eq 'Approved'" +
                            " or ApproverStatus eq 'Enquired'" +
                            " or (ApproverStatus eq 'Pending' and IsActive eq 1)" +
                            " or ApproverStatus eq 'Rejected'" +
                            " or IsCompleted eq '1'") + ")" +
                           "&$expand=FormId,Author&$top=5000";


                        var responseApprovalMaster = await client.GetAsync(responseApprovalMasterUrl);

                        var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                        {
                            var modelApprovalResult = JsonConvert.DeserializeObject<DashboardModel>(responseTextApprovalMaster);
                            approvalMasterList = modelApprovalResult.Data.Forms;
                        }

                        formRelationList.AddRange(formRelationListAppList.Select(x => new FormData()
                        {
                            UniqueFormId = x.FormRelation.Id,
                            Status = x.ApproverStatus,
                            FormName = x.FormRelation.FormName,
                            UniqueFormName = x.FormRelation.UniqueFormName,
                            FormCreatedDate = x.FormRelation.CreatedDate,
                            Author = new Author() { Submitter = x.Author.Submitter },
                            BusinessNeed = x.BusinessNeed,
                            DataRowId = x.FormRelation.DataRowId,
                            ControllerName = x.FormRelation.ControllerName,
                            Comment = x.Comment
                        }).ToList());

                        formRelationMainList = formRelationList.GroupBy(x => new { x.UniqueFormId, x.Status, x.FormName, x.UniqueFormName, x.BusinessNeed, x.DataRowId, x.FormCreatedDate }).Select(group => group.First()).ToList();
                    }

                    var response = await client.GetAsync(url);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                        modelResult.Data.Forms.AddRange(formRelationMainList);
                        if (modelResult != null && modelResult.Data != null && modelResult.Data.Forms != null && modelResult.Data.Forms.Count > 0)
                            result = modelResult.Data.Forms;
                    }

                    resultMain.AddRange(result);

                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return resultMain;
        }

        public async Task<List<BTFData>> ViewBTFOldExcelData(List<BTFData> userDet)
        {
            List<BTFData> mainList = new List<BTFData>();
            try
            {
                for (int j = 0; j < userDet.Count; j++)
                {
                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataSet ds = new DataSet();

                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("GetBusOldReport", con);
                    cmd.Parameters.Add(new SqlParameter("@Flag", 'S'));
                    cmd.Parameters.Add(new SqlParameter("@EmpNumber", userDet[j].EmployeeCode));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(ds);
                    con.Close();

                    List<BTFData> routeList = new List<BTFData>();
                    if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            BTFData route = new BTFData();
                            route.CompanyName = Convert.ToString(ds.Tables[0].Rows[i]["CompanyName"]);
                            route.EmployeeType = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeType"]);
                            route.OldEmployeeContactNo = Convert.ToString(ds.Tables[0].Rows[i]["ContactNo"]);
                            route.OldEmployeeNumber = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                            route.EmployeeName = ds.Tables[0].Rows[i]["RequestFrom"].ToString();
                            route.BusinessNeed = ds.Tables[0].Rows[i]["BusinessNeeds"].ToString();
                            route.Created_Date = Convert.ToDateTime(ds.Tables[0].Rows[i]["RecievedDate"]);
                            route.TransportationRequired = Convert.ToString(ds.Tables[0].Rows[i]["TransportRequired"]);
                            route.Gender = Convert.ToString(ds.Tables[0].Rows[i]["Gender"]);
                            route.BusShift = Convert.ToString(ds.Tables[0].Rows[i]["Shift"]);
                            route.BusRouteNumber = ds.Tables[0].Rows[i]["RouteNumber"].ToString();
                            route.BusRouteName = ds.Tables[0].Rows[i]["RouteName"].ToString();
                            route.PickupPoint = ds.Tables[0].Rows[i]["PickUpPoint"].ToString();
                            route.Distance = ds.Tables[0].Rows[i]["DistancefrmResidenceToPickupPoint"].ToString();
                            route.Address = ds.Tables[0].Rows[i]["Address"].ToString();
                            route.Slab = ds.Tables[0].Rows[i]["Slab"].ToString();
                            route.Amount = ds.Tables[0].Rows[i]["SlabAmount"].ToString();
                            route.BusLocationName = ds.Tables[0].Rows[i]["LocationName"].ToString();

                            routeList.Add(route);
                        }
                    }


                    mainList.AddRange(routeList);
                }

            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return mainList;
        }

        public DataTable GetBusAdminApprover()
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetBusAdminApprover", con);
                cmd.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    return ds.Tables[0];
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return null;
        }

    }
}
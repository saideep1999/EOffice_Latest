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
    public class GSFDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        public async Task<dynamic> GetGSFDetails(int rowId, int formId)
        {
            dynamic reissueIDCard = new ExpandoObject();
            List<ReissueIDCardData> MainList = new List<ReissueIDCardData>();
            try
            {

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<GoalSettingData> item = new List<GoalSettingData>();
                AttachmentFilesResults attachmentFilesResults = new AttachmentFilesResults();
                GoalSettingData model = new GoalSettingData();
                GoalSettingResults results = new GoalSettingResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewGoalSettingForm", conn);
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


                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32((dt.Rows[i]["FormID"]));
                        model.FormID = item1;

                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);

                        model.FormId = Convert.ToInt32(dt.Rows[i]["formid"]);

                        model.Created = Convert.ToDateTime(dt.Rows[0]["Created"]);
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
                        model.GoalTitle = Convert.ToString(dt.Rows[0]["GoalTitle"]);
                        model.GoalDescription = Convert.ToString(dt.Rows[0]["GoalDescription"]);
                        model.StartDate = Convert.ToDateTime(dt.Rows[0]["StartDate"]);
                        model.EndDate = Convert.ToDateTime(dt.Rows[0]["EndDate"]);
                        model.MeasurementCriteria = Convert.ToString(dt.Rows[0]["MeasurementCriteria"]);
                        model.PriorityLevel = Convert.ToString(dt.Rows[0]["PriorityLevel"]);
                        item.Add(model);
                    }
                }
                reissueIDCard.one = item;

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
                        modelData.Comment = Convert.ToString(dt1.Rows[i]["Comment"]);

                        modeldatalist.Add(modelData);

                    }
                }

                nodeClass.Data = modeldatalist;
                approvalMasterModel.Node = nodeClass;

                if (approvalMasterModel.Node.Data.Count > 0)
                {

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



                    reissueIDCard.two = modeldatalist;
                    reissueIDCard.three = items;

                }
                else
                {
                    reissueIDCard.two = new List<string>();
                    reissueIDCard.three = new List<string>();
                }

                //approval end

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return reissueIDCard;
        }

        public async Task<ResponseModel<object>> SaveGSFDAL(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();

            int RowId = 0;
            string formShortName = "GSF";
            string formName = "Goal Setting Form";
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
                var loc = requestSubmissionFor == "Self" ? form["ddEmpLocation"]
                     : (otherEmpType == "SAVWIPLEmployee" ? form["ddOtherEmpLocation"]
                         : (otherEmpType == "Others" ? form["ddOtherNewEmpLocation"] : ""));
                int isPKI = form["chkTypeOfCard"] == "PKI" ? 1 : 0;
                string requesterEmployeeType = requestSubmissionFor == "Self" ? form["chkEmployeeType"] : (otherEmpType == "SAVWIPLEmployee" ? form["chkOtherEmployeeType"] : form["chkOtherNewEmployeeType"]);
                int isInternal = requesterEmployeeType == "Internal" ? 1 : 0;

                string externlOrgName = requestSubmissionFor == "Self" ? form["txtExternalOrganizationName"] : (otherEmpType == "SAVWIPLEmployee" ? form["txtOtherExternalOrganizationName"] : form["txtOtherNewExternalOrganizationName"]);
                // externlOrgName = externlOrgName == null ? "" : externlOrgName;


                var response = await GetGoalSettingApprovers(empNum, ccNum, loc, isPKI, isInternal, externlOrgName);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }


                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();

                if (FormId == 0)
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "GoalSetting"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"] == null ? " " : form["txtBusinessNeed"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 56));
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

                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formID", FormId));
                    cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                    cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterId", DBNull.Value));
                    if (FormId == 0)
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "GoalSetting"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"] == null ? " " : form["txtBusinessNeed"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 56));
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

                    ListDAL dal = new ListDAL();
                    //var resubmitResult = await dal.ResubmitUpdate(formId);

                    DataTable dt = new DataTable();
                    var con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_updateFlagInApprovalMaster", con);
                    cmd_form.Parameters.Add(new SqlParameter("@formId", FormId));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", AppRowId));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(dt);
                    con.Close();

                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            new ResponseModel<object> { Message = Convert.ToString(dt.Rows[i]["message"]), Status = Convert.ToInt32(dt.Rows[i]["Status"]) };
                        }
                    }
                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData1(form, listName, formId, FormId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                //var newRow = userDetailsResponse.Model;
                RowId = Convert.ToInt32(userDetailsResponse.RowId);


                string _path = "";

                string _pathfORSaving = "";
        


                //Transaction
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_UpdateGoalSettingForm", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@GoalTitle", form["GoalTitle"])); // Assuming txtGoalTitle is the form field for Goal Title
                cmd.Parameters.Add(new SqlParameter("@GoalDescription", form["GoalDescription"])); // Assuming txtGoalDescription is the form field for Goal Description
                cmd.Parameters.Add(new SqlParameter("@StartDate", DateTime.Parse(form["StartDate"]))); // Converting string to DateTime for Start Date
                cmd.Parameters.Add(new SqlParameter("@EndDate", DateTime.Parse(form["EndDate"]))); // Converting string to DateTime for End Date
                cmd.Parameters.Add(new SqlParameter("@MeasurementCriteria", form["MeasurementCriteria"])); // Assuming txtMeasurementCriteria is the form field for Measurement Criteria
                cmd.Parameters.Add(new SqlParameter("@PriorityLevel",form["PriorityLevel"])); // Assuming txtPriorityLevel is the form field for Priority Level


                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();

                //Approval Tracking
                result.Status = 200;
                result.Message = formId.ToString();
                var approverIdList = response.Model;
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
                //    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
                //    UniqueFormName = formShortName,
                //    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                //    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
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
            }
            return result;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetGoalSettingApprovers(long empNum, long ccNum, string loc, int isPKI, int isInternal, string externlOrgName)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_GSFApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@loc", loc));
                cmd.Parameters.Add(new SqlParameter("@isPKI", isPKI));
                cmd.Parameters.Add(new SqlParameter("@isInternal", isInternal));
                cmd.Parameters.Add(new SqlParameter("@externlOrgName", externlOrgName));
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
                        app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailID"]);
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
                // appList = common.CallAssistantAndDelegateFunc(appList);

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
                return new ResponseModel<List<ApprovalMatrix>> { Status = 200, Model = null, Message = "Error while fetching appprovers." };
            }
        }

    }
}
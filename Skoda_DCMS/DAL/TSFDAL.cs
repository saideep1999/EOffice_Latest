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
using System.Threading.Tasks;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class TSFDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        dynamic approverEmailIds;

        public async Task<ResponseModel<object>> SaveTSF(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            var listName = GlobalClass.ListNames.ContainsKey("TSF") ? GlobalClass.ListNames["TSF"] : "";
            string formShortName = "TSF";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List name not found.";
                return result;
            }
            int RowId;
            string formName = "Time Sheet Form";
            try
            {


                int formId = 0, FormId = Convert.ToInt32(form["FormId"]);
                int prevItemId = Convert.ToInt32(form["FormSrId"]);
                bool IsResubmit = FormId == 0 ? false : true;
                int AppRowId = Convert.ToInt32(form["AppRowId"]);

                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                object txtBusinessNeed = DBNull.Value;
                if (form["txtBusinessNeed"] != null)
                    txtBusinessNeed = form["txtBusinessNeed"];
                bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                int empLocationId = isSelf ? Convert.ToInt32(form["EmpLocationID"]) : (isSAVWIPL ? Convert.ToInt32(form["OtherEmpLocationID"]) : Convert.ToInt32(form["OtherNewEmpLocationID"]));
                var response = GetApprovalTSF(empNum, ccNum, empLocationId);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                //var approverIdList = response.Model;
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "TSF"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", txtBusinessNeed));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 55));
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "TSF"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", txtBusinessNeed));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 55));
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

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData1(form, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model != null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                RowId = Convert.ToInt32(userDetailsResponse.RowId);


                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);

                cmd = new SqlCommand("USP_UpdateTSForm", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@EmployeeID", SqlDbType.NVarChar, 50));
                cmd.Parameters["@EmployeeID"].Value = form["EmployeeID"];
                cmd.Parameters.Add(new SqlParameter("@WeekEndingDate", SqlDbType.Date));
                cmd.Parameters["@WeekEndingDate"].Value = Convert.ToDateTime(form["WeekEndingDate"]);
                cmd.Parameters.Add(new SqlParameter("@TotalHoursWorked", SqlDbType.Decimal));
                cmd.Parameters["@TotalHoursWorked"].Value = form["TotalHoursWorked"];
                cmd.Parameters.Add(new SqlParameter("@LeaveTaken", SqlDbType.Decimal));
                if (!string.IsNullOrEmpty(form["LeaveTaken"]))
                {
                    cmd.Parameters["@LeaveTaken"].Value = form["LeaveTaken"];
                }
                else
                {
                    cmd.Parameters["@LeaveTaken"].Value = DBNull.Value;
                }
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();

                var TSFormList = JsonConvert.DeserializeObject<List<DailyHour>>(form["hdnTSFDataList"]);
                // Create a DataTable to hold the DailyHours list
                DataTable tempTable = new DataTable();
                tempTable.Columns.Add("DailyHourId", typeof(int));
                tempTable.Columns.Add("TSFId", typeof(int));
                tempTable.Columns.Add("CostCode", typeof(string));
                tempTable.Columns.Add("MondayHours", typeof(decimal));
                tempTable.Columns.Add("TuesdayHours", typeof(decimal));
                tempTable.Columns.Add("WednesdayHours", typeof(decimal));
                tempTable.Columns.Add("ThursdayHours", typeof(decimal));
                tempTable.Columns.Add("FridayHours", typeof(decimal));
                tempTable.Columns.Add("SaturdayHours", typeof(decimal));
                tempTable.Columns.Add("SundayHours", typeof(decimal));

                // Populate the DataTable with the list data
                foreach (var hour in TSFormList)
                {
                    tempTable.Rows.Add(
                        hour.DailyHourId,
                        userDetailsResponse.RowId,
                        hour.CostCode,
                        hour.MondayHours,
                        hour.TuesdayHours,
                        hour.WednesdayHours,
                        hour.ThursdayHours,
                        hour.FridayHours,
                        hour.SaturdayHours.HasValue ? hour.SaturdayHours.Value : (object)DBNull.Value,
                        hour.SundayHours.HasValue ? hour.SundayHours.Value : (object)DBNull.Value
                    );
                }
                DataSet ds2 = new DataSet();
                con = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("SaveTSFDailyHours", con);
                cmd_form.Parameters.AddWithValue("@DailyHours", tempTable);
                cmd_form.CommandType = CommandType.StoredProcedure;
                adapter_form.SelectCommand = cmd_form;
                con.Open();
                adapter_form.Fill(ds2);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();
                var approverIdList = response.Model;
                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }


                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

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
                //emailService.SendMail(emailData);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                result.Status = 500;
                //result.Message = "There were some issue while saving form data.";
                result.Message = ex.Message;
                return result;
            }
            return result;
        }

        public ResponseModel<List<ApprovalMatrix>> GetApprovalTSF(long EmpNum, long CCNum, int LocationId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_TSFApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", EmpNum));
                cmd.Parameters.Add(new SqlParameter("@CCNum", CCNum));
                cmd.Parameters.Add(new SqlParameter("@LocationId", LocationId));
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

        public async Task<dynamic> ViewTSFormData(int rowId, int formId)
        {
            dynamic TSFDataList = new ExpandoObject();
            try
            {

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<DailyHour> OtherList = new List<DailyHour>();
                List<TSFModel> item = new List<TSFModel>();
                List<Attachments> attachments = new List<Attachments>();
                Attachments attachments1 = new Attachments();
                AttachmentFilesResults attachmentFilesResults = new AttachmentFilesResults();
                TSFModel model = new TSFModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewTSFormDetails", conn);
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
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);

                        model.EmployeeID = Convert.ToString(dt.Rows[0]["EmployeeID"]);
                        model.WeekEndingDate = Convert.ToDateTime(dt.Rows[0]["WeekEndingDate"]);
                        model.TotalHoursWorked = Convert.ToDecimal(dt.Rows[0]["TotalHoursWorked"]);
                        if (dt.Rows[0]["LeaveTaken"] != DBNull.Value)
                        {
                            model.LeaveTaken = Convert.ToDecimal(dt.Rows[0]["LeaveTaken"]);
                        }
                        else
                        {
                            model.LeaveTaken = null;
                        }
                        item.Add(model);
                    }
                }
                TSFDataList.one = item;
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
                        if(ds1.Rows[i]["SundayHours"] != DBNull.Value)
                            model1.SundayHours = Convert.ToDecimal(ds1.Rows[i]["SundayHours"]);
                        else
                            model1.SundayHours = null;
                        OtherList.Add(model1);
                    }

                }
                TSFDataList.four = OtherList;
                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                TSFDataList.two = r1.Model;
                TSFDataList.three = r2.Model;

                return TSFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return TSFDataList;
            }
        }

    }
}
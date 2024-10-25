using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class NEIFDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        dynamic approverEmailIds;
        public async Task<ResponseModel<object>> SaveNEIF(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            var listName = GlobalClass.ListNames.ContainsKey("NEIF") ? GlobalClass.ListNames["NEIF"] : "";
            string formShortName = "NEIF";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List name not found.";
                return result;
            }
            int RowId;
            string formName = "New Employee Information Form";
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
                var response = GetApprovalNEIF(empNum, ccNum, empLocationId);
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "NEIF"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", txtBusinessNeed));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 54));
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "NEIF"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", txtBusinessNeed));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 54));
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

                cmd = new SqlCommand("USP_UpdateNEIForm", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@FullName", form["FullName"]));
                cmd.Parameters.Add(new SqlParameter("@PreferredName", form["PreferredName"]));
                cmd.Parameters.Add(new SqlParameter("@DateOfBirth", Convert.ToDateTime(form["DateOfBirth"])));
                cmd.Parameters.Add(new SqlParameter("@Gender", form["Gender"]));
                cmd.Parameters.Add(new SqlParameter("@MaritalStatus", form["MaritalStatus"]));
                cmd.Parameters.Add(new SqlParameter("@Nationality", form["Nationality"]));
                cmd.Parameters.Add(new SqlParameter("@PersonalEmail", form["PersonalEmail"]));
                cmd.Parameters.Add(new SqlParameter("@MobilePhoneNumber", form["MobilePhoneNumber"]));
                cmd.Parameters.Add(new SqlParameter("@EmergencyContactName", form["EmergencyContactName"]));
                cmd.Parameters.Add(new SqlParameter("@EmergencyContactRelationship", form["EmergencyContactRelationship"]));
                cmd.Parameters.Add(new SqlParameter("@EmergencyContactPhoneNumber", form["EmergencyContactPhoneNumber"]));
                cmd.Parameters.Add(new SqlParameter("@CurrentAddress", form["CurrentAddress"]));
                cmd.Parameters.Add(new SqlParameter("@PermanentAddress", form["PermanentAddress"]));
                cmd.Parameters.Add(new SqlParameter("@PostalCode", form["PostalCode"]));
                cmd.Parameters.Add(new SqlParameter("@JobTitle", form["JobTitle"]));
                cmd.Parameters.Add(new SqlParameter("@Department", form["Department"]));
                cmd.Parameters.Add(new SqlParameter("@DateOfJoining", Convert.ToDateTime(form["DateOfJoining"])));
                cmd.Parameters.Add(new SqlParameter("@EmploymentType", form["EmploymentType"]));
                cmd.Parameters.Add(new SqlParameter("@ManagerName", form["ManagerName"]));
                cmd.Parameters.Add(new SqlParameter("@BankAccountNumber", form["BankAccountNumber"]));
                cmd.Parameters.Add(new SqlParameter("@BankName", form["BankName"]));
                cmd.Parameters.Add(new SqlParameter("@IFSCCode", form["IFSCCode"]));
                cmd.Parameters.Add(new SqlParameter("@TaxIDNumber", form["TaxIDNumber"]));
                cmd.Parameters.Add(new SqlParameter("@DrivingLicense", form["DrivingLicense"]));
                cmd.Parameters.Add(new SqlParameter("@Passport", form["Passport"]));
                cmd.Parameters.Add(new SqlParameter("@ValidVisa", form["ValidVisa"]));
                cmd.Parameters.Add(new SqlParameter("@PAN", form["PAN"]));
                cmd.Parameters.Add(new SqlParameter("@AADHAR", form["AADHAR"]));
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", txtBusinessNeed));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();


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
                emailService.SendMail(emailData);
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

        public ResponseModel<List<ApprovalMatrix>> GetApprovalNEIF(long EmpNum, long CCNum, int LocationId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_NEIFApproval", con);
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
<<<<<<< HEAD
                appList = common.CallAssistantAndDelegateFunc(appList);
=======
                // appList = common.CallAssistantAndDelegateFunc(appList);
>>>>>>> latestcode25102024

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

        public async Task<dynamic> ViewNEIFormData(int rowId, int formId)
        {
            dynamic NEIFDataList = new ExpandoObject();
            try
            {

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<NEIFModel> item = new List<NEIFModel>();
                List<Attachments> attachments = new List<Attachments>();
                Attachments attachments1 = new Attachments();
                AttachmentFilesResults attachmentFilesResults = new AttachmentFilesResults();
                NEIFModel model = new NEIFModel();
                NEIFResults NEIFResults = new NEIFResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewNEIFormDetails", conn);
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
                        item.Add(model);
                    }
                }
                NEIFDataList.one = item;

                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                NEIFDataList.two = r1.Model;
                NEIFDataList.three = r2.Model;

                return NEIFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return NEIFDataList;
            }
        }

    }
}
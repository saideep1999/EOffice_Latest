﻿using Skoda_DCMS.App_Start;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;


namespace Skoda_DCMS.DAL
{
    public class CAFDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        dynamic approverEmailIds;
        public async Task<ResponseModel<object>> SaveCAF(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            var listName = GlobalClass.ListNames.ContainsKey("CAF") ? GlobalClass.ListNames["CAF"] : "";
            string formShortName = "CAF";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List name not found.";
                return result;
            }
            int RowId;
            string formName = "Candidate Application Form";
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
                var response = GetApprovalCAF(empNum, ccNum, empLocationId);
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "CAF"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["EmployeeComments"] ?? (object)DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 58));
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "CAF"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["EmployeeComments"] ?? (object)DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 58));
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
                string fullPath = "";
                string attachedfile3 = "";
                try
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string customPath = HttpContext.Current.Server.MapPath("~/Doc/CAForm/" + formId + "/");

                        // Create the directory if it doesn't exist
                        if (Directory.Exists(customPath))
                        {
                            // If directory exists, delete the directory and its contents
                            Directory.Delete(customPath, true); // true to delete the directory contents
                        }

                        // Recreate the directory
                        Directory.CreateDirectory(customPath);
                        // Generate a custom file name (e.g., add timestamp to avoid duplicates)
                        string customFileName = Path.GetFileNameWithoutExtension(file.FileName)
                                                + "_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                                                + Path.GetExtension(file.FileName);

                        // Combine the path and custom file name
                        fullPath = Path.Combine(customPath, customFileName);

                        // Save the file to the custom path
                        file.SaveAs(fullPath);

                    }
                }
                catch (Exception ex)
                {
                }
                if (form["attachedfile3"] != null && form["attachedfile3"] != "" && (file == null && file.ContentLength < 0))
                    attachedfile3 = form["attachedfile3"];
                else
                    attachedfile3 = fullPath;

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);

                cmd = new SqlCommand("USP_UpdateCAForm", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@FullName", form["FullName"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@DateOfBirth", form["DateOfBirth"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Gender", form["Gender"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Nationality", form["Nationality"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@ContactNumber", form["ContactNumber"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EmailAddress", form["EmailAddress"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@CurrentAddress", form["CurrentAddress"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@PermanentAddress", form["PermanentAddress"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@LinkedInProfile", form["LinkedInProfile"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@WebsitePortfolio", form["WebsitePortfolio"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@PositionAppliedFor", form["PositionAppliedFor"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@ExpectedSalary", form["ExpectedSalary"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@NoticePeriod", form["NoticePeriod"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@HighestQualification", form["HighestQualification"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@InstitutionName", form["InstitutionName"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@YearOfGraduation", form["YearOfGraduation"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Specialization", form["Specialization"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@AdditionalCertifications", form["AdditionalCertifications"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TotalYearsOfExperience", form["TotalYearsOfExperience"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@LastCompany", form["LastCompany"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@LastJobTitle", form["LastJobTitle"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TechnicalSkills", form["TechnicalSkills"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@OtherSkills", form["OtherSkills"] ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@ResumeFilePath", attachedfile3 ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", DBNull.Value));
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

        public ResponseModel<List<ApprovalMatrix>> GetApprovalCAF(long EmpNum, long CCNum, int LocationId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_CAFApproval", con);
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

        public async Task<dynamic> ViewCAFormData(int rowId, int formId)
        {
            dynamic CAFDataList = new ExpandoObject();
            try
            {

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<CAFModel> item = new List<CAFModel>();
                List<Attachments> attachments = new List<Attachments>();
                Attachments attachments1 = new Attachments();
                AttachmentFilesResults attachmentFilesResults = new AttachmentFilesResults();
                CAFModel model = new CAFModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewCAFormDetails", conn);
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

                        if (dt.Rows.Count > 0)
                        {
                            DataRow row = dt.Rows[0];

                            model.FullName = Convert.ToString(row["FullName"]);
                            model.DateOfBirth = Convert.ToDateTime(row["DateOfBirth"]);
                            model.Gender = Convert.ToString(row["Gender"]);
                            model.Nationality = Convert.ToString(row["Nationality"]);
                            model.ContactNumber = Convert.ToString(row["ContactNumber"]);
                            model.EmailAddress = Convert.ToString(row["EmailAddress"]);
                            model.CurrentAddress = Convert.ToString(row["CurrentAddress"]);
                            model.PermanentAddress = Convert.ToString(row["PermanentAddress"]);
                            model.LinkedInProfile = Convert.ToString(row["LinkedInProfile"]);
                            model.WebsitePortfolio = Convert.ToString(row["WebsitePortfolio"]);
                            model.PositionAppliedFor = Convert.ToString(row["PositionAppliedFor"]);
                            model.ExpectedSalary = Convert.ToDecimal(row["ExpectedSalary"]);
                            model.NoticePeriod = Convert.ToString(row["NoticePeriod"]);
                            model.HighestQualification = Convert.ToString(row["HighestQualification"]);
                            model.InstitutionName = Convert.ToString(row["InstitutionName"]);
                            model.YearOfGraduation = Convert.ToInt32(row["YearOfGraduation"]);
                            model.Specialization = Convert.ToString(row["Specialization"]);
                            model.AdditionalCertifications = Convert.ToString(row["AdditionalCertifications"]);
                            model.TotalYearsOfExperience = Convert.ToDecimal(row["TotalYearsOfExperience"]);
                            model.LastCompany = Convert.ToString(row["LastCompany"]);
                            model.LastJobTitle = Convert.ToString(row["LastJobTitle"]);
                            model.TechnicalSkills = Convert.ToString(row["TechnicalSkills"]);
                            model.OtherSkills = Convert.ToString(row["OtherSkills"]);
                            model.ResumeFilePath = Convert.ToString(row["ResumeFilePath"]);
                        }

                        item.Add(model);
                    }
                }
                CAFDataList.one = item;

                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                CAFDataList.two = r1.Model;
                CAFDataList.three = r2.Model;

                return CAFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return CAFDataList;
            }
        }
    }
}
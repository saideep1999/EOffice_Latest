using Microsoft.SharePoint;
using Microsoft.AspNetCore.Http;
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
using System.Drawing;
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
using Microsoft.Web.Hosting.Administration;
using System.Web.UI.WebControls;

namespace Skoda_DCMS.DAL
{
    public class DrivingAuthorizationDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        dynamic approverEmailIds;

        public async Task<ResponseModel<object>> SaveDAF(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file, HttpPostedFileBase file1)
        {

           
           

            ResponseModel<object> result = new ResponseModel<object>();
            

         
            var listName = GlobalClass.ListNames.ContainsKey("DAF") ? GlobalClass.ListNames["DAF"] : "";
            string formShortName = "DAF";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List name not found.";
                return result;
            }
            int RowId;
            string formName = "Driving Authorization Form";
            try
            {
               

                int formId = 0, FormId = Convert.ToInt32(form["FormId"]);
                int prevItemId = Convert.ToInt32(form["FormSrId"]);
                bool IsResubmit = FormId == 0 ? false : true;
                int AppRowId = Convert.ToInt32(form["AppRowId"]);

                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                int empLocationId = isSelf ? Convert.ToInt32(form["EmpLocationID"]) : (isSAVWIPL ? Convert.ToInt32(form["OtherEmpLocationID"]) : Convert.ToInt32(form["OtherNewEmpLocationID"]));
                var response = GetApprovalDAF(empNum, ccNum, empLocationId);
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "DrivingAuthorization"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 16));
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "DrivingAuthorization"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 16));
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

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData1( form, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model != null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                RowId = Convert.ToInt32(userDetailsResponse.RowId);

                string _pathfORSaving = "";
                string _pathfORSavinglicense = "";
                if (file != null && file1 != null)
                {
                    string _path = "";
                    string _path1 = "";
                    if (file.ContentLength > 0 || file1.ContentLength > 0)
                    {
                        string _FileName = Path.GetFileName(file.FileName);
                        string _FileName1 = Path.GetFileName(file1.FileName);
                        _path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/DrivingAuth"), _FileName);
                        _path1 = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/DrivingAuth"), _FileName1);
                        _pathfORSaving = "/DrivingAuth/" + _FileName;
                        _pathfORSavinglicense = "/DrivingAuth/" + _FileName1;
                        file.SaveAs(_path);
                        file1.SaveAs(_path1);
                    }

                }

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);

                cmd = new SqlCommand("USP_UpdateDrivingAuthorizationForm", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@SubDepartment", form["txtSubDepartment"]));
                cmd.Parameters.Add(new SqlParameter("@DateOfBirth", form["txtDateOfBirth"]));
                cmd.Parameters.Add(new SqlParameter("@LicenseNumber", form["txtLicenseNumber"]));
                cmd.Parameters.Add(new SqlParameter("@ValidFrom", form["txtValidFrom"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@ValidTill", form["txtValidTill"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@DrivingExperience", form["txtDrivingExperience"]));
                cmd.Parameters.Add(new SqlParameter("@VehiclesDriven", form["txtVehiclesDriven"]));
                cmd.Parameters.Add(new SqlParameter("@Address", form["txtAddress"]));
                cmd.Parameters.Add(new SqlParameter("@AuthorizationForInternal", form["AuthInternal"]));
                cmd.Parameters.Add(new SqlParameter("@AuthorizationForTestTrack", form["AuthTestTrack"]));
                cmd.Parameters.Add(new SqlParameter("@AuthorizationForExternal", form["AuthExternal"]));
                cmd.Parameters.Add(new SqlParameter("@AuthorizationForMaterialHandling", form["AuthMaterialHandling"]));
                cmd.Parameters.Add(new SqlParameter("@ImagePath", _pathfORSaving));
                cmd.Parameters.Add(new SqlParameter("@licenseimgpath", _pathfORSavinglicense));
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"]));
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

        public int SaveApproverResponse(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("DAF") ? GlobalClass.ListNames["DAF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                //ListItem newItem = list.GetItemById(formId);

                //newItem["BloodGroup"] = form["drpBloodGroup"];
                //newItem["EyeSight"] = form["drpEyeSight"];
                //newItem["LT"] = form["txtLT"];
                //newItem["RT"] = form["txtRT"];
                //newItem["HistoryofEpilepsy"] = form["drpHistoryofEpilepsy"];
                //newItem["Certification"] = form["drpCertification"];
                //newItem["Remarks"] = form["txtRemarks"];

                //newItem.Update();
                //_context.Load(newItem);
                //_context.ExecuteQuery();
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

        public async Task<dynamic> ViewDAFData(int rowId, int formId)
        {
            dynamic DAFDataList = new ExpandoObject();
            try
            {

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<DAFData> item = new List<DAFData>();
                List<Attachments> attachments = new List<Attachments>();
                Attachments attachments1 = new Attachments();
                AttachmentFilesResults attachmentFilesResults = new AttachmentFilesResults();
                DAFData model = new DAFData();
                DAFResults dafResults = new DAFResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewPhotographyDrivingAuthorizationDetails", conn);
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
                        model.SubDepartment = Convert.ToString(dt.Rows[0]["SubDepartment"]);
                        model.DateOfBirth = null;
                        if (dt.Rows[0]["DateOfBirth"] != DBNull.Value)
                            model.DateOfBirth = Convert.ToDateTime(dt.Rows[0]["DateOfBirth"]);
                        model.LicenseNumber = Convert.ToString(dt.Rows[0]["LicenseNumber"]);
                        model.ValidFrom = null;
                        if (dt.Rows[0]["ValidFrom"] != DBNull.Value)
                            model.ValidFrom = Convert.ToDateTime(dt.Rows[0]["ValidFrom"]);
                        model.ValidTill = null;
                        if (dt.Rows[0]["ValidTill"] != DBNull.Value)
                            model.ValidTill = Convert.ToDateTime(dt.Rows[0]["ValidTill"]);
                        model.DrivingExperience = 0;
                        if (dt.Rows[0]["DrivingExperience"] != DBNull.Value)
                            model.DrivingExperience = Convert.ToInt32(dt.Rows[0]["DrivingExperience"]);
                        model.VehiclesDriven = Convert.ToString(dt.Rows[0]["VehiclesDriven"]);
                        model.Address = Convert.ToString(dt.Rows[0]["Address"]);
                        model.AuthorizationForInternal = Convert.ToString(dt.Rows[0]["AuthorizationForInternal"]);
                        model.AuthorizationForTestTrack = Convert.ToString(dt.Rows[0]["AuthorizationForTestTrack"]);
                        model.AuthorizationForExternal = Convert.ToString(dt.Rows[0]["AuthorizationForExternal"]);
                        model.AuthorizationForMaterialHandling = Convert.ToString(dt.Rows[0]["AuthorizationForMaterialHandling"]);
                        model.BusinessNeed = Convert.ToString(dt.Rows[0]["BusinessNeed"]);
                        model.Imagepath = Convert.ToString(dt.Rows[0]["ImagePath"]);
                        model.Imagepathforlicence = Convert.ToString(dt.Rows[0]["licenseimgpath"]);
                       




                        item.Add(model);
                    }
                }
                DAFDataList.one = item;

                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                DAFDataList.two = r1.Model;
                DAFDataList.three = r2.Model;

                return DAFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return DAFDataList;
            }
        }

        public ResponseModel<List<ApprovalMatrix>> GetApprovalDAF(long EmpNum, long CCNum, int LocationId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_DrivingApproval", con);
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

        private async Task<ResponseModel<List<byte[]>>> DownloadAttachmentData(string listName, int itemId)
        {
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('DrivingAuthorizationForm')/items?$select=AttachmentFiles"
                + "&$filter=(ID eq '" + itemId + "')&$expand=AttachmentFiles");
                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                AttachmentFilesResults AttachmentList = null;
                if (!string.IsNullOrEmpty(responseText))
                {
                    var DAFResults = JsonConvert.DeserializeObject<DrivingAuthorizationFormModel>(responseText, settings);
                    AttachmentList = DAFResults?.daflist?.dafData?[0].AttachmentFiles;
                }
                //List<Image> images = new List<Image>();
                List<byte[]> ImageByteData = new List<byte[]>();
                //if (AttachmentList != null && AttachmentList.Attachments != null && AttachmentList.Attachments.Count > 0)
                //{
                //    foreach (var Attachment in AttachmentList.Attachments)
                //    {
                //        ClientContext _context = new ClientContext(new Uri(conString));
                //        _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //        var web = _context.Web;
                //        int attachmentID = itemId;

                //        List docs = web.Lists.GetByTitle(listName);
                //        ListItem itemAttach = docs.GetItemById(attachmentID);

                //        var attInfo = new AttachmentCreationInformation();

                //        attInfo.FileName = Attachment.FileName;

                //        var file = _context.Web.GetFileByServerRelativeUrl(Attachment.ServerRelativeUrl);
                //        _context.Load(file);
                //        _context.ExecuteQuery();
                //        ClientResult<System.IO.Stream> data = file.OpenBinaryStream();
                //        _context.Load(file);
                //        _context.ExecuteQuery();
                //        Image img = null;
                //        using (System.IO.MemoryStream mStream = new System.IO.MemoryStream())
                //        {
                //            if (data != null)
                //            {
                //                data.Value.CopyTo(mStream);
                //                //if (mStream != null && mStream.Length > 0)
                //                //    img = Image.FromStream(mStream);
                //                ImageByteData.Add(mStream.ToArray());
                //            }
                //        }
                //        //if (img != null)
                //        //    images.Add(img);
                //        //for (int i = 0; i < images.Count; i++)
                //        //{
                //        //    using (var ms = new MemoryStream())
                //        //    {
                //        //        images[i].Save(ms, images[i].RawFormat);
                //        //        ImageByteData.Add(ms.ToArray());
                //        //    }
                //        //}
                //    }
                //}
                return new ResponseModel<List<byte[]>> { Status = 200, Model = ImageByteData, Message = "Image downloaded successfully." };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<byte[]>>
                {
                    Status = 500,
                    Message = "There were some issue while copying file.",
                    Model = null
                };
            }
        }
    }
}
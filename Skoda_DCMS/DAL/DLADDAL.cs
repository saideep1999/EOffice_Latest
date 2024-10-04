using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SharePoint;
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


namespace Skoda_DCMS.DAL
{
    public class DLADDAL : CommonDAL
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

        //public ArchiveData Archive = HttpContext.Current.Session != null ? (ArchiveData)(HttpContext.Current.Session["ArchiveData"]) : new ArchiveData();
        public readonly string ArchiveconString = ConfigurationManager.AppSettings["ArchiveSharepointServerURL"];
        public readonly string spPass1 = ConfigurationManager.AppSettings["ArchiveSharepointPass"];
        public readonly string spUsername1 = ConfigurationManager.AppSettings["ArchiveSharepointUsername"];
        public async Task<dynamic> ViewDLADFormData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();
            try
            {

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();
                List<DLADFormModel> item = new List<DLADFormModel>();
                List<DLADFormModel> DLADTableDataList = new List<DLADFormModel>();
                DLADFormModel model = new DLADFormModel();

                DLADResults iPAFResults = new DLADResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewDLADDetails", con);
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
                        //model.OtherEmployeeContactNo = Convert.ToInt64(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeContactNo = Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]);
                        model.OtherEmployeeUserId = Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]);
                        model.OtherEmployeeName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        model.OtherEmployeeDepartment = Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]);
                        model.OtherEmployeeLocation = Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]);
                        model.OtherEmployeeDesignation = Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]);
                        model.OtherExternalOrganizationName = Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]);
                        model.OtherEmployeeEmailId = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        model.Location = Convert.ToString(dt.Rows[0]["Location"]);
                        model.DurationType = Convert.ToString(dt.Rows[0]["DurationType"]);
                        model.isNewLaptopAdminAccess = Convert.ToString(dt.Rows[0]["isNewLaptopAdminAccess"]);
                        model.isNewServerAdminAccess = Convert.ToString(dt.Rows[0]["isNewServerAdminAccess"]);
                        model.BusinessReason = Convert.ToString(dt.Rows[0]["BusinessReason"]);
                        model.DLAD_Admin = Convert.ToString(dt.Rows[0]["DLAD_Admin"]);
                        model.AccessType_Admin = Convert.ToString(dt.Rows[0]["AccessType_Admin"]);
                        model.Duration_Admin = Convert.ToString(dt.Rows[0]["Duration_Admin"]);
                        model.FromDate_Admin = Convert.ToDateTime(dt.Rows[0]["FromDate_Admin"]);
                        model.ToDate_Admin = Convert.ToDateTime(dt.Rows[0]["ToDate_Admin"]);
                        model.FromDate_LD = Convert.ToDateTime(dt.Rows[0]["FromDate_LD"]);
                        model.ToDate_LD = Convert.ToDateTime(dt.Rows[0]["ToDate_LD"]);
                        model.FromDate_Individual = Convert.ToDateTime(dt.Rows[0]["FromDate_Individual"]);
                        model.ToDate_Individual = Convert.ToDateTime(dt.Rows[0]["ToDate_Individual"]);
                        model.TemporaryDateTo = Convert.ToDateTime(dt.Rows[0]["TemporaryDateTo"]);
                        model.TemporaryDateFrom = Convert.ToDateTime(dt.Rows[0]["TemporaryDateFrom"]);
                        model.DLAD_LD = Convert.ToString(dt.Rows[0]["DLAD_LD"]);
                        model.AccessType_LD = Convert.ToString(dt.Rows[0]["AccessType_LD"]);
                        model.Duration_LD = Convert.ToString(dt.Rows[0]["Duration_LD"]);
                        model.DLAD_Individual = Convert.ToString(dt.Rows[0]["DLAD_Individual"]);
                        model.Duration_Individual = Convert.ToString(dt.Rows[0]["Duration_Individual"]);
                        model.HostName = Convert.ToString(dt.Rows[0]["HostName"]);
                        model.FormType = Convert.ToString(dt.Rows[0]["FormType"]);
                        model.AssetType = Convert.ToString(dt.Rows[0]["AssetType"]);
                        model.ServiceType = Convert.ToInt32(dt.Rows[0]["ServiceType"]);
                        model.ServerTypeDDL = Convert.ToString(dt.Rows[0]["ServerTypeDDL"]);
                        model.ticketNumber = Convert.ToString(dt.Rows[0]["ticketNumber"]);
                        model.ActionDLAD = Convert.ToString(dt.Rows[0]["DLAD_Admin"]);
                        model.fileToUpload = Convert.ToString(dt.Rows[0]["ImagePath"]);
                        //var file = model.fileToUpload;
                        var file = model.fileToUpload.Replace("/DLADUpload/", "");
                        model.fileToUpload = file;
                        model.CreatedDate = Convert.ToDateTime(dt.Rows[0]["Created"]);
                        item.Add(model);
                    }

                }
                URCFData.one = item;


                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewDLADDataList", con);
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
                        //IMACFormModel itemDataList = new IMACFormModel();

                        DLADFormModel model1 = new DLADFormModel();
                        model1.FormId = Convert.ToInt32(ds1.Rows[i]["FormId"]);
                        //model1.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                        model1.ApplicationNameRole = Convert.ToString(ds1.Rows[i]["ApplicationNameRole"]);
                        model1.ServerHostName = Convert.ToString(ds1.Rows[i]["ServerHostName"]);
                        model1.ServerIPAddress = Convert.ToString(ds1.Rows[i]["ServerIPAddress"]);

                        DLADTableDataList.Add(model1);
                    }
                }
                URCFData.Four = DLADTableDataList;


                var (r1, r2) = await GetApproversData(user, rowId, formId);

                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                URCFData.two = r1.Model;
                URCFData.three = r2.Model;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return URCFData;
        }

        internal async Task<ResponseModel<object>> SaveDLAD(DLADFormModel model, UserData userData, HttpPostedFileBase file)
        {
            model.FormType = model.chkName;

            ResponseModel<object> result = new ResponseModel<object>();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            //Web web = _context.Web;
            string formShortName = "DLAD";
            string formName = "ServerLaptopDesktopLocalAdminForm";
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

            if (model.chk1 == "IndividualServer" && model.chk2 == "AdminServer")
            {
                model.FormType = "IndividualServer, AdminServer";
            }
            else if (model.chk1 == "IndividualServer")
            {
                model.FormType = "IndividualServer";
            }
            else if (model.chk2 == "AdminServer")
            {
                model.FormType = "AdminServer";
            }
            else if (model.chk3 == "DeskTopLaptopServer")
            {
                model.FormType = "DeskTopLaptopServer";
            }


            var response = await GetApprovalDLADForm(empNum, ccNum, empDes, model);
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
                cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "DLAD"));
                cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 50));
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


                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(model, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }


                if (file != null)
                {
                    string _path = "";
                    string _pathfORSaving = "";
                    if (file.ContentLength > 0)
                    {
                        string _FileName = Path.GetFileName(file.FileName);
                        _path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/DLADUpload"), _FileName);
                        _pathfORSaving = "/DLADUpload/" + _FileName;
                        file.SaveAs(_path);

                    }



                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataSet ds1 = new DataSet();
                    RowId = Convert.ToInt32(userDetailsResponse.RowId);
                    con = new SqlConnection(sqlConString);

                    cmd = new SqlCommand("USP_UpdateDLADForm", con);
                    cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd.Parameters.Add(new SqlParameter("@RowId", RowId));
                    cmd.Parameters.Add(new SqlParameter("@Location", model.Location));
                    cmd.Parameters.Add(new SqlParameter("@DurationType", model.DurationType));
                    cmd.Parameters.Add(new SqlParameter("@isNewDLAD", model.isNewLaptopAdminAccess));
                    //cmd.Parameters.Add(new SqlParameter("@isNewDLAD", model.isNewLaptopAdminAccess != null || model.isNewLaptopAdminAccess = DBNull.Value || model.isNewLaptopAdminAccess != "0" ? model.isNewLaptopAdminAccess : null));
                    cmd.Parameters.Add(new SqlParameter("@isIndividual", model.isNewServerAdminAccess));
                    cmd.Parameters.Add(new SqlParameter("@isNewServerAdminAccess", model.isNewServerAdminAccess));
                    cmd.Parameters.Add(new SqlParameter("@isNewLaptopAdminAccess", model.isNewLaptopAdminAccess));
                    cmd.Parameters.Add(new SqlParameter("@BusinessReason", model.BusinessReason));
                    cmd.Parameters.Add(new SqlParameter("@DLAD_Admin", model.DLAD_Admin));
                    cmd.Parameters.Add(new SqlParameter("@AccessType_Admin", model.AccessType_Admin));
                    cmd.Parameters.Add(new SqlParameter("@Duration_Admin", model.Duration_Admin));
                    cmd.Parameters.Add(new SqlParameter("@FromDate_Admin", model.FromDate_Admin.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.FromDate_Admin.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@ToDate_Admin", model.ToDate_Admin.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.ToDate_Admin.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@DLAD_LD", model.DLAD_LD));
                    cmd.Parameters.Add(new SqlParameter("@AccessType_LD", model.AccessType_LD));
                    cmd.Parameters.Add(new SqlParameter("@Duration_LD", model.Duration_LD));
                    cmd.Parameters.Add(new SqlParameter("@FromDate_LD", model.FromDate_LD.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.FromDate_LD.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@ToDate_LD", model.ToDate_LD.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.ToDate_LD.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@DLAD_Individual", model.DLADIndividual));
                    cmd.Parameters.Add(new SqlParameter("@AccessType_Individual", model.AccessTypeIndividual));
                    cmd.Parameters.Add(new SqlParameter("@Duration_Individual", model.DurationIndividual));
                    cmd.Parameters.Add(new SqlParameter("@FromDate_Individual", model.FromDate_Individual.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.FromDate_Individual.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@ToDate_Individual", model.ToDate_Individual.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.ToDate_Individual.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@TemporaryDateTo", model.TemporaryDateTo.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.TemporaryDateTo.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@TemporaryDateFrom", model.TemporaryDateFrom.ToString("dd-MM-yyyy") == "01-01-0001" ? null : model.TemporaryDateFrom.ToString("yyyy-MM-ddThh:mm:ss")));
                    cmd.Parameters.Add(new SqlParameter("@HostName", model.HostName));
                    cmd.Parameters.Add(new SqlParameter("@FormType", model.FormType));
                    cmd.Parameters.Add(new SqlParameter("@AssetType", model.AssetType));
                    cmd.Parameters.Add(new SqlParameter("@ServiceType", model.ServiceType));
                    cmd.Parameters.Add(new SqlParameter("@ServerTypeDDL", model.ServerTypeDDL));
                    cmd.Parameters.Add(new SqlParameter("@ImagePath", _pathfORSaving));
                    //cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(ds1);
                    con.Close();
                    result.Status = 200;
                    result.Message = formId.ToString();

                }


                int SrNo = 1;
                foreach (var item in model.IndividualAdminServerList)
                {
                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataSet ds = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_SaveDLADList", con);
                    cmd1.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd1.Parameters.Add(new SqlParameter("@RowId", RowId));
                    cmd1.Parameters.Add(new SqlParameter("@AccessType", ""));
                    cmd1.Parameters.Add(new SqlParameter("@SrNo", SrNo++));
                    cmd1.Parameters.Add(new SqlParameter("@ApplicationNameRole", item.ApplicationNameRole));
                    cmd1.Parameters.Add(new SqlParameter("@ServerHostName", item.ServerHostName));
                    cmd1.Parameters.Add(new SqlParameter("@ServerIPAddress", item.ServerIPAddress));
                    cmd1.Parameters.Add(new SqlParameter("@ListItemId", RowId));
                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));

                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(ds);
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


                if (AppRowId != 0)
                {
                    DataSet ds2 = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_ChangeApprovalMasterStatusDARF", con);
                    cmd_form.Parameters.Add(new SqlParameter("@Id", AppRowId == null || AppRowId == 0 ? 0 : Convert.ToInt64(AppRowId)));
                    cmd_form.Parameters.Add(new SqlParameter("@FormId", formId == null || formId == 0 ? 0 : Convert.ToInt64(formId)));
                    cmd_form.Parameters.Add(new SqlParameter("@ApproverStatus", "Resubmitted"));
                    cmd_form.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds2);
                    con.Close();
                }


                var approverIdList = response.Model;
                int isactive = 0;

                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
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


                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
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

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalDLADForm(long empNum, long ccNum, string empDes, DLADFormModel model)
        {
            try
            {
                string formType = "";

                if (model.ServiceType == 1)
                {
                    formType = "IndividualServer";
                }
                else if (model.ServiceType == 0 && (model.chk2 == "AdminServer" || model.chk1 == "IndividualServer"))
                {
                    formType = "AdminServer";
                }
                else if (model.ServiceType == 0 && model.chk3 == "DeskTopLaptopServer")
                {
                    formType = "DeskTopLaptopServer";
                }

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_getDLADFormApprovals", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@formType", formType));
                cmd.Parameters.Add(new SqlParameter("@loctaion", model.Location));


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
                        app.ApprovalLevel = Convert.ToInt32(ds.Tables[0].Rows[i]["approvalLevel"]);
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
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
            }

        }

        internal async Task<ResponseModel<object>> UpdateData(DLADFormModel model, UserData userData)
        {
            model.FormType = model.chkName;

            ResponseModel<object> result = new ResponseModel<object>();



            var listName = "ServerLaptopDesktopLocalAdminForm";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                return result;
            }

            int formId = Convert.ToInt32(model.FormId);
            try
            {
                string DLAD_Individual = "";
                string DLAD_Admin = "";
                if (model.chk1 == "IndividualServer" && model.ActionDLAD != null)
                {
                    DLAD_Individual = model.ActionDLAD;
                }
                if (model.chk2 == "AdminServer" && model.ActionDLAD != null)
                {
                    DLAD_Admin = model.ActionDLAD;
                }
                int RowId = 0;
                //List list = _context.Web.Lists.GetByTitle(listName);
                //ListItem newItem = list.GetItemById(formId);



                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataSet ds = new DataSet();
                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("USP_SaveDatalist", con);
                cmd1.Parameters.Add(new SqlParameter("@ticketNumber", model.ticketNumber));
                cmd1.Parameters.Add(new SqlParameter("@DLAD_Admin", DLAD_Admin));
                cmd1.Parameters.Add(new SqlParameter("@DLAD_Individual", DLAD_Individual));
                cmd1.Parameters.Add(new SqlParameter("@ActionDLAD", model.ActionDLAD));
                cmd1.Parameters.Add(new SqlParameter("@FormID", formId.ToString()));

                cmd1.CommandType = CommandType.StoredProcedure;
                adapter1.SelectCommand = cmd1;
                con.Open();
                adapter1.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();




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

    }
}
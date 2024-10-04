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
using System.Drawing;
using System.Dynamic;
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
    public class PhotographyPermitDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        public async Task<ResponseModel<object>> SavePPF(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            string formShortName = "PAF";
            string formName = "Photography Authorization Form";
            string listName = string.Empty;

            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

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
                var thirdPartyPhotographer = form["ChkThirdPartyPhotographer"];
                var exceptionalPhoto = form["drpExceptionalPhoto"];
                otherEmpType = form["chkEmployeeType"] ?? "";
                var rdOnBehalfOption = form["rdOnBehalfOption"] ?? "";
                var zoneType = form["txtZone"] ?? "";
                long txtEmployeeCode = Convert.ToInt32(form["txtEmployeeCode"]);
                long txtCostCenterNo = Convert.ToInt32(form["txtCostcenterCode"]);
                long txtOnBehalfEmpId = 0;
                long txtOnBehalfCostCenterNo = 0;
                long txtCostCenterNumberByZone = Convert.ToInt32(form["txtCostCenterNumberByZone"]);
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (rdOnBehalfOption == "SAVWIPLEmployee")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherCostcenterCode"]);
                    }
                    else if (rdOnBehalfOption == "Others")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherNewEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherNewCostcenterCode"]);
                    }

                    otherEmpType = form["chkOtherEmployeeType"] ?? "";
                }

                var response = await GetApprovalPPF(user, requestSubmissionFor, txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo, txtCostCenterNumberByZone, thirdPartyPhotographer, zoneType, exceptionalPhoto);

                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }

               
                int formId = 0;
                int FormId = Convert.ToInt32(form["FormId"]);
                int AppRowId = Convert.ToInt32(form["AppRowId"]);
                bool IsResubmit = FormId == 0 ? false : true;
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "PhotographyPermit"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtPurpose"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 3));
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "PhotographyPermit"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtPurpose"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 3));
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

                //var userDetailsResponse = SaveSubmitterAndApplicantDetails(web, form, listName, formId);
                //if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                //{
                //    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                //}
                //var newItem = userDetailsResponse.Model;
                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData1( form, listName, formId);
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

                cmd = new SqlCommand("USP_UpdatePhotographyPermit", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId ", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraDevice", form["chkVideoCamera"]));
                cmd.Parameters.Add(new SqlParameter("@CameraDevice", form["chkCamera"]));
                cmd.Parameters.Add(new SqlParameter("@MobileDevice", form["chkMobile"]));
                cmd.Parameters.Add(new SqlParameter("@TabletDevice", form["chkTablet"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraMake", form["txtVideoCameraMake"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraModel", form["txtVideoCameraModel"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraSerialIMEINo", form["txtVideoCameraSerialIMEINo"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraSAVWIPLOwned", form["drpVideoCameraSAVWIPLOwned"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraCaptureVoiceSound", form["drpVideoCameraCaptureVoiceSound"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraCaptureVideo", form["drpVideoCameraCaptureVideo"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraCaptureImages", form["drpVideoCameraCaptureImages"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraBluetoothWireless", form["drpVideoCameraBluetoothWireless"]));
                cmd.Parameters.Add(new SqlParameter("@VideoCameraOther", form["txtVideoCameraOther"]));
                cmd.Parameters.Add(new SqlParameter("@CameraMake", form["txtCameraMake"]));
                cmd.Parameters.Add(new SqlParameter("@CameraModel", form["txtCameraModel"]));
                cmd.Parameters.Add(new SqlParameter("@CameraSerialIMEINo", form["txtCameraSerialIMEINo"]));
                cmd.Parameters.Add(new SqlParameter("@CameraSAVWIPLOwned", form["drpCameraSAVWIPLOwned"]));
                cmd.Parameters.Add(new SqlParameter("@CameraCaptureVoiceSound", form["drpCameraCaptureVoiceSound"]));
                cmd.Parameters.Add(new SqlParameter("@CameraCapture", form["drpCameraCapture"]));
                cmd.Parameters.Add(new SqlParameter("@CameraCaptureImages", form["drpCameraCaptureImages"]));
                cmd.Parameters.Add(new SqlParameter("@CameraBluetoothWireless", form["drpCameraBluetoothWireless"]));
                cmd.Parameters.Add(new SqlParameter("@CameraOther", form["txtCameraOther"]));
                cmd.Parameters.Add(new SqlParameter("@MobileMake",    form["txtMobileMake"]));
                cmd.Parameters.Add(new SqlParameter("@MobileModel", form["txtMobileModel"]));
                cmd.Parameters.Add(new SqlParameter("@MobileSerialIMEINo", form["txtMobileSerialIMEINo"]) );
                cmd.Parameters.Add(new SqlParameter("@MobileSAVWIPLOwned", form["drpMobileSAVWIPLOwned"]));
                cmd.Parameters.Add(new SqlParameter("@MobileCaptureVoiceSound",form["drpMobileCaptureVoiceSound"]));
                cmd.Parameters.Add(new SqlParameter("@MobileCapture",form["drpMobileCapture"]));
                cmd.Parameters.Add(new SqlParameter("@MobileCaptureImages", form["drpMobileCaptureImages"]));
                cmd.Parameters.Add(new SqlParameter("@MobileBluetoothWireless",form["drpMobileBluetoothWireless"]));
                cmd.Parameters.Add(new SqlParameter("@MobileOther", form["txtMobileOther"]));
                cmd.Parameters.Add(new SqlParameter("@TabletMake",  form["txtTabletMake"]));
                cmd.Parameters.Add(new SqlParameter("@TabletModel",  form["txtTabletModel"]));
                cmd.Parameters.Add(new SqlParameter("@TabletSerialIMEINo", form["txtTabletSerialIMEINo"]));
                cmd.Parameters.Add(new SqlParameter("@TabletSAVWIPLOwned", form["drpTabletSAVWIPLOwned"]));
                cmd.Parameters.Add(new SqlParameter("@TabletCaptureVoiceSound", form["drpTabletCaptureVoiceSound"]));
                cmd.Parameters.Add(new SqlParameter("@TabletCapture", form["drpTabletCapture"]));
                cmd.Parameters.Add(new SqlParameter("@TabletCaptureImages", form["drpTabletCaptureImages"]));
                cmd.Parameters.Add(new SqlParameter("@TabletBluetoothWireless", form["drpTabletBluetoothWireless"]));
                cmd.Parameters.Add(new SqlParameter("@TabletOther", form["txtTabletOther"]));
                cmd.Parameters.Add(new SqlParameter("@ThirdPartyPhotographer", form["ChkThirdPartyPhotographer"]));
                cmd.Parameters.Add(new SqlParameter("@PAFLocation", form["drpPAFLocation"]));
                cmd.Parameters.Add(new SqlParameter("@Zone", form["txtZone"]));
                cmd.Parameters.Add(new SqlParameter("@CostCenterNumberByZone", form["txtCostCenterNumberByZone"]));
                cmd.Parameters.Add(new SqlParameter("@ValidFrom",form["txtValidFrom"]));
                cmd.Parameters.Add(new SqlParameter("@ValidTo", form["txtValidTo"]));
                cmd.Parameters.Add(new SqlParameter("@ExceptionalPhoto", form["drpExceptionalPhoto"]));
                cmd.Parameters.Add(new SqlParameter("@Purpose", form["txtPurpose"]));
                cmd.Parameters.Add(new SqlParameter("@PreSeriesCarOrPart", form["drpPreSeriesCarOrPart"]));


                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open(); 
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();

                //Approval Tracking
                
                int isactive = 0;
                /// List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
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

        public async Task<int> ValidityUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential(spUsername, spPass);

            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("PAF") ? GlobalClass.ListNames["PAF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);

                newItem["ValidFrom"] = form["txtValidFrom"] == "" ? null : form["txtValidFrom"];
                newItem["ValidTo"] = form["txtValidTo"] == "" ? null : form["txtValidTo"];

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

        public async Task<dynamic> ViewPPFData(int rowId, int formId)
        {
            dynamic PPFDataList = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                List<PPFData> item = new List<PPFData>();
                PPFData model = new PPFData();
                PPFResults dAFResults = new PPFResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewPhotographyAuthorizationFormDetails", conn);
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
                        model.VideoCameraDevice = Convert.ToString(dt.Rows[0]["VideoCameraDevice"]);
                        model.CameraDevice = Convert.ToString(dt.Rows[0]["CameraDevice"]);
                        model.MobileDevice = Convert.ToString(dt.Rows[0]["MobileDevice"]);
                        model.TabletDevice = Convert.ToString(dt.Rows[0]["TabletDevice"]);
                        model.VideoCameraMake = Convert.ToString(dt.Rows[0]["VideoCameraMake"]);
                        model.VideoCameraModel = Convert.ToString(dt.Rows[0]["VideoCameraModel"]);
                        model.VideoCameraSerialIMEINo = Convert.ToString(dt.Rows[0]["VideoCameraSerialIMEINo"]);
                        model.VideoCameraSAVWIPLOwned = Convert.ToString(dt.Rows[0]["VideoCameraSAVWIPLOwned"]);
                        model.VideoCameraCaptureVoiceSound = Convert.ToString(dt.Rows[0]["VideoCameraCaptureVoiceSound"]);
                        model.VideoCameraCaptureVideo = Convert.ToString(dt.Rows[0]["VideoCameraCaptureVideo"]);
                        model.VideoCameraCaptureImages = Convert.ToString(dt.Rows[0]["VideoCameraCaptureImages"]);
                        model.VideoCameraBluetoothWireless = Convert.ToString(dt.Rows[0]["VideoCameraBluetoothWireless"]);
                        model.VideoCameraOther = Convert.ToString(dt.Rows[0]["VideoCameraOther"]);
                        model.CameraMake = Convert.ToString(dt.Rows[0]["CameraMake"]);
                        model.CameraModel = Convert.ToString(dt.Rows[0]["CameraModel"]);
                        model.CameraSerialIMEINo = Convert.ToString(dt.Rows[0]["CameraSerialIMEINo"]);
                        model.CameraSAVWIPLOwned = Convert.ToString(dt.Rows[0]["CameraSAVWIPLOwned"]);
                        model.CameraCaptureVoiceSound = Convert.ToString(dt.Rows[0]["CameraCaptureVoiceSound"]);
                        model.CameraCapture = Convert.ToString(dt.Rows[0]["CameraCapture"]);
                        model.CameraCaptureImages = Convert.ToString(dt.Rows[0]["CameraCaptureImages"]);
                        model.CameraBluetoothWireless = Convert.ToString(dt.Rows[0]["CameraBluetoothWireless"]);
                        model.CameraOther = Convert.ToString(dt.Rows[0]["CameraOther"]);
                        model.MobileMake = Convert.ToString(dt.Rows[0]["MobileMake"]);
                        model.MobileModel = Convert.ToString(dt.Rows[0]["MobileModel"]);
                        model.MobileSerialIMEINo = Convert.ToString(dt.Rows[0]["MobileSerialIMEINo"]);
                        model.MobileSAVWIPLOwned = Convert.ToString(dt.Rows[0]["MobileSAVWIPLOwned"]);
                        model.MobileCaptureVoiceSound = Convert.ToString(dt.Rows[0]["MobileCaptureVoiceSound"]);
                        model.MobileCapture = Convert.ToString(dt.Rows[0]["MobileCapture"]);
                        model.MobileCaptureImages = Convert.ToString(dt.Rows[0]["MobileCaptureImages"]);
                        model.MobileBluetoothWireless = Convert.ToString(dt.Rows[0]["MobileBluetoothWireless"]);
                        model.MobileOther = Convert.ToString(dt.Rows[0]["MobileOther"]);
                        model.TabletMake = Convert.ToString(dt.Rows[0]["TabletMake"]);
                        model.TabletModel = Convert.ToString(dt.Rows[0]["TabletModel"]);
                        model.TabletSerialIMEINo = Convert.ToString(dt.Rows[0]["TabletSerialIMEINo"]);
                        model.TabletSAVWIPLOwned = Convert.ToString(dt.Rows[0]["TabletSAVWIPLOwned"]);
                        model.TabletCaptureVoiceSound = Convert.ToString(dt.Rows[0]["TabletCaptureVoiceSound"]);
                        model.TabletCapture = Convert.ToString(dt.Rows[0]["TabletCapture"]);
                        model.TabletCaptureImages = Convert.ToString(dt.Rows[0]["TabletCaptureImages"]);
                        model.TabletBluetoothWireless = Convert.ToString(dt.Rows[0]["TabletBluetoothWireless"]);
                        model.TabletOther = Convert.ToString(dt.Rows[0]["TabletOther"]);
                        model.ThirdPartyPhotographer = Convert.ToString(dt.Rows[0]["ThirdPartyPhotographer"]);
                        model.PAFLocation = Convert.ToString(dt.Rows[0]["PAFLocation"]);
                        model.Zone = Convert.ToString(dt.Rows[0]["Zone"]);
                        model.CostCenterNumberByZone = Convert.ToInt32(dt.Rows[0]["CostCenterNumberByZone"]);
                        model.PreSeriesCarOrPart = Convert.ToString(dt.Rows[0]["PreSeriesCarOrPart"]);
                        model.ValidFrom = Convert.ToDateTime(dt.Rows[0]["ValidFrom"] );
                        model.ValidTo = Convert.ToDateTime(dt.Rows[0]["ValidTo"] );
                        model.ExceptionalPhoto = Convert.ToString(dt.Rows[0]["ExceptionalPhoto"]);
                        model.Purpose = Convert.ToString(dt.Rows[0]["Purpose"] );
                        model.Created_Date = Convert.ToDateTime(dt.Rows[0]["Created"]);
                        item.Add(model);
                    }
                }

                PPFDataList.one = item;

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
                        modelData.Comment = Convert.ToString(dt1.Rows[i]["Comment"]);
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



                    PPFDataList.two = modeldatalist;
                    PPFDataList.three = items;

                }
                else
                {
                    PPFDataList.two = new List<string>();
                    PPFDataList.three = new List<string>();
                }
                return PPFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return PPFDataList;
            }
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalPPF(UserData user, string requestSubmissionFor, long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo, long txtCostCenterNumberByZone, string thirdPartyPhotographer, string zoneType, string exceptionalPhoto)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_PAFApproval", con);
                if (requestSubmissionFor == "Self")
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtEmployeeCode));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtCostCenterNo));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@EmpNo", txtOnBehalfEmpId));
                    cmd.Parameters.Add(new SqlParameter("@ccnum", txtOnBehalfCostCenterNo));
                }
                cmd.Parameters.Add(new SqlParameter("@CostCenterNumberByZone", txtCostCenterNumberByZone));
                cmd.Parameters.Add(new SqlParameter("@ZoneType", zoneType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeType", thirdPartyPhotographer));
                cmd.Parameters.Add(new SqlParameter("@ExceptionalPhoto", exceptionalPhoto));

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
                        app.ApprovalLevel = Convert.ToInt32(ds.Tables[0].Rows[i]["AppLevel"]);
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

        public async Task<dynamic> GetAreas()
        {
            dynamic result = new PPFResults();
            try
            {
                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<PPFData> PPFDataList = new List<PPFData>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetPAFLocationZoneDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        PPFData PPFData = new PPFData();
                        PPFData.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["Id"]);
                        PPFData.PAFLocation = Convert.ToString(ds.Tables[0].Rows[i]["LocationName"]);
                        PPFData.Zone = Convert.ToString(ds.Tables[0].Rows[i]["LocationZone"]);
                        if (!Convert.IsDBNull(ds.Tables[0].Rows[i]["CostCenter"]))
                            PPFData.CostCenterNumberByZone = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        PPFData.IsActive = Convert.ToString(ds.Tables[0].Rows[i]["IsActive"]);
                        PPFDataList.Add(PPFData);
                    }
                }

                result = PPFDataList.ToList();
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
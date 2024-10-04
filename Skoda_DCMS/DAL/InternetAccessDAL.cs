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
    public class InternetAccessDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        //public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        //public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        //public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        //public async Task<dynamic> CreateInternetAccessRequest(System.Web.Mvc.FormCollection form, UserData user)
        public async Task<ResponseModel<object>> CreateInternetAccessRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            //dynamic result = new ExpandoObject();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);


            int RowId = 0;
            //Web web = _context.Web;
            string formShortName = "IA";
            string formName = "Internet Access Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                //result.one = 0;
                //result.two = 0;
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
                var specialRequest = form["chkSpecialRequestAccess"] ?? "";
                string drpRequestSubmissionFor = form["drpRequestSubmissionFor"];
                var EmployeeType = "";
                //if (drpRequestSubmissionFor == "Self")
                //{
                //    EmployeeType = Convert.ToString(form["chkEmployeeType"]);
                //}
                //else
                //{

                //    EmployeeType = Convert.ToString(form["chkOtherEmployeeType"]);
                //}
                if (drpRequestSubmissionFor == "Self")
                {
                    EmployeeType = Convert.ToString(form["chkEmployeeType"]);
                }
                else
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        EmployeeType = Convert.ToString(form["chkOtherEmployeeType"]);
                    }
                    else
                    {
                        EmployeeType = Convert.ToString(form["chkOtherNewEmployeeType"]);
                    }

                }
                //if (EmployeeType == "External")
                //{
                //    var response = await GetApprovalForInternetAccess(empNum, ccNum, loc, specialRequest);
                //    if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                //    {
                //        result.Status = 500;
                //        result.Message = response.Message;
                //        return result;
                //    }
                //    var approverIdList = response.Model;
                //}
                //else
                //{



                //}

                var response = await GetApprovalForInternetAccess(empNum, ccNum, loc, specialRequest, EmployeeType);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approverIdList = response.Model;

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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "InternetAccess"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 8));
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "InternetAccess"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"]));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 8));
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
                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData_IA(form, listName, formId, FormId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                //var newRow = userDetailsResponse.Model;
                RowId = Convert.ToInt32(userDetailsResponse.RowId);


                DataSet ds = new DataSet();
                SqlDataAdapter adapter_form1 = new SqlDataAdapter();
                SqlCommand cmd = new SqlCommand();
                var con1 = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_SaveDataInInternetAccess", con1);
                cmd.Parameters.Add(new SqlParameter("@RowId", RowId));
                cmd.Parameters.Add(new SqlParameter("@EmployeeRequestType", form["chkRequestType"]));
                cmd.Parameters.Add(new SqlParameter("@IsSpecialRequest", form["chkSpecialRequestAccess"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@MoreInformation", form["txtMoreInformation"] ?? ""));
                cmd.Parameters.Add(new SqlParameter("@TempFrom", form["txtTempFrom"] == "" ? null : form["txtTempFrom"]));
                cmd.Parameters.Add(new SqlParameter("@TempTo", form["txtTempTo"] == "" ? null : form["txtTempTo"]));
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"]));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter_form1.SelectCommand = cmd;
                con1.Open();
                adapter_form1.Fill(ds);
                con1.Close();


                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }
                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                #region Commnet
                //Task Entry in Approval Master List
                // var rowid = newRow.Id;

                //if (EmployeeType == "Internal")
                //{
                //    //List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                //    int level = 1;
                //    for (var i = 0; i < approverIdList.Count; i++)
                //    {
                //        //ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                //        //ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);
                //        SqlCommand cmd = new SqlCommand();
                //        SqlDataAdapter adapter = new SqlDataAdapter();
                //        DataSet ds = new DataSet();

                //        var con = new SqlConnection(sqlConString);
                //        cmd = new SqlCommand("USP_SaveSubmitterAndApplicantDetails", con);

                //        cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                //        cmd.Parameters.Add(new SqlParameter("@RowId", RowId));
                //        cmd.Parameters.Add(new SqlParameter("@ApproverUserName", approverIdList[i].ApproverUserName));
                //        cmd.Parameters.Add(new SqlParameter("@Designation", approverIdList[i].Designation));
                //        cmd.Parameters.Add(new SqlParameter("@Level", approverIdList[i].ApprovalLevel));
                //        cmd.Parameters.Add(new SqlParameter("@Logic", approverIdList[i].Logic));



                //        if (approverIdList[i].ApprovalLevel == 1)
                //        {
                //            approvalMasteritem["IsActive"] = 1;
                //        }
                //        else
                //        {
                //            approvalMasteritem["IsActive"] = 0;
                //        }

                //        if (approverIdList[i].ApprovalLevel == approverIdList.Max(x => x.ApprovalLevel))
                //        {
                //            approvalMasteritem["NextApproverId"] = 0;
                //        }
                //        else
                //        {
                //            //var currentApproverLevel = approverIdList[i].ApprovalLevel;
                //            //approvalMasteritem["NextApproverId"] = approverIdList.Any(x => x.ApprovalLevel == currentApproverLevel + 1) ? approverIdList.Where(x => x.ApprovalLevel == currentApproverLevel + 1).FirstOrDefault().ApproverUserName : "";
                //            approvalMasteritem["NextApproverId"] = 0;
                //        }

                //        approvalMasteritem["ApproverStatus"] = "Approved";

                //        approvalMasteritem["RunWorkflow"] = "No";

                //        approvalMasteritem["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                //        approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                //        approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                //        approvalMasteritem.Update();
                //        _context.Load(approvalMasteritem);
                //        _context.ExecuteQuery();

                //    }
                //    var updateRowResponse = UpdateDataRowIdInFormsList(rowid, formId);
                //    if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                //    {
                //        return updateRowResponse;
                //    }



                //}
                //else
                //{
                //    List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                //    int level = 1;
                //    for (var i = 0; i < approverIdList.Count; i++)
                //    {
                //        ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                //        ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);

                //        approvalMasteritem["FormId"] = formId;
                //        approvalMasteritem["RowId"] = rowid;
                //        approvalMasteritem["ApproverUserName"] = approverIdList[i].ApproverUserName;
                //        approvalMasteritem["Designation"] = approverIdList[i].Designation;
                //        approvalMasteritem["Level"] = approverIdList[i].ApprovalLevel;
                //        approvalMasteritem["Logic"] = approverIdList[i].Logic;

                //        if (approverIdList[i].ApprovalLevel == 1)
                //        {
                //            approvalMasteritem["IsActive"] = 1;
                //        }
                //        else
                //        {
                //            approvalMasteritem["IsActive"] = 0;
                //        }

                //        if (approverIdList[i].ApprovalLevel == approverIdList.Max(x => x.ApprovalLevel))
                //        {
                //            approvalMasteritem["NextApproverId"] = 0;
                //        }
                //        else
                //        {
                //            //var currentApproverLevel = approverIdList[i].ApprovalLevel;
                //            //approvalMasteritem["NextApproverId"] = approverIdList.Any(x => x.ApprovalLevel == currentApproverLevel + 1) ? approverIdList.Where(x => x.ApprovalLevel == currentApproverLevel + 1).FirstOrDefault().ApproverUserName : "";
                //            approvalMasteritem["NextApproverId"] = 0;
                //        }

                //        approvalMasteritem["ApproverStatus"] = "Pending";

                //        approvalMasteritem["RunWorkflow"] = "No";

                //        approvalMasteritem["BusinessNeed"] = form["txtBusinessNeed"] ?? "";

                //        approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                //        approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                //        approvalMasteritem.Update();
                //        _context.Load(approvalMasteritem);
                //        _context.ExecuteQuery();

                //    }


                //    //Approval Tracking
                //    result.Status = 200;
                //    result.Message = formId.ToString();
                //    var approverIdList = response.Model;
                //var approvalResponse = await SaveDataApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);
                //    if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                //    {
                //        return approvalResponse;
                //    }

                //    var updateRowResponse = UpdateDataRowIdInFormsList(rowid, formId);
                //    if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                //    {
                //        return updateRowResponse;
                //    }
                //}
                #endregion

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

                if (EmployeeType == "Internal")
                {
                    //status Approved
                    //DataSet ds2 = new DataSet();
                    //SqlDataAdapter adapter_form2 = new SqlDataAdapter();
                    //SqlCommand cmd2 = new SqlCommand();
                    //var con2 = new SqlConnection(sqlConString);
                    //cmd2 = new SqlCommand("UpdateFormStatusByFormId", con2);
                    //cmd2.Parameters.Add(new SqlParameter("@Forms", formId));
                    //cmd2.Parameters.Add(new SqlParameter("@FStatus", "Approved"));
                    //cmd2.CommandType = CommandType.StoredProcedure;
                    //adapter_form2.SelectCommand = cmd2;
                    //con2.Open();
                    //adapter_form2.Fill(ds2);
                    //con2.Close();



                    //SqlDataAdapter adapter1 = new SqlDataAdapter();
                    //SqlCommand cmd1 = new SqlCommand();
                    //DataSet ds3 = new DataSet();
                    //con = new SqlConnection(sqlConString);
                    //cmd1 = new SqlCommand("USP_UpdateApprovalMatrixIA", con);
                    //cmd1.Parameters.Add(new SqlParameter("@FormId", formId));
                    //cmd1.Parameters.Add(new SqlParameter("@RowId", RowId));
                    //cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                    //cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Approved"));
                    //cmd1.Parameters.Add(new SqlParameter("@Comment", ""));
                    //cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                    //cmd1.CommandType = CommandType.StoredProcedure;
                    //adapter1.SelectCommand = cmd1;
                    //con.Open();
                    //adapter1.Fill(ds3);
                    //con.Close();
                    #region Comment
                    //List formslist = _context.Web.Lists.GetByTitle("Forms");
                    //ListItem newItemApprove = formslist.GetItemById(formId);
                    //newItemApprove.RefreshLoad();
                    //_context.ExecuteQuery();
                    //newItemApprove["Status"] = "Approved";
                    //newItemApprove.Update();
                    //_context.Load(newItemApprove);
                    //_context.ExecuteQuery();
                    #endregion
                }
                result.Status = 200;
                result.Message = formId.ToString();
                string ActionTypeFormail = "";
                if (EmployeeType == "Internal" && IsResubmit == false)
                {
                    var emailData = new EmailDataModel()
                    {
                        FormId = formId.ToString(),
                        Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                        Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                        UniqueFormName = formShortName,
                        Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                        OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                        FormName = formName,
                        CurrentUser = user
                    };
                    var emailService = new EmailService();
                    emailService.SendMail(emailData);

                }
                else if (IsResubmit == true)
                {
                    var emailData = new EmailDataModel()
                    {
                        FormId = formId.ToString(),
                        Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                        Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                        UniqueFormName = formShortName,
                        Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                        OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                        FormName = formName,
                        CurrentUser = user
                    };
                    var emailService = new EmailService();
                    emailService.SendMail(emailData);


                }
                else
                {
                    var emailData = new EmailDataModel()
                    {
                        FormId = formId.ToString(),
                        Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                        Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                        UniqueFormName = formShortName,
                        Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                        OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                        FormName = formName,
                        CurrentUser = user
                    };
                    var emailService = new EmailService();
                    emailService.SendMail(emailData);


                }




                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                //result.one = 0;
                //result.two = 0;

                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                return result;
            }

        }
        /// <summary>
        /// InternetAccess-It is used for viewing data.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetInternetAccessRequestDetails(int rowId, int formId)
        {
            dynamic internet = new ExpandoObject();
            try
            {
                #region Comment
                //GlobalClass gc = new GlobalClass();
                //var user = gc.GetCurrentUser();
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response = await client.GetAsync("_api/web/lists/GetByTitle('InternetAccess')/items?$select=ID,EmployeeType,EmployeeCode," +
                //    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeLocation,EmployeeDepartment,EmployeeRequestType," +
                //    "IsSpecialRequest,TempFrom,TempTo,BusinessNeed,ExternalOrganizationName,ExternalOtherOrganizationName,RequestSubmissionFor,MoreInformation,EmailId," +
                //    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                //    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName," +
                //    "FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                //var responseText = await response.Content.ReadAsStringAsync();
                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var result = JsonConvert.DeserializeObject<InternetAcessModel>(responseText, settings);
                //    internet.one = result.List.InternetList;
                //}
                #endregion
                List<InternetAcessData> item = new List<InternetAcessData>();
                InternetAcessData model = new InternetAcessData();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("ViewIAFDetails", con);
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
                        model.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        if (dt.Rows[i]["Created"] != DBNull.Value)
                            item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            item1.Created = Convert.ToDateTime(dt.Rows[i]["Created"]);
                        model.FormID = item1;
                        model.FormId = item1;           // For PDF 
                        Author AI = new Author();
                        AI.Title = Convert.ToString(dt.Rows[i]["SubmitterUserName"]);
                        model.Author = AI;
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = Convert.ToString(dt.Rows[0]["EmployeeContactNo"]);
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
                        model.EmployeeRequestType = Convert.ToString(dt.Rows[0]["EmployeeRequestType"]);
                        model.BusinessNeed = Convert.ToString(dt.Rows[0]["BusinessNeed"]);
                        model.IsSpecialRequest = Convert.ToString(dt.Rows[0]["IsSpecialRequest"]);
                        model.MoreInformation = Convert.ToString(dt.Rows[0]["MoreInformation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        DateTime? TempFrom = null;
                        DateTime? TempTo = null;
                        if (dt.Rows[0]["TempFrom"] != DBNull.Value)
                            TempFrom = Convert.ToDateTime(dt.Rows[0]["TempFrom"]);
                        model.TempFrom = TempFrom;
                        if (dt.Rows[0]["TempTo"] != DBNull.Value)
                            TempTo = Convert.ToDateTime(dt.Rows[0]["TempTo"]);
                        model.TempTo = TempTo;
                        item.Add(model);
                    }
                }
                internet.one = item;
                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                internet.three = r1.Model;
                internet.four = r2.Model;
                #region Comment
                //approval start
                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,ApproverName,ApproverUserName,Comment,NextApproverId,Level,Logic,TimeStamp,Designation,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                //var responseText2 = await response2.Content.ReadAsStringAsync();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                //if (modelData.Node.Data.Count > 0)
                //{
                //    var client3 = new HttpClient(handler);
                //    client3.BaseAddress = new Uri(conString);
                //    client3.DefaultRequestHeaders.Accept.Clear();
                //    client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //    var names = new List<string>();
                //    var responseText3 = "";

                //    var items = modelData.Node.Data;
                //    var idString = "";


                //    //AD Code
                //    ListDAL obj = new ListDAL();
                //    for (int i = 0; i < items.Count; i++)
                //    {
                //        //string objectSid = user.ObjectSid;
                //        //string approverId = items[i].ApproverUserName;
                //        //string appName = obj.GetApproverNameFromAD(approverId);
                //        string appName = items[i].ApproverName;
                //        names.Add(appName);
                //    }
                //    //AD Code



                //    if (items.Count == names.Count)
                //    {
                //        for (int i = 0; i < items.Count; i++)
                //        {
                //            items[i].UserName = names[i];
                //        }
                //    }

                //    items = items.OrderBy(x => x.UserLevel).ToList();

                //    if (!string.IsNullOrEmpty(responseText2))
                //    {
                //        dynamic data2 = Json.Decode(responseText2);
                //        internet.two = data2.d.results;
                //        internet.three = items;
                //    }

                //}
                //else
                //{
                //    internet.two = new List<string>();
                //    internet.three = new List<string>();
                //}
                #endregion
                //approval end


            }
            catch (Exception ex)
            {
                con.Close();
                Log.Error(ex.Message, ex);
                return 0;
            }
            return internet;
        }
        /// <summary>
        /// InternetAccess-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForInternetAccess(long empNum, long ccNum, string loc, string specialRequest, string EmployeeType)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_InternetAccessApproval_forCR", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empLoc", loc));
                cmd.Parameters.Add(new SqlParameter("@specialRequest", specialRequest));
                cmd.Parameters.Add(new SqlParameter("@EmployeeType", EmployeeType));
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
                //client.Timeout = TimeSpan.FromSeconds(10);
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
                //  return appList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
                //return new List<ApprovalMatrix>();
            }

        }

    }
}
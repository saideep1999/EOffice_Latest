using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Skoda_DCMS.Models;
using Skoda_DCMS.Helpers;
using System.Data.SqlClient;
using System.Data;
using Microsoft.SharePoint.Client;
using System.Dynamic;
using System.Xml;
using System.Web.Helpers;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Models.CommonModels;
using static Skoda_DCMS.Helpers.Flags;
using Skoda_DCMS.Extension;

namespace Skoda_DCMS.DAL
{
    public class DoorAccessRequestDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        //public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        //public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        //public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        /// <summary>
        /// DoorAccessRequest-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> CreateDoorAccessRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            //int RowId = 0;
            //Web web = _context.Web;
            string formShortName = "DARF";
            string formName = "Door Access Request Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }

            DateTime tempDate = new DateTime(1500, 1, 1);
            //int formId = 0;
            int formId = Convert.ToInt32(form["FormID"]);
            int RowId = 0;
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            bool IsResubmit = formId == 0 ? false : true;

            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                long empNum = requestSubmissionFor == "Self" ? user.EmpNumber : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));
                long ccNum = requestSubmissionFor == "Self" ? user.CostCenter : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                var loc = requestSubmissionFor == "Self" ? form["ddEmpLocation"]
                     : (otherEmpType == "SAVWIPLEmployee" ? form["ddOtherEmpLocation"]
                         : (otherEmpType == "Others" ? form["ddOtherNewEmpLocation"] : ""));

                string pattern = ",";

                var count = Convert.ToInt32(form["totalrows"]);
                var drpId = string.Empty;
                var locName = string.Empty;
                var deptName = string.Empty;
                var doorName = string.Empty;

                for (var i = 1; i < count + 1; i++)
                {
                    drpId += (form["drpId" + i + ""] == "" ? "0" : form["drpId" + i + ""]) + pattern;
                    locName += form["drpLocation" + i + ""] + ",";
                    deptName += form["drpDepartment" + i + ""] + pattern;
                    doorName += form["drpDoorName" + i + ""] + pattern;
                }
                var drpIds = drpId != null ? drpId.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                drpIds = drpIds.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var locNames = locName != null ? locName.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                locNames = locNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var deptNames = deptName != null ? deptName.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                deptNames = deptNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var doorNames = doorName != null ? doorName.Split(new string[] { pattern }, StringSplitOptions.None).ToList() : new List<string>();
                doorNames = doorNames.Where(s => !string.IsNullOrEmpty(s)).ToList();

                List<string> doorNameList = new List<string>();
                List<int> doorIDList = new List<int>();
                string[] nameList;
                foreach (var item in doorNames)
                {
                    nameList = item.Split('+');
                    var id = Convert.ToInt32(nameList[0]);
                    var name = nameList[1];
                    doorNameList.Add(name);
                    doorIDList.Add(id);
                }
                string commaString = string.Join(",", doorIDList);

                var response = await GetApprovalForDoorAccess(empNum, ccNum, loc, commaString);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }


                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();
                #region Forms
                //if (FormId == 0)
                //{

                //    List FormsList = web.Lists.GetByTitle("Forms");
                //    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                //    ListItem item = FormsList.AddItem(itemCreated);
                //    item["FormName"] = formName;
                //    item["UniqueFormName"] = formShortName;
                //    item["FormParentId"] = 12;
                //    item["ListName"] = listName;
                //    item["SubmitterUserName"] = user.UserName;
                //    item["Status"] = "Submitted";
                //    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //    item["Department"] = user.Department;
                //    item["ControllerName"] = "DoorAccessRequest";
                //    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                //    if (requestSubmissionFor == "Self")
                //    {
                //        item["Location"] = form["ddEmpLocation"];
                //    }
                //    else
                //    {
                //        if (otherEmpType == "SAVWIPLEmployee")
                //        {
                //            item["Location"] = form["ddOtherEmpLocation"];
                //        }
                //        else
                //        {
                //            item["Location"] = form["ddOtherNewEmpLocation"];
                //        }
                //    }
                //    item.Update();
                //    _context.Load(item);
                //    _context.ExecuteQuery();

                //    formId = item.Id;
                #endregion


                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 12));
                cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "DoorAccessRequest"));
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
                cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"] ?? ""));
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
                #region Comment
                //}
                //else
                //{
                //    List flist = _context.Web.Lists.GetByTitle("Forms");
                //    ListItem item = flist.GetItemById(FormId);
                //    item["FormName"] = formName;
                //    item["UniqueFormName"] = formShortName;
                //    item["FormParentId"] = 12;
                //    item["ListName"] = listName;
                //    item["SubmitterUserName"] = user.UserName;
                //    item["Status"] = "Resubmitted";
                //    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //    item["Department"] = user.Department;
                //    item["ControllerName"] = "DoorAccessRequest";
                //    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                //    if (requestSubmissionFor == "Self")
                //    {
                //        item["Location"] = form["ddEmpLocation"];
                //    }
                //    else
                //    {
                //        if (otherEmpType == "SAVWIPLEmployee")
                //        {
                //            item["Location"] = form["ddOtherEmpLocation"];
                //        }
                //        else
                //        {
                //            item["Location"] = form["ddOtherNewEmpLocation"];
                //        }
                //    }
                //    item.Update();
                //    _context.Load(item);
                //    _context.ExecuteQuery();
                //    formId = item.Id;

                //    ListDAL dal = new ListDAL();
                //    var resubmitResult = await dal.ResubmitUpdate(formId);

                //    if (AppRowId != 0)
                //    {
                //        List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                //        ListItem alistItem = listApprovalMaster.GetItemById(AppRowId);
                //        alistItem["ApproverStatus"] = "Resubmitted";
                //        alistItem["IsActive"] = 0;
                //        alistItem.Update();
                //        _context.Load(alistItem);
                //        _context.ExecuteQuery();
                //    }
                //}
                #endregion
                //ApplicantDataModel AD = new ApplicantDataModel();
                //var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(web, AD, listName, formId);
                //if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                //{
                //    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                //}
                #region Comment
                //var newRow = userDetailsResponse.Model;
                //newRow["BusinessNeed"] = form["txtBusinessNeed"];
                //newRow["IDCardNumber"] = form["txtIDCardNumber"];
                //newRow["FormID"] = formId;
                //newRow.Update();
                //_context.Load(newRow);
                //_context.ExecuteQuery();

                //result.Status = 200;
                //result.Message = formId.ToString();

                //RowId = newRow.Id;

                #endregion
                //RowId = Convert.ToInt32(userDetailsResponse.RowId);

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("SaveDoorAccessRequestData", con);
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", form["txtBusinessNeed"] == null ? "" : form["txtBusinessNeed"]));
                cmd.Parameters.Add(new SqlParameter("@IDCardNumber", form["IDCardNumber"]));
                cmd.Parameters.Add(new SqlParameter("@FormID", Convert.ToString(formId)));
                cmd.Parameters.Add(new SqlParameter("@RowId", RowId));

                cmd.Parameters.Add(new SqlParameter("@EmployeeType", form["chkEmployeeType"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCode", form["txtEmployeeCode"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeCCCode", form["txtCostcenterCode"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeUserId", form["txtUserId"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeName", form["txtEmployeeName"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDepartment", form["txtDepartment"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeContactNo", form["txtContactNo"]));
                cmd.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                cmd.Parameters.Add(new SqlParameter("@ExternalOrganizationName", form["txtExternalOrganizationName"] ?? ""));
                //cmd.Parameters.Add(new SqlParameter("@ExternalOtherOrganizationName", form["txtExternalOrganizationName ?? ""));
                cmd.Parameters.Add(new SqlParameter("@EmployeeLocation", form["ddEmpLocation"]));
                cmd.Parameters.Add(new SqlParameter("@EmployeeDesignation", form["chkEmployeeType"] == "External" ? "Team Member" : form["ddEmpDesignation"]));
                cmd.Parameters.Add(new SqlParameter("@TriggerCreateWorkflow", ""));
                cmd.Parameters.Add(new SqlParameter("@RequestSubmissionFor", form["drpRequestSubmissionFor"]));
                cmd.Parameters.Add(new SqlParameter("@OnBehalfOption", otherEmpType));
                cmd.Parameters.Add(new SqlParameter("@EmployeeEmailId", user.Email));
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", form["chkOtherEmployeeType"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", form["txtOtherEmployeeCode"].ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", form["txtOtherCostcenterCode"].ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", form["txtOtherContactNo"].ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", form["txtOtherUserId"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", form["txtOtherEmployeeName"]));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", form["txtOtherDepartment"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", form["ddOtherEmpLocation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", form["chkOtherEmployeeType"] == "External" ? "Team Member" : form["ddOtherEmpDesignation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", form["txtOtherExternalOrganizationName"] ?? ""));
                        //cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", form["txtOtherExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", form["txtOtherEmailId"] ?? ""));
                    }
                    else
                    {
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeType", form["chkOtherNewEmployeeType"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCode", form["txtOtherNewEmployeeCode"].ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeCCCode", form["txtOtherNewCostcenterCode"].ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeContactNo", form["txtOtherNewContactNo"].ToString() ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeUserId", form["txtOtherNewUserId"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeName", form["txtOtherNewEmployeeName"]));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDepartment", form["txtOtherNewDepartment"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeLocation", form["ddOtherNewEmpLocation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeDesignation", form["chkOtherNewEmployeeType"] == "External" ? "Team Member" : form["ddOtherNewEmpDesignation"] ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherExternalOrganizationName", form["txtOtherNewExternalOrganizationName"] ?? ""));
                        //cmd.Parameters.Add(new SqlParameter("@OtherExternalOtherOrgName", form["txtOtherNewExternalOrganizationName ?? ""));
                        cmd.Parameters.Add(new SqlParameter("@OtherEmployeeEmailId", form["txtOtherNewEmailId"] ?? ""));
                    }
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
                        RowId = Convert.ToInt32(ds.Tables[0].Rows[i]["RowId"]);
                        result.Status = 200;
                        result.Message = RowId.ToString();
                    }
                }


                #region COMMENT
                //List AccessDoorDetailsList = web.Lists.GetByTitle("AccessDoorDetails");

                //for (int i = 0; i < locNames.Count; i++)
                //{
                //    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                //    ListItem newItem = AccessDoorDetailsList.AddItem(itemCreate);
                //    newItem["DoorAccessReqId"] = RowId;
                //    newItem["Location"] = locNames[i] ?? "";
                //    newItem["DoorDepartment"] = deptNames[i] ?? "";
                //    newItem["DoorName"] = doorNameList[i] ?? "";
                //    newItem["DoorID"] = doorIDList[i];
                //    newItem["FormID"] = formId;
                //    newItem.Update();
                //    _context.ExecuteQuery();
                //}
                #endregion
                for (int i = 0; i < locNames.Count; i++)
                {
                    DataSet ds1 = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveAccessDoorDataList", con);
                    cmd_form.Parameters.Add(new SqlParameter("@Id", drpIds == null || drpIds.Count == 0 || drpIds[i] == "0" ? 0 : Convert.ToInt64(drpIds[i])));
                    cmd_form.Parameters.Add(new SqlParameter("@DoorAccessReqId", RowId));
                    cmd_form.Parameters.Add(new SqlParameter("@Location", locNames[i] ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@DoorDepartment", deptNames[i] ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@DoorName", doorNameList[i] ?? ""));
                    cmd_form.Parameters.Add(new SqlParameter("@DoorID", Convert.ToInt64(doorIDList[i])));
                    cmd_form.Parameters.Add(new SqlParameter("@FormID", Convert.ToInt64(formId)));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds1);
                    con.Close();

                    if (ds1.Tables[0].Rows.Count > 0 && ds1.Tables[0] != null)
                    {
                        for (int j = 0; j < ds1.Tables[0].Rows.Count; j++)
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

                    

                    //List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                    //ListItem alistItem = listApprovalMaster.GetItemById(AppRowId);
                    //alistItem["ApproverStatus"] = "Resubmitted";
                    //alistItem["IsActive"] = 0;
                    //alistItem.Update();
                    //_context.Load(alistItem);
                    //_context.ExecuteQuery();
                }
                #region COMMENT
                //var approvalResponse = await SaveApprovalMasterData(approverIdList, form.BusinessNeed ?? "", RowId, formId);

                //if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                //{
                //    return approvalResponse;
                //}
                //List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                #endregion
                var approverIdList = response.Model;
                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, form["BusinessNeed"] ?? "", RowId, formId);
                //for (var i = 0; i < approverIdList.Count; i++)
                //{
                //    DataSet ds2 = new DataSet();
                //    SqlCommand cmd_Approver = new SqlCommand();
                //    SqlDataAdapter adapter_App = new SqlDataAdapter();
                //    con = new SqlConnection(sqlConString);
                //    cmd_Approver = new SqlCommand("USP_SaveApproverDetails", con);
                //    cmd_Approver.Parameters.Add(new SqlParameter("@FormID", formId));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@RowId", RowId));
                //    if (approverIdList[i].ApprovalLevel == 1)
                //    {
                //        cmd_Approver.Parameters.Add(new SqlParameter("@IsActive", 1));
                //    }
                //    else
                //    {
                //        cmd_Approver.Parameters.Add(new SqlParameter("@IsActive", "0"));
                //    }

                //    cmd_Approver.Parameters.Add(new SqlParameter("@NextAppId", DBNull.Value));

                //    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverStatus", "Pending"));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@Department", ""));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@FormParentId", 12));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@ControllerName", "DoorAccessRequest"));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@CreatedBy", approverIdList[i].FName));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@Email", approverIdList[i].EmailId));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@BusinessNeed", form["BusinessNeed"] ?? ""));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@Level", approverIdList[i].ApprovalLevel));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@Logic", approverIdList[i].Logic));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@Designation", approverIdList[i].Designation));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", approverIdList[i].DelegatedByEmpNum));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverUserName", approverIdList[i].ApproverUserName));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@ExtraDetails", approverIdList[i].ExtraDetails));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@AssistantForEmployeeUserName", approverIdList[i].AssistantForEmpUserName));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@RelationId", approverIdList[i].RelationId));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@RelationWith", approverIdList[i].RelationWith));
                //    cmd_Approver.Parameters.Add(new SqlParameter("@ApproverName", approverIdList[i].ApproverName));
                //    cmd_Approver.CommandType = CommandType.StoredProcedure;
                //    adapter_App.SelectCommand = cmd_Approver;
                //    con.Open();
                //    adapter_App.Fill(ds2);
                //    con.Close();
                //}
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
            }
            return result;
        }

        /// <summary>
        /// DoorAccess-It is used for viewing Door Access form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetDoorAccessDetails(int rowId, int formId)
        {
            dynamic doorAccess = new ExpandoObject();
            List<DoorAccessRequestData> MainList = new List<DoorAccessRequestData>();
            List<SelectedAcsessDoorDto> OtherList = new List<SelectedAcsessDoorDto>();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                #region Comment
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response = await client.GetAsync("_api/web/lists/GetByTitle('DoorAccessRequestForm')/items?$select=*,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                //var responseText = await response.Content.ReadAsStringAsync();

                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var result = JsonConvert.DeserializeObject<DoorAccessRequestModel>(responseText, settings);
                //    MainList = result.List.DoorAccessRequestList;
                //}
                //doorAccess.one = MainList;
                #endregion

                List<DoorAccessRequestData> item = new List<DoorAccessRequestData>();
                DoorAccessRequestData model = new DoorAccessRequestData();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewDARFDetails", con);
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
                        model.AppRowId = Convert.ToInt32(dt.Rows[i]["ID"]);
                        model.EmployeeType = Convert.ToString(dt.Rows[0]["EmployeeType"]);
                        model.EmployeeCode = Convert.ToInt64(dt.Rows[0]["EmployeeCode"]);
                        model.EmployeeCCCode = Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]);
                        model.EmployeeUserId = Convert.ToString(dt.Rows[0]["EmployeeUserId"]);
                        model.EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]);
                        model.EmployeeDepartment = Convert.ToString(dt.Rows[0]["EmployeeDepartment"]);
                        model.EmployeeContactNo = dt.Rows[0]["EmployeeContactNo"] != DBNull.Value || dt.Rows[0]["EmployeeContactNo"] != "0" ? Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]) : 0;
                        model.ExternalOrganizationName = Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]);
                        model.EmployeeLocation = Convert.ToString(dt.Rows[0]["EmployeeLocation"]);
                        model.EmployeeDesignation = Convert.ToString(dt.Rows[0]["EmployeeDesignation"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.OnBehalfOption = Convert.ToString(dt.Rows[0]["OnBehalfOption"]);
                        model.OtherEmployeeType = Convert.ToString(dt.Rows[0]["OtherEmployeeType"]);
                        model.OtherEmployeeCode = dt.Rows[0]["OtherEmployeeCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCode"] != "0" && dt.Rows[0]["OtherEmployeeCode"] != "" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]) : 0;
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null && dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
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
                        model.IDCardNumber = Convert.ToString(dt.Rows[0]["IDCardNumber"]);
                        item.Add(model);
                    }
                }
                doorAccess.one = item;
                #region COMMENT
                //approval start
                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('AccessDoorDetails')/items?$select=*&$filter=(DoorAccessReqId eq '" + rowId + "')");
                //var responseText2 = await response2.Content.ReadAsStringAsync();
                //var result1 = JsonConvert.DeserializeObject<SelectedAcsessDoorModel>(responseText2);
                //OtherList = result1.List.AccessDoorList;
                //doorAccess.two = OtherList;
                #endregion

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewDARFormDataList", con);
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
                        SelectedAcsessDoorDto model1 = new SelectedAcsessDoorDto();
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(ds1.Rows[i]["FormID"]);
                        model1.FormID = item1;
                        model1.ID = Convert.ToInt64(ds1.Rows[i]["Id"]);
                        model1.DoorAccessReqId = Convert.ToString(ds1.Rows[i]["DoorAccessReqId"]);
                        model1.Location = Convert.ToString(ds1.Rows[i]["Location"]);
                        model1.DoorDepartment = Convert.ToString(ds1.Rows[i]["DoorDepartment"]);
                        model1.DoorName = Convert.ToString(ds1.Rows[i]["DoorName"]);
                        model1.DoorID = ds1.Rows[0]["DoorID"] != DBNull.Value && ds1.Rows[0]["DoorID"] != "0" ? Convert.ToInt32(ds1.Rows[i]["DoorID"]) : 0;
                        OtherList.Add(model1);

                    }

                }
                doorAccess.two = OtherList;
                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                doorAccess.three = r1.Model;
                doorAccess.four = r2.Model;
                //approval start
                //var client3 = new HttpClient(handler);
                //client3.BaseAddress = new Uri(conString);
                //client3.DefaultRequestHeaders.Accept.Clear();
                //client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response3 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,Level,ApproverUserName,Logic,TimeStamp,Designation,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                //var responseText3 = await response3.Content.ReadAsStringAsync();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText3, settings);

                //if (modelData.Node.Data.Count > 0)
                //{
                //    var client4 = new HttpClient(handler);
                //    client4.BaseAddress = new Uri(conString);
                //    client4.DefaultRequestHeaders.Accept.Clear();
                //    client4.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //    var names = new List<string>();
                //    var responseText4 = "";

                //    var items = modelData.Node.Data;
                //    var idString = "";

                //    if (adCode.ToLower() == "yes")
                //    {
                //        //AD Code
                //        ListDAL obj = new ListDAL();
                //        for (int i = 0; i < items.Count; i++)
                //        {
                //            //string objectSid = user.ObjectSid;
                //            string approverId = items[i].ApproverUserName;
                //            string appName = obj.GetApproverNameFromAD(approverId);
                //            names.Add(appName);
                //        }
                //        //AD Code
                //    }
                //    else
                //    {
                //        //Local Code:- Sharepoint Code
                //        for (int i = 0; i < items.Count; i++)
                //        {
                //            var id = items[i];
                //            idString = $"Id eq '{id.ApproverUserName}'";
                //            items[i].UserLevel = i + 1;//
                //            var response4 = await client4.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + idString + ")");
                //            responseText4 = await response4.Content.ReadAsStringAsync();

                //            dynamic data4 = Json.Decode(responseText4);

                //            if (data4.Count != 0)
                //            {
                //                //names = new List<string>();
                //                foreach (var name in data4.d.results)
                //                {
                //                    names.Add(name.Title as string);
                //                }
                //            }
                //        }
                //        //Local Code:- Sharepoint Code
                //    }

                //    if (items.Count == names.Count)
                //    {
                //        for (int i = 0; i < items.Count; i++)
                //        {
                //            items[i].UserName = names[i];
                //        }
                //    }

                //    items = items.OrderBy(x => x.UserLevel).ToList();

                //    if (!string.IsNullOrEmpty(responseText3))
                //    {
                //        dynamic data3 = Json.Decode(responseText3);
                //        doorAccess.three = data3.d.results;
                //        doorAccess.four = items;
                //    }
                //}
                //else
                //{
                //    doorAccess.two = new List<string>();
                //    doorAccess.three = new List<string>();
                //    doorAccess.four = new List<string>();
                //}
                //approval end
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return doorAccess;
        }

        //public List<string> GetLocations()
        //{
        //    List<string> locationList = new List<string>();
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_GetLocations", con);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            { 
        //                locationList.Add(Convert.ToString(ds.Tables[0].Rows[i]["LocationName"]).Trim());
        //            }
        //        }               
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //    }
        //    return locationList;
        //}

        //public List<string> GetDepartments(string loc)
        //{
        //    List<string> departmentList = new List<string>();
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_GetDoorAccessDepartments", con);
        //        cmd.Parameters.Add(new SqlParameter("@loc", loc));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                departmentList.Add(Convert.ToString(ds.Tables[0].Rows[i]["Department"]).Trim());
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //    }
        //    return departmentList;
        //}

        //public List<LocationData> GetAccessDoors(string dept)
        //{
        //    List<LocationData> locationList = new List<LocationData>();
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_GetAccessDoors", con);
        //        cmd.Parameters.Add(new SqlParameter("@dept", dept));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                LocationData ld = new LocationData();
        //                ld.LocationId = Convert.ToInt32(ds.Tables[0].Rows[i]["DoorID"]);
        //                ld.LocationName = Convert.ToString(ds.Tables[0].Rows[i]["DoorName"]).Trim();
        //                locationList.Add(ld);
        //            }
        //        }
        //        return locationList; 
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //        return new List<LocationData>();
        //    }

        //}

        public List<AccessDoorListModel> GetAccessDoorListData()
        {
            List<AccessDoorListModel> accessDoorList = new List<AccessDoorListModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetAccessDoorListData", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        AccessDoorListModel model = new AccessDoorListModel();
                        // model.ID = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        model.LocationName = Convert.ToString(ds.Tables[0].Rows[i]["LocationName"]).Trim();
                        model.Department = Convert.ToString(ds.Tables[0].Rows[i]["Department"]).Trim();
                        model.DoorName = Convert.ToString(ds.Tables[0].Rows[i]["DoorName"]).Trim();
                        // model.EmailID = Convert.ToString(ds.Tables[0].Rows[i]["EmailID"]).Trim();
                        model.DoorID = Convert.ToInt32(ds.Tables[0].Rows[i]["DoorID"]);
                        accessDoorList.Add(model);
                    }
                }
                return accessDoorList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<AccessDoorListModel>();
            }
        }
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForDoorAccess(long empNum, long ccNum, string loc, string doorIds)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_DoorAccessApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", empNum));
                cmd.Parameters.Add(new SqlParameter("@CCNum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@loc", loc));
                cmd.Parameters.Add(new SqlParameter("@doorIds", doorIds));

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
                        app.LogicId = Convert.ToInt64(ds.Tables[0].Rows[i]["LogicId"]);
                        app.LogicWith = Convert.ToInt64(ds.Tables[0].Rows[i]["LogicWith"]);
                        long rel = 0;
                        if (ds.Tables[0].Rows[0]["RelationId"].ToString() == "")
                        {
                            rel = 0;
                        }
                        else
                        {
                            rel = Convert.ToInt64(ds.Tables[0].Rows[i]["RelationId"]);
                        }
                        app.RelationId = rel;
                        app.RelationWith = Convert.ToInt64(ds.Tables[0].Rows[i]["RelationWith"]);
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

    }
}
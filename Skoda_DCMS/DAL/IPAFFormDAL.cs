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


    public class IPAFFormDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        //public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        //public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        //public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        public async Task<ResponseModel<object>> SaveIPAFForm(IPAFData model, UserData user, HttpPostedFileBase file)
        {

            ResponseModel<object> result = new ResponseModel<object>();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);



            int RowId = 0;
            //Web web = _context.Web;
            string formShortName = "IPAF";
            string formName = "IPAF Form";
            //var listName = "IPAFForm";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }
            //int prevItemId = Convert.ToInt32(model.FormSrId);
            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            formId = Convert.ToInt32(model.FormId);
            bool IsResubmit = formId == 0 ? false : true;
            int AppRowId = Convert.ToInt32(model.AppRowId);


            var requestSubmissionFor = model.RequestSubmissionFor;
            var otherEmpType = model.OnBehalfOption ?? "";
            bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
            //long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCCCode) : Convert.ToInt64(model.OtherNewCostcenterCode));
            //long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCode) : Convert.ToInt64(model.OtherNewEmployeeCode));
            //string empDes = isSelf ? model.EmployeeDesignation : (isSAVWIPL ? Convert.ToString(model.OtherEmployeeDesignation) : Convert.ToString(model.OtherNewEmpDesignation));

            long ccNum = user.CostCenter;
            long empNum = user.EmpNumber;
            string empDes = model.EmployeeDesignation;


            var response = await GetApprovalIPAFForm(empNum, ccNum, empDes, model);
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

                //if (formId == 0)
                //{
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
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "IPAFForm"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", model.BusinessJustification));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 46));
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
                //}
                //else
                //{
                //    ListDAL dal = new ListDAL();
                //    //var resubmitResult = await dal.ResubmitUpdate(formId);

                //    DataTable dt = new DataTable();
                //    var con_form = new SqlConnection(sqlConString);
                //    cmd_form = new SqlCommand("USP_updateFlagInApprovalMaster", con_form);
                //    cmd_form.Parameters.Add(new SqlParameter("@formId", formId));
                //    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", AppRowId));
                //    cmd_form.CommandType = CommandType.StoredProcedure;
                //    adapter_form.SelectCommand = cmd_form;
                //    con_form.Open();
                //    adapter_form.Fill(dt);
                //    con_form.Close();

                //    if (dt.Rows.Count > 0)
                //    {
                //        for (int i = 0; i < dt.Rows.Count; i++)
                //        {
                //            new ResponseModel<object> { Message = Convert.ToString(dt.Rows[i]["message"]), Status = Convert.ToInt32(dt.Rows[i]["Status"]) };
                //        }
                //    }
                //}

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData( model, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }

                RowId = Convert.ToInt32(userDetailsResponse.RowId);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);

                cmd = new SqlCommand("USP_UpdateDataIPAF", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId", userDetailsResponse.RowId));
                cmd.Parameters.Add(new SqlParameter("@RequestType", model.RequestType));
                //cmd.Parameters.Add(new SqlParameter("@RequestFromDate", model.RequestFromDate));
                //cmd.Parameters.Add(new SqlParameter("@RequestToDate", model.RequestToDate));
                if (model.RequestFromDate?.ToString("yyyy-MM-dd HH:mm:ss") == "0001-01-01 00:00:00")
                {
                    cmd.Parameters.Add(new SqlParameter("@RequestFromDate", DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@RequestFromDate", model.RequestFromDate?.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                if (model.RequestToDate?.ToString("yyyy-MM-dd HH:mm:ss") == "0001-01-01 00:00:00")
                {
                    cmd.Parameters.Add(new SqlParameter("@RequestToDate", DBNull.Value));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@RequestToDate", model.RequestToDate?.ToString("yyyy-MM-dd HH:mm:ss")));
                }

                cmd.Parameters.Add(new SqlParameter("@BusinessJustification", model.BusinessJustification));


                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                result.Status = 200;
                result.Message = formId.ToString();
               

                int SrNo = 1;
                foreach (var item in model.IPAFFormDataList)
                {
                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_SaveIPAFDataList", con);
                    cmd.Parameters.Add(new SqlParameter("@Applicationname", item.Applicationname));
                    cmd.Parameters.Add(new SqlParameter("@Applicationurl", item.Applicationurl));
                    cmd.Parameters.Add(new SqlParameter("@Applicationaccess", item.Applicationaccess));
                    cmd.Parameters.Add(new SqlParameter("@Accessgroup", item.Accessgroup));
                    cmd.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd.Parameters.Add(new SqlParameter("@SrNo", SrNo++));
                    cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                    cmd.Parameters.Add(new SqlParameter("@RowId", RowId));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(ds);
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



                    //List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                    //ListItem alistItem = listApprovalMaster.GetItemById(AppRowId);
                    //alistItem["ApproverStatus"] = "Resubmitted";
                    //alistItem["IsActive"] = 0;
                    //alistItem.Update();
                    //_context.Load(alistItem);
                    //_context.ExecuteQuery();
                }


                ////Task Entry in Approval Master List
                //var approvalResponse = await SaveApprovalMasterData(approverIdList, model.BusinessNeed ?? "", RowId, formId);

                //if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                //{
                //    return approvalResponse;
                //}

                //var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                //if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                //{
                //    return updateRowResponse;
                //}

                var approverIdList = response.Model;
                int isactive = 0;

                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, model.BusinessJustification ?? "", RowId, formId);
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
        public async Task<dynamic> GetTypeAcessbyLink(string ApplicationUrl)
        {
            List<IPAFData> list = new List<IPAFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();


                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetTypAcessbylink", con);
                cmd.Parameters.Add(new SqlParameter("@ApplicationURL", ApplicationUrl));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();



                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        IPAFData iPAFData = new IPAFData();
                        iPAFData.Accessgroup = Convert.ToString(ds.Tables[0].Rows[i]["AccessGroup"]);
                        list.Add(iPAFData);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }

        public async Task<dynamic> Getdropdata()
        {
            List<IPAFData> list = new List<IPAFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();


                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetIPAFdropdowndata", con);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();



                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        IPAFData iPAFData = new IPAFData();
                        
                        iPAFData.Applicationaccess = Convert.ToString(ds.Tables[0].Rows[i]["TypeAccess"]);
                        list.Add(iPAFData);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }

        public async Task<dynamic> getapplictionurl()
        {
            List<IPAFData> list = new List<IPAFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();


                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetIPAFApplicationURLdata", con);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();



                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        IPAFData iPAFData = new IPAFData();
                        iPAFData.Applicationurl = Convert.ToString(ds.Tables[0].Rows[i]["ApplicationURL"]);
                        
                        list.Add(iPAFData);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalIPAFForm(long empNum, long ccNum, string empDes, IPAFData model)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                ///string  TypeAccess = model.Accessgroup.Join(",").ToString();

                string TypeAcess = string.Join(",", model.IPAFFormDataList.Select(p => p.Applicationaccess.ToString()));
                string AppURL = string.Join(",", model.IPAFFormDataList.Select(p => p.Applicationurl.ToString()));

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetIPAFApprovers", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empDes", empDes));
                cmd.Parameters.Add(new SqlParameter("@ApplicationURL", AppURL));
                cmd.Parameters.Add(new SqlParameter("@TypeAcess", TypeAcess));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
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



        public async Task<dynamic> ViewIPAFFormData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();
                
                // List<IMACFormModel> item = new List<IMACFormModel>();

                List<IPAFData> item = new List<IPAFData>();
                List<IPAFData> IPAFTableDataList = new List<IPAFData>();
                IPAFData model = new IPAFData();
               
                IPAFResults iPAFResults = new IPAFResults();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewIPAFDetails", con);
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
                        //FormLookup item1 = new FormLookup();
                        //item1.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        //if (dt.Rows[i]["Created"] != DBNull.Value)
                        //    item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                        //model.FormID = item1;

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
                        model.BusinessJustification = Convert.ToString(dt.Rows[0]["BusinessJustification"]);
                        model.RequestType = Convert.ToString(dt.Rows[0]["RequestType"]);
                        model.RequestFromDate = Convert.ToDateTime(dt.Rows[0]["RequestFromDate"]);
                        model.RequestToDate = Convert.ToDateTime(dt.Rows[0]["RequestToDate"]);
                        model.CreatedDate = Convert.ToDateTime(dt.Rows[0]["Created"]);
                        item.Add(model);
                    }
                    
                }
                URCFData.one = item;


                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ViewIPAFFormDataList", con);
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

                        IPAFData model1 = new IPAFData();
                        model1.FormId = Convert.ToInt32(ds1.Rows[i]["FormId"]);
                        //model1.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                        model1.Applicationname = Convert.ToString(ds1.Rows[i]["Applicationname"]);
                        model1.Applicationurl = Convert.ToString(ds1.Rows[i]["Applicationurl"]);
                        model1.Applicationaccess = Convert.ToString(ds1.Rows[i]["Applicationaccess"]);
                        model1.Accessgroup = Convert.ToString(ds1.Rows[i]["Accessgroup"]);
                        IPAFTableDataList.Add(model1);
                    }
                }


                //iPAFResults.IPAFData = item;
                //iPAFResults.IPAFData = IPAFTableDataList;
                
                URCFData.Four = IPAFTableDataList;
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

        


    }
}
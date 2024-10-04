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
    public class OCRFDAL : CommonDAL
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

        public List<UserData> GetOCRFEmployeeDetails(string empCode)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetOCRFEmployee", con);
                cmd.Parameters.Add(new SqlParameter("@EmpCode", empCode));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        UserData user = new UserData();
                        user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        user.UserName = user.FirstName;

                        users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return users;
        }

        public OCRFData GetDetByReportingMgrTo(string otherEmpId)
        {
            OCRFData user = new OCRFData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetDetByReportingMgrTo", con);
                cmd.Parameters.Add(new SqlParameter("@EmployeeNumber", otherEmpId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {

                    user.UserId = Convert.ToInt32(ds.Tables[0].Rows[0]["EmployeeNumber"]);
                    user.FirstName = ds.Tables[0].Rows[0]["FirstName"].ToString();
                    user.LastName = ds.Tables[0].Rows[0]["LastName"].ToString();
                    user.EmployeeName = user.FirstName + " " + user.LastName;
                    user.CostCentreTo = ds.Tables[0].Rows[0]["CostCenter"].ToString();
                    //user.DepartmentTo = ds.Tables[0].Rows[0]["Department"].ToString();
                    //user.SubDepartmentTo = ds.Tables[0].Rows[0]["SubDepartment"].ToString();
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return user;
        }

        public OCRFData GetOCRFExistingEmployeeDetails(string otherEmpId)
        {
            OCRFData user = new OCRFData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetOCRFExistingEmpDet", con);
                cmd.Parameters.Add(new SqlParameter("@EmployeeNumber", otherEmpId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {

                    user.UserId = Convert.ToInt32(ds.Tables[0].Rows[0]["EmployeeNumber"]);
                    user.FirstName = ds.Tables[0].Rows[0]["FirstName"].ToString();
                    user.LastName = ds.Tables[0].Rows[0]["LastName"].ToString();
                    user.EmployeeName = user.FirstName + " " + user.LastName;

                    user.CostCentreFrom = ds.Tables[0].Rows[0]["CostCenter"].ToString();
                    user.DepartmentFrom = ds.Tables[0].Rows[0]["Department"].ToString();
                    user.SubDepartmentFrom = ds.Tables[0].Rows[0]["SubDepartment"].ToString();
                    user.ReportingManagerFrom = ds.Tables[0].Rows[0]["ManagerEmployeeNumber"].ToString() + " | " + ds.Tables[1].Rows[0]["FirstName"].ToString() + " " + ds.Tables[1].Rows[0]["LastName"].ToString();

                    user.DivisionFrom = ds.Tables[2].Rows[0]["DivName"].ToString();
                    user.DepartmentFrom = ds.Tables[3].Rows[0]["DeptName"].ToString();
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return user;
        }

        public List<UserData> GetCCDetails(string CCCode)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetOCRFCC", con);
                cmd.Parameters.Add(new SqlParameter("@CC", CCCode));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        UserData user = new UserData();
                        user.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        user.Department = ds.Tables[0].Rows[i]["Department"].ToString();
                        //user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        //user.EmployeeName = user.FirstName + " " + user.LastName;
                        //user.CostCentreFrom = ds.Tables[0].Rows[i]["CostCenter"].ToString();
                        //user.DepartmentFrom= ds.Tables[0].Rows[i]["Department"].ToString();
                        //user.SubDepartmentFrom = ds.Tables[0].Rows[i]["SubDepartment"].ToString();
                        //user.ReportingManagerFrom = ds.Tables[0].Rows[i]["ManagerEmployeeNumber"].ToString();
                        user.UserName = user.FirstName;
                        //user.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return users;
        }


        public async Task<ResponseModel<object>> SaveOCRF(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            string formShortName = "OCRF";
            string formName = "OCRF Form";
            string listName = string.Empty;

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;

            listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }

            try
            {
                var drpReqType = form["drpReqType"];
                var rdOnBehalfOption = form["rdOnBehalfOption"] ?? "";
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var selfdesignation = form["ddEmpDesignation"];

                var reportingManagerTo = form["txtReportingManagerTo"];
                var chkTypeOfChange = form["CostCenterChk"];
                var otherEmpType = form["chkEmployeeType"] ?? "";
                long txtEmployeeCode = Convert.ToInt32(form["txtEmployeeCode"]);
                long txtCostCenterNo = Convert.ToInt32(form["txtCostcenterCode"]);

                string[] txtCostCentreFromArry = form["txtCostCentreFrom"].Split('|');
                string txtCostCentreFrom = txtCostCentreFromArry[0];

                string[] txtCostCentreToArry = form["txtCostCentreTo"].Split('|');
                string txtCostCentreTo = txtCostCentreToArry[0];

                long txtOnBehalfEmpId = 0;
                long txtOnBehalfCostCenterNo = 0;
                var empLocation = "";
                empLocation = form["ddEmpLocation"];
                var onbehalfesignation = form["ddOtherEmpDesignation"];
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (rdOnBehalfOption == "SAVWIPLEmployee")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherCostcenterCode"]);
                        empLocation = form["ddOtherEmpLocation"];
                        onbehalfesignation = form["ddOtherEmpDesignation"];
                    }
                    else if (rdOnBehalfOption == "Others")
                    {
                        txtOnBehalfEmpId = Convert.ToInt32(form["txtOtherNewEmployeeCode"]);
                        txtOnBehalfCostCenterNo = Convert.ToInt32(form["txtOtherNewCostcenterCode"]);
                        empLocation = form["ddOtherNewEmpLocation"];
                        onbehalfesignation = form["ddOtherNewEmpDesignation"];
                    }
                }

                var response = await GetApprovalOCRF(user, requestSubmissionFor, txtEmployeeCode, txtCostCenterNo, txtOnBehalfEmpId, txtOnBehalfCostCenterNo, drpReqType, selfdesignation, onbehalfesignation, reportingManagerTo, chkTypeOfChange, empLocation, txtCostCentreFrom, txtCostCentreTo);

                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }

                var approverIdList = response.Model;
                int formId = 0;
                int FormId = Convert.ToInt32(form["FormId"]);
                int AppRowId = Convert.ToInt32(form["AppRowId"]);
                bool IsResubmit = FormId == 0 ? false : true;


                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 20;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "OCRF";
                    item["BusinessNeed"] = form["txtReasonforChange"] ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (rdOnBehalfOption == "SAVWIPLEmployee")
                        {
                            item["Location"] = form["ddOtherEmpLocation"];
                        }
                        else
                        {
                            item["Location"] = form["ddOtherNewEmpLocation"];
                        }
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List flist = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = flist.GetItemById(FormId);
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 20;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "OCRF";
                    item["BusinessNeed"] = form["txtReasonforChange"] ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (rdOnBehalfOption == "SAVWIPLEmployee")
                        {
                            item["Location"] = form["ddOtherEmpLocation"];
                        }
                        else
                        {
                            item["Location"] = form["ddOtherNewEmpLocation"];
                        }
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();
                    formId = item.Id;

                    ListDAL dal = new ListDAL();
                    var resubmitResult = await dal.ResubmitUpdate(formId);

                    if (AppRowId != 0)
                    {
                        List listApprovalMaster = _context.Web.Lists.GetByTitle("ApprovalMaster");
                        ListItem alistItem = listApprovalMaster.GetItemById(AppRowId);
                        alistItem["ApproverStatus"] = "Resubmitted";
                        alistItem["IsActive"] = 0;
                        alistItem.Update();
                        _context.Load(alistItem);
                        _context.ExecuteQuery();
                    }
                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetails(web, form, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                var newItem = userDetailsResponse.Model;

                //Transaction
                newItem["RequestType"] = form["drpReqType"];

                newItem["ChkEmployeeTransfer"] = form["EmployeeTransferChk"];
                newItem["ChkCostCenter"] = form["CostCenterChk"];
                newItem["ChkReportingAuthority"] = form["ReportingAuthorityChk"];
                newItem["ReasonforChange"] = form["txtReasonforChange"];
                newItem["TransferEffectiveDate"] = form["txtTransferEffectiveDate"] == "" ? "" : form["txtTransferEffectiveDate"];
                var drpdrpReqType = form["drpReqType"];
                if (drpdrpReqType == "Organisation Change Request Form")
                {
                    newItem["ChkPosition"] = form["ChkPosition"];
                    newItem["CurrentRoleFrom"] = form["txtCurrentRoleFromWhite"];
                    newItem["CurrentRoleTo"] = form["txtCurrentRoleToWhite"];
                    newItem["WorkContractFrom"] = form["drpWorkContractFrom"];
                    newItem["WorkContractTo"] = form["drpWorkContractTo"];
                    newItem["SubDepartmentFrom"] = form["txtSubDepartmentFrom"];
                    newItem["SubDepartmentTo"] = form["txtSubDepartmentTo"];
                    newItem["WorkLocationFrom"] = form["txtWorkLocationFrom"];
                    newItem["WorkLocationTo"] = form["txtWorkLocationTo"];
                    newItem["BusinessLocationTo"] = form["txtWorkLocationTo1"];
                    newItem["BusinessLocationFrom"] = form["txtWorkLocationFrom1"];
                }
                else
                {
                    newItem["EmployeeCategoryFromTRF"] = form["txtCurrentRoleFromBlue"];
                    newItem["EmployeeCategoryToTRF"] = form["txtCurrentRoleToBlue"];
                    newItem["SubDepartment1FromTRF"] = form["txtSubDepartmentFrom1"];
                    newItem["SubDepartment1ToTRF"] = form["txtSubDepartmentTo1"];
                    newItem["SubDepartment2FromTRF"] = form["txtSubDepartmentFrom2"];
                    newItem["SubDepartment2ToTRF"] = form["txtSubDepartmentTo2"];
                }

                newItem["DivisionFrom"] = form["txtDivisionFrom"];
                newItem["DivisionTo"] = form["txtDivisionTo"];
                newItem["DepartmentFrom"] = form["txtDepartmentFrom"];
                newItem["DepartmentTo"] = form["txtDepartmentTo"];
                newItem["ReportingManagerFrom"] = form["txtReportingManagerFrom"];
                newItem["ReportingManagerTo"] = form["txtReportingManagerTo"];
                newItem["CostCentreFrom"] = form["txtCostCentreFrom"];
                newItem["CostCentreTo"] = form["txtCostCentreTo"];


                newItem["FormID"] = formId;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();

                //File Upload
                if (file != null)
                {
                    int attachmentID = newItem.Id;

                    string path = file.FileName;
                    path = path.Replace(" ", "");
                    string FileName = path;

                    List docs = web.Lists.GetByTitle(listName);
                    ListItem itemAttach = docs.GetItemById(attachmentID);

                    var attInfo = new AttachmentCreationInformation();

                    attInfo.FileName = FileName;

                    byte[] fileData = null;
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        fileData = binaryReader.ReadBytes(file.ContentLength);
                    }

                    attInfo.ContentStream = new MemoryStream(fileData);

                    Attachment att = itemAttach.AttachmentFiles.Add(attInfo); //Add to File

                    _context.Load(att);
                    _context.ExecuteQuery();
                }

                //File Upload on Edit mode
                var attachedfile = form["attachedfile"];
                if (attachedfile != null && attachedfile != "")
                {
                    int startListID = Convert.ToInt32(form["FormSrId"]);

                    Site oSite = _context.Site;
                    _context.Load(oSite);
                    _context.ExecuteQuery();

                    _context.Load(web);
                    _context.ExecuteQuery();

                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"";

                    List oList = _context.Web.Lists.GetByTitle(listName);
                    _context.Load(oList);
                    _context.ExecuteQuery();

                    ListItemCollection items = oList.GetItems(query);
                    _context.Load(items);
                    _context.ExecuteQuery();
                    byte[] fileContents = null;

                    Folder folder = web.GetFolderByServerRelativeUrl(oSite.Url + "/Lists/" + listName + "/Attachments/" + startListID);

                    _context.Load(folder);
                    _context.ExecuteQuery();

                    FileCollection attachments = folder.Files;
                    _context.Load(attachments);
                    _context.ExecuteQuery();

                    foreach (Microsoft.SharePoint.Client.File oFile in folder.Files)
                    {

                        FileInfo myFileinfo = new FileInfo(oFile.Name);
                        WebClient clientFile = new WebClient();
                        clientFile.Credentials = _context.Credentials;

                        string SharepointSiteURL = ConfigurationManager.AppSettings["SharepointSiteURL"];

                        fileContents = clientFile.DownloadData(SharepointSiteURL + oFile.ServerRelativeUrl);

                    }

                    var attachedfileName = form["attachedfileName"];
                    int attachmentID = newItem.Id;

                    string path = attachedfileName;
                    path = path.Replace(" ", "");
                    string FileName = path;

                    List docs = web.Lists.GetByTitle(listName);
                    ListItem itemAttach = docs.GetItemById(attachmentID);

                    var attInfo = new AttachmentCreationInformation();

                    attInfo.FileName = FileName;

                    attInfo.ContentStream = new MemoryStream(fileContents);

                    Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                    _context.Load(att);
                    _context.ExecuteQuery();
                }

                RowId = newItem.Id;
                result.Status = 200;
                result.Message = formId.ToString();

                //Approval Tracking
                var approvalResponse = await SaveApprovalMasterData(approverIdList, form["txtReasonforChange"] ?? "", RowId, formId);
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
                result.Message = "There were some issue while saving form data.";
                return result;
            }

            return result;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalOCRF(UserData user, string requestSubmissionFor, long txtEmployeeCode, long txtCostCenterNo, long txtOnBehalfEmpId, long txtOnBehalfCostCenterNo, string drpReqType, string selfdesignation, string onbehalfesignation, string reportingManagerTo, string chkTypeOfChange, string empLocation, string txtCostCentreFrom, string txtCostCentreTo)
        {
            try
            {
                string[] reportingManagerToArry = reportingManagerTo.Split('|');
                string reprtMgrTo = reportingManagerToArry[0];
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_OCRFApproval", con);
                cmd.Parameters.Add(new SqlParameter("@drpReqType", drpReqType));
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
                cmd.Parameters.Add(new SqlParameter("@ReportingManagerTo", reprtMgrTo));
                cmd.Parameters.Add(new SqlParameter("@ChkTypeOfChange", chkTypeOfChange));
                if (requestSubmissionFor == "Self")
                {
                    cmd.Parameters.Add(new SqlParameter("@Designation", selfdesignation));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@Designation", onbehalfesignation));
                }
                cmd.Parameters.Add(new SqlParameter("@LocationName", empLocation));
                
                cmd.Parameters.Add(new SqlParameter("@CostCentreFrom", txtCostCentreFrom));
                cmd.Parameters.Add(new SqlParameter("@CostCentreTo", txtCostCentreTo));
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
                        if (drpReqType == "Organisation Change Request Form")
                        {
                            app.ApprovalLevel = Convert.ToInt32(ds.Tables[0].Rows[i]["AppLevel"]);
                        }
                        else
                        {
                            app.ApprovalLevel = Convert.ToInt32(ds.Tables[0].Rows[i]["approvalLevel"]);
                        }

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

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
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

        public async Task<dynamic> ViewORCFFData(int rowId, int formId)
        {
            dynamic OCRFDataList = new ExpandoObject();
            //GetAttachmentUTL(rowId);
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

                var response = await client.GetAsync("_api/web/lists/GetByTitle('OCRFForms')/items?$select=*,FormID/ID"
           + "&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles");

                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var OCRFResult = JsonConvert.DeserializeObject<OCRFModel>(responseText, settings);
                    OCRFDataList.one = OCRFResult.ocrfflist.ocrfData;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,Designation,Level,Logic,TimeStamp,IsActive,ApproverName,ApproverUserName,Comment,NextApproverId,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);
                if (modelData.Node.Data.Count > 0)
                {
                    var clientApp = new HttpClient(handler);
                    clientApp.BaseAddress = new Uri(conString);
                    clientApp.DefaultRequestHeaders.Accept.Clear();
                    clientApp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var names = new List<string>();

                    var items = modelData.Node.Data;

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

                    if (!string.IsNullOrEmpty(responseText2))
                    {
                        dynamic data3 = Json.Decode(responseText2);
                        OCRFDataList.two = data3.d.results;
                        OCRFDataList.three = items;
                    }

                }
                else
                {
                    OCRFDataList.two = new List<string>();
                    OCRFDataList.three = new List<string>();
                }
                return OCRFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return OCRFDataList;
            }
        }

        public void GetAttachmentUTL(int rowId)
        {
            ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential(spUsername, spPass);
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("OCRF") ? GlobalClass.ListNames["OCRF"] : "";

            using (ClientContext clientContext = new ClientContext(conString))
            {
                // clientcontext.Web.Lists.GetById - This option also can be used to get the list using List GUID
                // This value is NOT List internal name
                List targetList = clientContext.Web.Lists.GetByTitle(listName);

                // Option 1: Get Item by ID
                ListItem oItem = targetList.GetItemById(rowId);

                // Option 2: Get Item using CAML Query
                CamlQuery oQuery = new CamlQuery();
                oQuery.ViewXml = @"<View><Query><Where>
                <Eq>
                <FieldRef Name='Title' />
                <Value Type='Text'>New List Item</Value>
                </Eq>
                </Where></Query></View>";

                ListItemCollection oItems = targetList.GetItems(oQuery);
                clientContext.Load(oItems);
                clientContext.ExecuteQuery();

                oItem = oItems.FirstOrDefault();
                // Option 2: Ends Here(Above line)

                AttachmentCollection oAttachments = oItem.AttachmentFiles;
                clientContext.Load(oAttachments);
                clientContext.ExecuteQuery();

                foreach (Attachment oAttachment in oAttachments)
                {
                    Console.WriteLine("File Name - " + oAttachment.FileName);
                }
            }

        }

        public List<OCRFData> GetDivisionMaster(string divId, string deptId)
        {
            List<OCRFData> divList = new List<OCRFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetDivDeptData", con);
                cmd.Parameters.Add(new SqlParameter("@Flag", "Division"));
                cmd.Parameters.Add(new SqlParameter("@DivId", divId));
                cmd.Parameters.Add(new SqlParameter("@DeptId", deptId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        OCRFData div = new OCRFData();
                        div.DivId = ds.Tables[0].Rows[i]["DivId"].ToString();
                        div.DivName = ds.Tables[0].Rows[i]["DivName"].ToString();
                        divList.Add(div);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return divList;
        }

        public List<OCRFData> GetDepartmentMaster(string divId, string deptId)
        {
            List<OCRFData> deptList = new List<OCRFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetDivDeptData", con);
                cmd.Parameters.Add(new SqlParameter("@Flag", "Department"));
                cmd.Parameters.Add(new SqlParameter("@DivId", divId));
                cmd.Parameters.Add(new SqlParameter("@DeptId", deptId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        OCRFData dept = new OCRFData();
                        dept.DeptId = ds.Tables[0].Rows[i]["DeptId"].ToString();
                        dept.DeptName = ds.Tables[0].Rows[i]["DeptName"].ToString();
                        deptList.Add(dept);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return deptList;
        }

        public List<OCRFData> GetSubDepartmentMaster(string divId, string deptId)
        {
            List<OCRFData> subdeptList = new List<OCRFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetDivDeptData", con);
                cmd.Parameters.Add(new SqlParameter("@Flag", "SubDepartment"));
                cmd.Parameters.Add(new SqlParameter("@DivId", divId));
                cmd.Parameters.Add(new SqlParameter("@DeptId", deptId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        OCRFData subdept = new OCRFData();
                        subdept.SubDeptId = ds.Tables[0].Rows[i]["SubDeptId"].ToString();
                        subdept.SubDeptName = ds.Tables[0].Rows[i]["SubDeptName"].ToString();
                        subdeptList.Add(subdept);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return subdeptList;
        }

        public List<string> GetHRCEmailIds(string fromCCNumber, string toCCNumber)
        {
            List<string> hrcEmailList = new List<string>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                string[] fromCCNumberArry = fromCCNumber.Split('|');
                string frmCCNumber = fromCCNumberArry[0];
                string[] toCCNumberArry = toCCNumber.Split('|');
                string toCoCNumber = toCCNumberArry[0];

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetHRCEmailId", con);
                cmd.Parameters.Add(new SqlParameter("@FromCCNumber", frmCCNumber));
                cmd.Parameters.Add(new SqlParameter("@ToCCNumber", toCoCNumber));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        var hrcEmail = "";
                        hrcEmail = ds.Tables[0].Rows[i]["HRCEmail"].ToString();
                        hrcEmailList.Add(hrcEmail);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return hrcEmailList;
        }

        public int ActionUpdate(System.Web.Mvc.FormCollection form, UserData user)
        {
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;

            string listName = GlobalClass.ListNames.ContainsKey("OCRF") ? GlobalClass.ListNames["OCRF"] : "";

            if (listName == "")
            {
                return 0;
            }

            int rowId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(rowId);
                newItem["CRNumber"] = form["txtCRNumber"];
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
    }
}
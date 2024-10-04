using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
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
    public class MMRFormDAL : CommonDAL
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

        public async Task<ResponseModel<object>> SaveMMRForm(MMRData model, UserData user, HttpPostedFileBase file)
        {

            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "MMRF";
            string formName = "MMR Form";
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
            //long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCCCode) : Convert.ToInt64(model.OtherNewCostcenterCode));
            //long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCode) : Convert.ToInt64(model.OtherNewEmployeeCode));
            //string empDes = isSelf ? model.EmployeeDesignation : (isSAVWIPL ? Convert.ToString(model.OtherEmployeeDesignation) : Convert.ToString(model.OtherNewEmpDesignation));

            long ccNum = user.CostCenter;
            long empNum = user.EmpNumber;
            string empDes = model.EmployeeDesignation;


            var response = await GetApprovalMMRForm(empNum, ccNum, empDes, model);
            if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
            {
                result.Status = 500;
                result.Message = response.Message;
                return result;
            }

            var approvers = response.Model;


            try
            {

                if (formId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 45;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "MMRForm";
                    item["BusinessNeed"] = model.BusinessNeed ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = model.EmployeeLocation;
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = model.OtherEmployeeLocation;
                        }
                        else
                        {
                            item["Location"] = model.OtherNewEmpLocation;
                        }
                    }
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(formId);
                    item["FormName"] = formName;
                    item["UniqueFormName"] = formShortName;
                    item["FormParentId"] = 45;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "MMRForm";
                    item["BusinessNeed"] = model.BusinessNeed ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = model.EmployeeLocation;
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = model.OtherEmployeeLocation;
                        }
                        else
                        {
                            item["Location"] = model.OtherNewEmpLocation;
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
                        ListItem listItem = listApprovalMaster.GetItemById(AppRowId);
                        listItem["ApproverStatus"] = "Resubmitted";
                        listItem["IsActive"] = 0;
                        listItem.Update();
                        _context.Load(listItem);
                        _context.ExecuteQuery();
                    }
                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(web, model, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SaveMMRFForm", con);
                cmd.Parameters.Add(new SqlParameter("@ExistingDepartment", model.ExistingDepartment));
                cmd.Parameters.Add(new SqlParameter("@NewDepartment", model.NewDepartment));
                cmd.Parameters.Add(new SqlParameter("@FutureOwner", model.FutureOwner));
                cmd.Parameters.Add(new SqlParameter("@MMRDescription", model.MMRDescription));
                cmd.Parameters.Add(new SqlParameter("@MMRIdentification", model.MMRIdentification));
                cmd.Parameters.Add(new SqlParameter("@HandoverDate", model.HandoverDate));
                cmd.Parameters.Add(new SqlParameter("@TransferType", model.TransferType));
                cmd.Parameters.Add(new SqlParameter("@MMREpus", model.MMREpus));
                cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.Parameters.Add(new SqlParameter("@TransferFromDate", model.TransferFromDate));
                cmd.Parameters.Add(new SqlParameter("@TransferToDate", model.TransferToDate));
                cmd.Parameters.Add(new SqlParameter("@BusinessNeed", model.BusinessNeed));
                cmd.Parameters.Add(new SqlParameter("@Details", model.Details));
                cmd.Parameters.Add(new SqlParameter("@formId", formId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                //var newRow = userDetailsResponse.Model;
                //newRow["ExistingDepartment"] = model.ExistingDepartment;
                //newRow["NewDepartment"] = model.NewDepartment;
                //newRow["FutureOwner"] = model.FutureOwner;
                //newRow["MMRDescription"] = model.MMRDescription;
                //newRow["MMRIdentification"] = model.MMRIdentification;
                //newRow["HandoverDate"] = model.HandoverDate.Value.ToString("yyyy-MM-dd 23:59:59");
                //newRow["TransferType"] = model.TransferType;
                //TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                //DateTime tstTime = TimeZoneInfo.ConvertTime(model.MMREpus, TimeZoneInfo.Local, tst);
                //newRow["MMREpus"] = TimeZoneInfo.ConvertTimeToUtc(tstTime, tst);
                //newRow["FutureOwnerEmail"] = model.FutureOwnerEmail;
                //if (model.TransferFromDate == null)
                //{
                //    newRow["TransferFromDate"] = model.TransferFromDate;
                //}
                //else
                //{
                //    newRow["TransferFromDate"] = model.TransferFromDate.Value.ToString("yyyy-MM-dd 23:59:59");
                //}
                //if (model.TransferToDate == null)
                //{
                //    newRow["TransferToDate"] = model.TransferToDate;
                //}
                //else
                //{
                //    newRow["TransferToDate"] = model.TransferToDate.Value.ToString("yyyy-MM-dd 23:59:59");
                //}
                

                //newRow["BusinessNeed"] = model.BusinessNeed;
                //newRow["Details"] = model.Details;
                //newRow["FormID"] = formId;
                //newRow.Update();
                //_context.Load(newRow);
                //_context.ExecuteQuery();
                //RowId = newRow.Id;

                result.Status = 200;
                result.Message = formId.ToString();

                var approverIdList = response.Model;

                if (file != null)
                {
                    int attachmentID = RowId;

                    string path = file.FileName;
                    path = path.Replace(" ", "");

                    string FileName = "PS_" + path;

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

                var attachedfile = model.attachedfile;
                if (attachedfile != null && attachedfile != "")
                {
                    int startListID = Convert.ToInt32(model.FormSrId);

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

                    var attachedfileName = model.attachedfileName;
                    int attachmentID = RowId;

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

                //Task Entry in Approval Master List
                var approvalResponse = await SaveApprovalMasterData(approverIdList, model.BusinessNeed ?? "", RowId, formId);

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

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalMMRForm(long empNum, long ccNum, string empDes, MMRData model)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_MMRFForm", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empDes", empDes));
                cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
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

        public async Task<dynamic> ViewMMRFFormData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client1 = new HttpClient(handler);
                client1.BaseAddress = new Uri(conString);
                client1.DefaultRequestHeaders.Accept.Clear();
                client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('MMRForm')/items?$select=*"
  + "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCFUserResult = JsonConvert.DeserializeObject<MMRModel>(responseText1, settings);
                    URCFData.one = SUCFUserResult.MMRResults.MMRData;
                    var istdate = TimeZoneInfo.ConvertTimeFromUtc(SUCFUserResult.MMRResults.MMRData[0].MMREpus, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    SUCFUserResult.MMRResults.MMRData[0].MMREpus = istdate;
                    if (SUCFUserResult.MMRResults.MMRData[0].NewOwnEPUS.ToString("dd-MM-yyyy hh:mm:ss") != "01-01-0001 00:00:00")
                    {
                        var istdate1 = TimeZoneInfo.ConvertTimeFromUtc(SUCFUserResult.MMRResults.MMRData[0].NewOwnEPUS, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                        SUCFUserResult.MMRResults.MMRData[0].NewOwnEPUS = istdate1;
                    }
                   
                }

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

        public async Task<int> UpdateData(MMRData model, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("IPAF") ? GlobalClass.ListNames["IPAF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {
                int RowId = 0;

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime tstTime = TimeZoneInfo.ConvertTime(model.NewOwnEPUS, TimeZoneInfo.Local, tst);
                newItem["NewOwnEPUS"] = TimeZoneInfo.ConvertTimeToUtc(tstTime, tst);
                //newItem["NewOwnEPUS"] = model.NewOwnEPUS.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
                RowId = newItem.Id;

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
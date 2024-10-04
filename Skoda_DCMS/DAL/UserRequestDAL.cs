using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections;
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
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class UserRequestDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;

        public async Task<ResponseModel<object>> SaveData(UserRequestData model, UserData user, HttpPostedFileBase file, HttpPostedFileBase file1)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "URCF";
            string formName = "User Request Form";
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

            string fileName1 = model.attachedfileName;
            string fileName2 = model.attachedfileName1;

            var requestSubmissionFor = model.RequestSubmissionFor;
            var otherEmpType = model.OnBehalfOption ?? "";
            bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
            long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCCCode) : Convert.ToInt64(model.OtherNewCostcenterCode));
            long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(model.OtherEmployeeCode) : Convert.ToInt64(model.OtherNewEmployeeCode));


            var response = await GetURCFApproval(empNum, ccNum, model);
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
                    item["FormParentId"] = 39;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "UserRequest";
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
                    item["FormParentId"] = 39;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "UserRequest";
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
                var newRow = userDetailsResponse.Model;

                newRow["Brand"] = model.Brand;
                newRow["ServiceType"] = model.ServiceType;
                newRow["TypeofRequest"] = model.TypeofRequest;
                newRow["BusinessNeed"] = model.BusinessNeed;

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                RowId = newRow.Id;

                result.Status = 200;
                result.Message = formId.ToString();

                //Fetch Existing Uploaded File
                if (file == null)
                {
                    if (prevItemId != 0)
                    {
                        var copyFileResult = await CopyExistingAttachmentFromOneItemToAnother(prevItemId, RowId, listName, fileName1);
                        if (copyFileResult.Status != 200 && copyFileResult == null)
                        {
                            return new ResponseModel<object> { Status = copyFileResult.Status, Message = copyFileResult.Message };
                        }
                    }

                }
                if (file1 == null)
                {
                    if (prevItemId != 0)
                    {
                        var copyFileResult = await CopyExistingAttachmentFromOneItemToAnother(prevItemId, RowId, listName, fileName2);
                        if (copyFileResult.Status != 200 && copyFileResult == null)
                        {
                            return new ResponseModel<object> { Status = copyFileResult.Status, Message = copyFileResult.Message };
                        }
                    }
                }
                //Fetch Existing Uploaded File


                //Upload New File
                if (file != null)
                {
                    var ext = Path.GetExtension(file.FileName);
                    //var uploadResult = UploadPhotoAndLicenseDetails(file, RowId, listName, "File1_" + file.FileName);

                    var uploadResult = UploadPhotoAndLicenseDetails(file, RowId, listName, "GekoFile" + ext);
                    if (uploadResult.Status != 200 && uploadResult == null)
                    {
                        return new ResponseModel<object> { Status = uploadResult.Status, Message = uploadResult.Message };
                    }
                }

                if (file1 != null)
                {
                    var ext = Path.GetExtension(file1.FileName);
                    //var uploadResult = UploadPhotoAndLicenseDetails(file1, RowId, listName, "File2_" + file1.FileName);
                    var uploadResult = UploadPhotoAndLicenseDetails(file1, RowId, listName, "SAGA2File" + ext);
                    if (uploadResult == null)
                    {
                        return new ResponseModel<object> { Status = uploadResult.Status, Message = uploadResult.Message };
                    }
                }
                //Upload New File

                //var count = Convert.ToInt32(model.ApplicationCategoryData.Count());
                int appId = RowId;
                List UserRequestApplicationListList = web.Lists.GetByTitle("UserRequestApplicationList");

                for (int i = 0; i < model.ApplicationCategoryData.Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newFormItem = UserRequestApplicationListList.AddItem(itemCreate);
                    newFormItem["AppId"] = RowId;
                    newFormItem["ServiceCategory"] = model.ApplicationCategoryData[i].ServiceCategory ?? "";
                    newFormItem["ServiceSubCategory"] = model.ApplicationCategoryData[i].ServiceSubCategory ?? "";
                    newFormItem["Role"] = model.ApplicationCategoryData[i].Role ?? "";
                    newFormItem["AccessType"] = model.ApplicationCategoryData[i].AccessType ?? "";
                    newFormItem["Brand"] = model.ApplicationCategoryData[i].BrandApp ?? "";
                    newFormItem["ApplicationUserID"] = model.ApplicationCategoryData[i].ApplicationUserID ?? "";
                    newFormItem["FormID"] = formId;
                    newFormItem.Update();
                    _context.ExecuteQuery();
                }


                var approverIdList = response.Model;

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
            }
            return result;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetURCFApproval(long empNum, long ccNum, UserRequestData model)
        {
            try
            {
                //HOD
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_UserRequestFormApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
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
                        app.Logic = Convert.ToString(ds.Tables[0].Rows[i]["logic"]);
                        app.ApprovalLevel = (int)ds.Tables[0].Rows[i]["approvalLevel"];
                        appList.Add(app);
                    }
                }

                //IT Manager( Dealer Connect Team)
                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataSet ds1 = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("sp_UserReqDealerConnectTeamApproval", con);
                for (int i = 0; i < model.ApplicationCategoryData.Count; i++)
                {
                    cmd1.Parameters.Clear();
                    cmd1.Parameters.Add(new SqlParameter("@ServiceCat", model.ApplicationCategoryData[i].ServiceCategory));
                    cmd1.Parameters.Add(new SqlParameter("@ServiceSubCat", model.ApplicationCategoryData[i].ServiceSubCategory));
                    if (model.ApplicationCategoryData[i].Role == "Select Role" || model.ApplicationCategoryData[i].Role == null)
                    {
                        cmd1.Parameters.Add(new SqlParameter("@Role", ""));
                    }
                    else
                    {
                        cmd1.Parameters.Add(new SqlParameter("@Role", model.ApplicationCategoryData[i].Role));
                    }

                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(ds1);
                    con.Close();
                }

                DataTable UniqueRecords = RemoveDuplicateRows(ds1.Tables[0], "EmailID");

                if (UniqueRecords.Rows.Count > 0 && UniqueRecords != null)
                {
                    for (int i = 0; i < UniqueRecords.Rows.Count; i++)
                    {
                        ApprovalMatrix app = new ApprovalMatrix();
                        app.EmpNumber = Convert.ToInt64(UniqueRecords.Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(UniqueRecords.Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(UniqueRecords.Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(UniqueRecords.Rows[i]["EmailID"]);
                        app.Designation = Convert.ToString(UniqueRecords.Rows[i]["desg"]);
                        app.Logic = Convert.ToString(UniqueRecords.Rows[i]["logic"]);
                        app.ApprovalLevel = (int)UniqueRecords.Rows[i]["approvalLevel"];
                        appList.Add(app);
                    }
                }


                if ((ds.Tables[0].Rows.Count == 0 && ds.Tables[0] != null) || (UniqueRecords.Rows.Count == 0 && UniqueRecords != null))
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
                client.Timeout = TimeSpan.FromSeconds(10);
                var count = appList.Count;

                //AD Code
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
        public List<ApplicationCategoryData> GetApplicationCategoryData()
        {
            List<ApplicationCategoryData> applicationList = new List<ApplicationCategoryData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetApplicationCategoryData", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        ApplicationCategoryData model = new ApplicationCategoryData();
                        model.ListAppId = Convert.ToInt32(ds.Tables[0].Rows[i]["Id"]);
                        model.ServiceType = Convert.ToString(ds.Tables[0].Rows[i]["ServiceType"]).Trim();
                        model.ServiceCategory = Convert.ToString(ds.Tables[0].Rows[i]["ServiceCategory"]).Trim();
                        model.ServiceSubCategory = Convert.ToString(ds.Tables[0].Rows[i]["ServiceSubCategory"]).Trim();
                        model.Role = Convert.ToString(ds.Tables[0].Rows[i]["Role"]).Trim();
                        model.OwnerName = Convert.ToString(ds.Tables[0].Rows[i]["OwnerName"]).Trim();
                        model.OwnerEmail = Convert.ToString(ds.Tables[0].Rows[i]["OwnerEmail"]).Trim();
                        applicationList.Add(model);
                    }
                }
                return applicationList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<ApplicationCategoryData>();
            }
        }

        public async Task<ResponseModel<string>> CopyExistingAttachmentFromOneItemToAnother(int prevItemId, int itemId, string listName, string fileName)
        {
            dynamic DAFDataList = new ExpandoObject();
            try
            {
                //File Upload
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('" + listName + "')/items?$select=AttachmentFiles"
                + "&$filter=(ID eq '" + prevItemId + "')&$expand=AttachmentFiles");
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
                if (AttachmentList != null && AttachmentList.Attachments != null && AttachmentList.Attachments.Count > 0)
                {
                    foreach (var Attachment in AttachmentList.Attachments)
                    {
                        if (Attachment.FileName == fileName)
                        {
                            var (_context, web) = CreateClientContextAndWeb();
                            int attachmentID = itemId;

                            List docs = web.Lists.GetByTitle(listName);
                            ListItem itemAttach = docs.GetItemById(attachmentID);

                            var attInfo = new AttachmentCreationInformation();

                            attInfo.FileName = Attachment.FileName;

                            string imgBase64Str = "";
                            var file = _context.Web.GetFileByServerRelativeUrl(Attachment.ServerRelativeUrl);
                            _context.Load(file);
                            _context.ExecuteQuery();
                            ClientResult<System.IO.Stream> data = file.OpenBinaryStream();
                            _context.Load(file);
                            _context.ExecuteQuery();
                            byte[] fileData = null;
                            using (System.IO.MemoryStream mStream = new System.IO.MemoryStream())
                            {
                                if (data != null)
                                {
                                    data.Value.CopyTo(mStream);
                                    fileData = mStream.ToArray();
                                }
                            }
                            attInfo.ContentStream = new MemoryStream(fileData);

                            Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                            _context.Load(att);
                            _context.ExecuteQuery();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<string>
                {
                    Status = 500,
                    Message = "There were some issue while copying file."
                };
            }
            return new ResponseModel<string>
            {
                Status = 200,
                Message = "File Copied Successfully"
            };
        }

        public async Task<dynamic> GetURCFDetails(int rowId, int formId)
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
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('UserRequestForm')/items?$select=*"
  + "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var URCFResult = JsonConvert.DeserializeObject<UserRequestModel>(responseText1, settings);
                    URCFData.one = URCFResult.URCFResults.UserRequestData;
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response = await client1.GetAsync("_api/web/lists/GetByTitle('UserRequestApplicationList')/items?$select=*&$filter=(AppId eq '" + rowId + "')");
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var SUCFUserResult = JsonConvert.DeserializeObject<ApplicationCategoryModel>(responseText, settings);
                        URCFData.one[0].ApplicationCategoryData = SUCFUserResult.ApplicationCategoryResults.data;
                    }
                }

                DrivingAuthorizationDAL obj = new DrivingAuthorizationDAL();
                var images = await obj.DownloadAttachmentData("UserRequestForm", rowId);
                if (images != null && images.Status == 200 && images.Model != null)
                {
                    int i = 0;
                    foreach (var item in images.Model)
                    {
                        URCFData.one[0].AttachmentFiles.Attachments[i] = item;
                        i++;
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

        public DataTable RemoveDuplicateRows(DataTable table, string DistinctColumn)
        {
            try
            {
                ArrayList UniqueRecords = new ArrayList();
                ArrayList DuplicateRecords = new ArrayList();

                // Check if records is already added to UniqueRecords otherwise,
                // Add the records to DuplicateRecords
                foreach (DataRow dRow in table.Rows)
                {
                    if (UniqueRecords.Contains(dRow[DistinctColumn]))
                        DuplicateRecords.Add(dRow);
                    else
                        UniqueRecords.Add(dRow[DistinctColumn]);
                }

                // Remove duplicate rows from DataTable added to DuplicateRecords
                foreach (DataRow dRow in DuplicateRecords)
                {
                    table.Rows.Remove(dRow);
                }

                // Return the clean DataTable which contains unique records.
                return table;
            }
            catch (Exception ex)
            {
                return null;
                Log.Error(ex.Message, ex);
            }
        }
    }
}
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
using System.Xml;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class GiftsInvitationDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;

        public async Task<ResponseModel<object>> SaveGiftsInvitation(GiftsInvitationData model, UserData user, HttpPostedFileBase file1)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "GAIF";
            string formName = "Gifts, Invitation and Compliance Consultation Form";
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
            string requestType = model.RequestType;


            var response = await GetApprovalGiftsInvitation(empNum, ccNum, requestType);
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
                    item["FormParentId"] = 37;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "GiftsInvitation";
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
                    item["FormParentId"] = 37;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "GiftsInvitation";
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

                newRow["BusinessNeed"] = model.BusinessNeed;
                newRow["RequestType"] = model.RequestType;
                newRow["Transaction"] = model.Transaction;

                var tran = model.Transaction;
                if (tran == "Giving")
                {
                    newRow["IsGiftOrInviteToPublicOfficial"] = model.IsGiftOrInviteToPublicOfficial;
                }

                newRow["NameRelationOtherDet"] = model.NameRelationOtherDet;
                newRow["FrequencyOfGiftsOrInvitationfrm"] = model.FrequencyOfGiftsOrInvitationfrm;
                newRow["ApproxValueOfGiftsInvt"] = model.ApproxValueOfGiftsInvt;
                newRow["ReasonForGiftingInvitation"] = model.ReasonForGiftingInvitation;

                if (tran == "Receiving")
                {
                    newRow["GiftIsAcceptedRefused"] = model.GiftIsAcceptedRefused;
                    var giftIsAcceptedRefused = model.GiftIsAcceptedRefused;
                    newRow["ReasonGiftIsAcceptedRefused"] = model.ReasonGiftIsAcceptedRefused;                   
                }

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                RowId = newRow.Id;
                result.Status = 200;
                result.Message = formId.ToString();

                RowId = newRow.Id;

                var chkGiftGivenVal = model.IsGiftOrInviteToPublicOfficial;

                //if (file == null)
                //{
                //    if (prevItemId != 0)
                //    {
                //        var copyFileResult = await CopyExistingAttachmentFromOneItemToAnother(prevItemId, RowId, listName, fileName1);
                //        if (copyFileResult.Status != 200 && copyFileResult == null)
                //        {
                //            return new ResponseModel<object> { Status = copyFileResult.Status, Message = copyFileResult.Message };
                //        }
                //    }

                //}
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

                //if (chkGiftGivenVal == "Yes")
                //{
                //    if (file != null)
                //    {
                //        var ext = Path.GetExtension(file.FileName);
                //        //var uploadResult = UploadPhotoAndLicenseDetails(file, RowId, listName, "ApprfrmExternalAffairs" + ext);
                //        var uploadResult = UploadPhotoAndLicenseDetails(file, RowId, listName, "File1_" + file.FileName);
                //        if (uploadResult.Status != 200 && uploadResult == null)
                //        {
                //            return new ResponseModel<object> { Status = uploadResult.Status, Message = uploadResult.Message };
                //        }
                //    }
                //}

                if (file1 != null)
                {
                    //var ext = Path.GetExtension(file1.FileName);
                    //var uploadResult = UploadPhotoAndLicenseDetails(file1, RowId, listName, "OtherDocument" + ext);
                    var uploadResult = UploadPhotoAndLicenseDetails(file1, RowId, listName, file1.FileName);
                    if (uploadResult == null)
                    {
                        return new ResponseModel<object> { Status = uploadResult.Status, Message = uploadResult.Message };
                    }
                }

                var chkRequestTypeVal = model.RequestType;
                if (chkRequestTypeVal == "Consultation")
                {
                    var count = Convert.ToInt32(model.totalrows);
                    int questionId = RowId;
                    List GiftsInvitationFormQuestionList = web.Lists.GetByTitle("GiftsInvitationFormQuestionList");

                    for (int i = 0; i < model.QuestionData.Count; i++)
                    {
                        ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                        ListItem newFormItem = GiftsInvitationFormQuestionList.AddItem(itemCreate);
                        newFormItem["QuestionId"] = RowId;
                        newFormItem["Question"] = model.QuestionData[i].Question ?? "";
                        newFormItem["FormID"] = formId;
                        newFormItem.Update();
                        _context.ExecuteQuery();
                    }
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

        public async Task<int> UpdateGAIFData(System.Web.Mvc.FormCollection form, UserData user)
        {
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("GAIF") ? GlobalClass.ListNames["GAIF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(form["FormSrId"]);
            try
            {
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                newItem["GiftTobeDepoWithGRC"] = form["chkGiftPublic"];
                newItem["Answers"] = form["txtAnswers"];
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

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalGiftsInvitation(long empNum, long ccNum, string requestType)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GiftsInvitationApproval", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@RequestType", requestType));
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

        public async Task<dynamic> GetGiftsInvitationDetails(int rowId, int formId)
        {
            dynamic GAIFData = new ExpandoObject();
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
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('GiftsInvitationForm')/items?$select=*"
  + "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var GAIFResult = JsonConvert.DeserializeObject<GAIFModel>(responseText1, settings);
                    GAIFData.one = GAIFResult.list.data;
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response = await client1.GetAsync("_api/web/lists/GetByTitle('GiftsInvitationFormQuestionList')/items?$select=Question&$filter=(QuestionId eq '" + rowId + "')");
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var SUCFUserResult = JsonConvert.DeserializeObject<QuestionModel>(responseText, settings);
                        GAIFData.one[0].QuestionData = SUCFUserResult.QuestionList.data;
                    }
                }

                DrivingAuthorizationDAL obj = new DrivingAuthorizationDAL();
                var images = await obj.DownloadAttachmentData("GiftsInvitationForm", rowId);
                if (images != null && images.Status == 200 && images.Model != null)
                {
                    int i = 0;
                    foreach (var item in images.Model)
                    {
                        GAIFData.one[0].AttachmentFiles.Attachments[i] = item;
                        i++;
                    }
                }

                var (r1, r2) = await GetApproversData(user, rowId, formId);

                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                GAIFData.two = r1.Model;
                GAIFData.three = r2.Model;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return GAIFData;
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
    }
}
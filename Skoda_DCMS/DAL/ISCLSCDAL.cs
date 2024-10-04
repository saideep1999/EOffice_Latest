using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.WebControls;
using Microsoft.Web.Hosting.Administration;
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
    public class ISCLSCDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        //public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        //public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        //public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        dynamic approverEmailIds;

        public async Task<ResponseModel<object>> SaveISCLSCForm(ISCLSData model, UserData user, HttpPostedFileBase files)
        {

            ResponseModel<object> result = new ResponseModel<object>();
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            //Web web = _context.Web;
            string formShortName = "ISLS";
            string formName = "ISC LST Form";
            string GPNo = model.GlobalProcessNumber;
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


            var response = await GetApprovalISCLSCForm(empNum, ccNum, empDes, model);
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
                #region Comment 
                //if (formId == 0)
                //{
                //    List FormsList = web.Lists.GetByTitle("Forms");
                //    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                //    ListItem item = FormsList.AddItem(itemCreated);
                //    item["FormName"] = formName;
                //    item["UniqueFormName"] = formShortName;
                //    item["FormParentId"] = 40;
                //    item["ListName"] = listName;
                //    item["SubmitterUserName"] = user.UserName;
                //    item["Status"] = "Submitted";
                //    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //    item["Department"] = user.Department;
                //    item["ControllerName"] = "ISCLSC";
                //    item["BusinessNeed"] = model.GlobalProcessNumber ?? "";
                //    if (requestSubmissionFor == "Self")
                //    {
                //        item["Location"] = model.EmployeeLocation;
                //    }
                //    else
                //    {
                //        if (otherEmpType == "SAVWIPLEmployee")
                //        {
                //            item["Location"] = model.OtherEmployeeLocation;
                //        }
                //        else
                //        {
                //            item["Location"] = model.OtherNewEmpLocation;
                //        }
                //    }
                //    item.Update();
                //    _context.Load(item);
                //    _context.ExecuteQuery();

                //    formId = item.Id;
                //}
                //else
                //{
                //    List list = _context.Web.Lists.GetByTitle("Forms");
                //    ListItem item = list.GetItemById(formId);
                //    item["FormName"] = formName;
                //    item["UniqueFormName"] = formShortName;
                //    item["FormParentId"] = 40;
                //    item["ListName"] = listName;
                //    item["SubmitterUserName"] = user.UserName;
                //    item["Status"] = "Resubmitted";
                //    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                //    item["Department"] = user.Department;
                //    item["ControllerName"] = "ISCLSC";
                //    item["BusinessNeed"] = model.GlobalProcessNumber ?? "";
                //    if (requestSubmissionFor == "Self")
                //    {
                //        item["Location"] = model.EmployeeLocation;
                //    }
                //    else
                //    {
                //        if (otherEmpType == "SAVWIPLEmployee")
                //        {
                //            item["Location"] = model.OtherEmployeeLocation;
                //        }
                //        else
                //        {
                //            item["Location"] = model.OtherNewEmpLocation;
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
                //        ListItem listItem = listApprovalMaster.GetItemById(AppRowId);
                //        listItem["ApproverStatus"] = "Resubmitted";
                //        listItem["IsActive"] = 0;
                //        listItem.Update();
                //        _context.Load(listItem);
                //        _context.ExecuteQuery();
                //    }
                //}
                #endregion


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
                cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "ISCLSC"));
                cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", model.GlobalProcessNumber ?? ""));
                cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 40));
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
                if (prevItemId != 0)
                {
                    cmd_form = new SqlCommand();
                    DataSet ds2 = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_ChangeApprovalMasterStatusDARF", con);
                    cmd_form.Parameters.Add(new SqlParameter("@Id", prevItemId == null || prevItemId == 0 ? 0 : Convert.ToInt64(prevItemId)));
                    cmd_form.Parameters.Add(new SqlParameter("@FormId", formId == null || formId == 0 ? 0 : Convert.ToInt64(formId)));
                    cmd_form.Parameters.Add(new SqlParameter("@ApproverStatus", "Resubmitted"));
                    cmd_form.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds2);
                    con.Close();
                }

                SqlDataAdapter adapter_form1 = new SqlDataAdapter();
                SqlCommand cmd_form1 = new SqlCommand();
                DataSet ds3 = new DataSet();
                con = new SqlConnection(sqlConString);
                cmd_form1 = new SqlCommand("USP_SaveISCLSCFormData", con);

                //var newRow = userDetailsResponse.Model;
                cmd_form1.Parameters.Add(new SqlParameter("@Id", userDetailsResponse.RowId));
                cmd_form1.Parameters.Add(new SqlParameter("@ActionType", model.ActionType == null ? "" : model.ActionType));
                cmd_form1.Parameters.Add(new SqlParameter("@BuyerName", model.BuyerName == null ? "" : model.BuyerName));
                cmd_form1.Parameters.Add(new SqlParameter("@Team", model.Team == null ? "" : model.Team));
                cmd_form1.Parameters.Add(new SqlParameter("@GlobalProcessNumber", model.GlobalProcessNumber == null ? "" : model.GlobalProcessNumber));
                cmd_form1.Parameters.Add(new SqlParameter("@Description", model.Description == null ? "" : model.Description));
                cmd_form1.Parameters.Add(new SqlParameter("@InitialBudget", model.InitialBudget == null ? "" : model.InitialBudget));
                cmd_form1.Parameters.Add(new SqlParameter("@Status1", model.Status1 == null ? "" : model.Status1));
                cmd_form1.Parameters.Add(new SqlParameter("@BusinessNeed", model.GlobalProcessNumber == null ? "" : model.GlobalProcessNumber));
                cmd_form1.Parameters.Add(new SqlParameter("@BestBidOffer", model.BestBidOffer == null ? "" : model.BestBidOffer));
                cmd_form1.Parameters.Add(new SqlParameter("@OrderVolume", model.OrderVolume == null ? "" : model.OrderVolume));
                cmd_form1.Parameters.Add(new SqlParameter("@TransactionVolume", model.TransactionVolume == null ? "" : model.TransactionVolume));
                cmd_form1.Parameters.Add(new SqlParameter("@DiffBudgetAmount", model.DiffBudgetAmount == null ? "" : model.DiffBudgetAmount));
                cmd_form1.Parameters.Add(new SqlParameter("@Status2", model.Status2 == null ? "" : model.Status2));
                cmd_form1.Parameters.Add(new SqlParameter("@FormID", formId));
                if (model.ActionType == "ISC")
                {
                    object BidderApprovalDate = DBNull.Value;
                    object RFQReceiptDate = DBNull.Value;
                    object RFQSentDate = DBNull.Value;
                    object OfferReceiptDate = DBNull.Value;
                    object SFODate = DBNull.Value;
                    object TargetClouseDate = DBNull.Value;
                    if (model.BidderApprovalDate != null)
                        BidderApprovalDate = model.BidderApprovalDate;
                    if (model.RFQReceiptDate != null)
                        RFQReceiptDate = model.RFQReceiptDate;
                    if (model.RFQSentDate != null)
                        RFQSentDate = model.RFQSentDate;
                    if (model.OfferReceiptDate != null)
                        OfferReceiptDate = model.OfferReceiptDate;
                    if (model.SFODate != null)
                        SFODate = model.SFODate;
                    if (model.TargetClouseDate != null)
                        TargetClouseDate = model.TargetClouseDate;

                    cmd_form1.Parameters.Add(new SqlParameter("@BidderApprovalDate", BidderApprovalDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@RFQReceiptDate", RFQReceiptDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@RFQSentDate", RFQSentDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@OfferReceiptDate", OfferReceiptDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@SFODate", SFODate));
                    cmd_form1.Parameters.Add(new SqlParameter("@TargetClouseDate", TargetClouseDate));
                }
                else
                {
                    cmd_form1.Parameters.Add(new SqlParameter("@BidderApprovalDate", model.BidderApprovalDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@RFQReceiptDate", model.RFQReceiptDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@RFQSentDate", model.RFQSentDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@OfferReceiptDate", model.OfferReceiptDate));
                    cmd_form1.Parameters.Add(new SqlParameter("@SFODate", model.SFODate));
                    cmd_form1.Parameters.Add(new SqlParameter("@TargetClouseDate", model.TargetClouseDate));
                }
                cmd_form1.CommandType = CommandType.StoredProcedure;
                adapter_form1.SelectCommand = cmd_form1;
                con.Open();
                adapter_form1.Fill(ds3);
                con.Close();
                if (ds3.Tables[0].Rows.Count > 0 && ds3.Tables[0] != null)
                {
                    for (int i = 0; i < ds3.Tables[0].Rows.Count; i++)
                    {
                        RowId = Convert.ToInt32(ds3.Tables[0].Rows[i]["RowId"]);
                    }
                }

                //newRow.Update();
                //_context.Load(newRow);
                //_context.ExecuteQuery();

                //RowId = newRow.Id;

                result.Status = 200;
                result.Message = formId.ToString();

                var approverIdList = response.Model;

                //string fullPath = Environment.CurrentDirectory + "\\Attachment\\ISCLSCAttachment\\" + formId;
                //if (!Directory.Exists(fullPath))
                //{
                //    Directory.CreateDirectory(fullPath);
                //}

                //var fileExtension = Path.GetExtension(file.FileName);
                //var fileName1 = Path.GetFileNameWithoutExtension(file.FileName);
                //var date = DateTime.Now;
                //string fileName = (fileName1 + date.ToString("dd-MM-yyy-HH-mm-ss")).Replace(" ", string.Empty) + fileExtension;
                //using (FileStream stream = new FileStream(Path.Combine(fullPath, fileName), FileMode.Create))
                //{
                //    file.CopyTo(stream);
                //}
                string path = "";
                if (files != null)
                {
                    path = System.Web.HttpContext.Current.Server.MapPath("~/Attachment/ISCLSCF/"+ formId+"/");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    else
                    {
                        Directory.Delete(path, recursive: true);
                        Directory.CreateDirectory(path);
                    }
                    files.SaveAs(path + Path.GetFileName(files.FileName));
                    path = "~/Attachment/ISCLSCF/" + formId + "/"+ files.FileName;
                    cmd_form = new SqlCommand();
                    DataSet ds2 = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveISCLSCFormAttachment", con);
                    cmd_form.Parameters.Add(new SqlParameter("@TableName", listName));
                    cmd_form.Parameters.Add(new SqlParameter("@RowId", RowId == null || RowId == 0 ? 0 : Convert.ToInt64(RowId)));
                    cmd_form.Parameters.Add(new SqlParameter("@Path", path == null || path == "" ? "" : path));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds2);
                    con.Close();
                }
                #region Comment
                //if (file != null)
                //{
                //    int attachmentID = RowId;

                //    string path = file.FileName;
                //    path = path.Replace(" ", "");

                //    string FileName = "First_" + path;

                //    //List docs = web.Lists.GetByTitle(listName);
                //    //ListItem itemAttach = docs.GetItemById(attachmentID);

                //    var attInfo = new AttachmentCreationInformation();

                //    attInfo.FileName = FileName;

                //    byte[] fileData = null;
                //    using (var binaryReader = new BinaryReader(file.InputStream))
                //    {
                //        fileData = binaryReader.ReadBytes(file.ContentLength);
                //    }

                //    attInfo.ContentStream = new MemoryStream(fileData);

                //    //Attachment att = itemAttach.AttachmentFiles.Add(attInfo); //Add to File

                //    //_context.Load(att);
                //    //_context.ExecuteQuery();
                //}
                ////var attachedfile = model["attachedfile"];
                //var attachedfile = model.attachedfile;
                //if (attachedfile != null && attachedfile != "")
                //{
                //    int startListID = Convert.ToInt32(model.FormSrId);

                //    Site oSite = _context.Site;
                //    _context.Load(oSite);
                //    _context.ExecuteQuery();

                //    _context.Load(web);
                //    _context.ExecuteQuery();

                //    CamlQuery query = new CamlQuery();
                //    query.ViewXml = @"";

                //    List oList = _context.Web.Lists.GetByTitle(listName);
                //    _context.Load(oList);
                //    _context.ExecuteQuery();

                //    ListItemCollection items = oList.GetItems(query);
                //    _context.Load(items);
                //    _context.ExecuteQuery();
                //    byte[] fileContents = null;

                //    Folder folder = web.GetFolderByServerRelativeUrl(oSite.Url + "/Lists/" + listName + "/Attachments/" + startListID);

                //    _context.Load(folder);
                //    _context.ExecuteQuery();

                //    FileCollection attachments = folder.Files;
                //    _context.Load(attachments);
                //    _context.ExecuteQuery();

                //    foreach (Microsoft.SharePoint.Client.File oFile in folder.Files)
                //    {

                //        FileInfo myFileinfo = new FileInfo(oFile.Name);
                //        WebClient clientFile = new WebClient();
                //        clientFile.Credentials = _context.Credentials;

                //        string SharepointSiteURL = ConfigurationManager.AppSettings["SharepointSiteURL"];

                //        fileContents = clientFile.DownloadData(SharepointSiteURL + oFile.ServerRelativeUrl);

                //    }

                //    var attachedfileName = model.attachedfileName;
                //    int attachmentID = RowId;

                //    string path = attachedfileName;
                //    path = path.Replace(" ", "");
                //    string FileName = path;

                //    List docs = web.Lists.GetByTitle(listName);
                //    ListItem itemAttach = docs.GetItemById(attachmentID);

                //    var attInfo = new AttachmentCreationInformation();

                //    attInfo.FileName = FileName;

                //    attInfo.ContentStream = new MemoryStream(fileContents);

                //    Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                //    _context.Load(att);
                //    _context.ExecuteQuery();
                //}
                #endregion

                //Task Entry in Approval Master List
                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, model.GlobalProcessNumber ?? "", RowId, formId);

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
                emailService.SendMailForISLS(emailData, GPNo);

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

        /// <summary>
        /// CRF-It is used for calculating the approvers.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalISCLSCForm(long empNum, long ccNum, string empDes, ISCLSData model)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ISLSForm", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empDes", empDes));
                cmd.Parameters.Add(new SqlParameter("@ActionType", model.ActionType));
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

        public async Task<dynamic> ViewISCLSCFormData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                #region Comment
                //              var handler = new HttpClientHandler();
                //              handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //              var client1 = new HttpClient(handler);
                //              client1.BaseAddress = new Uri(conString);
                //              client1.DefaultRequestHeaders.Accept.Clear();
                //              client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //              var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('ISCLSCForm')/items?$select=*"
                //+ "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                //              var responseText1 = await response1.Content.ReadAsStringAsync();
                //              var settings = new JsonSerializerSettings
                //              {
                //                  NullValueHandling = NullValueHandling.Ignore
                //              };
                //              if (!string.IsNullOrEmpty(responseText1))
                //              {
                //                  var SUCFUserResult = JsonConvert.DeserializeObject<ISCLSCModel>(responseText1, settings);
                //                  URCFData.one = SUCFUserResult.ISCLSResults.ISCLSData;
                //              }
                #endregion
                List<ISCLSData> item = new List<ISCLSData>();
                ISCLSData model = new ISCLSData();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("ViewISCLSCFDetails", con);
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
                        {
                            item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            item1.Created = Convert.ToDateTime(dt.Rows[i]["Created"]);
                        }
                        model.FormIDISLS = item1;        // For PDF 
                        //Author AI = new Author();
                        //AI.Title = Convert.ToString(dt.Rows[i]["SubmitterUserName"]);
                        //model.Author = AI;
                        model.FormSrId = Convert.ToString(dt.Rows[0]["FormID"]);
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
                        model.BusinessNeed = Convert.ToString(dt.Rows[0]["BusinessNeed"]);
                        model.RequestSubmissionFor = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]);
                        model.ActionType = Convert.ToString(dt.Rows[0]["ActionType"]);
                        model.BuyerName = Convert.ToString(dt.Rows[0]["BuyerName"]);
                        model.Team = Convert.ToString(dt.Rows[0]["Team"]);
                        model.GlobalProcessNumber = Convert.ToString(dt.Rows[0]["GlobalProcessNumber"]);
                        model.Description = Convert.ToString(dt.Rows[0]["Description"]);
                        model.InitialBudget = Convert.ToString(dt.Rows[0]["InitialBudget"]);
                        model.Status = Convert.ToString(dt.Rows[0]["Status"]);
                        model.BestBidOffer = Convert.ToString(dt.Rows[0]["BestBidOffer"]);
                        model.OrderVolume = Convert.ToString(dt.Rows[0]["OrderVolume"]);
                        model.TransactionVolume = Convert.ToString(dt.Rows[0]["TransactionVolume"]);
                        model.DiffBudgetAmount = Convert.ToString(dt.Rows[0]["DiffBudgetAmount"]);
                        model.Status1 = Convert.ToString(dt.Rows[0]["Status1"]);
                        model.Status2 = Convert.ToString(dt.Rows[0]["Status2"]);
                        model.AttachmentPath = Convert.ToString(dt.Rows[0]["AttachmentPath"]);
                        DateTime? BidderApprovalDate = null;
                        DateTime? RFQReceiptDate = null;
                        DateTime? RFQSentDate = null;
                        DateTime? OfferReceiptDate = null;
                        DateTime? SFODate = null;
                        DateTime? TargetClouseDate = null;
                        if (dt.Rows[0]["BidderApprovalDate"] != DBNull.Value)
                            BidderApprovalDate = Convert.ToDateTime(dt.Rows[0]["BidderApprovalDate"]);
                        model.BidderApprovalDate = BidderApprovalDate;
                        if (dt.Rows[0]["RFQReceiptDate"] != DBNull.Value)
                            RFQReceiptDate = Convert.ToDateTime(dt.Rows[0]["RFQReceiptDate"]);
                        if (dt.Rows[0]["RFQSentDate"] != DBNull.Value)
                            RFQSentDate = Convert.ToDateTime(dt.Rows[0]["RFQSentDate"]);
                        if (dt.Rows[0]["OfferReceiptDate"] != DBNull.Value)
                            OfferReceiptDate = Convert.ToDateTime(dt.Rows[0]["OfferReceiptDate"]);
                        if (dt.Rows[0]["SFODate"] != DBNull.Value)
                            SFODate = Convert.ToDateTime(dt.Rows[0]["SFODate"]);
                        if (dt.Rows[0]["TargetClouseDate"] != DBNull.Value)
                            TargetClouseDate = Convert.ToDateTime(dt.Rows[0]["TargetClouseDate"]);

                        model.TargetClouseDate = TargetClouseDate;
                        model.RFQReceiptDate = RFQReceiptDate;
                        model.RFQSentDate = RFQSentDate;
                        model.OfferReceiptDate = OfferReceiptDate;
                        model.SFODate = SFODate;
                        item.Add(model);
                    }
                }
                URCFData.one = item;
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

        public async Task<int> UpdateData(ISCLSData model, UserData user, HttpPostedFileBase file)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("ISLS") ? GlobalClass.ListNames["ISLS"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {
                int RowId = 0;

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);

                newItem["BidderApprovalDate"] = model.BidderApprovalDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["RFQReceiptDate"] = model.RFQReceiptDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["RFQSentDate"] = model.RFQSentDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["OfferReceiptDate"] = model.OfferReceiptDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["SFODate"] = model.SFODate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["BestBidOffer"] = model.BestBidOffer;
                newItem["OrderVolume"] = model.OrderVolume;
                newItem["TransactionVolume"] = model.TransactionVolume;
                newItem["DiffBudgetAmount"] = model.DiffBudgetAmount;
                newItem["TargetClouseDate"] = model.TargetClouseDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["Status2"] = model.Status2;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
                RowId = newItem.Id;

                #region Comment
                //if (file != null)
                //{
                //    int attachmentID = RowId;

                //    string path = file.FileName;
                //    path = path.Replace(" ", "");
                //    string FileName = path;

                //    List docs = web.Lists.GetByTitle(listName);
                //    ListItem itemAttach = docs.GetItemById(attachmentID);

                //    var attInfo = new AttachmentCreationInformation();

                //    attInfo.FileName = FileName;

                //    byte[] fileData = null;
                //    using (var binaryReader = new BinaryReader(file.InputStream))
                //    {
                //        fileData = binaryReader.ReadBytes(file.ContentLength);
                //    }

                //    attInfo.ContentStream = new MemoryStream(fileData);

                //    Attachment att = itemAttach.AttachmentFiles.Add(attInfo); //Add to File

                //    _context.Load(att);
                //    _context.ExecuteQuery();
                //}
                ////var attachedfile = model["attachedfile"];
                //var attachedfile = model.attachedfile;
                //if (attachedfile != null && attachedfile != "")
                //{
                //    int startListID = Convert.ToInt32(model.FormSrId);

                //    Site oSite = _context.Site;
                //    _context.Load(oSite);
                //    _context.ExecuteQuery();

                //    _context.Load(web);
                //    _context.ExecuteQuery();

                //    CamlQuery query = new CamlQuery();
                //    query.ViewXml = @"";

                //    List oList = _context.Web.Lists.GetByTitle(listName);
                //    _context.Load(oList);
                //    _context.ExecuteQuery();

                //    ListItemCollection items = oList.GetItems(query);
                //    _context.Load(items);
                //    _context.ExecuteQuery();
                //    byte[] fileContents = null;

                //    Folder folder = web.GetFolderByServerRelativeUrl(oSite.Url + "/Lists/" + listName + "/Attachments/" + startListID);

                //    _context.Load(folder);
                //    _context.ExecuteQuery();

                //    FileCollection attachments = folder.Files;
                //    _context.Load(attachments);
                //    _context.ExecuteQuery();

                //    foreach (Microsoft.SharePoint.Client.File oFile in folder.Files)
                //    {

                //        FileInfo myFileinfo = new FileInfo(oFile.Name);
                //        WebClient clientFile = new WebClient();
                //        clientFile.Credentials = _context.Credentials;

                //        string SharepointSiteURL = ConfigurationManager.AppSettings["SharepointSiteURL"];

                //        fileContents = clientFile.DownloadData(SharepointSiteURL + oFile.ServerRelativeUrl);

                //    }

                //    var attachedfileName = model.attachedfileName;
                //    int attachmentID = RowId;

                //    string path = attachedfileName;
                //    path = path.Replace(" ", "");
                //    string FileName = path;

                //    List docs = web.Lists.GetByTitle(listName);
                //    ListItem itemAttach = docs.GetItemById(attachmentID);

                //    var attInfo = new AttachmentCreationInformation();

                //    attInfo.FileName = FileName;

                //    attInfo.ContentStream = new MemoryStream(fileContents);

                //    Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                //    _context.Load(att);
                //    _context.ExecuteQuery();
                //}
                #endregion
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

        public async Task<int> UpdateDataForLST(ISCLSData model, UserData user, HttpPostedFileBase file)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("ISLS") ? GlobalClass.ListNames["ISLS"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {
                int RowId = 0;

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);

                newItem["BidderApprovalDate"] = model.BidderApprovalDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["BidderApprovalDate"] = model.BidderApprovalDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["RFQReceiptDate"] = model.RFQReceiptDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["RFQSentDate"] = model.RFQSentDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["OfferReceiptDate"] = model.OfferReceiptDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["SFODate"] = model.SFODate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["BestBidOffer"] = model.BestBidOffer;
                newItem["OrderVolume"] = model.OrderVolume;
                newItem["TransactionVolume"] = model.TransactionVolume;
                newItem["DiffBudgetAmount"] = model.DiffBudgetAmount;
                newItem["TargetClouseDate"] = model.TargetClouseDate.Value.ToString("yyyy-MM-dd 23:59:59");
                newItem["Status2"] = model.Status2;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
                RowId = newItem.Id;

                //if (file != null)
                //{
                //    int attachmentID = RowId;

                //    string path = file.FileName;
                //    path = path.Replace(" ", "");
                //    string FileName = path;

                //    List docs = web.Lists.GetByTitle(listName);
                //    ListItem itemAttach = docs.GetItemById(attachmentID);

                //    var attInfo = new AttachmentCreationInformation();

                //    attInfo.FileName = FileName;

                //    byte[] fileData = null;
                //    using (var binaryReader = new BinaryReader(file.InputStream))
                //    {
                //        fileData = binaryReader.ReadBytes(file.ContentLength);
                //    }

                //    attInfo.ContentStream = new MemoryStream(fileData);

                //    Attachment att = itemAttach.AttachmentFiles.Add(attInfo); //Add to File

                //    _context.Load(att);
                //    _context.ExecuteQuery();
                //}
                ////var attachedfile = model["attachedfile"];
                //var attachedfile = model.attachedfile;
                //if (attachedfile != null && attachedfile != "")
                //{
                //    int startListID = Convert.ToInt32(model.FormSrId);

                //    Site oSite = _context.Site;
                //    _context.Load(oSite);
                //    _context.ExecuteQuery();

                //    _context.Load(web);
                //    _context.ExecuteQuery();

                //    CamlQuery query = new CamlQuery();
                //    query.ViewXml = @"";

                //    List oList = _context.Web.Lists.GetByTitle(listName);
                //    _context.Load(oList);
                //    _context.ExecuteQuery();

                //    ListItemCollection items = oList.GetItems(query);
                //    _context.Load(items);
                //    _context.ExecuteQuery();
                //    byte[] fileContents = null;

                //    Folder folder = web.GetFolderByServerRelativeUrl(oSite.Url + "/Lists/" + listName + "/Attachments/" + startListID);

                //    _context.Load(folder);
                //    _context.ExecuteQuery();

                //    FileCollection attachments = folder.Files;
                //    _context.Load(attachments);
                //    _context.ExecuteQuery();

                //    foreach (Microsoft.SharePoint.Client.File oFile in folder.Files)
                //    {

                //        FileInfo myFileinfo = new FileInfo(oFile.Name);
                //        WebClient clientFile = new WebClient();
                //        clientFile.Credentials = _context.Credentials;

                //        string SharepointSiteURL = ConfigurationManager.AppSettings["SharepointSiteURL"];

                //        fileContents = clientFile.DownloadData(SharepointSiteURL + oFile.ServerRelativeUrl);

                //    }

                //    var attachedfileName = model.attachedfileName;
                //    int attachmentID = RowId;

                //    string path = attachedfileName;
                //    path = path.Replace(" ", "");
                //    string FileName = path;

                //    List docs = web.Lists.GetByTitle(listName);
                //    ListItem itemAttach = docs.GetItemById(attachmentID);

                //    var attInfo = new AttachmentCreationInformation();

                //    attInfo.FileName = FileName;

                //    attInfo.ContentStream = new MemoryStream(fileContents);

                //    Attachment att = itemAttach.AttachmentFiles.Add(attInfo);

                //    _context.Load(att);
                //    _context.ExecuteQuery();
                //}

            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }


        public async Task<int> UpdateBidApprovalDate(ISCLSData model, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("ISLS") ? GlobalClass.ListNames["ISLS"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);
                newItem["BidderApprovalDate"] = model.BidderApprovalDate.Value.ToString("yyyy-MM-dd 23:59:59");

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

        public async Task<int> ValidateGPNo(string GlobalProcessNo)
        {
            int formId = 40;
            var formData = new List<FormData>();
            var handler = new HttpClientHandler();
            GlobalClass gc = new GlobalClass();
            var currentUser = gc.GetCurrentUser();
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            try
            {
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ISCLSCForm')/items?$select=GlobalProcessNumber&$filter=(GlobalProcessNumber eq '" + GlobalProcessNo + "')");
                var responseText2 = await response2.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText2))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText2);
                    formData = modelResult.Data.Forms;
                }

                if (formData != null)
                {
                    return formData.Count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }

    }
}
using Skoda_DCMS.App_Start;
using Microsoft.SharePoint.Client;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;
using System.Dynamic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Data;
using System.Xml;
using Skoda_DCMS.Extension;

namespace Skoda_DCMS.DAL
{
    public class MaterialRequestDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        /// <summary>
        /// ReissueIDCard-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> CreateMaterialRequest(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "MRF";
            string formName = "Material Request Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
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

                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Material Request Form";
                    item["UniqueFormName"] = "MRF";
                    item["FormParentId"] = 30;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "MaterialRequest";
                    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
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
                    item["FormName"] = "Material Request Form";
                    item["UniqueFormName"] = "MRF";
                    item["FormParentId"] = 30;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "MaterialRequest";
                    item["BusinessNeed"] = form["txtBusinessNeed"] ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
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
                var newRow = userDetailsResponse.Model;
                newRow["BusinessNeed"] = form["txtBusinessNeed"];
                newRow["RequestNumber"] = form["txtRequestNumber"];
                newRow["RequestTo"] = form["drpRequestTo"];
                newRow["RequestFrom"] = form["drpRequestFrom"];
                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                result.Status = 200;
                result.Message = formId.ToString();
               

                RowId = newRow.Id;
                result.RowId = RowId.ToString();
                result.IsResubmit = IsResubmit;
                //result.Value = form["txtRequestNumber"];

                if (file != null)
                {
                    int attachmentID = newRow.Id;

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
                    int attachmentID = newRow.Id;

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

                List MaterialDetals = web.Lists.GetByTitle("MaterialDetails");

                string pattern = ",";

                var partNumber = form["txtPartNumPost"];
                var partDescription = form["txtPartDescPost"];
                var quantity = form["txtQtyPost"];
                var remark = form["txtRmkPost"];

                var partNumbers = partNumber.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                partNumbers = partNumbers.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var partDescriptions = partDescription.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                partDescriptions = partDescriptions.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var quantites = quantity.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                quantites = quantites.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var remarks = remark.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                remarks = remarks.Where(s => !string.IsNullOrEmpty(s)).ToList();

                for (int i = 0; i < partNumbers.Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newFormItem = MaterialDetals.AddItem(itemCreate);
                    newFormItem["MaterialRequestID"] = RowId;
                    newFormItem["PartNumber"] = partNumbers[i].Replace("|!", ",");
                    newFormItem["PartDescription"] = partDescriptions[i].Replace("|!", ",");
                    newFormItem["Quantity"] = quantites[i].Replace("|!", ",");
                    newFormItem["Remarks"] = remarks[i].Replace("|!", ",");
                    newFormItem["FormID"] = formId;
                    newFormItem.Update();
                    _context.ExecuteQuery();
                }

                var response = await GetApprovalForMaterialRequest(user);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approverIdList = response.Model;

                //Task Entry in Approval Master List
                var approvalResponse = await SaveApprovalMasterData(approverIdList, form["txtBusinessNeed"] ?? "", RowId, formId);

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

                //var emailService = new EmailService();
                //emailService.SendMail(emailData);


            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
            }
            return result;
        }


        public async Task<dynamic> GetMaterialRequestDetails(int rowId, int formId)
        {
            dynamic materialRequest = new ExpandoObject();
            List<MaterialRequestData> MainList = new List<MaterialRequestData>();
            List<MaterialDetailsData> OtherList = new List<MaterialDetailsData>();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('MaterialRequestForm')/items?$select=*,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<MaterialRequestModel>(responseText, settings);
                    MainList = result.List.MaterialRequestList;
                }
                materialRequest.one = MainList;
                //approval start
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('MaterialDetails')/items?$select=*&$filter=(MaterialRequestID eq '" + rowId + "')");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var result1 = JsonConvert.DeserializeObject<MaterialDetailsModel>(responseText2);
                OtherList = result1.List.MaterialDetailsList;
                materialRequest.two = OtherList;

                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                materialRequest.three = r1.Model;
                materialRequest.four = r2.Model;
                            
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return materialRequest;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForMaterialRequest(UserData user)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_MaterialRequestApproval", con);

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
                        app.Designation = Convert.ToString(ds.Tables[0].Rows[i]["Designation"]);
                        app.Logic = Convert.ToString(ds.Tables[0].Rows[i]["Logic"]);
                        app.ApprovalLevel = (int)ds.Tables[0].Rows[i]["Level"];                        
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

                //var common = new CommonDAL();
                //appList = common.AddMDAssistantToList(appList);
                //appList = common.ChangeDelegateApprover(appList);

                var common = new CommonDAL();
                appList = common.CallAssistantAndDelegateFunc(appList);

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.Timeout = TimeSpan.FromSeconds(10);
                var emailString = "";
                var count = appList.Count;

                if (adCode.ToLower() == "yes")
                {
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
                    }
                    //AD Code
                }
                else
                {
                    //Local Code :- Sharepoint Code
                    for (int i = 0; i < count; i++)
                    {
                        var email = appList[i];
                        emailString = $"EMail eq '{email.EmailId}'";

                        var response = await client.GetAsync($"_api/web/SiteUserInfoList/items?$select=Id,Title,EMail&$filter=({emailString})");
                        var responseText = await response.Content.ReadAsStringAsync();

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(responseText.ToString());
                        var title = doc.GetElementsByTagName("d:Title");
                        var id = doc.GetElementsByTagName("d:Id");
                        var emails = doc.GetElementsByTagName("d:EMail");

                        var currentId = Convert.ToInt32(id[0].InnerXml);

                        appList[i].ApproverUserName = Convert.ToString(currentId);
                    }
                    //Local Code :- Sharepoint Code
                }
                   
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
            }
        }

        public async Task<int> UpdateRequestNumber(UserData user, string rowId)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("MRF") ? GlobalClass.ListNames["MRF"] : "";
            if (listName == "")
                return 0;
            //long formId = Convert.ToInt64(form["FormId"]);
           // requestYear = DateTime.Now.Year; 
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetMaterialRequestNumber", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                long MaterialRequestNumber = 0;

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        MaterialRequestNumber = Convert.ToInt64(ds.Tables[0].Rows[i]["RequestNumber"]);
                    }
                }
                else
                {
                    return 0;
                }
                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(rowId);
                newItem["RequestNumber"] = DateTime.Now.Year+"_"+MaterialRequestNumber;
               
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
        static DateTime LastDayOfYear(DateTime d)
        {
            DateTime n = new DateTime(d.Year + 1, 1, 1);
            return n.AddDays(-1);
        }
    }
}
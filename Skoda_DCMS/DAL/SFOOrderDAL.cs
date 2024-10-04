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
    public class SFOOrderDAL
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


        public async Task<ResponseModel<object>> SaveOrder(System.Web.Mvc.FormCollection form, UserData user, HttpPostedFileBase file)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            //dynamic result = new ExpandoObject();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            int formId = 0;

            Web web = _context.Web;
            string formShortName = "SFO";
            string formName = "Suggestion for Order Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                //result.one = 0;
                //result.two = 0;
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }

            int FormId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            var onBehalfEmail = form["txtOnBehalfEmail"];
            var selfOnBehalf = form["drpRequestSubType"];
            bool IsResubmit = FormId == 0 ? false : true;

            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                long ccNum = requestSubmissionFor == "Self" ? user.CostCenter : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherCostcenterCode"]) : Convert.ToInt64(form["txtOtherNewCostcenterCode"]));
                long empNum = requestSubmissionFor == "Self" ? user.EmpNumber : (otherEmpType == "SAVWIPLEmployee" ? Convert.ToInt64(form["txtOtherEmployeeCode"]) : Convert.ToInt64(form["txtOtherNewEmployeeCode"]));


                string pattern = ",";
                var SuppName = form["txtSuppName"];
                var TechAccept = form["drpAccept"];
                var Price = form["txtOfferPricePost"];
                var Comment = form["txtComment"];
                var Currency = form["drpOfferCurrency"];
                var ConversionRate = form["txtConversionRate"];
                string[] valuesSuppName = form.GetValues("txtSuppName");
                string[] valuesComment = form.GetValues("txtComment");

                var SuppNames = SuppName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                SuppNames = SuppNames.Where(s => !string.IsNullOrEmpty(s)).ToList();
                var TechAccepts = TechAccept.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                var Prices = Price.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                var ConversionRates = ConversionRate.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                var Comments = Comment.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                var Currencies = Currency.Split(new string[] { pattern }, StringSplitOptions.None).ToList();

                var acceptedDropdownVals = new List<string>() { "Ok", "Not Ok", "Other" };
                var tempConversionRate = new List<string>();
                var tempCurrencies = new List<string>();
                int j = 0;
                ConversionRates.RemoveAll(x => x == "0");

                for (int i = 0; i < TechAccepts.Count; i++)
                {
                    if (acceptedDropdownVals.Contains(TechAccepts[i]))
                    {
                        tempConversionRate.Add(ConversionRates[j]);
                        tempCurrencies.Add(Currencies[j]);
                        j++;
                    }
                    else
                    {
                        tempConversionRate.Add("");
                        tempCurrencies.Add("");
                    }
                }

                List<double> ConvertedValueList = new List<double>();

                for (int i = 0; i < Prices.Count; i++)
                {
                    if (!string.IsNullOrEmpty(Prices[i]) || !string.IsNullOrEmpty(tempConversionRate[i]))
                    {
                        ConvertedValueList.Add(Convert.ToDouble(Prices[i].Replace("|!", ",")) * Convert.ToDouble(tempConversionRate[i]));
                    }
                }

                var minValue = ConvertedValueList.Min(x => x);
                var example = form["txtOfferPrice1"];

                var level = 0;
                if (minValue <= 1000000)
                {
                    level = 1;
                }
                else if (minValue > 1000000 && minValue <= 10000000)
                {
                    level = 2;
                }
                else if (minValue > 10000000)
                {
                    level = 3;

                }

                var response = await GetApprovalForSFOForm(empNum, ccNum, level);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approverIdList = response.Model;

                //Forms List 
                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Suggestion for Order Form";
                    item["UniqueFormName"] = "SFO";
                    item["FormParentId"] = 1;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SFOOrder";
                    item["BusinessNeed"] = form["inputTextDesc"];
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
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(FormId);
                    item["FormName"] = "Suggestion For Order Form";
                    item["UniqueFormName"] = "SFO";
                    item["FormParentId"] = form["formParentId"];
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SFOOrder";
                    item["BusinessNeed"] = form["inputTextDesc"];
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
                    var resubmitResult = dal.ResubmitUpdate(formId);

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

                //Order Details List
                List OrderDetailsList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = OrderDetailsList.AddItem(itemCreateInfo);
                if (FormId == 0)
                {
                    newItem["TriggerCreateWorkflow"] = "Yes";
                }
                else
                {
                    newItem["TriggerCreateWorkflow"] = "No";
                }
                newItem["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newItem["EmployeeType"] = form["chkEmployeeType"];
                var empType = form["chkEmployeeType"];
                newItem["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newItem["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newItem["EmployeeCode"] = form["txtEmployeeCode"];
                if (empType == "External")
                {
                    newItem["EmployeeDesignation"] = "Team Member";
                }
                else
                {
                    newItem["EmployeeDesignation"] = form["ddEmpDesignation"];// DropDown selection
                }
                newItem["EmployeeLocation"] = form["ddEmpLocation"]; //Dropdown selection
                newItem["EmployeeCCCode"] = form["txtCostcenterCode"]; //
                newItem["EmployeeUserId"] = form["txtUserId"]; //SharePoint user Id
                newItem["EmployeeName"] = form["txtEmployeeName"];
                newItem["EmployeeDepartment"] = form["txtDepartment"];
                newItem["EmployeeContactNo"] = form["txtContactNo"];
                newItem["EmployeeEmailId"] = user.Email;
                //Other Employee Details
                // newItem["OnBehalfOption"] = otherEmpType;
                if (requestSubmissionFor == "OnBehalf")
                {
                    if (otherEmpType == "SAVWIPLEmployee")
                    {
                        newItem["OnBehalfOption"] = otherEmpType;
                        newItem["OtherEmployeeName"] = form["txtOtherEmployeeName"];
                        newItem["OtherEmployeeCode"] = form["txtOtherEmployeeCode"] ?? "";
                        newItem["OtherEmployeeType"] = form["chkOtherEmployeeType"] ?? "";
                        var otherEmployeeType = form["chkOtherEmployeeType"];
                        if (otherEmployeeType == "External")
                        {
                            newItem["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newItem["OtherEmployeeDesignation"] = form["ddOtherEmpDesignation"] ?? "";// DropDown selection
                        }
                        newItem["OtherEmployeeLocation"] = form["ddOtherEmpLocation"] ?? ""; //Dropdown selection
                        newItem["OtherEmployeeCCCode"] = form["txtOtherCostcenterCode"] ?? ""; //
                        newItem["OtherEmployeeUserId"] = form["txtOtherUserId"] ?? ""; //SharePoint user Id
                        newItem["OtherEmployeeDepartment"] = form["txtOtherDepartment"] ?? "";
                        newItem["OtherEmployeeContactNo"] = form["txtOtherContactNo"] ?? "";
                        newItem["OtherEmployeeEmailId"] = form["txtOtherEmailId"] ?? "";
                        newItem["OtherExternalOrganizationName"] = form["ddOtherExternalOrganizationName"] ?? "";
                        newItem["OtherExternalOtherOrgName"] = form["txtOtherExternalOtherOrganizationName"] ?? "";
                    }
                    else
                    {
                        newItem["OnBehalfOption"] = otherEmpType;
                        newItem["OtherEmployeeName"] = form["txtOtherNewEmployeeName"];
                        newItem["OtherEmployeeCode"] = form["txtOtherNewEmployeeCode"] ?? "";
                        var otherNewEmployeeType = form["chkOtherNewEmployeeType"];
                        if (otherNewEmployeeType == "External")
                        {
                            newItem["OtherEmployeeDesignation"] = "Team Member";
                        }
                        else
                        {
                            newItem["OtherEmployeeDesignation"] = form["ddOtherNewEmpDesignation"] ?? "";// DropDown selection
                        }
                        newItem["OtherEmployeeLocation"] = form["ddOtherNewEmpLocation"] ?? ""; //Dropdown selection
                        newItem["OtherEmployeeCCCode"] = form["txtOtherNewCostcenterCode"] ?? ""; //
                        newItem["OtherEmployeeUserId"] = form["txtOtherNewUserId"] ?? ""; //SharePoint user Id
                        newItem["OtherEmployeeDepartment"] = form["txtOtherNewDepartment"] ?? "";
                        newItem["OtherEmployeeContactNo"] = form["txtOtherNewContactNo"] ?? "";
                        newItem["OtherEmployeeEmailId"] = form["txtOtherNewEmailId"] ?? "";
                        newItem["OtherEmployeeType"] = form["chkOtherNewEmployeeType"] ?? "";
                        newItem["OtherExternalOrganizationName"] = form["ddOtherNewExternalOrganizationName"] ?? "";
                        newItem["OtherExternalOtherOrgName"] = form["txtOtherNewExternalOtherOrganizationName"] ?? "";
                    }
                }

                newItem["Department"] = form["txtDept"];
                newItem["ShopCartNumber"] = form["txtShopCart"];
                newItem["Date"] = form["txtDate"] == "" ? null : DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                newItem["ConcernSection"] = form["txtSection"];
                newItem["Budget"] = form["txtBudget"];
                newItem["Currency"] = form["drpCurrency"];
                newItem["Description"] = form["inputTextDesc"];
                newItem["TechDisqualify"] = form["txtTechDisqualify"];
                newItem["SuggestOrder"] = form["txtSuggOrder"];
                newItem["DeviationNoteForm"] = form["checkBoxDeviationNote"];
                decimal conversionValue = 0;
                if (form["drpCurrency"] == "Rupee")
                {
                    newItem["ConversionValue"] = 1;
                    conversionValue = 1;
                }
                else
                {
                    newItem["ConversionValue"] = form["txtConvert"].ToString();
                    conversionValue = Convert.ToDecimal(form["txtConvert"]);
                }
                decimal budgetRupeeConversion = Convert.ToDecimal(newItem["Budget"]) * conversionValue;

                newItem["FormID"] = formId;

                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();

                if (form["checkBoxDeviationNote"] == "DeviationNoteForm")
                {
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
                }

                result.Status = 200;
                result.Message = formId.ToString();

                int OrderId = newItem.Id;

                //Order Items List
                List OrderItemsList = web.Lists.GetByTitle("OrderItems");
                for (int i = 0; i < valuesSuppName.ToList().Count; i++)
                {
                    ListItemCreationInformation itemCreate = new ListItemCreationInformation();
                    ListItem newRow = OrderItemsList.AddItem(itemCreate);
                    newRow["OrderDetailsID"] = OrderId;
                    newRow["SupplierName"] = valuesSuppName[i];
                    newRow["TechAcceptance"] = TechAccepts[i];
                    newRow["OfferPrice"] = Prices[i].Replace("|!", ",");
                    newRow["Comments"] = valuesComment[i];
                    newRow["Currency"] = tempCurrencies[i];
                    newRow["ConversionRate"] = tempConversionRate[i];
                    newRow["FormId"] = formId;
                    newRow.Update();
                    _context.ExecuteQuery();
                }

                //Task Entry in Approval Master List
                var rowid = newItem.Id;
                List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                for (var i = 0; i < approverIdList.Count; i++)
                {
                    ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                    ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);

                    approvalMasteritem["FormId"] = formId;
                    approvalMasteritem["RowId"] = rowid;
                    approvalMasteritem["ApproverUserName"] = approverIdList[i].ApproverUserName;
                    approvalMasteritem["Designation"] = approverIdList[i].Designation;
                    approvalMasteritem["Level"] = approverIdList[i].ApprovalLevel;
                    approvalMasteritem["Logic"] = approverIdList[i].Logic;

                    if (approverIdList[i].ApprovalLevel == 1)
                    {
                        approvalMasteritem["IsActive"] = 1;
                    }
                    else
                    {
                        approvalMasteritem["IsActive"] = 0;
                    }

                    if (approverIdList[i].ApprovalLevel == approverIdList.Max(p => p.ApprovalLevel))
                    {
                        approvalMasteritem["NextApproverId"] = 0;
                    }
                    else
                    {
                        //var currentApproverLevel = approverIdList[i].ApprovalLevel;
                        //approvalMasteritem["NextApproverId"] = approverIdList.Any(p => p.ApprovalLevel == currentApproverLevel + 1) ? approverIdList.Where(p => p.ApprovalLevel == currentApproverLevel + 1).FirstOrDefault().ApproverUserName : "";
                        approvalMasteritem["NextApproverId"] = 0;
                    }

                    approvalMasteritem["ApproverStatus"] = "Pending";
                    approvalMasteritem["RunWorkflow"] = "No";
                    approvalMasteritem["BusinessNeed"] = form["inputTextDesc"] ?? "";

                    approvalMasteritem["DelegatedByEmpNo"] = approverIdList[i].DelegatedByEmpNum;

                    approvalMasteritem["ApproverName"] = approverIdList[i].ApproverName;

                    approvalMasteritem.Update();
                    _context.Load(approvalMasteritem);
                    _context.ExecuteQuery();
                }

                //Data Row ID Update in Forms List
                List formslist = _context.Web.Lists.GetByTitle("Forms");
                ListItem newFormItem = formslist.GetItemById(formId);
                newFormItem.RefreshLoad();
                _context.ExecuteQuery();
                newFormItem["DataRowId"] = rowid;
                newFormItem.Update();
                _context.Load(newFormItem);
                _context.ExecuteQuery();

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, rowid);
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
        /// SFO-It is used to get the approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalForSFOForm(long empNum, long ccNum, int level)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SFO", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@level", level));
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
                client.Timeout = TimeSpan.FromSeconds(10);
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
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." }; ;
            }

        }

        /// <summary>
        /// SFO-It is used for viewing the SFO form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> ViewForm(int rowId, int formId)
        {
            string codeStatus = "";
            dynamic sfo = new ExpandoObject();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('OrderDetails')/items?$select=ID, EmployeeType, EmployeeCode, " +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDesignation,EmployeeDepartment," +
                    "OtherEmployeeType,OtherEmployeeCode,OtherEmployeeCCCode,OtherEmployeeUserId,OtherEmployeeName,OtherEmployeeContactNo,OtherEmployeeDesignation,OtherEmployeeDepartment," +
                    "OtherEmployeeLocation,EmployeeEmailId,OtherEmployeeEmailId,OnBehalfOption,OtherExternalOrganizationName,OtherExternalOtherOrgName,ExternalOrganizationName,ExternalOtherOrganizationName," +
                    "EmployeeLocation,RequestSubmissionFor,Department, Date,ShopCartNumber," +
                    "ConcernSection,Budget,Currency,Description,TechDisqualify,SuggestOrder,ConversionValue,AttachmentFiles,DeviationNoteForm,FormID/ID,FormID/Created,FormID/Status&$filter=(ID eq '" + rowId + "')&$expand=FormID,AttachmentFiles");
                var responseText = await response.Content.ReadAsStringAsync();
                codeStatus += " rt1";
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<SuggestionForOrderModel>(responseText, settings);
                    sfo.one = result.List.SuggestionForOrderList;
                }

                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('OrderItems')/items?$select=*&$filter=(OrderDetailsID eq '" + rowId + "' and FormId eq '" + formId + "')");
                var responseText2 = await response2.Content.ReadAsStringAsync();

                codeStatus += " rt2";

                var result1 = JsonConvert.DeserializeObject<SFOOrderItemsModel>(responseText2, settings);

                List<string> list = new List<string>();
                foreach (var item in result1.List.SFOOrderItemsList)
                {
                    Convert.ToDecimal(item.OfferPrice).ToString("n0");
                }

                sfo.two = result1.List.SFOOrderItemsList;
                sfo.Count = sfo.two.Count;


                var client3 = new HttpClient(handler);
                client3.BaseAddress = new Uri(conString);
                client3.DefaultRequestHeaders.Accept.Clear();
                client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response3 = await client3.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,ApproverName,ApproverUserName,NextApproverId,Level,Logic,TimeStamp,Designation,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText3 = await response3.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText3, settings);
                var names = new List<string>();
                codeStatus += " rt3";
                if (modelData.Node.Data.Count > 0)
                {
                    var client4 = new HttpClient(handler);
                    client4.BaseAddress = new Uri(conString);
                    client4.DefaultRequestHeaders.Accept.Clear();
                    client4.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var items = modelData.Node.Data;
                    var idString = "";


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
                    codeStatus += " 500";


                    if (!string.IsNullOrEmpty(responseText) && !string.IsNullOrEmpty(responseText2))
                    {
                        dynamic data3 = Json.Decode(responseText3);

                        sfo.three = data3.d.results;
                        sfo.four = items;

                    }
                }
                else
                {
                    sfo.two = new List<string>();
                    sfo.three = new List<string>();
                    sfo.four = new List<string>();
                }
                return sfo;
            }
            catch (Exception ex)
            {
                Log.Error(codeStatus + ex.Message + " Stack here " + ex.StackTrace, ex);
                return 0;
            }
        }
    }
}
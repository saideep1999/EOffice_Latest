using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using System.Configuration;
using Skoda_DCMS.Models;
using System.Security;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using Skoda_DCMS.Helpers;
using System.Web.Helpers;
using System.Dynamic;
using FormCollection = System.Web.Mvc.FormCollection;
using System.Text;
using System.Net.Http.Headers;
using System.Web.Routing;
using System.Web.Mvc;
using System.IO;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Newtonsoft.Json.Linq;
using Skoda_DCMS.Models.CommonModels;
using static Skoda_DCMS.Helpers.Flags;
using System.DirectoryServices;
using Microsoft.Web.Hosting.Administration;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Office.Word;
using TheArtOfDev.HtmlRenderer.Adapters;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Skoda_DCMS.DAL
{
    public class ListDAL
    {
        public UserData user/* = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData()*/; public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        public ListDAL()
        {
            GlobalClass obj = new GlobalClass();
            user = obj.GetCurrentUser();
        }

        public int CreateList(FormCollection form)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            Web web = _context.Web;

            ListCreationInformation creationInfo = new ListCreationInformation();
            creationInfo.Title = form["listName"];
            creationInfo.TemplateType = (int)ListTemplateType.GenericList;
            List list = web.Lists.Add(creationInfo);
            _context.Load(list);
            _context.ExecuteQuery();

            HideField(form["listName"]);

            List targetList = web.Lists.GetByTitle(form["listName"]);
            string pattern = ",";
            var fieldName = form["fieldName"];
            var fieldType = form["fieldType"];
            var txtChoice = form["txtChoice"];

            try
            {
                var fieldNames = fieldName.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                var fieldTypes = fieldType.Split(new string[] { pattern }, StringSplitOptions.None).ToList();
                var txtChoices = txtChoice.Split(new string[] { pattern }, StringSplitOptions.None).ToList();

                for (int i = 0; i < fieldNames.Count; i++)
                {
                    if (fieldNames[i] == "")
                    {
                        if (fieldTypes[i] != "Choice")
                        {
                            targetList.Fields.AddFieldAsXml("<Field DisplayName='" + fieldNames[i] + "' Type='" + fieldTypes[i] + "' />",
                            true,
                            AddFieldOptions.DefaultValue);
                            //FieldNumber fldNumber = _context.CastTo<FieldNumber>(field);
                            //fldNumber.MaximumValue = 100;
                            //fldNumber.MinimumValue = 35;
                            //fldNumber.Update();
                            _context.ExecuteQuery();
                        }
                        else if (fieldTypes[i] == "Choice")
                        {
                            var Choices = txtChoices[i].Split(new string[] { ";" }, StringSplitOptions.None).ToList();

                            string strChoice = "<Field Type='Choice' DisplayName='" + fieldNames.ElementAtOrDefault(i) + "' Format='Dropdown'><CHOICES> ";
                            //+ "<Default>Meat menu</Default>"
                            for (int j = 0; j < Choices.Count(); j++)
                            {
                                strChoice += "<CHOICE>" + Choices[j] + "</CHOICE>";
                            }
                            strChoice += "</CHOICES></Field>";
                            targetList.Fields.AddFieldAsXml(strChoice,
                            true,
                            AddFieldOptions.DefaultValue);
                            _context.ExecuteQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            //string schemaChoiceField = "<Field Type='MultiChoice' ";
            //schemaChoiceField += "DisplayName='Side dishes'> <Default>Patatoes</Default> <CHOICES> <CHOICE>Fresh vegetables</CHOICE> <CHOICE>Beans</CHOICE>";
            //schemaChoiceField += " <CHOICE>Pepper Sauce</CHOICE> </CHOICES> </Field>";
            //targetList.Fields.AddFieldAsXml(schemaChoiceField, true, AddFieldOptions.DefaultValue);
            //_context.ExecuteQuery();

            return 1;
        }

        public void HideField(string listName)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);


            List list = _context.Web.Lists.GetByTitle(listName);
            Field titleField = list.Fields.GetByInternalNameOrTitle("Title");
            titleField.Hidden = true;
            titleField.Required = false;
            titleField.Update();

            try
            {
                _context.ExecuteQuery();
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }

        }
        /// <summary>
        /// ALL-It is used to create mapping for form creation .
        /// </summary>
        /// <returns></returns>
        public async Task<(string, dynamic)> GetFormByFormName(string formName)
        {
            var mappings = GlobalClass.Mapping;
            dynamic model = null;

            //var formData = mappings.Where(x => x.Item1 == formName).Select(y => y).FirstOrDefault();
            var formData = mappings.Where(x => x.Item1.ToLower() == formName.ToLower()).Select(y => y).FirstOrDefault();

            if (formData != null && formData.Item3 != null)
            {
                model = await formData.Item3.Invoke();
            }
            return (formData.Item2, model);
            //Dictionary<string,  proc = new Dictionary<string, Func<Task<string>>>();
            //proc.Add("BEI", GetBei);
            //var value = await proc["BEI"]();
            //return value;
        }
        /// <summary>
        /// Dashboard/FormDashboard-It is used to create the mapping for form viewing.
        /// </summary>
        /// <returns></returns>
        public async Task<(string, dynamic)> GetFilledFormByFormName(string formName, int rowId, int formId)
        {
            try
            {
                var mappings = GlobalClass.Mapping;
                dynamic model = null;

                var formData = mappings.Where(x => x.Item1 == formName).Select(y => y).FirstOrDefault();
                if (formData != null && formData.Item4 != null)
                {
                    model = await formData.Item4.Invoke(rowId, formId);
                }
                return (formData.Item2, model);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return ("", "");
            }

        }

        //public async Task<List<FormData>> GetPendingForms()
        //{

        //    if (user == null)
        //        return new List<FormData>();

        //    List<FormData> result = new List<FormData>();
        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
        //        var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,RowId,NextApproverId,Modified,Author/Title,FormId/FormName," +
        //            "FormId/Id,FormId/Created,FormId/ListName,FormId/UniqueFormName&$filter=(ApproverId eq '" + user.UserId + "' and IsActive eq '1')&$expand=FormId,Author");
        //        var responseText = await response.Content.ReadAsStringAsync();

        //        if (!string.IsNullOrEmpty(responseText))
        //        {
        //            var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
        //            result = modelResult.Data.Forms;
        //        }
        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return null;
        //    }
        //}

        /// <summary>
        /// Dashboard-It is used to get pending forms from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetPendingForms(string Checked, string Filter)
        {
            CommonDAL obj = new CommonDAL();
            var handler = obj.GetHttpClientHandler();
            if (user == null)
                return new List<FormData>();

            List<FormData> result = new List<FormData>();
            List<FormData> resultNew = new List<FormData>();
            DashboardModel modelResult1 = new DashboardModel();
            DataModel dataModel = new DataModel();
            List<FormData> formDataList = new List<FormData>();


            try
            {
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                if (Checked == "1")
                {
                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataTable dt = new DataTable();

                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_getMyTask", con);
                    cmd.Parameters.Add(new SqlParameter("@ApproverName", user.UserName));
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(dt);
                    con.Close();

                    //var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,BusinessNeed,ApproverUserName,Level,Logic,RunWorkflow,Department,ApproverStatus,RowId,NextApproverId,Modified,Author/Title,FormId/FormName," +
                    //                  "FormId/Id,FormId/Created,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/Status&$filter=(ApproverUserName eq '" + user.UserName + "' and IsActive eq '1')&$expand=FormId,Author&$top=10000");
                    //var responseText = await response.Content.ReadAsStringAsync();
                    //var settings = new JsonSerializerSettings
                    //{
                    //    NullValueHandling = NullValueHandling.Ignore
                    //};
                    //var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText, settings);

                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            FormData FormDataitem = new FormData();
                            FormLookup formLookup = new FormLookup();
                            Author author = new Author();
                            FormDataitem.Id = Convert.ToInt32(dt.Rows[i]["Id"]);
                            FormDataitem.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                            FormDataitem.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
                            FormDataitem.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            FormDataitem.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                            FormDataitem.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                            FormDataitem.UniqueFormId = Convert.ToInt32(dt.Rows[i]["UniqueID"]);
                            FormDataitem.BusinessNeed = Convert.ToString(dt.Rows[i]["BusinessNeed"]);
                            FormDataitem.AuthorityToEdit = Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                            FormDataitem.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                            FormDataitem.Department = Convert.ToString(dt.Rows[i]["Department"]);
                            FormDataitem.NextApproverId = Convert.ToInt32(dt.Rows[i]["NextApproverId"]);
                            FormDataitem.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                            author.Submitter = Convert.ToString(dt.Rows[i]["ApproverUserName"]);

                            FormDataitem.Author = author;
                            formLookup.ControllerName = Convert.ToString(dt.Rows[i]["ControllerName"]);
                            formLookup.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            formLookup.FormName = Convert.ToString(dt.Rows[i]["FormName"]);
                            formLookup.FormStatus = Convert.ToString(dt.Rows[i]["Status"]);
                            formLookup.Id = Convert.ToInt32(dt.Rows[i]["Id"]);
                            formLookup.ListName = Convert.ToString(dt.Rows[i]["ListName"]);
                            formLookup.UniqueFormName = Convert.ToString(dt.Rows[i]["UniqueFormName"]);
                            FormDataitem.FormRelation = formLookup;
                            formDataList.Add(FormDataitem);
                        }
                    }
                    DashboardModel dataModel1 = new DashboardModel();
                    dataModel.Forms = formDataList;
                    modelResult1.Data = dataModel;
                    var modelResult = modelResult1;
                    result = modelResult.Data.Forms;

                    if (Filter == "2")
                    {
                        foreach (var word in modelResult.Data.Forms.ToList())
                        {
                            System.DateTime RecievedDate = word.RecievedDate;
                            System.DateTime TodayDate = DateTime.Now;
                            System.TimeSpan diffResult = TodayDate.Subtract(RecievedDate);

                            DateTime date_1 = (RecievedDate).Date;
                            DateTime date_2 = DateTime.Now.Date;
                            var numberOfDays = (date_2 - date_1).Days;
                            if (numberOfDays <= 2)
                            {
                                resultNew.Add(word);
                            }
                        }
                        result = resultNew;
                    }
                    else if (Filter == "4")
                    {
                        foreach (var word in modelResult.Data.Forms.ToList())
                        {
                            System.DateTime RecievedDate = word.RecievedDate;
                            System.DateTime TodayDate = DateTime.Now;
                            System.TimeSpan diffResult = TodayDate.Subtract(RecievedDate);

                            DateTime date_1 = (RecievedDate).Date;
                            DateTime date_2 = DateTime.Now.Date;
                            var numberOfDays = (date_2 - date_1).Days;
                            if (numberOfDays > 2 && numberOfDays <= 5)
                            {
                                resultNew.Add(word);
                            }
                        }
                        result = resultNew;
                    }
                    else if (Filter == "5")
                    {
                        foreach (var word in modelResult.Data.Forms.ToList())
                        {

                            System.DateTime RecievedDate = word.RecievedDate;
                            System.DateTime TodayDate = DateTime.Now;
                            System.TimeSpan diffResult = TodayDate.Subtract(RecievedDate);
                            DateTime date_1 = (RecievedDate).Date;
                            DateTime date_2 = DateTime.Now.Date;
                            var numberOfDays = (date_2 - date_1).Days;
                            if (numberOfDays > 5)
                            {
                                resultNew.Add(word);
                            }
                        }
                        result = resultNew;
                    }
                    else
                    {
                        modelResult = modelResult1;
                        result = modelResult.Data.Forms;
                    }
                }
                else
                {


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataTable dt1 = new DataTable();
                    var username = user.UserName;
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_MyTask", con);
                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", username));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData FormDataitem = new FormData();
                            FormLookup formLookup = new FormLookup();
                            Author author = new Author();
                            FormDataitem.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            FormDataitem.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                            FormDataitem.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            FormDataitem.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            FormDataitem.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
                            FormDataitem.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
                            FormDataitem.UniqueFormId = Convert.ToInt32(dt1.Rows[i]["UniqueID"]);
                            FormDataitem.BusinessNeed = Convert.ToString(dt1.Rows[i]["BusinessNeed"]);
                            //FormDataitem.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
                            FormDataitem.AuthorityToEdit = dt1.Rows[0]["AuthorityToEdit"] != null || dt1.Rows[0]["AuthorityToEdit"] != DBNull.Value || dt1.Rows[0]["AuthorityToEdit"] != "0" ? Convert.ToInt32(dt1.Rows[0]["AuthorityToEdit"]) : 0;
                            FormDataitem.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
                            FormDataitem.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            //FormDataitem.NextApproverId = Convert.ToInt32(dt1.Rows[i]["NextApproverId"]);
                            FormDataitem.NextApproverId = dt1.Rows[0]["NextApproverId"] != null || dt1.Rows[0]["NextApproverId"] != DBNull.Value || dt1.Rows[0]["NextApproverId"] != "0" ? Convert.ToInt32(dt1.Rows[0]["NextApproverId"]) : 0;
                            FormDataitem.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);

                            author.Submitter = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);

                            FormDataitem.Author = author;
                            formLookup.ControllerName = Convert.ToString(dt1.Rows[i]["ControllerName"]);
                            formLookup.CreatedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            formLookup.FormName = Convert.ToString(dt1.Rows[i]["FormName"]);
                            formLookup.FormStatus = Convert.ToString(dt1.Rows[i]["Status"]);
                            //formLookup.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            formLookup.Id = Convert.ToInt32(dt1.Rows[i]["UniqueID"]);
                            formLookup.ListName = Convert.ToString(dt1.Rows[i]["ListName"]);
                            formLookup.UniqueFormName = Convert.ToString(dt1.Rows[i]["UniqueFormName"]);
                            FormDataitem.FormRelation = formLookup;
                            formDataList.Add(FormDataitem);
                        }
                    }
                    DashboardModel dataModel1 = new DashboardModel();
                    dataModel.Forms = formDataList;
                    modelResult1.Data = dataModel;
                    var modelResult = modelResult1;
                    result = modelResult.Data.Forms;
                    //var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,BusinessNeed,Level,ApproverUserName,Logic,RunWorkflow,Department,ApproverStatus,RowId,NextApproverId,Modified,Author/Title,FormId/FormName," +
                    //    "FormId/Id,FormId/Created,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/Status&$filter=(ApproverUserName eq '" + user.UserName + "' and IsActive eq '1')&$expand=FormId,Author&$top=10000");
                    //var responseText = await response.Content.ReadAsStringAsync();
                    //var settings = new JsonSerializerSettings
                    //{
                    //    NullValueHandling = NullValueHandling.Ignore
                    //};
                    //if (!string.IsNullOrEmpty(responseText))
                    //{
                    //    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText, settings);
                    //    result = modelResult.Data.Forms;
                    //}

                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        /// <summary>
        /// Dashboard-It is used to get pending forms from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetMyRequest(string Checked, string Filter)
        {
            if (user == null)
                return new List<FormData>();

            List<FormData> result = new List<FormData>();
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var url = "_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,ControllerName,SubmitterUserName,Status,FormParentId/Id,Created,Modified,Department,Author/Title,UniqueFormName,FormName" +
                    "&$filter=SubmitterUserName eq '" + user.UserName +
                    "'&$expand=FormParentId,Author&$top=10000";

                var response = await client.GetAsync(url);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    if (modelResult != null && modelResult.Data != null && modelResult.Data.Forms != null && modelResult.Data.Forms.Count > 0)
                        result = modelResult.Data.FormsRequest;
                }


                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,Department,ApproverStatus,RowId,NextApproverId,Modified,Author/Title,FormId/FormName," +
                //    "FormId/Id,FormId/Created,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/Status&$filter=(ApproverId eq '" + user.UserId + "' and IsActive eq '1')&$expand=FormId,Author&$top=1000");
                //var responseText = await response.Content.ReadAsStringAsync();

                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                //    result = modelResult.Data.FormsRequest;
                //}              

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }
        /// <summary>
        /// Dashboard-It is used to get status count from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public FormStatus GetStatusCount()
        {

            var status = new FormStatus();
            try
            {
                ClientContext _context = new ClientContext(conString);
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                List list = _context.Web.Lists.GetByTitle("Forms");

                var camlQuery = new CamlQuery();
                var camlQueryString = "<View><Query><Where><Eq><FieldRef Name = 'SubmitterUserName'/><Value Type = 'String'>'" +
                    user.UserName + "'</Value></Eq></Where></Query></View>";
                camlQuery.ViewXml = camlQueryString;

                ListItemCollection colListItem = list.GetItems(camlQuery);
                _context.Load(colListItem);
                _context.ExecuteQuery();

                var listItems = colListItem.ToList();

                status.Approved = listItems.Where(x => x.FieldValues["Status"].ToString() == "Approved").Count();
                status.Rejected = listItems.Where(x => x.FieldValues["Status"].ToString() == "Rejected").Count();
                status.Processed = listItems.Where(x => x.FieldValues["Status"].ToString() == "Initiated").Count();
                status.Cancelled = listItems.Where(x => x.FieldValues["Status"].ToString() == "Cancelled").Count();
                status.Submitted = listItems.Where(x => x.FieldValues["Status"].ToString() == "Submitted").Count();
                status.Resubmitted = listItems.Where(x => x.FieldValues["Status"].ToString() == "Resubmitted").Count();
                status.Submitted = status.Submitted + status.Resubmitted;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return status;
        }
        /// <summary>
        /// ALL-It is used to get all forms list on the FormDashboard.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetAllFormsListByName(string uniqueFormName = "", string deparment = "", string status = "")
        {
            var result = new List<FormData>();
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var url = "_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,SubmitterUserName,ControllerName,Status,FormParentId/Id,Created,Modified,BusinessNeed,Department,Author/Title,UniqueFormName,FormName" +
                    "&$filter=SubmitterUserName eq '" + user.UserName + "'" +
                    (!string.IsNullOrEmpty(uniqueFormName) ? (" and UniqueFormName eq '" + uniqueFormName + "'") : "") +
                    (!string.IsNullOrEmpty(deparment) ? (" and Department eq '" + deparment + "'") : "") +
                    (!string.IsNullOrEmpty(status) ? (" and Status eq '" + status + "'") : "") +
                    "&$expand=FormParentId,Author&$top=10000";

                var response = await client.GetAsync(url);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    if (modelResult != null && modelResult.Data != null && modelResult.Data.Forms != null && modelResult.Data.Forms.Count > 0)
                        result = modelResult.Data.Forms;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
        public FormStatus GetStatusCountByForm(string FormName)
        {

            var status = new FormStatus();
            try
            {
                ClientContext _context = new ClientContext(conString);
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                List list = _context.Web.Lists.GetByTitle("Forms");

                var camlQuery = new CamlQuery();
                var camlQueryString = "<View><Query><Where><Eq><FieldRef Name = 'SubmitterUserName'/><Value Type = 'Integer'>'" +
                    user.UserName + "'</Value></Eq></Where></Query></View>";
                camlQuery.ViewXml = camlQueryString;

                ListItemCollection colListItem = list.GetItems(camlQuery);
                _context.Load(colListItem);
                _context.ExecuteQuery();

                var listItems = colListItem.ToList();

                status.Approved = listItems.Where(x => x.FieldValues["UniqueFormName"].ToString() == FormName).Where(x => x.FieldValues["Status"].ToString() == "Approved").Count();
                status.Rejected = listItems.Where(x => x.FieldValues["UniqueFormName"].ToString() == FormName).Where(x => x.FieldValues["Status"].ToString() == "Rejected").Count();
                status.Processed = listItems.Where(x => x.FieldValues["UniqueFormName"].ToString() == FormName).Where(x => x.FieldValues["Status"].ToString() == "Initiated").Count();
                status.Resubmitted = listItems.Where(x => x.FieldValues["UniqueFormName"].ToString() == FormName).Where(x => x.FieldValues["Status"].ToString() == "Resubmitted").Count();
                status.Cancelled = listItems.Where(x => x.FieldValues["UniqueFormName"].ToString() == FormName).Where(x => x.FieldValues["Status"].ToString() == "Cancelled").Count();
                status.Submitted = listItems.Where(x => x.FieldValues["UniqueFormName"].ToString() == FormName).Where(x => x.FieldValues["Status"].ToString() == "Submitted").Count();

                status.Submitted = status.Submitted + status.Resubmitted;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return status;
        }
        /// <summary>
        /// Dashboard-It is used to get all the forms from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<DataModel> GetForms()
        {
            var uniqueFormList = new List<FormData>();
            var newForms = new List<FormData>();

            // var freqForms = new List<FormData>();
            var data = new DataModel() { FreqUsedForms = new List<FormData>(), NewlyAddedForms = new List<FormData>(), DepartmentList = new List<(string, int)>() };
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                ////new forms
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.Timeout = TimeSpan.FromSeconds(10);
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response = await client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id,Department,ControllerName,FormOwner,UniqueFormName,FormName,ReleaseDate&$orderby=ReleaseDate desc&$filter=IsComplete eq '1'");
                //var responseText = response.Content.ReadAsStringAsync().Result;
                //if (responseText == null)
                //{
                //    return new DataModel();
                //}
                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                //    var newForms2 = modelResult.Data.Forms.ToList();
                //    newForms = newForms2;
                //    uniqueFormList = modelResult.Data.Forms;
                //}

                DashboardModel dashboardModel = new DashboardModel();
                List<FormData> formDatas = new List<FormData>();
                DataModel dataModel = new DataModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                var conn = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_GetForms", conn);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                conn.Open();
                adapter.Fill(ds);
                conn.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        FormData formData = new FormData();
                        formData.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        formData.Department = Convert.ToString(ds.Tables[0].Rows[i]["Department"]);
                        formData.ControllerName = Convert.ToString(ds.Tables[0].Rows[i]["ControllerName"]);
                        formData.FormOwner = Convert.ToString(ds.Tables[0].Rows[i]["FormOwner"]);
                        formData.UniqueFormName = Convert.ToString(ds.Tables[0].Rows[i]["UniqueFormName"]);
                        formData.FormName = Convert.ToString(ds.Tables[0].Rows[i]["FormName"]);
                        formData.ReleaseDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["ReleaseDate"]);
                        uniqueFormList.Add(formData);
                    }
                }
                dataModel.Forms = uniqueFormList;
                dashboardModel.Data = dataModel;




                //frequent forms
                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,Status,FormParentId/" +
                //   "Id,Created,Modified,Department,SubmitterUserName,UniqueFormName,FormName&$filter=SubmitterUserName eq '" + user.UserName + "'&$expand=FormParentId&$top=5000");

                //var responseText2 = await response2.Content.ReadAsStringAsync();

                DashboardModel dashboardModel_Form = new DashboardModel();
                List<FormData> FormsDEtailsList = new List<FormData>();
                DataModel dataModel_form = new DataModel();

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                var conn1 = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("USP_GetFormsDetails", conn);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter1.SelectCommand = cmd1;
                conn1.Open();
                adapter1.Fill(ds1);
                conn1.Close();
                if (ds1.Tables[0].Rows.Count > 0 && ds1.Tables[0] != null)
                {
                    for (int i = 0; i < ds1.Tables[0].Rows.Count; i++)
                    {

                        FormData item = new FormData();
                        FormParentModel parentModel = new FormParentModel();
                        parentModel.Id = Convert.ToInt32(ds1.Tables[0].Rows[i]["ID"]);
                        item.FormParent = parentModel;
                        item.Id = Convert.ToInt32(ds1.Tables[0].Rows[i]["ID"]);
                        item.Created_Date = Convert.ToDateTime(ds1.Tables[0].Rows[i]["Created"]);
                        item.Department = Convert.ToString(ds1.Tables[0].Rows[i]["Department"]);
                        item.SubmitterUserName = Convert.ToString(ds1.Tables[0].Rows[i]["SubmitterUserName"]);
                        item.UniqueFormName = Convert.ToString(ds1.Tables[0].Rows[i]["UniqueFormName"]);
                        item.FormName = Convert.ToString(ds1.Tables[0].Rows[i]["FormName"]);
                        FormsDEtailsList.Add(item);
                    }
                }


                dataModel_form.Forms = FormsDEtailsList;
                dashboardModel.Data = dataModel_form;

                var freqForms = FormsDEtailsList;
                //freqForms[0].FormParent.Id

                for (int i = 0; i < uniqueFormList.Count; i++)
                {
                    uniqueFormList[i].FormCount = freqForms.Count(x => x.FormParent.Id == uniqueFormList[i].Id);
                }
                var joinedList = (from freq in freqForms
                                  join detail in uniqueFormList on freq.FormParent.Id equals detail.Id
                                  select new { freq.FormName, detail.UniqueFormId, detail.FormCount, detail.UniqueFormName }).ToList();

                var freqFormList = uniqueFormList.Select(item => new FormData()
                {
                    FormName = item.FormName,
                    UniqueFormId = item.UniqueFormId,
                    FormCount = item.FormCount,
                    UniqueFormName = item.UniqueFormName,
                    Department = item.Department,
                    FormOwner = item.FormOwner,
                    ControllerName = item.ControllerName
                }).OrderByDescending(x => x.FormCount).ToList();

                for (int i = 0; i < freqFormList.Count; i++)
                {
                    data.FreqUsedForms.Add(freqFormList[i]);
                }

                for (int i = 0; i < newForms.Count; i++)
                {
                    newForms[i].FormCount = freqForms.Count(x => x.FormParent.Id == newForms[i].Id);
                    data.NewlyAddedForms.Add(newForms[i]);
                }

                var deptGroup = newForms.GroupBy(x => x.Department);
                //var deptGroup = freqForms.GroupBy(x => x.Department);
                foreach (var item in deptGroup)
                {
                    data.DepartmentList.Add((item.Key, item.Count()));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return data;
        }
        /// <summary>
        /// Dashboard-It is used to get all the forms.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetAllFormsList(string uniqueFormName = "", string deparment = "", string status = "")
        {
            var result = new List<FormData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                List<FormData> resultNew = new List<FormData>();
                DashboardModel modelResult1 = new DashboardModel();
                DataModel dataModel = new DataModel();
                List<FormData> formDataList = new List<FormData>();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_getMyRequest", con);
                cmd.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                cmd.Parameters.Add(new SqlParameter("@UniqueFormName", uniqueFormName));
                cmd.Parameters.Add(new SqlParameter("@Department", deparment));
                cmd.Parameters.Add(new SqlParameter("@Status", status));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                for (int i = 0; i < ds1.Rows.Count; i++)
                {
                    FormData FormDataitem = new FormData();
                    FormLookup formLookup = new FormLookup();
                    Author author = new Author();
                    FormDataitem.Id = Convert.ToInt32(ds1.Rows[i]["Id"]);
                    FormDataitem.RecievedDate = Convert.ToDateTime(ds1.Rows[i]["Created"]);
                    FormDataitem.strRecievedDate = Convert.ToString(ds1.Rows[i]["StringCreated"]);
                    // FormDataitem.BusinessNeed = Convert.ToString(ds1.Rows[i]["BusinessNeed"]);
                    FormDataitem.BusinessNeed = ds1.Rows[i]["BusinessNeed"] != null || ds1.Rows[i]["BusinessNeed"] != DBNull.Value || ds1.Rows[i]["BusinessNeed"] != "0" ? Convert.ToString(ds1.Rows[i]["BusinessNeed"]) : null;
                    FormDataitem.UniqueFormId = Convert.ToInt32(ds1.Rows[i]["Id"]);
                    FormDataitem.UniqueFormName = Convert.ToString(ds1.Rows[i]["UniqueFormName"]);
                    FormDataitem.UniqueFormId = Convert.ToInt32(ds1.Rows[i]["Id"]);
                    FormDataitem.ApplicantName = Convert.ToString(ds1.Rows[i]["SubmitterUserName"]);
                    FormDataitem.Status = Convert.ToString(ds1.Rows[i]["Status"]);

                    //FormDataitem.DataRowId = Convert.ToInt32(ds1.Rows[i]["DataRowId"]);
                    FormDataitem.DataRowId = ds1.Rows[i]["DataRowId"] != null && ds1.Rows[i]["DataRowId"] != DBNull.Value && ds1.Rows[i]["DataRowId"] != "" ? Convert.ToInt32(ds1.Rows[i]["DataRowId"]) : 0;
                    //FormDataitem.Id = Convert.ToInt32(ds1.Rows[i]["Id"]);
                    FormDataitem.Id = ds1.Rows[i]["Id"] != null || ds1.Rows[i]["Id"] != DBNull.Value || ds1.Rows[i]["Id"] != "0" ? Convert.ToInt32(ds1.Rows[i]["Id"]) : 0; ;
                    FormDataitem.ControllerName = Convert.ToString(ds1.Rows[i]["ControllerName"]);
                    FormDataitem.Department = Convert.ToString(ds1.Rows[i]["Department"]);
                    FormDataitem.ApproverStatus = Convert.ToString(ds1.Rows[i]["Status"]);
                    FormDataitem.FormName = Convert.ToString(ds1.Rows[i]["FormName"]);
                    author.Submitter = Convert.ToString(ds1.Rows[i]["SubmitterUserName"]);
                    FormDataitem.Author = author;
                    formLookup.ControllerName = Convert.ToString(ds1.Rows[i]["ControllerName"]);
                    formLookup.CreatedDate = Convert.ToDateTime(ds1.Rows[i]["Created"]);
                    formLookup.FormName = Convert.ToString(ds1.Rows[i]["FormName"]);
                    formLookup.FormStatus = Convert.ToString(ds1.Rows[i]["Status"]);
                    formLookup.Id = Convert.ToInt32(ds1.Rows[i]["Id"]);
                    formLookup.UniqueFormName = Convert.ToString(ds1.Rows[i]["UniqueFormName"]);
                    FormDataitem.FormRelation = formLookup;
                    formDataList.Add(FormDataitem);
                }
                DashboardModel dataModel1 = new DashboardModel();
                dataModel.Forms = formDataList;
                modelResult1.Data = dataModel;
                var modelResult = modelResult1;
                result = modelResult.Data.Forms;
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
        /// <summary>
        /// Dashboard-It is used to get department wise forms.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetDepartmentWiseForms(string deparment = "")
        {
            var result = new List<FormData>();
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var url = "_api/web/lists/GetByTitle('FormParent')/items?$select=Id,Department,Created,ControllerName,FormOwner,UniqueFormName,FormName,ReleaseDate&$orderby=ReleaseDate desc&$filter=IsComplete eq " + "1" + " " +
               (!string.IsNullOrEmpty(deparment) ? ("and Department eq '" + deparment + "'") : "");
                var response = await client.GetAsync(url);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    if (modelResult != null && modelResult.Data != null && modelResult.Data.Forms != null && modelResult.Data.Forms.Count > 0)
                    {
                        //var groupList = modelResult.Data.Forms.Where(x=>x.UniqueFormName.Equals("PAF")).GroupBy(x => x.UniqueFormName);
                        var groupList = modelResult.Data.Forms.GroupBy(x => x.UniqueFormName);
                        foreach (var item in groupList)
                        {
                            var model = new FormData();
                            var firstItem = item.FirstOrDefault();
                            if (firstItem != null)
                            {
                                model.FormName = firstItem.FormName;
                                model.UniqueFormName = firstItem.UniqueFormName;
                                model.FormCount = item.Count();
                                model.FormParent = new FormParentModel();
                                model.FormParent.Id = firstItem.Id;
                                model.Department = firstItem.Department;
                                model.FormOwner = firstItem.FormOwner;
                                model.FormCreatedDate = firstItem.FormCreatedDate;
                                model.ControllerName = firstItem.ControllerName;
                                result.Add(model);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
        /// <summary>
        /// FormDashboard/..Form-It is used to get the bread crumb title.
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<string, string>> GetBreadCrumbTitle(string formName = "", string department = "", string status = "", string fullName = "")
        {
            var breadCrumbsList = new List<KeyValuePair<string, string>>();
            breadCrumbsList.Add(new KeyValuePair<string, string>("Dashboard", UrlHelper.GenerateUrl(null, "",
                "Dashboard", new RouteValueDictionary(new { formName, department }), RouteTable.Routes, HttpContext.Current.Request.RequestContext, false)));

            if (!string.IsNullOrEmpty(department))
                breadCrumbsList.Add(new KeyValuePair<string, string>($"{department} Dashboard", UrlHelper.GenerateUrl(null, "GetDepartmentWiseForms",
                "Dashboard", new RouteValueDictionary(new { department }), RouteTable.Routes, HttpContext.Current.Request.RequestContext, false)));

            if (!string.IsNullOrEmpty(status))
                breadCrumbsList.Add(new KeyValuePair<string, string>($"{status} Dashboard", UrlHelper.GenerateUrl(null, "GetMyForms",
                "Dashboard", new RouteValueDictionary(new { status }), RouteTable.Routes, HttpContext.Current.Request.RequestContext, false)));

            if (!string.IsNullOrEmpty(formName))
            {
                if (!string.IsNullOrEmpty(department))
                {
                    breadCrumbsList.Add(new KeyValuePair<string, string>($"{fullName} Dashboard", UrlHelper.GenerateUrl(null, "GetMyForms",
               "Dashboard", new RouteValueDictionary(new { department, formName, fullName }), RouteTable.Routes, HttpContext.Current.Request.RequestContext, false)));
                }
                else if (!string.IsNullOrEmpty(status))
                {
                    breadCrumbsList.Add(new KeyValuePair<string, string>($"{fullName} Dashboard", UrlHelper.GenerateUrl(null, "GetMyForms",
                "Dashboard", new RouteValueDictionary(new { status, formName, fullName }), RouteTable.Routes, HttpContext.Current.Request.RequestContext, false)));
                }
                else
                {
                    breadCrumbsList.Add(new KeyValuePair<string, string>($"{fullName} Dashboard", UrlHelper.GenerateUrl(null, "GetMyForms",
                "Dashboard", new RouteValueDictionary(new { formName, fullName }), RouteTable.Routes, HttpContext.Current.Request.RequestContext, false)));
                }
            }

            return breadCrumbsList;
        }

        /// <summary>
        /// ALL-It is used to save approver response in ApprovalMaster.
        /// </summary>
        /// <returns></returns>
        //public async Task<int> SaveResponse(string response, int appRowId, string comment, int approvalType)
        //{

        //    int formId = 0;
        //    int approverLevel = 0;
        //    int rowId = 0;

        //    try
        //    {
        //        DashboardModel dashboardModel = new DashboardModel();
        //        DataModel dataModel = new DataModel();
        //        List<FormData> dataList = new List<FormData>();
        //        List<FormData> approverIdList = new List<FormData>();
        //        FormData formData = new FormData();

        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();

        //        DataTable dt = new DataTable();
        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("USP_SaveResponseAppRowId", con);
        //        cmd.Parameters.Add(new SqlParameter("@appRowId", appRowId));
        //        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(dt);
        //        con.Close();

        //        if (dt.Rows.Count > 0)
        //        {
        //            for (int i = 0; i < dt.Rows.Count; i++)
        //            {
        //                FormLookup item = new FormLookup();
        //                item.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
        //                formData.FormRelation = item;
        //                formData.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
        //                formData.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
        //                formData.AuthorityToEdit = Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
        //                formData.ApproverId = Convert.ToInt32(dt.Rows[i]["ApproverId"]);
        //                formData.IsActive = Convert.ToInt32(dt.Rows[i]["IsActive"]);
        //                formData.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
        //                formData.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
        //                formData.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
        //                formData.Department = Convert.ToString(dt.Rows[i]["Department"]);
        //                formData.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
        //                formData.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
        //                formData.NextApproverId = Convert.ToInt32(dt.Rows[i]["Level"]);
        //                formData.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Modified"]);
        //                formData.ApproverUserName = Convert.ToString(dt.Rows[i]["ApproverUserName"]);
        //                formData.RelationWith = Convert.ToInt32(dt.Rows[i]["RelationWith"]);
        //                formData.RelationId = Convert.ToInt32(dt.Rows[i]["RelationId"]);
        //                dataList.Add(formData);

        //            }
        //        }

        //        dashboardModel.Data = dataModel;
        //        dataModel.Forms = dataList;

        //        if (dataList[0].RunWorkflow.ToLower() == "no")
        //        {
        //            rowId = dataList[0].RowId;
        //            formId = dataList[0].FormRelation.Id;


        //            DashboardModel dashboardModels = new DashboardModel();
        //            DataModel DataModel1 = new DataModel();
        //            List<FormData> DataList = new List<FormData>();
        //            List<FormData> ApproverIdList = new List<FormData>();


        //            SqlCommand cmd1 = new SqlCommand();
        //            SqlDataAdapter adapter1 = new SqlDataAdapter();

        //            DataTable dt1 = new DataTable();
        //            con = new SqlConnection(sqlConString);
        //            cmd1 = new SqlCommand("USP_SaveResponseforAprroval", con);
        //            cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
        //            cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
        //            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
        //            cmd1.CommandType = CommandType.StoredProcedure;
        //            adapter1.SelectCommand = cmd1;
        //            con.Open();
        //            adapter1.Fill(dt1);
        //            con.Close();
        //            if (dt1.Rows.Count > 0)
        //            {
        //                for (int i = 0; i < dt1.Rows.Count; i++)
        //                {
        //                    FormData formData1 = new FormData();
        //                    formData1.Id = Convert.ToInt32(dt1.Rows[i]["ID"]);
        //                    formData1.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
        //                    formData1.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
        //                    formData1.ApproverId = Convert.ToInt32(dt1.Rows[i]["ApproverId"]);
        //                    formData1.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
        //                    formData1.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
        //                    formData1.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
        //                    formData1.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
        //                    formData1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
        //                    formData1.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);
        //                    formData1.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
        //                    formData1.NextApproverId = Convert.ToInt32(dt1.Rows[i]["Level"]);
        //                    formData1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
        //                    formData1.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
        //                    formData1.RelationWith = Convert.ToInt32(dt1.Rows[i]["RelationWith"]);
        //                    formData1.RelationId = Convert.ToInt32(dt1.Rows[i]["RelationId"]);
        //                    ApproverIdList.Add(formData1);

        //                }
        //            }
        //            dashboardModels.Data = DataModel1;
        //            DataModel1.Forms = ApproverIdList;

        //            var currentLevelApprovers = ApproverIdList.Where(x => x.IsActive == 1); // Add OR Condn 
        //            var currentApprover = currentLevelApprovers.Where(x => x.ApproverUserName == user.UserName).FirstOrDefault();

        //            var currentLevel = currentApprover.Level;
        //            approverLevel = currentLevel.Value;
        //            var minLevel = ApproverIdList.Min(x => x.Level);
        //            var maxLevel = ApproverIdList.Max(x => x.Level);
        //            var nextLevelApprovers = ApproverIdList.Where(x => x.IsActive == 0 && x.Level == currentLevel + 1);

        //            switch (currentApprover.Logic)
        //            {
        //                case "NOT":
        //                    {
        //                        if (response == "Approved")
        //                        {
        //                            SqlCommand cmd_A = new SqlCommand();
        //                            SqlDataAdapter adapter_A = new SqlDataAdapter();
        //                            con = new SqlConnection(sqlConString);
        //                            cmd_A = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                            cmd_A.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
        //                            cmd_A.Parameters.Add(new SqlParameter("@IsActive", 10));
        //                            cmd_A.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                            cmd_A.Parameters.Add(new SqlParameter("@Comment", comment));
        //                            cmd_A.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                            cmd_A.CommandType = CommandType.StoredProcedure;
        //                            adapter_A.SelectCommand = cmd_A;
        //                            con.Open();
        //                            adapter_A.Fill(dt1);
        //                            con.Close();



        //                            foreach (var nextApprover in nextLevelApprovers)
        //                            {

        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }

        //                            //Forms List Update
        //                            if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
        //                            {
        //                                //update in formstatus as approved

        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();


        //                            }
        //                            else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
        //                            {
        //                                //update in formstatus as approved
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }
        //                            else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && approverIdList.Where(x => x.IsActive == 1).Count() == 1)
        //                            {
        //                                //update in formstatus as approved
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }
        //                            else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
        //                            {
        //                                //update in formstatus as approved
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }
        //                            else if (currentApprover.Level == minLevel)
        //                            {
        //                                //update in formstatus as initiliazed
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();

        //                            }
        //                        }
        //                        if (response == "Enquired")
        //                        {
        //                            foreach (var ail in ApproverIdList)
        //                            {
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                }
        //                                else
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                }

        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();

        //                            }
        //                            //Forms List Update

        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();

        //                            //Approval Master New Record added after Enquired

        //                            int submitterId = 0;
        //                            string submitterUserName = "";
        //                            string businessNeed = "";

        //                            submitterUserName = dataList[0].SubmitterUserName;
        //                            businessNeed = dataList[0].BusinessNeed;

        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
        //                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                            cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
        //                            cmd1.Parameters.Add(new SqlParameter("@Department", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
        //                            cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
        //                            cmd1.Parameters.Add(new SqlParameter("@Email", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
        //                            cmd1.Parameters.Add(new SqlParameter("@Level", 1));
        //                            cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", 0));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();




        //                            //List approvalMasterlist = _context.Web.Lists.GetByTitle("ApprovalMaster");
        //                            //approvalMasterlist.RefreshLoad();
        //                            //_context.ExecuteQuery();
        //                            //ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
        //                            //ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);
        //                            //approvalMasteritem["FormId"] = formId;
        //                            //approvalMasteritem["RowId"] = rowId;
        //                            //approvalMasteritem["ApproverUserName"] = submitterUserName;
        //                            //approvalMasteritem["IsActive"] = 1;
        //                            //approvalMasteritem["NextApproverId"] = 0;
        //                            //approvalMasteritem["ApproverStatus"] = "Enquired";
        //                            //approvalMasteritem["RunWorkflow"] = "No";
        //                            //approvalMasteritem["BusinessNeed"] = businessNeed;
        //                            //approvalMasteritem.Update();
        //                            //_context.Load(approvalMasteritem);
        //                            //_context.ExecuteQuery();

        //                        }
        //                        if (response == "Rejected")
        //                        {
        //                            foreach (var ail in ApproverIdList)
        //                            {

        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                }
        //                                else
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Rejected by " + user.FullName));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                }

        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();


        //                            }
        //                            //Forms List Update
        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();

        //                        }
        //                        break;
        //                    }
        //                case "OR":
        //                    {
        //                        if (response == "Approved")
        //                        {
        //                            //0 means No Assistant
        //                            List<FormData> groupedList = new List<FormData>();
        //                            int countPendingApprovers = 0;
        //                            var mainApprover = currentApprover;
        //                            currentApprover.ApproverStatus = "Approved";
        //                            while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
        //                            {
        //                                mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
        //                            }
        //                            if (mainApprover.RelationWith != null)
        //                            {
        //                                countPendingApprovers += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
        //                            }
        //                            if (countPendingApprovers == -1)
        //                            {
        //                                return -1;
        //                            }
        //                            if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "and"))
        //                            {

        //                                currentApprover.ApproverStatus = response;
        //                                groupedList.AddRange(
        //                                    GetRelatedOrApprover(currentLevelApprovers, mainApprover)
        //                                );
        //                                groupedList.Add(mainApprover);

        //                            }
        //                            else
        //                            {
        //                                groupedList.AddRange(currentLevelApprovers);
        //                            }

        //                            foreach (var cla in groupedList)
        //                            {
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", cla.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                if (cla.ApproverUserName == user.UserName && cla.IsActive == 1)
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                }
        //                                else
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Approved by " + user.FullName));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                }

        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();

        //                            }

        //                            if (countPendingApprovers < 1)
        //                            {
        //                                foreach (var nextApprover in nextLevelApprovers)
        //                                {
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();

        //                                }
        //                                //Forms List Update
        //                                if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
        //                                {
        //                                    //update in formstatus as approved

        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                //I guess yeh condn yaha nahi aayega?
        //                                else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                //I guess yeh condn yaha nahi aayega?
        //                                else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                else if (currentApprover.Level == minLevel)
        //                                {
        //                                    //update in formstatus as initiliazed
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                            }
        //                        }
        //                        if (response == "Enquired")
        //                        {
        //                            var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
        //                            if (currentUser != null)
        //                            {
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();

        //                                //ListItem currentItem = list.GetItemById(currentUser.Id);
        //                                //currentItem["ApproverStatus"] = response;
        //                                //currentItem["Comment"] = comment;
        //                                //currentItem.Update();
        //                                //_context.Load(currentItem);
        //                                //_context.ExecuteQuery();
        //                            }
        //                            foreach (var ail in ApproverIdList)
        //                            {
        //                                if (currentUser.Id != ail.Id)
        //                                {
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Pending"));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }

        //                            }
        //                            //Forms List Update

        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();

        //                            //Approval Master New Record added after Enquired
        //                            int submitterId = 0;
        //                            object submitterUserName = DBNull.Value;
        //                            string businessNeed = "";
        //                            if (dataList[0].SubmitterUserName != null)
        //                                submitterUserName = dataList[0].SubmitterUserName;
        //                            businessNeed = dataList[0].BusinessNeed;


        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
        //                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                            cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
        //                            cmd1.Parameters.Add(new SqlParameter("@Department", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
        //                            cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
        //                            cmd1.Parameters.Add(new SqlParameter("@Email", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
        //                            cmd1.Parameters.Add(new SqlParameter("@Level", 1));
        //                            cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", 0));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();


        //                        }
        //                        if (response == "Rejected")
        //                        {
        //                            var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
        //                            if (currentUser != null)
        //                            {

        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                if (currentUser.ApproverUserName != user.UserName)
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Rejected by " + user.FullName));
        //                                }
        //                                else
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                }

        //                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }
        //                            foreach (var ail in ApproverIdList)
        //                            {
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                if (ail.ApproverUserName != user.UserName)
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Rejected by " + user.FullName));
        //                                }
        //                                else
        //                                {
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                }

        //                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();

        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }
        //                            //Forms List Update
        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();

        //                        }
        //                        break;
        //                    }

        //                case "AND":
        //                    {
        //                        if (response == "Approved")
        //                        {
        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
        //                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();



        //                            //var pendingStatusCount = currentLevelApprovers.ToList().Count();
        //                            var mainApprover = currentApprover;
        //                            currentApprover.ApproverStatus = "Approved";
        //                            while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
        //                            {
        //                                mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
        //                            }
        //                            int pendingStatusCount = 0;
        //                            if (mainApprover.RelationWith != null)
        //                            {
        //                                //pendingStatusCount = GetCountOfPendingRelationApproval(currentLevelApprovers, mainApprover);
        //                                pendingStatusCount += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
        //                            }

        //                            //OR Condn code
        //                            var orCondnApprovers = currentLevelApprovers.Where(x => x.Logic.ToLower() == "or").ToList();
        //                            if (orCondnApprovers != null && orCondnApprovers.Count() > 0)
        //                            {
        //                                //Assuming AND case will not be a assist. but can be approver
        //                                List<FormData> groupedList = new List<FormData>();
        //                                var AppAssistList = GetRelatedOrApprover(currentLevelApprovers, mainApprover);
        //                                if (AppAssistList != null && AppAssistList.Count() > 0)
        //                                {
        //                                    groupedList.AddRange(AppAssistList);
        //                                }
        //                                foreach (var item in AppAssistList)
        //                                {
        //                                    item.ApproverStatus = "Approved";//Temp Making all current approver assist as approved
        //                                }
        //                                foreach (var cla in groupedList)
        //                                {
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", cla.Id));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();


        //                                    //Remove Current approver and Its Assist as it is currently approved...
        //                                    orCondnApprovers.Remove(cla);
        //                                }

        //                                if (pendingStatusCount == -1)
        //                                {
        //                                    return -1;
        //                                }

        //                            }

        //                            if (pendingStatusCount == 0)
        //                            {
        //                                foreach (var nextApprover in nextLevelApprovers)
        //                                {
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();


        //                                }
        //                            }

        //                            if (pendingStatusCount == 0)
        //                            {
        //                                //Forms List Update
        //                                if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();

        //                                }
        //                                else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
        //                                {
        //                                    //update in formstatus as approved
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }
        //                                else if (currentApprover.Level == minLevel)
        //                                {
        //                                    //update in formstatus as initiliazed
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();

        //                                }
        //                            }
        //                        }
        //                        if (response == "Enquired")
        //                        {
        //                            var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
        //                            if (currentUser != null)
        //                            {
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }

        //                            foreach (var ail in ApproverIdList)
        //                            {

        //                                if (currentUser.Id != ail.Id)
        //                                {
        //                                    con = new SqlConnection(sqlConString);
        //                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                    cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Pending"));
        //                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                    cmd1.CommandType = CommandType.StoredProcedure;
        //                                    adapter1.SelectCommand = cmd1;
        //                                    con.Open();
        //                                    adapter1.Fill(dt1);
        //                                    con.Close();
        //                                }

        //                            }
        //                            //Forms List Update

        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();



        //                            //Approval Master New Record added after Enquired
        //                            int submitterId = 0;
        //                            object submitterUserName = DBNull.Value;
        //                            object businessNeed = DBNull.Value;
        //                            if (dataList[0].SubmitterUserName != null)
        //                                submitterUserName = dataList[0].SubmitterUserName;
        //                            if (dataList[0].BusinessNeed != null)
        //                                businessNeed = dataList[0].BusinessNeed;

        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
        //                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                            cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
        //                            cmd1.Parameters.Add(new SqlParameter("@Department", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
        //                            cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
        //                            cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
        //                            cmd1.Parameters.Add(new SqlParameter("@Email", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
        //                            cmd1.Parameters.Add(new SqlParameter("@Level", 1));
        //                            cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", "0"));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
        //                            cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
        //                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();

        //                        }
        //                        if (response == "Rejected")
        //                        {
        //                            var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
        //                            if (currentUser != null)
        //                            {

        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();
        //                            }

        //                            foreach (var ail in ApproverIdList)
        //                            {
        //                                con = new SqlConnection(sqlConString);
        //                                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                                cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                                cmd1.CommandType = CommandType.StoredProcedure;
        //                                adapter1.SelectCommand = cmd1;
        //                                con.Open();
        //                                adapter1.Fill(dt1);
        //                                con.Close();

        //                            }

        //                            //Forms List Update

        //                            con = new SqlConnection(sqlConString);
        //                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
        //                            cmd1.CommandType = CommandType.StoredProcedure;
        //                            adapter1.SelectCommand = cmd1;
        //                            con.Open();
        //                            adapter1.Fill(dt1);
        //                            con.Close();

        //                        }
        //                        break;
        //                    }
        //            }
        //        }
        //        else if (dataList[0].RunWorkflow.ToLower() == "yes")
        //        {

        //            DashboardModel dashboardModels = new DashboardModel();
        //            DataModel DataModel1 = new DataModel();
        //            List<FormData> DataList = new List<FormData>();
        //            List<FormData> ApproverIdList = new List<FormData>();


        //            SqlCommand cmd1 = new SqlCommand();
        //            SqlDataAdapter adapter1 = new SqlDataAdapter();

        //            rowId = dataList[0].RowId;
        //            formId = dataList[0].FormRelation.Id;

        //            DataTable dt1 = new DataTable();
        //            con = new SqlConnection(sqlConString);
        //            cmd1 = new SqlCommand("USP_SaveResponseforAprroval", con);
        //            cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
        //            cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
        //            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
        //            cmd1.CommandType = CommandType.StoredProcedure;
        //            adapter1.SelectCommand = cmd1;
        //            con.Open();
        //            adapter1.Fill(dt1);
        //            con.Close();
        //            if (dt1.Rows.Count > 0)
        //            {
        //                for (int i = 0; i < dt1.Rows.Count; i++)
        //                {
        //                    FormData formData1 = new FormData();
        //                    formData1.Id = Convert.ToInt32(dt1.Rows[i]["ID"]);
        //                    formData1.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
        //                    formData1.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
        //                    formData1.ApproverId = Convert.ToInt32(dt1.Rows[i]["ApproverId"]);
        //                    formData1.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
        //                    formData1.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
        //                    formData1.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
        //                    formData1.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
        //                    formData1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
        //                    formData1.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);
        //                    formData1.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
        //                    formData1.NextApproverId = Convert.ToInt32(dt1.Rows[i]["Level"]);
        //                    formData1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
        //                    formData1.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
        //                    formData1.RelationWith = Convert.ToInt32(dt1.Rows[i]["RelationWith"]);
        //                    formData1.RelationId = Convert.ToInt32(dt1.Rows[i]["RelationId"]);
        //                    approverIdList.Add(formData1);

        //                }
        //            }
        //            dashboardModels.Data = DataModel1;
        //            DataModel1.Forms = ApproverIdList;

        //            var currentLevelApprovers = approverIdList.Where(x => x.IsActive == 1);
        //            var currentApprover = currentLevelApprovers.Where(x => x.ApproverUserName == user.UserName).FirstOrDefault();
        //            var currentLevel = currentApprover.Level;
        //            approverLevel = currentLevel.Value;
        //            var minLevel = approverIdList.Min(x => x.Level);
        //            var maxLevel = approverIdList.Max(x => x.Level);
        //            var nextLevelApprovers = approverIdList.Where(x => x.IsActive == 0 && x.Level == currentLevel + 1);

        //            if (response == "Approved")
        //            {
        //                con = new SqlConnection(sqlConString);
        //                cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                cmd1.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
        //                cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                cmd1.CommandType = CommandType.StoredProcedure;
        //                adapter1.SelectCommand = cmd1;
        //                con.Open();
        //                adapter1.Fill(dt1);
        //                con.Close();



        //                foreach (var nextApprover in nextLevelApprovers)
        //                {
        //                    con = new SqlConnection(sqlConString);
        //                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                    cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
        //                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                    cmd1.CommandType = CommandType.StoredProcedure;
        //                    adapter1.SelectCommand = cmd1;
        //                    con.Open();
        //                    adapter1.Fill(dt1);
        //                    con.Close();

        //                }

        //                //Forms List Update
        //                if (currentApprover.Level == maxLevel)
        //                {
        //                    //update in formstatus as approved
        //                    con = new SqlConnection(sqlConString);
        //                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
        //                    cmd1.CommandType = CommandType.StoredProcedure;
        //                    adapter1.SelectCommand = cmd1;
        //                    con.Open();
        //                    adapter1.Fill(dt1);
        //                    con.Close();
        //                }
        //                else if (currentApprover.Level == minLevel)
        //                {
        //                    //update in formstatus as initiliazed

        //                    con = new SqlConnection(sqlConString);
        //                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
        //                    cmd1.CommandType = CommandType.StoredProcedure;
        //                    adapter1.SelectCommand = cmd1;
        //                    con.Open();
        //                    adapter1.Fill(dt1);
        //                    con.Close();

        //                }
        //            }

        //            if (response == "Enquired")
        //            {
        //                foreach (var ail in approverIdList)
        //                {

        //                    con = new SqlConnection(sqlConString);
        //                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                    cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                    cmd1.CommandType = CommandType.StoredProcedure;
        //                    adapter1.SelectCommand = cmd1;
        //                    con.Open();
        //                    adapter1.Fill(dt1);
        //                    con.Close();

        //                }
        //                //Forms List Update

        //                con = new SqlConnection(sqlConString);
        //                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
        //                cmd1.CommandType = CommandType.StoredProcedure;
        //                adapter1.SelectCommand = cmd1;
        //                con.Open();
        //                adapter1.Fill(dt1);
        //                con.Close();

        //                //Approval Master New Record added after Enquired
        //                int submitterId = 0;
        //                object submitterUserName = DBNull.Value;
        //                string businessNeed = "";
        //                if (dataList[0].SubmitterUserName != null)
        //                    submitterUserName = dataList[0].SubmitterUserName;
        //                businessNeed = dataList[0].BusinessNeed;

        //                DataTable dtResult = new DataTable();
        //                SqlCommand cmd2 = new SqlCommand();
        //                SqlDataAdapter adapter2 = new SqlDataAdapter();
        //                con = new SqlConnection(sqlConString);
        //                cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
        //                cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
        //                cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
        //                cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
        //                cmd2.Parameters.Add(new SqlParameter("@Department", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@FormParentId", 0));
        //                cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
        //                cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
        //                cmd2.Parameters.Add(new SqlParameter("@Email", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
        //                cmd2.Parameters.Add(new SqlParameter("@Level", 1));
        //                cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", 0));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


        //                cmd2.CommandType = CommandType.StoredProcedure;
        //                adapter2.SelectCommand = cmd2;
        //                con.Open();
        //                adapter2.Fill(dtResult);
        //                con.Close();


        //            }
        //            if (response == "Rejected")
        //            {
        //                foreach (var ail in approverIdList)
        //                {
        //                    con = new SqlConnection(sqlConString);
        //                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
        //                    cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
        //                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
        //                    if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
        //                    {
        //                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                    }
        //                    else
        //                    {
        //                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
        //                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
        //                    }

        //                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

        //                    cmd1.CommandType = CommandType.StoredProcedure;
        //                    adapter1.SelectCommand = cmd1;
        //                    con.Open();
        //                    adapter1.Fill(dt1);
        //                    con.Close();

        //                }
        //                //Forms List Update

        //                con = new SqlConnection(sqlConString);
        //                cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
        //                cmd1.Parameters.Add(new SqlParameter("@Id", formId));
        //                cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
        //                cmd1.CommandType = CommandType.StoredProcedure;
        //                adapter1.SelectCommand = cmd1;
        //                con.Open();
        //                adapter1.Fill(dt1);
        //                con.Close();

        //            }
        //        }
        //        else
        //        {
        //            if (approvalType == 1 || approvalType == 0)
        //            {
        //                DataTable dtResult = new DataTable();
        //                SqlCommand cmd2 = new SqlCommand();
        //                SqlDataAdapter adapter2 = new SqlDataAdapter();
        //                con = new SqlConnection(sqlConString);
        //                cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
        //                cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
        //                cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
        //                cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
        //                cmd2.Parameters.Add(new SqlParameter("@Department", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
        //                cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@CreatedBy", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
        //                cmd2.Parameters.Add(new SqlParameter("@Email", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Level", 1));
        //                cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", 0));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverName", user.UserName));


        //                cmd2.CommandType = CommandType.StoredProcedure;
        //                adapter2.SelectCommand = cmd2;
        //                con.Open();
        //                adapter2.Fill(dtResult);
        //                con.Close();

        //            }
        //            else if (approvalType == 2)
        //            {
        //                DataTable dtResult = new DataTable();
        //                SqlCommand cmd2 = new SqlCommand();
        //                SqlDataAdapter adapter2 = new SqlDataAdapter();
        //                con = new SqlConnection(sqlConString);
        //                cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
        //                cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
        //                cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
        //                cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
        //                cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
        //                cmd2.Parameters.Add(new SqlParameter("@Department", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
        //                cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@CreatedBy", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
        //                cmd2.Parameters.Add(new SqlParameter("@Email", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Level", 1));
        //                cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", 0));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
        //                cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
        //                cmd2.Parameters.Add(new SqlParameter("@ApproverName", user.UserName));


        //                cmd2.CommandType = CommandType.StoredProcedure;
        //                adapter2.SelectCommand = cmd2;
        //                con.Open();
        //                adapter2.Fill(dtResult);
        //                con.Close();
        //            }


        //        }


        //        //SendEmail(formId, user, response, comment, approverLevel, rowId, appRowId);

        //        return 1;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //        return 0;
        //    }
        //}

        public async Task<int> SaveResponse(string response, int appRowId, string comment, int approvalType)
        {

            int formId = 0;
            int approverLevel = 0;
            int rowId = 0;

            try
            {
                DashboardModel dashboardModel = new DashboardModel();
                DataModel dataModel = new DataModel();
                List<FormData> dataList = new List<FormData>();
                List<FormData> approverIdList = new List<FormData>();
                FormData formData = new FormData();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();

                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_SaveResponseAppRowId", con);
                cmd.Parameters.Add(new SqlParameter("@appRowId", appRowId));
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
                        FormLookup item = new FormLookup();
                        item.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        formData.FormRelation = item;
                        formData.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        formData.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                        formData.AuthorityToEdit = Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                        formData.ApproverId = Convert.ToInt32(dt.Rows[i]["ApproverId"]);
                        formData.IsActive = Convert.ToInt32(dt.Rows[i]["IsActive"]);
                        formData.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
                        formData.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                        formData.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                        formData.Department = Convert.ToString(dt.Rows[i]["Department"]);
                        formData.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                        formData.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                        formData.NextApproverId = Convert.ToInt32(dt.Rows[i]["Level"]);
                        formData.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Modified"]);
                        formData.ApproverUserName = Convert.ToString(dt.Rows[i]["ApproverUserName"]);
                        formData.RelationWith = Convert.ToInt32(dt.Rows[i]["RelationWith"]);
                        formData.RelationId = Convert.ToInt32(dt.Rows[i]["RelationId"]);
                        dataList.Add(formData);

                    }
                }

                dashboardModel.Data = dataModel;
                dataModel.Forms = dataList;

                if (dataList[0].RunWorkflow.ToLower() == "no")
                {
                    rowId = dataList[0].RowId;
                    formId = dataList[0].FormRelation.Id;


                    DashboardModel dashboardModels = new DashboardModel();
                    DataModel DataModel1 = new DataModel();
                    List<FormData> DataList = new List<FormData>();
                    List<FormData> ApproverIdList = new List<FormData>();


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();

                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_SaveResponseforAprroval", con);
                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData formData1 = new FormData();
                            formData1.Id = Convert.ToInt32(dt1.Rows[i]["ID"]);
                            formData1.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
                            formData1.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
                            formData1.ApproverId = Convert.ToInt32(dt1.Rows[i]["ApproverId"]);
                            formData1.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
                            formData1.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                            formData1.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
                            formData1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            formData1.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);
                            formData1.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
                            formData1.NextApproverId = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            formData1.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
                            formData1.RelationWith = Convert.ToInt32(dt1.Rows[i]["RelationWith"]);
                            formData1.RelationId = Convert.ToInt32(dt1.Rows[i]["RelationId"]);
                            formData1.Comment = Convert.ToString(dt1.Rows[i]["Comment"]);
                            ApproverIdList.Add(formData1);

                        }
                    }
                    dashboardModels.Data = DataModel1;
                    DataModel1.Forms = ApproverIdList;

                    var currentLevelApprovers = ApproverIdList.Where(x => x.IsActive == 1); // Add OR Condn 
                    var currentApprover = currentLevelApprovers.Where(x => x.ApproverUserName == user.UserName).FirstOrDefault();

                    var currentLevel = currentApprover.Level;
                    approverLevel = currentLevel.Value;
                    var minLevel = ApproverIdList.Min(x => x.Level);
                    var maxLevel = ApproverIdList.Max(x => x.Level);
                    var nextLevelApprovers = ApproverIdList.Where(x => x.IsActive == 0 && x.Level == currentLevel + 1);

                    switch (currentApprover.Logic)
                    {
                        case "NOT":
                            {
                                if (response == "Approved")
                                {
                                    SqlCommand cmd_A = new SqlCommand();
                                    SqlDataAdapter adapter_A = new SqlDataAdapter();
                                    con = new SqlConnection(sqlConString);
                                    cmd_A = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    cmd_A.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
                                    cmd_A.Parameters.Add(new SqlParameter("@IsActive", 10));
                                    cmd_A.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                    cmd_A.Parameters.Add(new SqlParameter("@Comment", comment));
                                    cmd_A.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                    cmd_A.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                    cmd_A.CommandType = CommandType.StoredProcedure;
                                    adapter_A.SelectCommand = cmd_A;
                                    con.Open();
                                    adapter_A.Fill(dt1);
                                    con.Close();



                                    foreach (var nextApprover in nextLevelApprovers)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }

                                    //Forms List Update
                                    if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
                                    {
                                        //update in formstatus as approved

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();


                                    }
                                    else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
                                    {
                                        //update in formstatus as approved
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && approverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                    {
                                        //update in formstatus as approved
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                    {
                                        //update in formstatus as approved
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    else if (currentApprover.Level == minLevel)
                                    {
                                        //update in formstatus as initiliazed
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();

                                    }
                                }
                                if (response == "Enquired")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
                                    //foreach (var ail in ApproverIdList)
                                    //{

                                    //    con = new SqlConnection(sqlConString);
                                    //    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    //    cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                    //    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                    //    if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
                                    //    {
                                    //        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", currentUser.Id != ail.Id ? "Pending" : response));
                                    //        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //    }
                                    //    else
                                    //    {
                                    //        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", currentUser.Id != ail.Id ? "Pending" : response));
                                    //        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //    }

                                    //    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                                    //    cmd1.CommandType = CommandType.StoredProcedure;
                                    //    adapter1.SelectCommand = cmd1;
                                    //    con.Open();
                                    //    adapter1.Fill(dt1);
                                    //    con.Close();

                                    //}

                                    if (currentUser != null)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }

                                    foreach (var ail in ApproverIdList)
                                    {

                                        if (currentUser.Id != ail.Id)
                                        {
                                            object Comment = DBNull.Value;
                                            if (ail.Comment != null)
                                                Comment = ail.Comment;

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }

                                    }
                                    //con = new SqlConnection(sqlConString);
                                    //cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    //cmd1.Parameters.Add(new SqlParameter("@Id", ApproverIdList[0].Id));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(1)));
                                    //cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ApproverIdList[0].ApproverStatus));
                                    //cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                                    //cmd1.CommandType = CommandType.StoredProcedure;
                                    //adapter1.SelectCommand = cmd1;
                                    //con.Open();
                                    //adapter1.Fill(dt1);
                                    //con.Close();

                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                    //Approval Master New Record added after Enquired
                                    object submitterUserName = DBNull.Value;
                                    object businessNeed = DBNull.Value;
                                    DataTable DT = new DataTable();
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("GetFormDataByFormId", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(DT);
                                    con.Close();

                                    if (DT.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < DT.Rows.Count; i++)
                                        {

                                            submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                            businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                                        }
                                    }

                                    int submitterId = 0;

                                    //if (submitterUserName != null)
                                    //    submitterUserName = dataList[0].SubmitterUserName;
                                    //if (businessNeed != null)
                                    //    businessNeed = dataList[0].BusinessNeed;//submitterUserName = dataList[0].SubmitterUserName;
                                    //businessNeed = dataList[0].BusinessNeed;

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                                    cmd1.Parameters.Add(new SqlParameter("@Department", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                                    cmd1.Parameters.Add(new SqlParameter("@Email", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                                    cmd1.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();




                                    //List approvalMasterlist = _context.Web.Lists.GetByTitle("ApprovalMaster");
                                    //approvalMasterlist.RefreshLoad();
                                    //_context.ExecuteQuery();
                                    //ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                                    //ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);
                                    //approvalMasteritem["FormId"] = formId;
                                    //approvalMasteritem["RowId"] = rowId;
                                    //approvalMasteritem["ApproverUserName"] = submitterUserName;
                                    //approvalMasteritem["IsActive"] = 1;
                                    //approvalMasteritem["NextApproverId"] = 0;
                                    //approvalMasteritem["ApproverStatus"] = "Enquired";
                                    //approvalMasteritem["RunWorkflow"] = "No";
                                    //approvalMasteritem["BusinessNeed"] = businessNeed;
                                    //approvalMasteritem.Update();
                                    //_context.Load(approvalMasteritem);
                                    //_context.ExecuteQuery();

                                }
                                if (response == "Rejected")
                                {
                                    foreach (var ail in ApproverIdList)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        }
                                        else
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus == "Pending" ? "Rejected by " + user.FullName : ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", ail.ApproverStatus == "Pending" ? "" : ail.Comment));
                                        }

                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();


                                    }
                                    //Forms List Update
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                break;
                            }
                        case "OR":
                            {
                                if (response == "Approved")
                                {
                                    //0 means No Assistant
                                    List<FormData> groupedList = new List<FormData>();
                                    int countPendingApprovers = 0;
                                    var mainApprover = currentApprover;
                                    currentApprover.ApproverStatus = "Approved";
                                    while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
                                    {
                                        mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
                                    }
                                    if (mainApprover.RelationWith != null)
                                    {
                                        countPendingApprovers += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
                                    }
                                    if (countPendingApprovers == -1)
                                    {
                                        return -1;
                                    }
                                    if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "and"))
                                    {

                                        currentApprover.ApproverStatus = response;
                                        groupedList.AddRange(
                                            GetRelatedOrApprover(currentLevelApprovers, mainApprover)
                                        );
                                        groupedList.Add(mainApprover);

                                    }
                                    else
                                    {
                                        groupedList.AddRange(currentLevelApprovers);
                                    }

                                    foreach (var cla in groupedList)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", cla.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        if (cla.ApproverUserName == user.UserName && cla.IsActive == 1)
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        }
                                        else
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Approved by " + user.FullName));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        }

                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();

                                    }

                                    if (countPendingApprovers < 1)
                                    {
                                        foreach (var nextApprover in nextLevelApprovers)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                        //Forms List Update
                                        if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
                                        {
                                            //update in formstatus as approved

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        //I guess yeh condn yaha nahi aayega?
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        //I guess yeh condn yaha nahi aayega?
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == minLevel)
                                        {
                                            //update in formstatus as initiliazed
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                    }
                                }
                                if (response == "Enquired")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();

                                        //ListItem currentItem = list.GetItemById(currentUser.Id);
                                        //currentItem["ApproverStatus"] = response;
                                        //currentItem["Comment"] = comment;
                                        //currentItem.Update();
                                        //_context.Load(currentItem);
                                        //_context.ExecuteQuery();
                                    }
                                    foreach (var ail in ApproverIdList)
                                    {

                                        if (currentUser.Id != ail.Id)
                                        {
                                            object Comment = DBNull.Value;
                                            if (ail.Comment != null)
                                                Comment = ail.Comment;

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }

                                    }
                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                    //Approval Master New Record added after Enquired
                                    int submitterId = 0;
                                    object submitterUserName = DBNull.Value;
                                    object businessNeed = DBNull.Value;
                                    DataTable DT = new DataTable();
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("GetFormDataByFormId", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(DT);
                                    con.Close();

                                    if (DT.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < DT.Rows.Count; i++)
                                        {

                                            submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                            businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                                        }
                                    }
                                    //if (submitterUserName != null)
                                    //    submitterUserName = dataList[0].SubmitterUserName;
                                    //if (businessNeed != null)
                                    //    businessNeed = dataList[0].BusinessNeed;//submitterUserName = dataList[0].SubmitterUserName;
                                    //businessNeed = dataList[0].BusinessNeed;

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                                    cmd1.Parameters.Add(new SqlParameter("@Department", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                                    cmd1.Parameters.Add(new SqlParameter("@Email", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                                    cmd1.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();


                                }
                                if (response == "Rejected")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        if (currentUser.ApproverUserName != user.UserName)
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Rejected by " + user.FullName));
                                        }
                                        else
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        }

                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    foreach (var ail in ApproverIdList)
                                    {
                                        if (currentUser.Id != ail.Id)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            if (ail.ApproverUserName != user.UserName)
                                            {
                                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus == "Pending" ? "Rejected by " + user.FullName : ail.ApproverStatus));
                                            }
                                            else
                                            {
                                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            }

                                            cmd1.Parameters.Add(new SqlParameter("@Comment", ail.ApproverStatus == "Pending" ? "" : ail.Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();

                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                    }
                                    //Forms List Update
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                break;
                            }

                        case "AND":
                            {
                                if (response == "Approved")
                                {
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();



                                    //var pendingStatusCount = currentLevelApprovers.ToList().Count();
                                    var mainApprover = currentApprover;
                                    currentApprover.ApproverStatus = "Approved";
                                    while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
                                    {
                                        mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
                                    }
                                    int pendingStatusCount = 0;
                                    if (mainApprover.RelationWith != null)
                                    {
                                        //pendingStatusCount = GetCountOfPendingRelationApproval(currentLevelApprovers, mainApprover);
                                        pendingStatusCount += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
                                    }

                                    //OR Condn code
                                    var orCondnApprovers = currentLevelApprovers.Where(x => x.Logic.ToLower() == "or").ToList();
                                    if (orCondnApprovers != null && orCondnApprovers.Count() > 0)
                                    {
                                        //Assuming AND case will not be a assist. but can be approver
                                        List<FormData> groupedList = new List<FormData>();
                                        var AppAssistList = GetRelatedOrApprover(currentLevelApprovers, mainApprover);
                                        if (AppAssistList != null && AppAssistList.Count() > 0)
                                        {
                                            groupedList.AddRange(AppAssistList);
                                        }
                                        foreach (var item in AppAssistList)
                                        {
                                            item.ApproverStatus = "Approved";//Temp Making all current approver assist as approved
                                        }
                                        foreach (var cla in groupedList)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", cla.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();


                                            //Remove Current approver and Its Assist as it is currently approved...
                                            orCondnApprovers.Remove(cla);
                                        }

                                        if (pendingStatusCount == -1)
                                        {
                                            return -1;
                                        }

                                    }

                                    if (pendingStatusCount == 0)
                                    {
                                        foreach (var nextApprover in nextLevelApprovers)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();


                                        }
                                    }

                                    if (pendingStatusCount == 0)
                                    {
                                        //Forms List Update
                                        if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == minLevel)
                                        {
                                            //update in formstatus as initiliazed
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                    }
                                }
                                if (response == "Enquired")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }


                                    //con = new SqlConnection(sqlConString);
                                    //cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    //cmd1.Parameters.Add(new SqlParameter("@Id", ApproverIdList[0].Id));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(1)));
                                    //cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ApproverIdList[0].ApproverStatus));
                                    //cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                                    //cmd1.CommandType = CommandType.StoredProcedure;
                                    //adapter1.SelectCommand = cmd1;
                                    //con.Open();
                                    //adapter1.Fill(dt1);
                                    //con.Close();

                                    foreach (var ail in ApproverIdList)
                                    {

                                        if (currentUser.Id != ail.Id)
                                        {
                                            object Comment = DBNull.Value;
                                            if (ail.Comment != null)
                                                Comment = ail.Comment;

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }

                                    }
                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();



                                    //Approval Master New Record added after Enquired
                                    int submitterId = 0;

                                    object submitterUserName = DBNull.Value;
                                    object businessNeed = DBNull.Value;
                                    //if (dataList[0].SubmitterUserName != null)
                                    //    submitterUserName = dataList[0].SubmitterUserName;
                                    //if (dataList[0].BusinessNeed != null)
                                    //    businessNeed = dataList[0].BusinessNeed;
                                    DataTable DT = new DataTable();
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("GetFormDataByFormId", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(DT);
                                    con.Close();

                                    if (DT.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < DT.Rows.Count; i++)
                                        {

                                            submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                            businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                                        }
                                    }
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                                    cmd1.Parameters.Add(new SqlParameter("@Department", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                                    cmd1.Parameters.Add(new SqlParameter("@Email", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                                    cmd1.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                if (response == "Rejected")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == user.UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }

                                    foreach (var ail in ApproverIdList)
                                    {
                                        if (currentUser.Id != ail.Id)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus == "Pending" ? response : ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", ail.ApproverStatus == "Pending" ? "" : ail.Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                    }

                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                break;
                            }
                    }
                }
                else if (dataList[0].RunWorkflow.ToLower() == "yes")
                {

                    DashboardModel dashboardModels = new DashboardModel();
                    DataModel DataModel1 = new DataModel();
                    List<FormData> DataList = new List<FormData>();
                    List<FormData> ApproverIdList = new List<FormData>();


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();

                    rowId = dataList[0].RowId;
                    formId = dataList[0].FormRelation.Id;

                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_SaveResponseforAprroval", con);
                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData formData1 = new FormData();
                            formData1.Id = Convert.ToInt32(dt1.Rows[i]["ID"]);
                            formData1.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
                            formData1.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
                            formData1.ApproverId = Convert.ToInt32(dt1.Rows[i]["ApproverId"]);
                            formData1.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
                            formData1.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                            formData1.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
                            formData1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            formData1.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);
                            formData1.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
                            formData1.NextApproverId = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            formData1.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
                            formData1.RelationWith = Convert.ToInt32(dt1.Rows[i]["RelationWith"]);
                            formData1.RelationId = Convert.ToInt32(dt1.Rows[i]["RelationId"]);
                            approverIdList.Add(formData1);

                        }
                    }
                    dashboardModels.Data = DataModel1;
                    DataModel1.Forms = ApproverIdList;

                    var currentLevelApprovers = approverIdList.Where(x => x.IsActive == 1);
                    var currentApprover = currentLevelApprovers.Where(x => x.ApproverUserName == user.UserName).FirstOrDefault();
                    var currentLevel = currentApprover.Level;
                    approverLevel = currentLevel.Value;
                    var minLevel = approverIdList.Min(x => x.Level);
                    var maxLevel = approverIdList.Max(x => x.Level);
                    var nextLevelApprovers = approverIdList.Where(x => x.IsActive == 0 && x.Level == currentLevel + 1);

                    if (response == "Approved")
                    {
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(dt1);
                        con.Close();



                        foreach (var nextApprover in nextLevelApprovers)
                        {
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }

                        //Forms List Update
                        if (currentApprover.Level == maxLevel)
                        {
                            //update in formstatus as approved
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();
                        }
                        else if (currentApprover.Level == minLevel)
                        {
                            //update in formstatus as initiliazed

                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }
                    }

                    if (response == "Enquired")
                    {
                        foreach (var ail in approverIdList)
                        {

                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }
                        //Forms List Update

                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(dt1);
                        con.Close();

                        //Approval Master New Record added after Enquired
                        int submitterId = 0;

                        object submitterUserName = DBNull.Value;
                        object businessNeed = DBNull.Value;
                        //if (dataList[0].SubmitterUserName != null)
                        //    submitterUserName = dataList[0].SubmitterUserName;
                        //if (dataList[0].BusinessNeed != null)
                        //    businessNeed = dataList[0].BusinessNeed;

                        DataTable DT = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("GetFormDataByFormId", con);
                        cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(DT);
                        con.Close();

                        if (DT.Rows.Count > 0)
                        {
                            for (int i = 0; i < DT.Rows.Count; i++)
                            {

                                submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                            }
                        }

                        DataTable dtResult = new DataTable();
                        SqlCommand cmd2 = new SqlCommand();
                        SqlDataAdapter adapter2 = new SqlDataAdapter();
                        con = new SqlConnection(sqlConString);
                        cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
                        cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
                        cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
                        cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                        cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                        cmd2.Parameters.Add(new SqlParameter("@Department", ""));
                        cmd2.Parameters.Add(new SqlParameter("@FormParentId", 0));
                        cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                        cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                        cmd2.Parameters.Add(new SqlParameter("@Email", ""));
                        cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                        cmd2.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
                        cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                        cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                        cmd2.CommandType = CommandType.StoredProcedure;
                        adapter2.SelectCommand = cmd2;
                        con.Open();
                        adapter2.Fill(dtResult);
                        con.Close();


                    }
                    if (response == "Rejected")
                    {
                        foreach (var ail in approverIdList)
                        {
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                            if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
                            {
                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            }
                            else
                            {
                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            }

                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }
                        //Forms List Update

                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(dt1);
                        con.Close();

                    }
                }
                else
                {
                    if (approvalType == 1 || approvalType == 0)
                    {
                        DataTable dtResult = new DataTable();
                        SqlCommand cmd2 = new SqlCommand();
                        SqlDataAdapter adapter2 = new SqlDataAdapter();
                        con = new SqlConnection(sqlConString);
                        cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
                        cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
                        cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
                        cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                        cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                        cmd2.Parameters.Add(new SqlParameter("@Department", ""));
                        cmd2.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@CreatedBy", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                        cmd2.Parameters.Add(new SqlParameter("@Email", ""));
                        cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Level", 1));
                        cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
                        cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverName", user.UserName));


                        cmd2.CommandType = CommandType.StoredProcedure;
                        adapter2.SelectCommand = cmd2;
                        con.Open();
                        adapter2.Fill(dtResult);
                        con.Close();

                    }
                    else if (approvalType == 2)
                    {
                        DataTable dtResult = new DataTable();
                        SqlCommand cmd2 = new SqlCommand();
                        SqlDataAdapter adapter2 = new SqlDataAdapter();
                        con = new SqlConnection(sqlConString);
                        cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
                        cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
                        cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
                        cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                        cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                        cmd2.Parameters.Add(new SqlParameter("@Department", ""));
                        cmd2.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@CreatedBy", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                        cmd2.Parameters.Add(new SqlParameter("@Email", ""));
                        cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Level", 1));
                        cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
                        cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverName", user.UserName));


                        cmd2.CommandType = CommandType.StoredProcedure;
                        adapter2.SelectCommand = cmd2;
                        con.Open();
                        adapter2.Fill(dtResult);
                        con.Close();
                    }


                }


                SendEmail(formId, user, response, comment, approverLevel, rowId, appRowId);

                return 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }

        public async Task<int> ResubmitUpdate(int formId)
        {
            try
            {
                ClientContext _context = new ClientContext(new Uri(conString));
                Web web = _context.Web;
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ID,ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,Level,Logic,"
              + "FormId/Id,FormId/Created,Author/Title&$filter=(FormId eq '" + formId + "' and IsActive eq 1)&$expand=FormId,Author");
                var responseText = await response.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText);
                var result = modelData.Node.Data;

                List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                foreach (var row in result)
                {
                    var currentItem = approvalMasterlist.GetItemById(row.Id);
                    //currentItem.RefreshLoad();
                    //_context.ExecuteQuery();
                    currentItem["IsActive"] = 0;
                    currentItem.Update();
                    _context.Load(currentItem);
                    _context.ExecuteQuery();
                }


                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public async Task SendEmail(int formId, UserData currentUser, string response, string comment, int level, int rowid, int approwid)
        {
            var uniqueFormName = GetFormShortNameByFormId(formId);
            var completeFormName = await GetFormNameByUniqueName(uniqueFormName);
            var users = await GetApproverEmailIds(formId, currentUser, response, level, rowid, approwid);
            users.AddRange(await GetSubmitterDetails(formId, uniqueFormName, rowid));

            var action = FormStates.None;
            switch (response.ToLower())
            {
                case "approved":
                    {
                        action = users.Any(x => x.IsLastApprover) ? FormStates.FinalApproval : FormStates.PartialApproval;
                        break;
                    }
                case "rejected":
                    {
                        action = FormStates.Reject;
                        break;
                    }
                case "enquired":
                    {
                        action = FormStates.Enquire;
                        break;
                    }
            }

            var emailData = new EmailDataModel()
            {
                FormId = formId.ToString(),
                FormName = completeFormName,
                Action = action,
                Recipients = users.Where(x => x.IsApprover),
                ParallelRecipients = users.Where(x => x.IsParallelApprover),
                UniqueFormName = uniqueFormName,
                Sender = users.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                OnBehalfSender = users.Where(x => x.IsOnBehalf).FirstOrDefault(),
                CurrentUser = currentUser,
                Comment = comment
            };

            var emailService = new EmailService();
            if (uniqueFormName == "ISLS")
            {
                var GPNo = "";
                await emailService.SendMailForISLS(emailData, GPNo);
            }
            else
            {
                await emailService.SendMail(emailData);
            }

        }

        public async Task<string> GetFormNameByUniqueName(string uniqueFormName)
        {
            string formName = "";
            try
            {
                List<DataModel> item = new List<DataModel>();
                List<FormData> item3 = new List<FormData>();
                con = new SqlConnection(sqlConString);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                cmd = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                cmd.Parameters.Add(new SqlParameter("@UniqueFormName", uniqueFormName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        formName = dt.Rows[0]["FormName"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[0]["FormName"]);
                    }

                }
                return formName;
            }
            catch (Exception ex)
            {
                return formName;
            }

        }

        public string GetFormShortNameByFormId(int formId)
        {
            var formName = "";
            try
            {
                List<DataModel> item = new List<DataModel>();
                List<FormData> item3 = new List<FormData>();
                con = new SqlConnection(sqlConString);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                cmd = new SqlCommand("GetFormDataByFormId", con);
                cmd.Parameters.Add(new SqlParameter("@FormId", formId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        formName = dt.Rows[0]["UniqueFormName"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[0]["UniqueFormName"]);
                    }

                }

                return formName;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return "";
        }

        //public async Task<List<UserData>> GetSubmitterDetails(int formId, string uniqueFormName, int rowid)
        //{
        //    List<UserData> submitters = new List<UserData>();
        //    try
        //    {
        //        var listName = GlobalClass.ListNames.ContainsKey(uniqueFormName) ? GlobalClass.ListNames[uniqueFormName] : "";
        //        //var listName = "IPAFForm";

        //        ClientContext _context = new ClientContext(new Uri(conString));
        //        _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
        //        Web web = _context.Web;
        //        List list = web.Lists.GetByTitle(listName);

        //        if (list.ContainsField("RequestSubmissionFor"))
        //        {
        //            var handler = new HttpClientHandler();
        //            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //            var client = new HttpClient(handler) { BaseAddress = new Uri(conString) };
        //            client.DefaultRequestHeaders.Accept.Clear();
        //            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");


        //            SqlCommand cmd = new SqlCommand();
        //            SqlDataAdapter adapter = new SqlDataAdapter();
        //            DataSet ds = new DataSet();

        //            con = new SqlConnection(sqlConString);

        //            cmd = new SqlCommand("SP_EmailIPAF", con);
        //            cmd.Parameters.Add(new SqlParameter("@listName", listName));
        //            cmd.Parameters.Add(new SqlParameter("@FormID", formId));
        //            cmd.Parameters.Add(new SqlParameter("@rowid", rowid));
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            adapter.SelectCommand = cmd;
        //            con.Open();
        //            adapter.Fill(ds);
        //            con.Close();

        //            var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('" + listName + "')/items?$select=EmployeeName,EmployeeEmailId,RequestSubmissionFor,OtherEmployeeName,OtherEmployeeEmailId&$filter=(FormID eq '" + formId + "' and ID eq '" + rowid + "')")).Result;
        //            var responseText = await response.Content.ReadAsStringAsync();

        //            if (responseText.Contains("401 UNAUTHORIZED"))
        //                GlobalClass.IsUserLoggedOut = true;

        //            JObject listObj = JObject.Parse(responseText);

        //            var mainUser = new UserData()
        //            {
        //                EmployeeName = listObj.SelectToken("d.results.[0].EmployeeName").ToString(),
        //                Email = listObj.SelectToken("d.results.[0].EmployeeEmailId").ToString(),
        //            };
        //            submitters.Add(mainUser);

        //            bool IsOnBehalf = listObj.SelectToken("d.results.[0].RequestSubmissionFor").ToString().ToLower() == "onbehalf";
        //            string otherEmpName = "";
        //            string otherEmpEmail = "";
        //            if (IsOnBehalf)
        //            {
        //                otherEmpName = listObj.SelectToken("d.results.[0].OtherEmployeeName").ToString();
        //                otherEmpEmail = listObj.SelectToken("d.results.[0].OtherEmployeeEmailId").ToString();

        //                var onBehalfUser = new UserData() { EmployeeName = otherEmpName, Email = otherEmpEmail, IsOnBehalf = true };
        //                submitters.Add(onBehalfUser);
        //            }
        //        }
        //        return submitters;
        //    }
        //    catch (Exception ex)
        //    {
        //        return submitters;
        //    }

        //}


        public async Task<List<UserData>> GetSubmitterDetails(int formId, string uniqueFormName, int rowid)
        {
            List<UserData> submitters = new List<UserData>();
            try
            {
                var listName = GlobalClass.ListNames.ContainsKey(uniqueFormName) ? GlobalClass.ListNames[uniqueFormName] : "";
                //var listName = "IPAFForm";
                //ClientContext _context = new ClientContext(new Uri(conString));
                //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //Web web = _context.Web;
                //List list = web.Lists.GetByTitle(listName);
                //if (list.ContainsField("RequestSubmissionFor"))
                //{
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                SqlConnection con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("USP_GetSubmitterDetails", con);
                sqlCommand.Parameters.Add(new SqlParameter("@ListName", listName));
                sqlCommand.Parameters.Add(new SqlParameter("@formId", formId));
                sqlCommand.Parameters.Add(new SqlParameter("@rowId", rowid));
                sqlCommand.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = sqlCommand;
                con.Open();
                adapter.Fill(dt);
                con.Close();
                if (dt.Rows.Count > 0)
                {
                    var mainUser = new UserData()
                    {
                        EmployeeName = Convert.ToString(dt.Rows[0]["EmployeeName"]),
                        Email = Convert.ToString(dt.Rows[0]["EmployeeEmailId"]),
                    };
                    submitters.Add(mainUser);
                    bool IsOnBehalf = Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]).ToLower() == "onbehalf";
                    string otherEmpName = "";
                    string otherEmpEmail = "";
                    if (IsOnBehalf)
                    {
                        otherEmpName = Convert.ToString(dt.Rows[0]["OtherEmployeeName"]);
                        otherEmpEmail = Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]);
                        var onBehalfUser = new UserData() { EmployeeName = otherEmpName, Email = otherEmpEmail, IsOnBehalf = true };
                        submitters.Add(onBehalfUser);
                    }
                }
                //}
                return submitters;
            }
            catch (Exception ex)
            {
                return submitters;
            }
        }


        public async Task<List<UserData>> GetApproverEmailIds(int formId, UserData user, string action, int level, int rowid, int approwid)
        {
            List<UserData> users = new List<UserData>();

            try
            {
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                //var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApproverId,ApproverUserName,ApproverStatus,IsActive,Modified,Logic,Level,RunWorkflow,NextApproverId,AssistantForEmployeeUserId,RelationWith,RelationId,Author/Title&$filter=(FormId eq " + formId + " and RowId eq " + rowid + ")&$expand=Author")).Result;
                //var responseText = await response.Content.ReadAsStringAsync();

                //var settings = new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore
                //};
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText, settings);

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                List<ApprovalDataModel> modelData = new List<ApprovalDataModel>();

                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("sp_GetApprovalMaster", con);
                cmd1.Parameters.Add(new SqlParameter("@RowId", rowid));
                cmd1.Parameters.Add(new SqlParameter("@FormId", formId));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter1.SelectCommand = cmd1;
                con.Open();
                adapter1.Fill(ds1);
                con.Close();
                if (ds1.Tables[0].Rows.Count > 0 && ds1.Tables[0] != null)
                {
                    for (int i = 0; i < ds1.Tables[0].Rows.Count; i++)
                    {
                        ApprovalDataModel app = new ApprovalDataModel();
                        FormLookup item1 = new FormLookup();
                        item1.Id = Convert.ToInt32(ds1.Tables[0].Rows[i]["FormID"]);
                        item1.CreatedDate = ds1.Tables[0].Rows[i]["Created"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds1.Tables[0].Rows[i]["Created"]);
                        app.FormId = item1;
                        Author item2 = new Author();
                        item2.Submitter = Convert.ToString(ds1.Tables[0].Rows[i]["Title"]);
                        app.Author = item2;
                        app.ApproverId = ds1.Tables[0].Rows[i]["ApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["ApproverId"]);
                        app.ApproverStatus = ds1.Tables[0].Rows[i]["ApproverStatus"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["ApproverStatus"]);
                        app.Modified = ds1.Tables[0].Rows[i]["Modified"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds1.Tables[0].Rows[i]["Modified"]);
                        app.Designation = ds1.Tables[0].Rows[i]["Designation"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["Designation"]);
                        app.ApproverName = ds1.Tables[0].Rows[i]["ApproverName"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["ApproverName"]);
                        app.Level = ds1.Tables[0].Rows[i]["Level"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["Level"]);
                        app.Logic = ds1.Tables[0].Rows[i]["Logic"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["Logic"]);
                        app.TimeStamp = ds1.Tables[0].Rows[i]["TimeStamp"] == DBNull.Value ? Convert.ToDateTime("01-01-1900") : Convert.ToDateTime(ds1.Tables[0].Rows[i]["TimeStamp"]);
                        app.IsActive = ds1.Tables[0].Rows[i]["IsActive"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["IsActive"]);
                        app.NextApproverId = ds1.Tables[0].Rows[i]["NextApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(ds1.Tables[0].Rows[i]["NextApproverId"]);
                        app.Comment = ds1.Tables[0].Rows[i]["Comment"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["Comment"]);
                        app.RunWorkflow = ds1.Tables[0].Rows[i]["RunWorkflow"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["RunWorkflow"]);
                        app.ApproverUserName = ds1.Tables[0].Rows[i]["ApproverUserName"] == DBNull.Value ? null : Convert.ToString(ds1.Tables[0].Rows[i]["ApproverUserName"]);
                        modelData.Add(app);
                    }
                }

                if (modelData.Any(x => x.RunWorkflow.ToLower() == "no")) //check for parallel
                {
                    var currentApprover = modelData.Where(x =>
                        x.Level == level &&
                        (
                            !user.IsFinalMailTriggeredManually
                                ? x.ApproverUserName == user.UserName
                                : (!string.IsNullOrEmpty(user.FinalMailApproverUserName) && x.ApproverUserName == user.FinalMailApproverUserName)
                        )
                    ).FirstOrDefault();
                    if (currentApprover == null)
                        return users;

                    switch (currentApprover.Logic)
                    {
                        case "NOT":
                            {
                                var nextApprovers = modelData.Where(x => x.Level == level + 1);
                                if (nextApprovers == null || nextApprovers.Count() == 0) //meaning current approver is the last approver
                                {
                                    users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsLastApprover = true, IsApprover = true });
                                    return users;
                                }
                                else
                                {
                                    users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });

                                    var nextLevelApprovers = await GetUsersByIds(nextApprovers.Select(x => x.ApproverUserName));
                                    nextLevelApprovers.ForEach(x => { x.IsNextApprover = true; x.IsApprover = true; });
                                    users.AddRange(nextLevelApprovers);
                                }
                                break;
                            }
                        case "AND":
                            {
                                var currentLevelApprovers = modelData.Where(x => x.Level == level);
                                //users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });

                                var orCondnApprovers = currentLevelApprovers.Where(x => x.Logic.ToLower() == "or").ToList();
                                int pendingCount = 0;
                                List<ApprovalDataModel> tempOrApprovers = new List<ApprovalDataModel>();
                                if (orCondnApprovers != null && orCondnApprovers.Count() > 0)
                                {
                                    List<ApprovalDataModel> groupedList = new List<ApprovalDataModel>();
                                    //groupedList.Add(currentApprover);
                                    //var AppAssistList = currentLevelApprovers.Where(x => x.AssistantForEmpUserId == currentApprover.ApproverId).ToList();
                                    //if (AppAssistList != null && AppAssistList.Count > 0)
                                    //{
                                    //    groupedList.AddRange(AppAssistList);
                                    //}
                                    //foreach (var item in AppAssistList)
                                    //{
                                    //    item.ApproverStatus = currentApprover.ApproverStatus;//Temp Making all current approver assist as approved
                                    //}
                                    //orPendingCount = currentLevelApprovers.Where(x => x.ApproverStatus.ToLower() == "pending" && x.Logic.ToLower() == "or" && x.IsActive == 1).Count();
                                    //tempOrApprovers = AppAssistList.ToList();

                                    //_____________________

                                    //foreach (var item in currentLevelApprovers.Where(x => x.RelationWith == 0))
                                    //{
                                    //    //Assuming All top level approvers have same logic.
                                    //    pendingCount += GetCountOfPendingRelationApproval(currentLevelApprovers, item);
                                    //}
                                    var mainApprover = currentApprover;
                                    while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
                                    {
                                        mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
                                    }
                                    //pendingCount += GetCountOfPendingRelationApproval(currentLevelApprovers.Where(x => x.IsActive == 1), mainApprover);
                                    pendingCount += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
                                    tempOrApprovers = currentLevelApprovers.Where(x =>
                                        x.AssistantForEmployeeUserName?.ToLower() == currentApprover.ApproverUserName.ToLower()
                                        || (currentApprover.RelationWith != null && currentApprover.RelationWith != 0 && x.RelationId == currentApprover.RelationWith && x.Logic.ToLower() == "or")
                                        || (x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or")
                                    ).ToList();

                                    //_________________
                                    //var AppAssistList = currentLevelApprovers.Where(x => x.AssistantForEmpUserId == currentApprover.ApproverId).ToList();
                                    //foreach (var cla in AppAssistList)
                                    //{
                                    //    //Remove Current approver and Its Assist as it is currently approved...
                                    //    orCondnApprovers.Remove(cla);
                                    //}
                                    //List<int> temp = new List<int>();
                                    //foreach (var item in orCondnApprovers)
                                    //{
                                    //    if (!temp.Contains(item.ApproverId) || !temp.Contains(item.AssistantForEmpUserId))
                                    //    {
                                    //        if (orCondnApprovers.Where(x => x.ApproverId == item.ApproverId || x.AssistantForEmpUserId == item.ApproverId)
                                    //            .All(x => x.ApproverStatus.ToLower() == "pending"))
                                    //        {
                                    //            temp.Add(item.ApproverId);
                                    //            orPendingCount++;
                                    //        }
                                    //    }
                                    //}
                                }

                                //if (!currentLevelApprovers.Any(y => y.Logic.ToLower() == "and" && y.IsActive == 1 && y.ApproverStatus.ToLower() == "pending") && orPendingCount == 0)
                                if (pendingCount == 0)
                                {
                                    var nextLevelApprovers = modelData.Where(x => x.Level == level + 1);
                                    //var currentLvlApprovers = modelData.Node.Data.Where(x => x.IsActive == 1);
                                    //var pendingStatus = currentLvlApprovers.Any(x => x.ApproverStatus == "Pending" & x.Logic == "AND");
                                    if (nextLevelApprovers == null || nextLevelApprovers.Count() == 0)// && pendingStatus == false //meaning current approver is the last approver
                                    {
                                        users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true, IsLastApprover = true });
                                        return users;
                                    }
                                    else
                                    {
                                        users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });

                                        //if (pendingStatus == false)
                                        //{
                                        var nextApprovers = await GetUsersByIds(nextLevelApprovers.Select(x => x.ApproverUserName));
                                        nextApprovers.ForEach(x => { x.IsNextApprover = true; x.IsApprover = true; });
                                        users.AddRange(nextApprovers);
                                        //}

                                    }
                                }
                                else
                                {
                                    users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });
                                    if (tempOrApprovers.Count() > 0)
                                    {
                                        var userListParallelApprovers = await GetUsersByIds(tempOrApprovers.Select(x => x.ApproverUserName));
                                        userListParallelApprovers.ForEach(x => { x.IsParallelApprover = true; x.IsApprover = true; });
                                        users.AddRange(userListParallelApprovers);
                                    }
                                }
                                break;
                            }
                        case "OR":
                            {
                                //Parallel Approver user
                                List<ApprovalDataModel> groupedList = new List<ApprovalDataModel>();
                                var data = modelData;
                                var currentLevelApprovers = data.Where(x => x.Level == level);
                                int countPendingApprovers = 0;
                                if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "and"))
                                {
                                    //var andPendingApproverList = currentLevelApprovers.Where(x => x.Logic.ToLower() == "and" && x.ApproverStatus.ToLower() == "pending").ToList();
                                    //foreach (var item in andPendingApproverList)
                                    //{
                                    //    if (currentLevelApprovers.Any(x => x.ApproverStatus.ToLower() == "approved" && item.ApproverId == x.AssistantForEmpUserId))
                                    //    {
                                    //        item.ApproverStatus = "Approved";//Temp making status as approved as it is approved by assist....
                                    //    }
                                    //}
                                    //countPendingApprovers = andPendingApproverList.Where(x => x.ApproverStatus.ToLower() == "pending").Count();
                                    //groupedList.AddRange(currentLevelApprovers.Where(x => x.ApproverId == currentApprover.AssistantForEmpUserId || x.AssistantForEmpUserId == currentApprover.AssistantForEmpUserId));

                                    //-------------

                                    //foreach (var item in currentLevelApprovers.Where(x => x.RelationWith == 0))
                                    //{
                                    //    //Assuming All top level approvers have same logic.
                                    //    countPendingApprovers += GetCountOfPendingRelationApproval(currentLevelApprovers, item);
                                    //}
                                    var mainApprover = currentApprover;
                                    while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
                                    {
                                        mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
                                    }
                                    //countPendingApprovers += GetCountOfPendingRelationApproval(currentLevelApprovers.Where(x => x.IsActive == 1), mainApprover);
                                    countPendingApprovers += GetStatusOfApproval(currentLevelApprovers.Where(x => x.IsActive == 1).ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
                                    //countPendingApprovers += GetCountOfPendingRelationApproval(currentLevelApprovers, currentApprover);
                                    groupedList.AddRange(
                                        //currentLevelApprovers.Where(x =>
                                        //    x.RelationId == currentApprover.RelationWith
                                        //    || x.RelationWith == currentApprover.RelationWith && x.Logic.ToLower() == "or"
                                        //)
                                        GetRelatedOrApprover(currentLevelApprovers, mainApprover)
                                    );
                                    groupedList.Add(mainApprover);
                                    //-------------

                                    //groupedList.AddRange(
                                    //    data.Where(x =>
                                    //        x.Logic.ToLower() == "or" && x.Level == currentApprover.Level
                                    //        && (
                                    //            x.ApproverId == currentApprover.ApproverId
                                    //            || x.AssistantForEmpUserId == currentApprover.ApproverId
                                    //            || (
                                    //                currentApprover.AssistantForEmpUserId != 0 //Assist.
                                    //                ? currentApprover.AssistantForEmpUserId == x.ApproverId
                                    //                    || currentApprover.AssistantForEmpUserId == x.AssistantForEmpUserId
                                    //                : false
                                    //            )
                                    //        )
                                    //    ).ToList()
                                    //);
                                    //groupedList.Add(currentApprover);
                                    //var AppAssistList = data.Where(x => x.AssistantForEmpUserId == currentApprover.ApproverId).ToList();
                                    //if (AppAssistList != null && AppAssistList.Count > 0)
                                    //{
                                    //    groupedList.AddRange(AppAssistList);
                                    //}
                                    ////When Assist is approving
                                    //var AppList = data.Where(x => x.ApproverId == currentApprover.AssistantForEmpUserId
                                    //    || x.AssistantForEmpUserId == currentApprover.AssistantForEmpUserId).ToList();
                                    //if (currentApprover.AssistantForEmpUserId != 0 && AppList != null && AppList.Count > 0)
                                    //{
                                    //    groupedList.AddRange(AppList);
                                    //}
                                    //// When Approver is not assist. and approver don't have its own assist.
                                    //if (currentApprover.Logic.ToLower() == "or" && currentApprover.AssistantForEmpUserId == 0)
                                    //{
                                    //    var orList = data.Where(x => x.Logic.ToLower() == "or").ToList();
                                    //    if (orList != null && orList.Count > 0)
                                    //    {
                                    //        groupedList.AddRange(orList);
                                    //    }
                                    //}
                                }
                                else
                                {
                                    groupedList.AddRange(currentLevelApprovers);
                                }
                                var userParallel = groupedList.Where(x =>
                                    x.Level == currentApprover.Level
                                    && x.ApproverUserName.ToLower() != currentApprover.ApproverUserName.ToLower()
                                );

                                var userListParallelApprovers = await GetUsersByIds(userParallel.Select(x => x.ApproverUserName));
                                userListParallelApprovers.ForEach(x => { x.IsParallelApprover = true; x.IsApprover = true; });
                                users.AddRange(userListParallelApprovers);
                                //Parallel Approver user

                                if (countPendingApprovers < 1)
                                {
                                    var nextLevelApprovers = modelData.Where(x => x.Level == level + 1);

                                    if (nextLevelApprovers == null || nextLevelApprovers.Count() == 0) //meaning current approver is the last approver
                                    {
                                        users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true, IsLastApprover = true });
                                        return users;
                                    }
                                    else
                                    {
                                        var nextApprovers = await GetUsersByIds(nextLevelApprovers.Select(x => x.ApproverUserName));
                                        nextApprovers.ForEach(x => { x.IsNextApprover = true; x.IsApprover = true; });
                                        users.AddRange(nextApprovers);
                                        users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });
                                    }
                                }
                                else
                                    users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });
                                break;
                            }
                    }
                }
                else
                {
                    var currentApprover = modelData.Where(x => x.Id == approwid).FirstOrDefault();
                    if (currentApprover == null)
                    {
                        Log.Error("Current user is null in GetApproverEmaiIds" + DateTime.Now);
                        return users;
                    }

                    var nextApprover = modelData.Where(x => x.ApproverUserName == Convert.ToString(currentApprover.NextApproverId) && x.Id != approwid).FirstOrDefault();
                    if (nextApprover == null && currentApprover.NextApproverId == 0) //meaning current approver is the last approver
                    {
                        users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsLastApprover = true, IsCurrentApprover = true, IsApprover = true });

                        return users;
                    }
                    else
                    {
                        users.Add(new UserData { EmployeeName = user.FullName, Email = user.Email, IsCurrentApprover = true, IsApprover = true });
                    }

                    if (adCode.ToLower() == "yes")
                    {
                        //AD Code
                        //string objectSid = user.ObjectSid;
                        string approverId = nextApprover.ApproverUserName;
                        string en = GetApproverNameFromAD(approverId);
                        string em = GetApproverEmaiilFromAD(approverId);
                        var data = new UserData
                        {
                            EmployeeName = en,
                            Email = em,
                            IsApprover = true,
                            IsNextApprover = true,
                        };
                        users.Add(data);
                        //AD Code
                    }
                    else
                    {

                        var EmailId = "";
                        var title = "";
                        SqlCommand cmdUN = new SqlCommand();
                        SqlDataAdapter UNadapter = new SqlDataAdapter();
                        DataTable dt2 = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmdUN = new SqlCommand("sp_GetUserByUserName", con);
                        cmdUN.Parameters.Add(new SqlParameter("@UserName", nextApprover.ApproverUserName));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmdUN.CommandType = CommandType.StoredProcedure;
                        UNadapter.SelectCommand = cmdUN;
                        con.Open();
                        UNadapter.Fill(dt2);
                        con.Close();
                        if (dt2.Rows.Count > 0)
                        {

                            for (int i = 0; i < dt2.Rows.Count; i++)
                            {
                                EmailId = Convert.ToString(dt2.Rows[i]["EmailID"]);
                                title = Convert.ToString(dt2.Rows[i]["FirstName"]) + " " + Convert.ToString(dt2.Rows[i]["LastName"]);
                            }

                        }
                        var data = new UserData
                        {
                            EmployeeName = title,
                            Email = EmailId,
                            IsApprover = true,
                            IsNextApprover = true,
                        };

                        users.Add(data);
                        //Local Code:- Sharepoint Code
                        users = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<UserData>();
            }
            return users;
        }

        public async Task<List<UserData>> GetUsersByIds(IEnumerable<string> ids)
        {
            List<UserData> users = new List<UserData>();
            List<string> idList = ids.ToList();
            try
            {
                if (adCode.ToLower() == "yes")
                {
                    //AD Code               
                    for (int i = 0; i < idList.Count; i++)
                    {
                        //string objectSid = user.ObjectSid;
                        string approverId = idList[i];
                        string en = GetApproverNameFromAD(approverId);
                        string em = GetApproverEmaiilFromAD(approverId);
                        string id = idList[i];
                        var data = new UserData
                        {
                            EmployeeName = en,
                            Email = em,
                            ID = id
                        };
                        users.Add(data);
                    }
                    //AD Code
                }
                else
                {
                    //Local Code:- Sharepoint Code
                    //var handler = new HttpClientHandler();
                    //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                    //var client = new HttpClient(handler);
                    //client.BaseAddress = new Uri(conString);
                    //client.DefaultRequestHeaders.Accept.Clear();
                    //client.Timeout = TimeSpan.FromSeconds(10);

                    //string concatQueryString = "";
                    //for (int i = 0; i < ids.Count(); i++)
                    //{
                    //    var currentId = ids.ElementAt(i);
                    //    concatQueryString += $"Id eq {currentId} {(i != ids.Count() - 1 ? "or " : "")}";
                    //}
                    //var response = await client.GetAsync("_api/web/SiteUserInfoList/items?$select=Id,Title,EMail&$filter=(" + concatQueryString + ")");
                    //var responseText = await response.Content.ReadAsStringAsync();

                    //XmlDocument doc = new XmlDocument();
                    //var responseString = responseText.ToString();
                    //doc.LoadXml(responseString);

                    //var id = doc.GetElementsByTagName("d:Id");
                    //var title = doc.GetElementsByTagName("d:Title");
                    //var email = doc.GetElementsByTagName("d:EMail");
                    //----------------

                    string concatQueryString = "";
                    for (int i = 0; i < idList.Count(); i++)
                    {
                        //var currentId = ids.ElementAt(i);
                        concatQueryString += idList[i] + ",";
                    }
                    concatQueryString = concatQueryString.Remove(concatQueryString.Length - 1, 1);
                    var EmailId = "";
                    List<UserData> empdata = new List<UserData>();
                    SqlCommand cmdUN = new SqlCommand();
                    SqlDataAdapter UNadapter = new SqlDataAdapter();
                    DataTable dt2 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmdUN = new SqlCommand("sp_GetUsersByUserNames", con);
                    cmdUN.Parameters.Add(new SqlParameter("@UserName", concatQueryString));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmdUN.CommandType = CommandType.StoredProcedure;
                    UNadapter.SelectCommand = cmdUN;
                    con.Open();
                    UNadapter.Fill(dt2);
                    con.Close();
                    if (dt2.Rows.Count > 0)
                    {

                        for (int i = 0; i < dt2.Rows.Count; i++)
                        {
                            UserData item = new UserData();
                            item.EmpNumber = Convert.ToInt32(dt2.Rows[i]["EmployeeNumber"]);
                            item.Email = Convert.ToString(dt2.Rows[i]["EmailID"]);
                            item.EmployeeName = Convert.ToString(dt2.Rows[i]["FirstName"]) + " " + Convert.ToString(dt2.Rows[i]["LastName"]);
                            empdata.Add(item);
                        }

                    }

                    for (int i = 0; i < empdata.Count; i++)
                    {
                        var data = new UserData
                        {
                            EmployeeName = empdata[i].EmployeeName,
                            Email = empdata[i].Email,
                            ID = Convert.ToString(empdata[i].EmpNumber)
                        };
                        users.Add(data);
                    }
                    //Local Code:- Sharepoint Code
                }

                return users;
            }
            catch (Exception ex)
            {
                return users;
            }
        }
        /// <summary>
        /// ALL-It is used to cancel the form.
        /// </summary>
        /// <returns></returns>
        public async Task<int> CancelForm(int formId)
        {
            var formData = new List<FormData>();
            //var handler = new HttpClientHandler();
            //GlobalClass gc = new GlobalClass();
            //var currentUser = gc.GetCurrentUser();
            //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            //ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            try
            {
                #region Comment
                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,Status&$filter=(Id eq '" + formId + "')");
                //var responseText2 = await response2.Content.ReadAsStringAsync();

                //if (!string.IsNullOrEmpty(responseText2))
                //{
                //    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText2);
                //    formData = modelResult.Data.Forms;
                //}
                #endregion
                //DataModel model = new DataModel();
                List<DataModel> item = new List<DataModel>();
                List<FormData> item3 = new List<FormData>();
                con = new SqlConnection(sqlConString);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                cmd = new SqlCommand("GetFormDataByFormId", con);
                cmd.Parameters.Add(new SqlParameter("@FormId", formId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        FormData item1 = new FormData();
                        item1.Id = Convert.ToInt32(dt.Rows[i]["Id"]);
                        item1.Status = dt.Rows[0]["Status"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[0]["Status"]);
                        item3.Add(item1);
                    }

                }
                formData = item3;



                if ((formData[0].Status == "Submitted") || (formData[0].Status == "Initiated") || (formData[0].Status == "Resubmitted"))
                {
                    DataSet ds2 = new DataSet();
                    SqlDataAdapter adapter_form2 = new SqlDataAdapter();
                    SqlCommand cmd2 = new SqlCommand();
                    var con2 = new SqlConnection(sqlConString);
                    cmd2 = new SqlCommand("UpdateFormStatusByFormId", con2);
                    cmd2.Parameters.Add(new SqlParameter("@Forms", formId));
                    cmd2.Parameters.Add(new SqlParameter("@FStatus", "Cancelled"));
                    cmd2.CommandType = CommandType.StoredProcedure;
                    adapter_form2.SelectCommand = cmd2;
                    con2.Open();
                    adapter_form2.Fill(ds2);
                    con2.Close();

                    #region Comment
                    //List list = _context.Web.Lists.GetByTitle("Forms");
                    //ListItem listItem = list.GetItemById(formId);
                    //listItem["Status"] = "Cancelled";
                    //listItem.Update();
                    //_context.Load(listItem);
                    //_context.ExecuteQuery();
                    #endregion
                    ListDAL dal = new ListDAL();
                    var resubmitResult = dal.CancelApprovalMaster(formId);

                    return 1;
                }
                else if (formData[0].Status == "Cancelled")
                {
                    return 3;
                }
                else
                {
                    return 2;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }


        /// <summary>
        /// PAF-It is used for populating the Location Dropdown from the LocationDetails master in SharePoint list.
        /// </summary>
        /// <returns></returns>

        public async Task<int> SaveForms(string formName, string uniqueFormName, string listName)
        {
            int formId = 0;
            ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential(spUsername, spPass);
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            Web web = _context.Web;

            var handler = new HttpClientHandler();
            //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(conString);
            client.DefaultRequestHeaders.Accept.Clear();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
            var response = client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id&$filter=(UniqueFormName eq '" + uniqueFormName + "')").Result;

            var responseText = await response.Content.ReadAsStringAsync();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(responseText.ToString());
            var formParentId = doc.GetElementsByTagName("d:Id");

            try
            {
                List FormsList = web.Lists.GetByTitle("Forms");
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = FormsList.AddItem(itemCreateInfo);
                newItem["FormName"] = formName;
                newItem["UniqueFormName"] = uniqueFormName;
                newItem["ListName"] = listName;
                newItem["SubmitterUserName"] = user.UserName;
                newItem["Status"] = "Submitted";
                newItem["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                newItem["Department"] = user.Department;
                newItem["FormParentId"] = 4;
                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();

                formId = newItem.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return formId;
        }

        public async Task<int> GetCisoId()
        {
            int cisoId = 0;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = client.GetAsync("_api/web/lists/GetByTitle('Ciso')/items?$select=Id").Result;
                var responseText = response.Content.ReadAsStringAsync();
                var responseString = responseText.Result.ToString();
                //var responseString = responseText.ToString();
                //doc.LoadXml(responseText.ToString());
                //XmlDocument doc = new XmlDocument();
                //string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                //if (responseString.StartsWith(_byteOrderMarkUtf8))
                //{
                //    responseString = responseString.Remove(0, _byteOrderMarkUtf8.Length);
                //}

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseString);

                var id = doc.GetElementsByTagName("d:Id");
                cisoId = Convert.ToInt16(id[0].InnerXml);
                if (!(cisoId > 0))
                    cisoId = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return cisoId;
        }

        public async Task<int> GetLisoId()
        {
            int lisoId = 0;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('Liso')/items?$select=Id");
                var responseText = await response.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseText.ToString());
                var id = doc.GetElementsByTagName("d:Id");
                lisoId = Convert.ToInt16(id[0].InnerXml);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return lisoId;
        }

        public async Task<int> GetManagerId()
        {
            int managerId = 0;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ServiceDeskManager')/items?$select=Id");
                var responseText = await response.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseText.ToString());
                var id = doc.GetElementsByTagName("d:Id");
                managerId = Convert.ToInt16(id[0].InnerXml);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return managerId;
        }

        /// <summary>
        /// InternetAccess-It is used to get the organizations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        //public async Task<List<SubmitterData>> GetOrganizations()
        //{
        //    List<SubmitterData> organizations = new List<SubmitterData>();//constructor called, always expandoobject for dynamic dataType

        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
        //        var response = await client.GetAsync("_api/web/lists/GetByTitle('Organization')/items?$select=ID,Organization");
        //        var responseText = await response.Content.ReadAsStringAsync();

        //        if (!string.IsNullOrEmpty(responseText))
        //        {
        //            var result = JsonConvert.DeserializeObject<SubmitterModel>(responseText);
        //            organizations = result.List.JobList;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //    }
        //    return organizations;
        //}

        public async Task<List<ExternalOrganizationModel>> GetExternalOrganization()
        {
            List<ExternalOrganizationModel> organizations = new List<ExternalOrganizationModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetExternalOrganizations", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        ExternalOrganizationModel extOrg = new ExternalOrganizationModel();
                        extOrg.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeID"]);
                        extOrg.Organization = ds.Tables[0].Rows[i]["Organization"].ToString();

                        organizations.Add(extOrg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<ExternalOrganizationModel>();
            }
            return organizations;
        }
        /// <summary>
        /// InternetAccess/SmartPhone-It is used to get the locations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<List<LocationData>> GetLocations()
        {
            List<LocationData> locations = new List<LocationData>();
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response = await client.GetAsync("_api/web/lists/GetByTitle('LocationMaster')/items?$select=LocationId,LocationName&$filter=(IsActive eq '" + 1 + "')");
                //var responseText = await response.Content.ReadAsStringAsync();

                //if (!string.IsNullOrEmpty(responseText))
                //{
                //    var result = JsonConvert.DeserializeObject<LocationModel>(responseText);
                //    locations = result.List.Locations;
                //}
                //List<LocationData> locationDatas = new List<LocationData>;
                // List<LocationData> locations = new List<LocationData>();//constructor called, always expandoobject for dynamic dataType

                SqlDataAdapter adapter_form = new SqlDataAdapter();
                SqlCommand cmd_form = new SqlCommand();
                DataTable dt = new DataTable();
                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("sp_GetLocation", con_form);
                cmd_form.CommandType = CommandType.StoredProcedure;
                adapter_form.SelectCommand = cmd_form;
                con_form.Open();
                adapter_form.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        LocationData locationData = new LocationData();
                        locationData.LocationId = Convert.ToInt32((dt.Rows[i]["LocationId"]));
                        locationData.LocationName = Convert.ToString((dt.Rows[i]["LocationName"]));

                        locations.Add(locationData);
                    }



                }



            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<LocationData>();
            }
            return locations;
        }

        //public List<LocationData> GetLocation()
        //{

        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();
        //        List<LocationData> locationList = new List<LocationData>();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_GetITServiceDeskLocations", con);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                LocationData lm = new LocationData();
        //                lm.LocationId = Convert.ToInt32(ds.Tables[0].Rows[i]["LocationId"]);
        //                lm.LocationName = Convert.ToString(ds.Tables[0].Rows[i]["LocationName"]);

        //                //dm.IsActive = Convert.ToInt32(ds.Tables[0].Rows[i]["IsActive"]);
        //                locationList.Add(lm);
        //            }
        //        }
        //        return locationList;
        //    }
        //    catch (Exception e)
        //    {
        //        return new List<LocationData>();
        //    }

        //}

        /// <summary>
        /// InternetAccess-It is used to get the employee details from sql db.
        /// </summary>
        /// <returns></returns>
        public List<UserData> GetEmployeeDetails(string firstName)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetEmpDetails", con);
                cmd.Parameters.Add(new SqlParameter("@FirstName", firstName));
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
                        //user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        user.EmployeeName = ds.Tables[0].Rows[i]["FullName"].ToString();
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }

        public List<UserData> GetEmployeeDetailsByEmpNum(long EmpNum)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetEmpDetailsByEmpNum", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", EmpNum));
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
                        user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        user.EmployeeName = ds.Tables[0].Rows[i]["FullName"].ToString();
                        user.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmpNumber"]);
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }

        public List<UserData> SearchEmployeeDetails(string firstName, string lastName)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SearchEmpDetails", con);
                cmd.Parameters.Add(new SqlParameter("@FirstName", firstName));
                cmd.Parameters.Add(new SqlParameter("@LastName", lastName));
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
                        // user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["Id"]);
                        user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        //user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        user.EmployeeName = ds.Tables[0].Rows[i]["FullName"].ToString();
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }
        /// <summary>
        /// InternetAccess-It is used to get the employee details using userid as parameter from sql db.
        /// </summary>
        /// <returns></returns>
        public UserData GetExistingEmployeeDetails(string otherEmpUserId, long EmpNumber)
        {
            UserData userData = new UserData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetExistingEmployeeDetails", con);
                cmd.Parameters.Add(new SqlParameter("@emailId", otherEmpUserId));
                cmd.Parameters.Add(new SqlParameter("@EmpNum", EmpNumber));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        userData.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        userData.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        userData.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        userData.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        //userData.FullName = user.FirstName + " " + user.LastName;
                        userData.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        userData.Department = ds.Tables[0].Rows[i]["Department"].ToString();
                        userData.PhoneNumber = Convert.ToString(ds.Tables[0].Rows[i]["PhoneNumber"]);

                    }
                }

                // var handler = new HttpClientHandler();
                // handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                // //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                // var client = new HttpClient(handler);
                // client.BaseAddress = new Uri(conString);
                // client.DefaultRequestHeaders.Accept.Clear();
                // client.Timeout = TimeSpan.FromSeconds(10);
                // var emailString = "";
                // emailString += $"EMail eq '{userData.Email}'";

                // var response = client.GetAsync($"_api/web/SiteUserInfoList/items?$select=Id,Title,EMail,UserName,Office,Name&$filter=({emailString})").Result;
                // if (response == null)
                // {
                //     List<UserData> app = new List<UserData>();
                // }
                // var responseText = response.Content.ReadAsStringAsync();


                // XmlDocument doc = new XmlDocument();
                // var responseString = responseText.Result.ToString();

                // doc.LoadXml(responseString);
                // var title = doc.GetElementsByTagName("d:Title");
                // //var id = doc.GetElementsByTagName("d:Id");
                // var email = doc.GetElementsByTagName("d:EMail");
                // var name = doc.GetElementsByTagName("d:Name");
                //// userData.UserId = XmlConvert.ToInt32(id[0].InnerXml);
                // var nodeesName = new List<XmlNode>(name.Cast<XmlNode>());
                // var userName = nodeesName[0].InnerXml;
                // var str = userName.Replace("i:0#.w|apac\\", "");
                // userData.UserName = str;

                // if (userData.Email.Contains("extern"))
                // {
                //call method to get company from ad
                var data = GetCompanyNameFromAD(userData.Email);
                userData.CompanyName = data.CompanyName;
                userData.UserName = data.UserName;
                // }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return userData;
        }
        /// <summary>
        /// InternetAccess-It is used to get the cost center from sql db.
        /// </summary>
        /// <returns></returns>
        public List<int> GetCostCenterDetails(string searchText)
        {
            List<int> costCenters = new List<int>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetCostCenters", con);
                cmd.Parameters.Add(new SqlParameter("@CostCenter", searchText));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        costCenters.Add(Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]));
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }

            return costCenters;
        }
        /// <summary>
        /// InternetAccess-It is used for viewing the Internet Access form.
        /// </summary>
        /// <returns></returns>

        public List<ApprovalMatrix> GetEmailIds()
        {
            try
            {
                return approverEmailIds;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        #region Search Form in Dashboard - Auto

        /// <summary>
        /// Dashboard-It is used to Search Form Name - Auto-Complete Code
        /// </summary>
        /// <returns></returns>
        public async Task<DataModel> SearchForms(string Prefix)
        {
            //var uniqueFormList = new List<FormData>();
            var newForms = new List<FormData>();
            var ObjList = new List<FormData>();
            var data = new DataModel() { NewlyAddedForms = new List<FormData>() };
            try
            {
                DashboardModel dashboardModel = new DashboardModel();
                DataModel dataModel = new DataModel();
                List<FormData> formDataList = new List<FormData>();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetFormParent", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        FormData item = new FormData();
                        item.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        item.FormName = Convert.ToString(ds.Tables[0].Rows[i]["FormName"]);
                        item.ReleaseDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["ReleaseDate"]);
                        item.UniqueFormName = Convert.ToString(ds.Tables[0].Rows[i]["UniqueFormName"]);
                        item.Department = Convert.ToString(ds.Tables[0].Rows[i]["Department"]);
                        item.ControllerName = Convert.ToString(ds.Tables[0].Rows[i]["ControllerName"]);
                        item.Message = Convert.ToString(ds.Tables[0].Rows[i]["Message"]);
                        formDataList.Add(item);
                    }
                }
                dataModel.Forms = formDataList;
                dashboardModel.Data = dataModel;

                var modelResult = dashboardModel;
                var newForms2 = modelResult.Data.Forms.ToList();
                newForms = newForms2;
                //uniqueFormList = modelResult.Data.Forms;


                for (int i = 0; i < newForms.Count; i++)
                {
                    // newForms[i].FormCount = freqForms.Count(x => x.FormParent.Id == newForms[i].Id);
                    data.NewlyAddedForms.Add(newForms[i]);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return data;
        }

        /// <summary>
        /// Dashboard-It is used to Search Form List Data For  Auto-Complete Code
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetFormDet(string FormName)
        {

            var result = new List<FormData>();
            try
            {
                DashboardModel dashboardModel = new DashboardModel();
                DataModel dataModel = new DataModel();
                List<FormData> formDataList = new List<FormData>();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetFormParent", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        if (Convert.ToString(ds.Tables[0].Rows[i]["FormName"]) == FormName)
                        {
                            FormData item = new FormData();
                            item.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                            item.FormName = Convert.ToString(ds.Tables[0].Rows[i]["FormName"]);
                            item.ReleaseDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["ReleaseDate"]);
                            item.UniqueFormName = Convert.ToString(ds.Tables[0].Rows[i]["UniqueFormName"]);
                            item.Department = Convert.ToString(ds.Tables[0].Rows[i]["Department"]);
                            item.ControllerName = Convert.ToString(ds.Tables[0].Rows[i]["ControllerName"]);
                            item.Message = Convert.ToString(ds.Tables[0].Rows[i]["Message"]);
                            formDataList.Add(item);
                        }
                    }
                }
                dataModel.Forms = formDataList;
                dashboardModel.Data = dataModel;

                var modelResult = dashboardModel;
                result = modelResult.Data.Forms;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }


        #endregion


        //Commented Section
        //public int CoordinatesFromPdf(string PdfGuid)
        //{

        //    try
        //    {
        //        //Uses the Syncfusion.EJ2.PdfViewer assembly
        //        PdfRenderer pdfExtractText = new PdfRenderer();
        //        pdfExtractText.Load("E:\\Test.pdf");
        //        //Returns the bounds of the text
        //        List<Syncfusion.EJ2.PdfViewer.TextData> textCollection = new List<Syncfusion.EJ2.PdfViewer.TextData>();
        //        //Extracts the text from the first page of the PDF document along with its bounds
        //        string text = pdfExtractText.ExtractText(0, out textCollection);
        //        //System.IO.File.WriteAllText("E:\\Test.pdf", text);

        //        return 1;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return 0;
        //    }
        //}


        //public async Task<bool> AddUserToSharepointList(string userName)
        //{
        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
        //        string urlOld = conString + "_api/contextinfo";
        //        HttpClient client1 = new HttpClient(handler);
        //        client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose;charset=utf-8");
        //        var contentOld = new StringContent("");
        //        contentOld.Headers.ContentLength = 0;
        //        HttpResponseMessage responseOld = await client1.PostAsync(urlOld, contentOld);
        //        var anonymous = new
        //        {
        //            d = new { GetContextWebInformation = new { FormDigestValue = "" } }
        //        };
        //        var resultOld = await responseOld.Content.ReadAsStringAsync();
        //        var formDigestTwo = JsonConvert.DeserializeAnonymousType(resultOld, anonymous);
        //        var fd = formDigestTwo.d.GetContextWebInformation.FormDigestValue;


        //        HttpWebRequest endpointRequest = (HttpWebRequest)HttpWebRequest.Create(conString + "_api/contextinfo");
        //        endpointRequest.Method = "POST";
        //        endpointRequest.Accept = "application/json;odata=verbose";
        //        endpointRequest.ContentLength = 0;
        //        endpointRequest.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
        //        HttpWebResponse endpointResponse = (HttpWebResponse)endpointRequest.GetResponse();
        //        var temp = endpointResponse.GetResponseHeader("FormDigestValue");

        //        ClientContext clientContext = new ClientContext(conString);
        //        clientContext.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
        //        FormDigestInfo formDigest = clientContext.GetFormDigestDirect();

        //        // X-RequestDigest header value
        //        string headerValue = formDigest.DigestValue;



        //        handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
        //        string urlNew = conString + "_api/web/sitegroups(6)/users";
        //        HttpClient client = new HttpClient(handler);
        //        //client.DefaultRequestHeaders

        //        var jsonObject = new
        //        {
        //            __metadata = new { type = "SP.User" },
        //            LoginName = "i:0#.w|apac\\cpu"
        //        };
        //        string jsonData = JsonConvert.SerializeObject(jsonObject);
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose;charset=utf-8");
        //        var content = new StringContent(jsonData, Encoding.UTF8);
        //        MediaTypeHeaderValue sharePointJsonMediaType = null;
        //        MediaTypeHeaderValue.TryParse("application/json;odata=verbose;charset=utf-8", out sharePointJsonMediaType);
        //        content.Headers.ContentType = sharePointJsonMediaType;
        //        client.DefaultRequestHeaders.Add("X-RequestDigest", fd);
        //        HttpResponseMessage response = await client.PostAsync(urlNew, content);
        //        var result = await response.Content.ReadAsStringAsync();
        //        //arrayData = JsonConvert.DeserializeAnonymousType(result, arrayData);
        //        //var returnableValue = arrayData != null && arrayData.Count() > 0 ? arrayData.FirstOrDefault() : null;
        //        //return returnableValue;
        //        return true;

        //        //using (ClientContext clientContext = new ClientContext(new Uri(conString)))
        //        //{
        //        //    clientContext.Credentials = new NetworkCredential("sysaipl001", "Volkswagen@98900");
        //        //    // Get group object using group name
        //        //    Group oGroup = clientContext.Web.SiteGroups.GetById(6);

        //        //    // Get user using Logon name
        //        //    User oUser = clientContext.Web.EnsureUser(userName);

        //        //    // Add user to the group
        //        //    oGroup.Users.AddUser(oUser);

        //        //    clientContext.ExecuteQuery();
        //        //    return true;
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}


        //public int SaveApproval(int formId, int rowId, List<ApprovalMatrix> approvers, int userId)
        //{
        //    ClientContext _context = new ClientContext(new Uri(conString));
        //    _context.Credentials = new NetworkCredential(spUsername, spPass);

        //    Web web = _context.Web;
        //    try
        //    {
        //        List applist = _context.Web.Lists.GetByTitle("ApprovalMaster");
        //        for (int i = 0; i < approvers.Count; i++)
        //        {
        //            ListItemCreationInformation itemCreate = new ListItemCreationInformation();
        //            ListItem newRow = applist.AddItem(itemCreate);
        //            newRow["FormId"] = formId;
        //            newRow["RowId"] = rowId;
        //            newRow["ApproverId"] = approvers[i].UserId;
        //            newRow["ApproverStatus"] = "Pending";
        //            newRow["NextApproverId"] = (i == approvers.Count - 1) ? 0 : approvers[i + 1].UserId;
        //            newRow["InitiatorId"] = userId;
        //            newRow["IsActive"] = (i == 0) ? 1 : 0;
        //            newRow.Update();
        //            _context.ExecuteQuery();
        //        }

        //        return 1;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return 0;
        //    }
        //}


        //public int View(int RowId)
        //{
        //    try
        //    {
        //        ClientContext _context = new ClientContext(conString);
        //        _context.Credentials = new NetworkCredential(spUsername, spPass);
        //        List oList = _context.Web.Lists.GetByTitle("OrderItems");
        //        List list = _context.Web.Lists.GetByTitle("OrderDetails");

        //        var camlQuery = new CamlQuery();
        //        var camQuery = new CamlQuery();
        //        var camlQueryString = "" +
        //            "<View>" +
        //            "<ViewFields>" +
        //                "<FieldRef Name='SupplierName'/>" +
        //                "<FieldRef Name='TechAcceptance'/>" +
        //                "<FieldRef Name='OrderDetailsID'/>" +
        //                "<FieldRef Name='OfferPrice'/>" +
        //            "</ViewFields>" +
        //            "<Query><Where>" +
        //                "<Eq><FieldRef Name='OrderDetailsID' LookupId='TRUE' /><Value Type='Lookup'>" + RowId + "</Value></Eq>" +
        //            "</Where></Query>" +
        //            "</View>";
        //        var queryString = "" +
        //            "<View> " +
        //            "<ViewFields>" +
        //                "<FieldRef Name='ConcernSection'/>" +
        //                "<FieldRef Name='TechDisqualify'/>" +
        //                "<FieldRef Name='SuggestOrder'/>" +
        //                "<FieldRef Name='Currency'/>" +
        //                "<FieldRef Name='Department'/>" +
        //                "<FieldRef Name='ShopCartNumber'/>" +
        //                "<FieldRef Name='Date'/>" +
        //                "<FieldRef Name='Budget'/>" +
        //                "<FieldRef Name='Description'/>" +
        //                "<FieldRef Name='Initiator'/>" +
        //                "<FieldRef Name='InitiatorDate'/>" +
        //                "<FieldRef Name='Created'/>" +
        //            "</ViewFields>" +
        //            "<Query><Where>" +
        //                "<Eq><FieldRef Name='ID' /><Value Type='Integer'>" + RowId + "</Value></Eq>" +
        //            "</Where></Query>" +
        //            "</View>";
        //        camlQuery.ViewXml = camlQueryString;
        //        camQuery.ViewXml = queryString;

        //        ListItemCollection colListItem = oList.GetItems(camlQuery);
        //        ListItemCollection colItem = list.GetItems(camQuery);

        //        _context.Load(colListItem);
        //        _context.Load(colItem);

        //        _context.ExecuteQuery();

        //        var col1 = colListItem.Cast<ListItem>();
        //        var col2 = colItem.Cast<ListItem>();
        //        var data = col1.Concat(col2);

        //        var temp1 = data.Select(x => x.FieldValues);
        //        var temp2 = data.SelectMany(x => x.FieldValues);

        //        return 1;
        //    }
        //    catch (Exception e)
        //    {
        //        return 0;
        //    }
        //}


        //public List<FormData> GetUserFormsList(string formName)
        //{
        //    List<FormData> result = new List<FormData>();
        //    try
        //    {
        //        ClientContext _context = new ClientContext(conString);
        //        _context.Credentials = new NetworkCredential(spUsername, spPass);
        //        List list = _context.Web.Lists.GetByTitle("ApprovalMaster");

        //        var camlQuery = new CamlQuery();
        //        var camlQueryString = "" +
        //            "<View>" +
        //            "<Query>" +
        //            "<Where>" +
        //                "<And>" +
        //                   "<Eq>" +
        //                      "<FieldRef Name='SubmitterId' />" +
        //                      "<Value Type='Lookup'>" + user.UserId + "</Value>" +
        //                   "</Eq>" +
        //                   "<Eq>" +
        //                      "<FieldRef Name='UniqueFormName' />" +
        //                      "<Value Type='Lookup'>" + formName + "</Value>" +
        //                   "</Eq>" +
        //                "</And>" +
        //            "</Where>" +
        //            "<OrderBy><FieldRef Name='TimeStamp' Ascending='FALSE'/></OrderBy>" +
        //            "</Query>" +
        //            "<ViewFields>" +
        //                "<FieldRef Name='UniqueFormName'/>" +
        //                "<FieldRef Name='FormName'/>" +
        //                "<FieldRef Name='SubmitterId'/>" +
        //                "<FieldRef Name='ApproverStatus'/>" +
        //                "<FieldRef Name='Comment'/>" +
        //                "<FieldRef Name='Modified'/>" +
        //                "<FieldRef Name='Status'/>" +
        //                "<FieldRef Name='FormId'/>" +
        //                "<FieldRef Name='Created'/>" +
        //                "<FieldRef Name='RowId'/>" +
        //                "<FieldRef Name='Id'/>" +
        //                "<FieldRef Name='TimeStamp'/>" +
        //            "</ViewFields>" +
        //              "<ProjectedFields>" +
        //                "<Field Name='UniqueFormName' Type='Lookup' List='Forms' ShowField='UniqueFormName' />" +
        //                "<Field Name='FormName' Type='Lookup' List='Forms' ShowField='FormName' />" +
        //                "<Field Name='SubmitterId' Type='Lookup' List='Forms' ShowField='SubmitterId' />" +
        //                "<Field Name='Status' Type='Lookup' List='Forms' ShowField='Status' />" +
        //                "<Field Name='TimeStamp' Type='Lookup' List='Forms' ShowField='TimeStamp' />" +
        //              "</ProjectedFields>" +
        //            "<Joins> " +
        //              "<Join Type='INNER' ListAlias='Forms'>" +
        //                "<Eq>" +
        //                 " <FieldRef Name='FormId' RefType='Id' />" +
        //                 " <FieldRef List='Forms' Name='ID' />" +
        //                "</Eq>" +
        //              "</Join>" +
        //            "</Joins>" +
        //            "</View>";
        //        camlQuery.ViewXml = camlQueryString;

        //        ListItemCollection colListItem = list.GetItems(camlQuery);

        //        _context.Load(colListItem);

        //        _context.ExecuteQuery();
        //        var formList = colListItem.ToList();

        //        var formDataList = new List<FormData>();

        //        foreach (var item in formList)
        //        {
        //            var formData = new FormData();
        //            formData.Status = item.FieldValues["Status"].ToString();
        //            formData.ApproverStatus = item.FieldValues["ApproverStatus"].ToString();
        //            formData.Comment = item.FieldValues["Comment"].ToString();
        //            formData.FormName = item.FieldValues["FormName"].ToString();
        //            formData.TimeStamp = Convert.ToDateTime(item.FieldValues["TimeStamp"]);
        //            formData.UniqueFormName = item.FieldValues["UniqueFormName"].ToString();
        //            formDataList.Add(formData);
        //        }

        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}


        //public async Task<dynamic> ViewIDCFData(int rowId, int formId)
        //{
        //    dynamic IDCFDataList = new ExpandoObject();//constructor called, always expandoobject for dynamic dataType

        //    try
        //    {
        //        GlobalClass gc = new GlobalClass();
        //        var user = gc.GetCurrentUser();

        //        var handler = new HttpClientHandler();
        //        //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

        //        var response = await client.GetAsync("_api/web/lists/GetByTitle('IDCardForm')/items?$select=ID,EmployeeType,Surname,EmployeeName,"
        //            + "DateofJoining,EmployeeNumber,CostCentre,Department,DateofIssue,IDCardNumber," +
        //            "Profile" +
        //            "&$filter=(FormID eq '" + formId + "')");

        //        var responseText = await response.Content.ReadAsStringAsync();


        //        if (!string.IsNullOrEmpty(responseText))
        //        {
        //            var IDCFResult = JsonConvert.DeserializeObject<IDCFModel>(responseText);
        //            IDCFDataList.one = IDCFResult.idcfflist.idcfData;
        //        }
        //        return IDCFDataList;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return 0;
        //    }
        //}

        //public async Task<int> SaveIDCF(System.Web.Mvc.FormCollection form, UserData user)
        //{

        //    ClientContext _context = new ClientContext(new Uri(conString));
        //    _context.Credentials = new NetworkCredential(spUsername, spPass);

        //    Web web = _context.Web;
        //    var listName = GlobalClass.ListNames.ContainsKey("IDCF") ? GlobalClass.ListNames["IDCF"] : "";
        //    if (listName == "")
        //        return 0;

        //    try
        //    {


        //        //var approverIdList = await GetApprovalIDCF();
        //        //if (approverIdList == null || approverIdList.Count == 0)
        //        //    return 0;

        //        List FormsList = web.Lists.GetByTitle("Forms");
        //        ListItemCreationInformation itemCreated = new ListItemCreationInformation();
        //        ListItem item = FormsList.AddItem(itemCreated);
        //        item["FormName"] = "ID Card Form";
        //        item["UniqueFormName"] = "IDCF";
        //        item["FormParentId"] = 5;
        //        item["ListName"] = listName;
        //        item["SubmitterId"] = user.UserId;
        //        item["Status"] = "Submitted";
        //        item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
        //        item["Department"] = user.Department;
        //        item.Update();
        //        _context.Load(item);
        //        _context.ExecuteQuery();

        //        int formId = item.Id;

        //        List IDcardDetailsList = web.Lists.GetByTitle(listName);
        //        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
        //        ListItem newItem = IDcardDetailsList.AddItem(itemCreateInfo);
        //        newItem["EmployeeType"] = form["drpEmployeeType"];
        //        newItem["Surname"] = form["txtSurname"];
        //        newItem["EmployeeName"] = form["txtEmployeeName"];
        //        newItem["DateofJoining"] = form["txtDateofJoining"];
        //        newItem["EmployeeNumber"] = form["txtEmployeeNumber"];
        //        newItem["CostCentre"] = form["drpCostCentre"];
        //        newItem["Department"] = form["drpDepartment"];
        //        newItem["DateofIssue"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
        //        newItem["Department"] = form["drpDepartment"];
        //        newItem["IDCardNumber"] = form["txtIDCardNumber"];
        //        newItem["Profile"] = form["LocationId"];

        //        //for (int i = 0; i < approverIdList.Count; i++)
        //        //{
        //        //    newItem["Approver" + (i + 1)] = approverIdList[i].UserId;
        //        //}

        //        //newItem["Approver1"] = approverIdList[0].UserId;
        //        newItem["Approver1"] = 1;
        //        newItem["FormID"] = formId;
        //        newItem.Update();
        //        _context.Load(newItem);
        //        _context.ExecuteQuery();
        //    }

        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return 0;
        //    }

        //    return 1;
        //}

        //public async Task<dynamic> GetDepartment()
        //{
        //    IDCFResults IDCFData = new IDCFResults();
        //    dynamic result = IDCFData;
        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
        //        var response = await client.GetAsync("_api/web/lists/GetByTitle('DepartmentDetails')/items?$select=ID,Department");
        //        var responseText = await response.Content.ReadAsStringAsync();

        //        if (responseText.Contains("401 UNAUTHORIZED"))
        //            GlobalClass.IsUserLoggedOut = true;

        //        if (!string.IsNullOrEmpty(responseText))
        //        {
        //            var locResult = JsonConvert.DeserializeObject<IDCFModel>(responseText);
        //            IDCFData = locResult.idcfflist;
        //        }
        //        result = IDCFData.idcfData;
        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        return result;
        //    }
        //}

        //public async Task<dynamic> GetCostCenter()
        //{
        //    IDCFResults IDCFData = new IDCFResults();
        //    dynamic result = IDCFData;
        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
        //        var response = await client.GetAsync("_api/web/lists/GetByTitle('CostCenterDetails')/items?$select=ID,CostCenter,Location");
        //        var responseText = await response.Content.ReadAsStringAsync();

        //        if (responseText.Contains("401 UNAUTHORIZED"))
        //            GlobalClass.IsUserLoggedOut = true;

        //        if (!string.IsNullOrEmpty(responseText))
        //        {
        //            var locResult = JsonConvert.DeserializeObject<IDCFModel>(responseText);
        //            IDCFData = locResult.idcfflist;
        //        }
        //        result = IDCFData.idcfData;
        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        return result;
        //    }
        //}

        //public async Task<List<ApprovalMatrix>> GetApprovalIDCF()
        //{
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();
        //        List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("sp_IDCFApproval", con);
        //        //cmd.Parameters.Add(new SqlParameter("@emailId", user.Email));
        //        //cmd.Parameters.Add(new SqlParameter("@zone", loc));
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                ApprovalMatrix app = new ApprovalMatrix();
        //                app.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
        //                app.FName = Convert.ToString(ds.Tables[0].Rows[i]["FirstName"]);
        //                app.LName = Convert.ToString(ds.Tables[0].Rows[i]["LastName"]);
        //                app.EmailId = Convert.ToString(ds.Tables[0].Rows[i]["EmailId"]);
        //                appList.Add(app);
        //            }
        //        }

        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        var emailString = "";
        //        var count = appList.Count;
        //        for (int i = 0; i < count; i++)
        //        {
        //            var email = appList[i];
        //            emailString += $"EMail eq '{email.EmailId}' {(i != count - 1 ? "or " : "")}";
        //        }
        //        var response = await client.GetAsync($"_api/web/SiteUserInfoList/items?$select=Id,Title,EMail&$filter=({emailString})");
        //        var responseText = await response.Content.ReadAsStringAsync();

        //        XmlDocument doc = new XmlDocument();
        //        doc.LoadXml(responseText.ToString());
        //        var title = doc.GetElementsByTagName("d:Title");
        //        var id = doc.GetElementsByTagName("d:Id");
        //        var emails = doc.GetElementsByTagName("d:EMail");

        //        for (int i = 0; i < id.Count; i++)
        //        {
        //            var currentEmail = emails[i].InnerXml;
        //            var currentId = Convert.ToInt32(id[i].InnerXml);
        //            var matchingUser = appList.Where(x => x.EmailId == currentEmail).FirstOrDefault();
        //            if (matchingUser != null)
        //                matchingUser.UserId = currentId;
        //        }

        //        return appList;
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }

        //}





        //public async Task<DataModel> SearchForms(string Prefix)
        //{
        //    //var uniqueFormList = new List<FormData>();
        //    var newForms = new List<FormData>();
        //    // var freqForms = new List<FormData>();
        //    var ObjList = new List<FormData>();
        //    //var freqFormList = new FormData();
        //    var data = new DataModel() { NewlyAddedForms = new List<FormData>() };
        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

        //        //new forms
        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
        //        var response = await client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id,UniqueFormName,FormName,ReleaseDate&$orderby=ReleaseDate desc");
        //        var responseText = await response.Content.ReadAsStringAsync();
        //        if (!string.IsNullOrEmpty(responseText))
        //        {
        //            var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
        //            var newForms2 = modelResult.Data.Forms.Take(12).ToList();
        //            newForms = newForms2;
        //            //uniqueFormList = modelResult.Data.Forms;
        //        }

        //        for (int i = 0; i < newForms.Count; i++)
        //        {
        //            // newForms[i].FormCount = freqForms.Count(x => x.FormParent.Id == newForms[i].Id);
        //            data.NewlyAddedForms.Add(newForms[i]);
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //    }
        //    return data;
        //}
        //public List<ApprovalMatrix> GetEmailIds()
        //{
        //    try
        //    {
        //        return approverEmailIds;
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}

        #region Newly Added & Freq. Used Search Code
        /// <summary>
        /// Dashboard-It is used to Search Form Name on Footer section of dashboard Newly added & Freq. Used form.
        /// </summary>
        /// <returns></returns>

        public async Task<DataModel> GetFormsBySearch(string searchText = "")
        {

            var uniqueFormList = new List<FormData>();
            var newForms = new List<FormData>();
            var freqForms = new List<FormData>();
            var data = new DataModel() { FreqUsedForms = new List<FormData>(), NewlyAddedForms = new List<FormData>(), DepartmentList = new List<(string, int)>() };
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //new forms
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id,ControllerName,UniqueFormName,Department,FormOwner,FormName,ReleaseDate&$orderby=ReleaseDate desc&$filter=IsComplete eq '1'");
                var responseText = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    newForms = modelResult.Data.Forms.ToList();
                    uniqueFormList = modelResult.Data.Forms;
                }

                //frequent forms
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,Status,FormParentId/" +
                   "Id,Created,Modified,Department,UniqueFormName,FormName&$filter=SubmitterUserName eq '" + user.UserName + "'&$expand=FormParentId&$top=1000");
                var responseText2 = await response2.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText2))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText2);
                    freqForms = modelResult.Data.Forms;

                    for (int i = 0; i < uniqueFormList.Count; i++)
                    {
                        uniqueFormList[i].FormCount = freqForms.Count(x => x.FormParent.Id == uniqueFormList[i].Id);
                    }
                }
                //var joinedList = (from freq in freqForms
                //                  join detail in uniqueFormList on freq.FormParent.Id equals detail.Id
                //                  select new { freq.FormName, detail.UniqueFormId, detail.FormCount, detail.UniqueFormName }).ToList();

                var freqFormList = uniqueFormList.Select(item => new FormData()
                {
                    FormName = item.FormName,
                    UniqueFormId = item.UniqueFormId,
                    FormCount = item.FormCount,
                    UniqueFormName = item.UniqueFormName,
                    Department = item.Department,
                    FormOwner = item.FormOwner,
                    ControllerName = item.ControllerName
                }).OrderByDescending(x => x.FormCount).ToList();

                if (!string.IsNullOrEmpty(searchText))
                    data.FreqUsedForms = freqFormList.Where(x => x.FormName.ToLower().Contains(searchText.ToLower()) || x.UniqueFormName.ToLower().Contains(searchText.ToLower())).ToList();
                else
                    data.FreqUsedForms = freqFormList;

                for (int i = 0; i < newForms.Count; i++)
                {
                    newForms[i].FormCount = freqForms.Count(x => x.FormParent.Id == newForms[i].Id);
                }

                if (!string.IsNullOrEmpty(searchText))
                    data.NewlyAddedForms = newForms.Where(x => x.FormName.ToLower().Contains(searchText.ToLower()) || x.UniqueFormName.ToLower().Contains(searchText.ToLower())).ToList();
                else
                    data.NewlyAddedForms = newForms;

                //var deptGroup = freqForms.GroupBy(x => x.Department);
                //foreach (var item in deptGroup)
                //{
                //    data.DepartmentList.Add((item.Key, item.Count()));
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return data;
        }


        #endregion

        #region ID Card Form

        /// <summary>
        /// ID Card-It is used to get the Department Dropdown data.
        /// </summary>
        /// <returns></ret
        public async Task<dynamic> GetDepartment()
        {
            IDCFResults IDCFData = new IDCFResults();
            dynamic result = IDCFData;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('DepartmentDetails')/items?$select=ID,Department");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<IDCFModel>(responseText);
                    IDCFData = locResult.idcfflist;
                }
                result = IDCFData.idcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// ID Card-It is used to get the Cost Center Dropdown data.
        /// </summary>
        /// <returns></ret
        public async Task<dynamic> GetCostCenter()
        {
            IDCFResults IDCFData = new IDCFResults();
            dynamic result = IDCFData;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('CostCenterDetails')/items?$select=ID,CostCenter,Location");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<IDCFModel>(responseText);
                    IDCFData = locResult.idcfflist;
                }
                result = IDCFData.idcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// ID Card-It is used to get the Location Master Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetLocationName()
        {
            RIDCFResults RIDCFData = new RIDCFResults();
            dynamic result = RIDCFData;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('Location')/items?$select=ID,LocationName");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<RIDCFModel>(responseText);
                    RIDCFData = locResult.ridcfflist;
                }
                result = RIDCFData.ridcfData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }


        #endregion

        #region Report Section

        /// <summary>
        /// Dashboard-It is used to Filter Report Section.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetAllFormsListForReport(string uniqueFormName = "", string fromDate = "", string toDate = "", string status = "", string location = "")
        {
            var result = new List<FormData>();
            var resultNew = new List<FormData>();
            DataModel FormsDM = new DataModel();
            DataModel FormsDM1 = new DataModel();
            List<FormData> approvalMasterList = new List<FormData>();
            List<FormData> modelResult = new List<FormData>();
            List<FormData> formRelationList = new List<FormData>();
            List<FormData> formRelationListAppList = new List<FormData>();
            List<FormData> formRelationMainList = new List<FormData>();
            if (status == "Pending")
            {
                status = "Initiated";
            }

            try
            {
                if (uniqueFormName == "-1")
                {
                    uniqueFormName = "";
                }

                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var url = "";
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                if (string.IsNullOrEmpty(uniqueFormName) && string.IsNullOrEmpty(status) && string.IsNullOrEmpty(location))
                {
                    #region Comment
                    //url = "_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,ControllerName,Status,FormParentId/Id,Created,Modified,Department,Author/Title,BusinessNeed,UniqueFormName,FormName" +
                    //"&$filter=SubmitterUserName eq '" + user.UserName + "' " +
                    //"&$expand=FormParentId,Author&$top=5000";

                    //var responseApprovalMasterUrl = "_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,IsCompleted,BusinessNeed,Level,Logic,RunWorkflow,Department,ApproverStatus,RowId,Comment,NextApproverId,Modified,Author/Title,FormId/FormName," +
                    //     "FormId/Id,FormId/Created,FormId/DataRowId,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/SubmitterId,FormID/Status" +
                    //   "&$filter=ApproverUserName eq '" + user.UserName + "' " + "and " + "(" +
                    //    (" ApproverStatus eq 'Approved'" +
                    //    " or ApproverStatus eq 'Enquired'" +
                    //    " or (ApproverStatus eq 'Pending' and IsActive eq 1)" +
                    //    " or ApproverStatus eq 'Rejected'" +
                    //    " or IsCompleted eq '1'") + ")" +
                    //   "&$expand=FormId,Author&$top=5000";

                    //var responseApprovalMaster = await client.GetAsync(responseApprovalMasterUrl);

                    //var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();



                    //if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                    //{
                    //    var modelApprovalResult = JsonConvert.DeserializeObject<DashboardModel>(responseTextApprovalMaster);
                    //    approvalMasterList = modelApprovalResult.Data.Forms;
                    //}
                    #endregion

                    List<DataModel> item = new List<DataModel>();
                    DataModel model = new DataModel();

                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_GetFormsDataByUserName", con);
                    cmd1.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd1.Parameters.Add(new SqlParameter("@ISFilter", "0"));
                    cmd1.Parameters.Add(new SqlParameter("@FormName", DBNull.Value));
                    cmd1.Parameters.Add(new SqlParameter("@Location", DBNull.Value));
                    cmd1.Parameters.Add(new SqlParameter("@Status", DBNull.Value));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    List<FormData> FormDataList = new List<FormData>();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            //int emp;


                            FormData Formsitem2 = new FormData();

                            Formsitem2.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            Formsitem2.UniqueFormId = Convert.ToInt32(dt1.Rows[i]["Id"]);

                            Formsitem2.DataRowId = dt1.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt1.Rows[i]["DataRowId"]);
                            Formsitem2.ControllerName = dt1.Rows[i]["ControllerName"] == DBNull.Value ? "" : Convert.ToString(dt1.Rows[i]["ControllerName"]);
                            Formsitem2.Status = Convert.ToString(dt1.Rows[i]["Status"]);
                            FormParentModel FPMitem3 = new FormParentModel();
                            FPMitem3.Id = dt1.Rows[i]["FormParentId"] == DBNull.Value ? 0 : Convert.ToInt32(dt1.Rows[i]["FormParentId"]);
                            Formsitem2.FormParent = FPMitem3;
                            if (dt1.Rows[i]["Created"] != DBNull.Value)
                                Formsitem2.FormCreatedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            if (dt1.Rows[i]["Modified"] != DBNull.Value)
                                Formsitem2.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            Formsitem2.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            Author Aitem3 = new Author();
                            Aitem3.Submitter = Convert.ToString(dt1.Rows[i]["SubmitterUserName"]);
                            Formsitem2.Author = Aitem3;
                            Formsitem2.BusinessNeed = Convert.ToString(dt1.Rows[i]["BusinessNeed"]);
                            Formsitem2.UniqueFormName = Convert.ToString(dt1.Rows[i]["UniqueFormName"]);
                            Formsitem2.FormName = Convert.ToString(dt1.Rows[i]["FormName"]);
                            FormDataList.Add(Formsitem2);
                        }
                    }
                    FormsDM.Forms = FormDataList;
                    modelResult = FormsDM.Forms;

                    #region approvalMasterList
                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataTable dt = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_GetApproversDataByUserName", con);
                    cmd.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd.Parameters.Add(new SqlParameter("@ISFilter", "0"));
                    cmd.Parameters.Add(new SqlParameter("@FormName", DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Location", DBNull.Value));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(dt);
                    con.Close();
                    List<FormData> FormDataitem = new List<FormData>();
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            //int emp;


                            FormData item2 = new FormData();

                            item2.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                            item2.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                            item2.AuthorityToEdit = dt.Rows[i]["AuthorityToEdit"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                            item2.BusinessNeed = Convert.ToString(dt.Rows[i]["BusinessNeed"]);
                            item2.Level = dt.Rows[i]["Level"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Level"]);
                            item2.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                            item2.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                            item2.Department = Convert.ToString(dt.Rows[i]["Department"]);
                            item2.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                            item2.RowId = dt.Rows[i]["RowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["RowId"]);
                            item2.Comment = Convert.ToString(dt.Rows[i]["Comment"]);
                            item2.NextApproverId = dt.Rows[i]["NextApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["NextApproverId"]);
                            if (dt.Rows[i]["Modified"] != DBNull.Value)
                                item2.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Modified"]);
                            Author item3 = new Author();
                            item3.Submitter = Convert.ToString(dt.Rows[i]["SubmitterUserName"]);
                            item2.Author = item3;
                            item2.Id = dt.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Id"]);
                            //item2.Author = Convert.ToString(dt.Rows[i]["SubmitterUserName"]);
                            FormLookup item1 = new FormLookup();
                            item1.Id = dt.Rows[i]["Id1"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Id1"]);
                            if (dt.Rows[i]["Created"] != DBNull.Value)
                                item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            item1.DataRowId = dt.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["DataRowId"]);
                            item1.ControllerName = Convert.ToString(dt.Rows[i]["ControllerName"]);
                            item1.ListName = Convert.ToString(dt.Rows[i]["ListName"]);
                            item1.UniqueFormName = Convert.ToString(dt.Rows[i]["UniqueFormName"]);
                            item1.FormName = Convert.ToString(dt.Rows[i]["FormName"]);
                            item1.FormStatus = Convert.ToString(dt.Rows[i]["Status"]);
                            item1.SubmitterId = dt.Rows[i]["SubmitterId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["SubmitterId"]);
                            item2.FormRelation = item1;

                            FormDataitem.Add(item2);


                        }
                    }
                    FormsDM1.Forms = FormDataitem;
                    approvalMasterList = FormsDM1.Forms;
                    #endregion



                    formRelationList.AddRange(approvalMasterList.Select(x => new FormData()
                    {
                        Id = x.Id,
                        UniqueFormId = x.FormRelation.Id,
                        //Status = x.FormRelation.FormStatus,
                        Status = x.ApproverStatus,
                        FormName = x.FormRelation.FormName,
                        UniqueFormName = x.FormRelation.UniqueFormName,
                        FormCreatedDate = x.FormRelation.CreatedDate,
                        Author = new Author() { Submitter = x.Author.Submitter },
                        BusinessNeed = x.BusinessNeed,
                        DataRowId = x.FormRelation.DataRowId,
                        ControllerName = x.FormRelation.ControllerName,
                        Comment = x.Comment

                    }).ToList());



                    //formRelationMainList = formRelationList.GroupBy(x => new { x.UniqueFormId, x.FormName, x.UniqueFormName, x.BusinessNeed, x.DataRowId, x.FormCreatedDate }).Select(group => group.First()).ToList();


                    formRelationMainList = formRelationList.OrderByDescending(x => x.Id).GroupBy(x => x.UniqueFormId).Select(x => x.First()).ToList();
                }
                else if (!string.IsNullOrEmpty(uniqueFormName) || !string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(location))
                {
                    #region Comment
                    //url = "_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,ControllerName,Status,FormParentId/Id,Created,Modified,Department,Author/Title,BusinessNeed,UniqueFormName,FormName" +
                    // "&$filter=SubmitterUserName eq '" + user.UserName + "' " +
                    // (!string.IsNullOrEmpty(uniqueFormName) ? (" and UniqueFormName eq '" + uniqueFormName + "'") : "") +
                    // (!string.IsNullOrEmpty(location) ? (" and Location eq '" + location + "'") : "") +
                    // (!string.IsNullOrEmpty(status) ? (" and Status eq '" + status + "'") : "") +
                    // "&$expand=FormParentId,Author&$top=5000";

                    //var responseApprovalMasterUrl = "_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,BusinessNeed,Level,Logic,RunWorkflow,Department,ApproverStatus,RowId,Comment,NextApproverId,Modified,Author/Title,FormId/FormName," +
                    // "FormId/Id,FormId/Created,FormId/DataRowId,FormId/Location,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/SubmitterId,FormID/Status" +
                    //  "&$filter=ApproverUserName eq '" + user.UserName + "' " +
                    //(!string.IsNullOrEmpty(uniqueFormName) ? (" and FormId/UniqueFormName eq '" + uniqueFormName + "'") : "") +
                    //(!string.IsNullOrEmpty(location) ? (" and FormId/Location eq '" + location + "'") : "") + "and " + "(" +
                    //    (" ApproverStatus eq 'Approved'" +
                    //    " or ApproverStatus eq 'Enquired'" +
                    //    " or (ApproverStatus eq 'Pending' and IsActive eq 1)" +
                    //    " or ApproverStatus eq 'Rejected'" +
                    //    " or IsCompleted eq '1'") + ")" +
                    //   "&$expand=FormId,Author&$top=5000";


                    //var responseApprovalMaster = await client.GetAsync(responseApprovalMasterUrl);

                    //var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();

                    //if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                    //{
                    //    var modelApprovalResult = JsonConvert.DeserializeObject<DashboardModel>(responseTextApprovalMaster);
                    //    approvalMasterList = modelApprovalResult.Data.Forms;

                    //}
                    #endregion

                    object objuniqueFormName = DBNull.Value;
                    object objlocation = DBNull.Value;
                    object objstatus = DBNull.Value;
                    if (uniqueFormName != null || uniqueFormName != "")
                        objuniqueFormName = uniqueFormName;
                    if (location != null || location != "")
                        objlocation = location;
                    if (status != null || status != "")
                        objstatus = status;

                    List<DataModel> item = new List<DataModel>();
                    DataModel model = new DataModel();

                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_GetFormsDataByUserName", con);
                    cmd1.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd1.Parameters.Add(new SqlParameter("@ISFilter", "1"));
                    cmd1.Parameters.Add(new SqlParameter("@FormName", objuniqueFormName));
                    cmd1.Parameters.Add(new SqlParameter("@Location", objlocation));
                    cmd1.Parameters.Add(new SqlParameter("@Status", objstatus));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    List<FormData> FormDataList = new List<FormData>();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            //int emp;


                            FormData Formsitem2 = new FormData();

                            Formsitem2.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            Formsitem2.UniqueFormId = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            Formsitem2.DataRowId = dt1.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt1.Rows[i]["DataRowId"]);
                            Formsitem2.ControllerName = dt1.Rows[i]["ControllerName"] == DBNull.Value ? "" : Convert.ToString(dt1.Rows[i]["ControllerName"]);
                            Formsitem2.Status = Convert.ToString(dt1.Rows[i]["Status"]);
                            FormParentModel FPMitem3 = new FormParentModel();
                            FPMitem3.Id = dt1.Rows[i]["FormParentId"] == DBNull.Value ? 0 : Convert.ToInt32(dt1.Rows[i]["FormParentId"]);
                            Formsitem2.FormParent = FPMitem3;
                            if (dt1.Rows[i]["Created"] != DBNull.Value)
                                Formsitem2.FormCreatedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            if (dt1.Rows[i]["Modified"] != DBNull.Value)
                                Formsitem2.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            Formsitem2.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            Author Aitem3 = new Author();
                            Aitem3.Submitter = Convert.ToString(dt1.Rows[i]["SubmitterUserName"]);
                            Formsitem2.Author = Aitem3;
                            Formsitem2.BusinessNeed = Convert.ToString(dt1.Rows[i]["BusinessNeed"]);
                            Formsitem2.UniqueFormName = Convert.ToString(dt1.Rows[i]["UniqueFormName"]);
                            Formsitem2.FormName = Convert.ToString(dt1.Rows[i]["FormName"]);
                            FormDataList.Add(Formsitem2);
                        }
                    }
                    FormsDM.Forms = FormDataList;
                    modelResult = FormsDM.Forms;

                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataTable dt = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_GetApproversDataByUserName", con);
                    cmd.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd.Parameters.Add(new SqlParameter("@ISFilter", "1"));
                    cmd.Parameters.Add(new SqlParameter("@FormName", objuniqueFormName));
                    cmd.Parameters.Add(new SqlParameter("@Location", objlocation));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(dt);
                    con.Close();
                    List<FormData> FormDataList1 = new List<FormData>();
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            //int emp;


                            FormData item2 = new FormData();

                            item2.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                            item2.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                            item2.AuthorityToEdit = dt.Rows[i]["AuthorityToEdit"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                            item2.BusinessNeed = Convert.ToString(dt.Rows[i]["BusinessNeed"]);
                            item2.Level = dt.Rows[i]["Level"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Level"]);
                            item2.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                            item2.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                            item2.Department = Convert.ToString(dt.Rows[i]["Department"]);
                            item2.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                            item2.RowId = dt.Rows[i]["RowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["RowId"]);
                            item2.Comment = Convert.ToString(dt.Rows[i]["Comment"]);
                            item2.NextApproverId = dt.Rows[i]["NextApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["NextApproverId"]);
                            if (dt.Rows[i]["Modified"] != DBNull.Value)
                                item2.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Modified"]);
                            Author item3 = new Author();
                            item3.Submitter = Convert.ToString(dt.Rows[i]["SubmitterUserName"]);
                            item2.Author = item3;
                            item2.Id = dt.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Id"]);

                            FormLookup item1 = new FormLookup();
                            item1.Id = dt.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Id"]);
                            if (dt.Rows[i]["Created"] != DBNull.Value)
                                item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            item1.DataRowId = dt.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["DataRowId"]);
                            item1.ControllerName = Convert.ToString(dt.Rows[i]["ControllerName"]);
                            item1.ListName = Convert.ToString(dt.Rows[i]["ListName"]);
                            item1.UniqueFormName = Convert.ToString(dt.Rows[i]["UniqueFormName"]);
                            item1.FormName = Convert.ToString(dt.Rows[i]["FormName"]);
                            item1.FormStatus = Convert.ToString(dt.Rows[i]["Status"]);
                            item1.SubmitterId = dt.Rows[i]["SubmitterId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["SubmitterId"]);
                            item2.FormRelation = item1;

                            FormDataList1.Add(item2);

                        }
                    }
                    FormsDM1.Forms = FormDataList1;
                    approvalMasterList = FormsDM1.Forms;
                    if (!string.IsNullOrEmpty(status))
                    {
                        string newstatus = "";
                        if (status == "Initiated")
                        {
                            newstatus = "Pending";
                        }
                        else
                        {
                            newstatus = status;
                        }

                        formRelationListAppList = approvalMasterList.Where(x => x.ApproverStatus == newstatus).ToList();

                    }
                    else
                    {
                        formRelationListAppList = approvalMasterList;
                    }

                    if (formRelationListAppList != null)
                    {
                        formRelationList.AddRange(formRelationListAppList.Select(x => new FormData()
                        {
                            UniqueFormId = x.FormRelation.Id,
                            //Status = x.FormRelation.FormStatus,
                            Status = x.ApproverStatus,
                            FormName = x.FormRelation.FormName,
                            UniqueFormName = x.FormRelation.UniqueFormName,
                            FormCreatedDate = x.FormRelation.CreatedDate,
                            Author = new Author() { Submitter = x.Author.Submitter },
                            BusinessNeed = x.BusinessNeed,
                            DataRowId = x.FormRelation.DataRowId,
                            ControllerName = x.FormRelation.ControllerName,
                            Comment = x.Comment
                        }).ToList());

                        formRelationMainList = formRelationList.OrderByDescending(x => x.Id).GroupBy(x => x.UniqueFormId).Select(x => x.First()).ToList();
                        //formRelationMainList = formRelationList.GroupBy(x => new { x.UniqueFormId, x.Status, x.FormName, x.UniqueFormName, x.BusinessNeed, x.DataRowId, x.FormCreatedDate }).Select(group => group.First()).ToList();
                    }
                }

                //var response = await client.GetAsync(url);
                //var responseText = await response.Content.ReadAsStringAsync();

                if (modelResult != null && modelResult.Count > 0)
                {
                    //var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    if (formRelationMainList != null && formRelationMainList.Count > 0)
                        modelResult.AddRange(formRelationMainList);
                    if (modelResult != null && modelResult.Count > 0)
                        if (fromDate != null && fromDate != "" && toDate != null && toDate != "")
                        {
                            foreach (var newlist in modelResult.ToList())
                            {
                                DateTime FormCreatedDate = newlist.FormCreatedDate;

                                if (FormCreatedDate.Date >= Convert.ToDateTime(fromDate).Date && Convert.ToDateTime(toDate).Date >= FormCreatedDate.Date)
                                {
                                    resultNew.Add(newlist);
                                }


                            }
                            result = resultNew;
                        }
                        else
                        {
                            result = modelResult;
                            result = result.GroupBy(x => new { x.UniqueFormId, x.FormName, x.UniqueFormName, x.BusinessNeed, x.DataRowId, x.FormCreatedDate }).Select(group => group.First()).OrderByDescending(s => s.FormCreatedDate).ToList();
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
        public async Task<dynamic> GetPieChart(string formName = "", string fromDate = "", string toDate = "", string formstatus = "", string location = "")
        {
            DataModel DataModel1 = new DataModel();
            DataModel DataModel2 = new DataModel();
            List<FormData> modelResult1 = new List<FormData>();
            List<FormData> approvalMasterList = new List<FormData>();
            List<FormData> formRelationList = new List<FormData>();
            List<FormData> formRelationListAppList = new List<FormData>();
            List<FormData> formRelationMainList = new List<FormData>();
            dynamic result = null;
            var resultNew = new List<FormData>();
            if (formstatus == "Pending")
            {
                formstatus = "Initiated";
            }
            try
            {
                if (formName == "All")
                {
                    formName = "";
                }
                var resultForm = new List<FormData>();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                if (!string.IsNullOrEmpty(formName) || !string.IsNullOrEmpty(formstatus) || !string.IsNullOrEmpty(location))

                {


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_GetFormsgrapicUserName", con);
                    cmd1.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd1.Parameters.Add(new SqlParameter("@ISGrapicFilter", "0"));
                    cmd1.Parameters.Add(new SqlParameter("@FormName", DBNull.Value));
                    cmd1.Parameters.Add(new SqlParameter("@Location", DBNull.Value));
                    cmd1.Parameters.Add(new SqlParameter("@formstatus", DBNull.Value));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    List<FormData> FormDataList1 = new List<FormData>();

                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData form1 = new FormData();
                            form1.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            form1.UniqueFormId = Convert.ToInt32(dt1.Rows[i]["Id"]);

                            form1.DataRowId = dt1.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt1.Rows[i]["DataRowId"]);
                            form1.ControllerName = dt1.Rows[i]["ControllerName"] == DBNull.Value ? "" : Convert.ToString(dt1.Rows[i]["ControllerName"]);
                            form1.Status = Convert.ToString(dt1.Rows[i]["Status"]);
                            FormParentModel FPMitem3 = new FormParentModel();
                            FPMitem3.Id = dt1.Rows[i]["FormParentId"] == DBNull.Value ? 0 : Convert.ToInt32(dt1.Rows[i]["FormParentId"]);
                            form1.FormParent = FPMitem3;
                            if (dt1.Rows[i]["Created"] != DBNull.Value)
                                form1.FormCreatedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            if (dt1.Rows[i]["Modified"] != DBNull.Value)
                                form1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            form1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            Author Aitem3 = new Author();
                            Aitem3.Submitter = Convert.ToString(dt1.Rows[i]["SubmitterUserName"]);
                            form1.Author = Aitem3;
                            form1.BusinessNeed = Convert.ToString(dt1.Rows[i]["BusinessNeed"]);
                            form1.UniqueFormName = Convert.ToString(dt1.Rows[i]["UniqueFormName"]);
                            form1.FormName = Convert.ToString(dt1.Rows[i]["FormName"]);

                            FormDataList1.Add(form1);

                        }
                        DataModel1.Forms = FormDataList1;
                        modelResult1 = DataModel1.Forms;
                    }

                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataTable dt = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_GetFormsgrapicapprovaldata", con);
                    cmd.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd.Parameters.Add(new SqlParameter("@ISGrapicFilter", "0"));
                    cmd.Parameters.Add(new SqlParameter("@FormName", DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Location", DBNull.Value));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(dt);
                    con.Close();
                    List<FormData> Dataitem = new List<FormData>();
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            //int emp;


                            FormData form = new FormData();

                            form.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                            form.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                            form.AuthorityToEdit = dt.Rows[i]["AuthorityToEdit"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                            form.BusinessNeed = Convert.ToString(dt.Rows[i]["BusinessNeed"]);
                            form.Level = dt.Rows[i]["Level"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Level"]);
                            form.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                            form.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                            form.Department = Convert.ToString(dt.Rows[i]["Department"]);
                            form.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                            form.RowId = dt.Rows[i]["RowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["RowId"]);
                            form.Comment = Convert.ToString(dt.Rows[i]["Comment"]);
                            form.NextApproverId = dt.Rows[i]["NextApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["NextApproverId"]);
                            if (dt.Rows[i]["Modified"] != DBNull.Value)
                                form.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Modified"]);
                            Author item3 = new Author();
                            item3.Submitter = Convert.ToString(dt.Rows[i]["Title"]);
                            form.Author = item3;
                            form.Id = dt.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Id"]);

                            FormLookup item1 = new FormLookup();
                            item1.Id = dt.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["Id"]);
                            if (dt.Rows[i]["Created"] != DBNull.Value)
                                item1.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            item1.DataRowId = dt.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["DataRowId"]);
                            item1.ControllerName = Convert.ToString(dt.Rows[i]["ControllerName"]);
                            item1.ListName = Convert.ToString(dt.Rows[i]["ListName"]);
                            item1.UniqueFormName = Convert.ToString(dt.Rows[i]["UniqueFormName"]);
                            item1.FormStatus = Convert.ToString(dt.Rows[i]["Status"]);
                            item1.SubmitterId = dt.Rows[i]["SubmitterId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["SubmitterId"]);
                            form.FormRelation = item1;

                            Dataitem.Add(form);


                        }
                        DataModel2.Forms = Dataitem;
                        approvalMasterList = DataModel2.Forms;
                    }


                    if (!string.IsNullOrEmpty(formstatus))
                    {
                        string newstatus = "";
                        if (formstatus == "Initiated")
                        {
                            newstatus = "Pending";
                        }
                        else
                        {
                            newstatus = formstatus;
                        }

                        formRelationListAppList = approvalMasterList.Where(x => x.ApproverStatus == newstatus).ToList();
                    }
                    else
                    {
                        formRelationListAppList = approvalMasterList;
                    }

                    formRelationList.AddRange(formRelationListAppList.Select(x => new FormData()
                    {
                        UniqueFormId = x.FormRelation.Id,
                        //Status = x.FormRelation.FormStatus,
                        Status = x.ApproverStatus,
                        FormName = x.FormRelation.FormName,
                        UniqueFormName = x.FormRelation.UniqueFormName,
                        FormCreatedDate = x.FormRelation.CreatedDate,
                        Author = new Author() { Submitter = x.Author.Submitter },
                        BusinessNeed = x.BusinessNeed,
                        DataRowId = x.FormRelation.DataRowId,
                        ControllerName = x.FormRelation.ControllerName,
                        Comment = x.Comment
                    }).ToList());

                    formRelationMainList = formRelationList.GroupBy(x => new { x.UniqueFormId, x.Status, x.FormName, x.UniqueFormName, x.BusinessNeed, x.DataRowId, x.FormCreatedDate }).Select(group => group.First()).ToList();
                }
                else
                {

                  


                    SqlCommand cmd2 = new SqlCommand();
                    SqlDataAdapter adapter2 = new SqlDataAdapter();
                    DataTable dt2 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd2 = new SqlCommand("USP_GetFormsdata", con);
                    cmd2.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd2.CommandType = CommandType.StoredProcedure;
                    adapter2.SelectCommand = cmd2;
                    con.Open();
                    adapter2.Fill(dt2);
                    con.Close();

                    List<FormData> FormDataList2 = new List<FormData>();

                    if (dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt2.Rows.Count; i++)
                        {
                            FormData form2 = new FormData();
                            form2.Id = Convert.ToInt32(dt2.Rows[i]["Id"]);
                            if (dt2.Rows[i]["Modified"] != DBNull.Value)
                                form2.RecievedDate = Convert.ToDateTime(dt2.Rows[i]["Modified"]);
                            form2.Department = Convert.ToString(dt2.Rows[i]["Department"]);
                            form2.BusinessNeed = Convert.ToString(dt2.Rows[i]["BusinessNeed"]);
                            FormLookup item1 = new FormLookup();
                            item1.DataRowId = dt2.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt2.Rows[i]["DataRowId"]);
                            item1.ControllerName = Convert.ToString(dt2.Rows[i]["ControllerName"]);
                            item1.FormStatus = Convert.ToString(dt2.Rows[i]["Status"]);
                            if (dt2.Rows[i]["Created"] != DBNull.Value)
                                item1.CreatedDate = Convert.ToDateTime(dt2.Rows[i]["Created"]);
                            item1.UniqueFormName = Convert.ToString(dt2.Rows[i]["UniqueFormName"]);
                            FormParentModel FPMitem3 = new FormParentModel();
                            FPMitem3.Id = dt2.Rows[i]["FormParentId"] == DBNull.Value ? 0 : Convert.ToInt32(dt2.Rows[i]["FormParentId"]);
                            Author item3 = new Author();
                            item3.Submitter = Convert.ToString(dt2.Rows[i]["Title"]);
                            form2.Author = item3;
                            form2.Id = dt2.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt2.Rows[i]["Id"]);
                            form2.FormName = Convert.ToString(dt2.Rows[i]["FormName"]);
                            form2.Status = Convert.ToString(dt2.Rows[i]["Status"]);
                            FormDataList2.Add(form2);

                        }
                        DataModel1.Forms = FormDataList2;
                        modelResult1 = DataModel1.Forms;

                    }


                   
                    SqlCommand cmd3 = new SqlCommand();
                    SqlDataAdapter adapter3 = new SqlDataAdapter();
                    DataTable dt3 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd3 = new SqlCommand("USP_GetApprovaldata", con);
                    cmd3.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd3.CommandType = CommandType.StoredProcedure;
                    adapter3.SelectCommand = cmd3;
                    con.Open();
                    adapter3.Fill(dt3);
                    con.Close();


                    List<FormData> Dataitem = new List<FormData>();
                    if (dt3.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt3.Rows.Count; i++)
                        {
                            //int emp;


                            FormData form3 = new FormData();

                            form3.Id = Convert.ToInt32(dt3.Rows[i]["ID"]);
                            form3.ApprovalType = Convert.ToString(dt3.Rows[i]["ApprovalType"]);
                            form3.AuthorityToEdit = dt3.Rows[i]["AuthorityToEdit"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["AuthorityToEdit"]);
                            form3.BusinessNeed = Convert.ToString(dt3.Rows[i]["BusinessNeed"]);
                            form3.Level = dt3.Rows[i]["Level"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["Level"]);
                            form3.Logic = Convert.ToString(dt3.Rows[i]["Logic"]);
                            form3.RunWorkflow = Convert.ToString(dt3.Rows[i]["RunWorkflow"]);
                            form3.Department = Convert.ToString(dt3.Rows[i]["Department"]);
                            form3.ApproverStatus = Convert.ToString(dt3.Rows[i]["ApproverStatus"]);
                            form3.RowId = dt3.Rows[i]["RowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["RowId"]);
                            form3.Comment = Convert.ToString(dt3.Rows[i]["Comment"]);
                            form3.NextApproverId = dt3.Rows[i]["NextApproverId"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["NextApproverId"]);
                            if (dt3.Rows[i]["Modified"] != DBNull.Value)
                                form3.RecievedDate = Convert.ToDateTime(dt3.Rows[i]["Modified"]);
                            Author item3 = new Author();
                            //item3.Submitter = Convert.ToString(dt3.Rows[i]["Title"]);
                            form3.Author = item3;
                            form3.Id = dt3.Rows[i]["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["Id"]);

                            FormLookup item1 = new FormLookup();
                            item1.Id = dt3.Rows[i]["Id1"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["Id1"]);
                            if (dt3.Rows[i]["Created"] != DBNull.Value)
                                item1.CreatedDate = Convert.ToDateTime(dt3.Rows[i]["Created"]);
                            item1.DataRowId = dt3.Rows[i]["DataRowId"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["DataRowId"]);
                            item1.ControllerName = Convert.ToString(dt3.Rows[i]["ControllerName"]);
                            item1.ListName = Convert.ToString(dt3.Rows[i]["ListName"]);
                            item1.UniqueFormName = Convert.ToString(dt3.Rows[i]["UniqueFormName"]);
                            item1.FormStatus = Convert.ToString(dt3.Rows[i]["Status"]);
                            item1.SubmitterId = dt3.Rows[i]["SubmitterId"] == DBNull.Value ? 0 : Convert.ToInt32(dt3.Rows[i]["SubmitterId"]);
                            form3.FormRelation = item1;

                            Dataitem.Add(form3);


                        }
                        DataModel2.Forms = Dataitem;
                        approvalMasterList = DataModel2.Forms;
                    }







                    //var responseApprovalMaster = await client.GetAsync(responseApprovalMasterUrl);

                    //var responseTextApprovalMaster = await responseApprovalMaster.Content.ReadAsStringAsync();

                    //if (!string.IsNullOrEmpty(responseTextApprovalMaster))
                    //{
                    //    var modelApprovalResult = JsonConvert.DeserializeObject<DashboardModel>(responseTextApprovalMaster);
                    //    approvalMasterList = modelApprovalResult.Data.Forms;

                    //}

                    formRelationList.AddRange(approvalMasterList.Select(x => new FormData()
                    {
                        UniqueFormId = x.FormRelation.Id,
                        //Status = x.FormRelation.FormStatus,
                        Status = x.ApproverStatus,
                        FormName = x.FormRelation.FormName,
                        UniqueFormName = x.FormRelation.UniqueFormName,
                        FormCreatedDate = x.FormRelation.CreatedDate,
                        Author = new Author() { Submitter = x.Author.Submitter },
                        BusinessNeed = x.BusinessNeed,
                        DataRowId = x.FormRelation.DataRowId,
                        ControllerName = x.FormRelation.ControllerName,
                        Comment = x.Comment
                    }).ToList());

                    formRelationMainList = formRelationList.GroupBy(x => new { x.UniqueFormId, x.Status, x.FormName, x.UniqueFormName, x.BusinessNeed, x.DataRowId, x.FormCreatedDate }).Select(group => group.First()).ToList();
                }
                //var response = await client.GetAsync(url);
                //var responseText = await response.Content.ReadAsStringAsync();

                //var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                //modelResult.Data.Forms.AddRange(formRelationMainList);

                //if (modelResult1 != null   && modelResult1.Data.Forms.Count > 0)
                //    if (fromDate != null && fromDate != "" && toDate != null && toDate != "")
                //    {
                //        foreach (var newlist in modelResult1.Data.Forms.ToList())
                //        {
                //            System.DateTime FormCreatedDate = newlist.FormCreatedDate;

                //            if (FormCreatedDate.Date >= Convert.ToDateTime(fromDate).Date && Convert.ToDateTime(toDate).Date >= FormCreatedDate.Date)
                //            {
                //                resultNew.Add(newlist);
                //            }

                //        }
                //        result = resultNew;
                //        resultForm = resultNew;
                //    }
                //    else
                //    {
                //        result = modelResult1;
                //        resultForm = result;

                //    }

                if (modelResult1 != null && modelResult1.Count > 0)
                {
                    if (formRelationMainList != null && formRelationMainList.Count > 0)
                        modelResult1.AddRange(formRelationMainList);
                    if (modelResult1 != null && modelResult1.Count > 0)
                        if (fromDate != null && fromDate != "" && toDate != null && toDate != "")
                        {
                            foreach (var newlist in modelResult1.ToList())
                            {
                                System.DateTime FormCreatedDate = newlist.FormCreatedDate;

                                if (FormCreatedDate.Date >= Convert.ToDateTime(fromDate).Date && Convert.ToDateTime(toDate).Date >= FormCreatedDate.Date)
                                {
                                    resultNew.Add(newlist);
                                }

                            }
                            result = resultNew;
                            resultForm = resultNew;
                        }
                        else
                        {
                            result = modelResult1;
                            resultForm = result;

                        }
                }

                var status = new FormStatus();


                var listItems = resultForm.ToList();

                status.Approved = listItems.Where(x => x.Status == "Approved").Count();
                status.Rejected = listItems.Where(x => x.Status == "Rejected").Count();
                status.Processed = listItems.Where(x => x.Status == "Initiated").Count();
                var pending = listItems.Where(x => x.Status == "Pending").Count();
                status.Cancelled = listItems.Where(x => x.Status == "Cancelled").Count();
                status.Submitted = listItems.Where(x => x.Status == "Submitted").Count();
                //status.Resubmitted = listItems.Where(x => x.Status == "Resubmitted").Count();
                //status.Submitted = status.Submitted + status.Resubmitted;
                status.Processed = status.Processed + pending;

                List<Graph> MainList = new List<Graph>
                   {
                     new Graph { FormStatus = "Submitted" + "|" + Convert.ToString(status.Submitted), FormCount =  Convert.ToString(status.Submitted) },
                     new Graph { FormStatus = "Approved"+ "|" + Convert.ToString(status.Approved), FormCount = Convert.ToString(status.Approved) },
                     new Graph { FormStatus = "Pending"+ "|" + Convert.ToString(status.Processed), FormCount =  Convert.ToString(status.Processed)  },
                     new Graph { FormStatus = "Rejected"+ "|" + Convert.ToString(status.Rejected),  FormCount = Convert.ToString(status.Rejected)  },
                     new Graph { FormStatus = "Cancelled"+ "|" + Convert.ToString(status.Cancelled), FormCount =  Convert.ToString(status.Cancelled)  }
                  };

                result = MainList;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        /// <summary>
        /// Dashboard-It is used to Get Report Section Forms List Dropdown.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetFormsList()
        {
            DataModel IDCFData = new DataModel();
            dynamic result = IDCFData;
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id,UniqueFormName,FormName,ReleaseDate&$orderby=ReleaseDate desc&$filter=IsComplete eq '1'");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    result = locResult.Data;
                }


                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }
        public async Task<DataModel> GetFormsList_SQL()
        {

            var result = new DataModel();
            try
            {
                DashboardModel dashboardModel = new DashboardModel();
                DataModel dataModel = new DataModel();
                List<FormData> formDataList = new List<FormData>();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetFormParent", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        FormData item = new FormData();
                        item.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                        item.FormName = Convert.ToString(ds.Tables[0].Rows[i]["FormName"]);
                        item.ReleaseDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["ReleaseDate"]);
                        item.UniqueFormName = Convert.ToString(ds.Tables[0].Rows[i]["UniqueFormName"]);
                        item.Department = Convert.ToString(ds.Tables[0].Rows[i]["Department"]);
                        item.ControllerName = Convert.ToString(ds.Tables[0].Rows[i]["ControllerName"]);
                        item.Message = Convert.ToString(ds.Tables[0].Rows[i]["Message"]);
                        formDataList.Add(item);
                    }
                }
                dataModel.Forms = formDataList;
                dashboardModel.Data = dataModel;

                var modelResult = dashboardModel;
                result = modelResult.Data;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
        #endregion

        #region Admin Section

        /// <summary>
        /// Dashboard-It is used to get the Admin Section Page.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetAllFormsListForAdmin(string uniqueFormName = "", string deparment = "", string status = "")
        {
            var result = new List<FormData>();
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var url = "_api/web/lists/GetByTitle('Forms')/items?$select=Id,DataRowId,SubmitterUserName,ControllerName,Status,FormParentId/Id,Created,Modified,Department,Author/Title,UniqueFormName,FormName" +
               "&$filter=SubmitterUserName eq '" + user.UserName + "' " +
               "&$expand=FormParentId,Author&$top=1000";
                var response = await client.GetAsync(url);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    if (modelResult != null && modelResult.Data != null && modelResult.Data.Forms != null && modelResult.Data.Forms.Count > 0)
                        result = modelResult.Data.Forms;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        #endregion

        #region On Behalf Functionality of PAF Form
        /// <summary>
        /// PAF Form-It is used to get the Employee Data in PAF Form.
        /// </summary>
        /// <returns></returns>
        public List<UserData> GetPAFEmployeeDetails(string empName)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetPAFEmployeeDetails", con);
                cmd.Parameters.Add(new SqlParameter("@FirstName", empName));
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
                        //user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        //user.UserName = user.FirstName + " " + user.LastName;
                        user.UserName = user.FirstName;
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }
        /// <summary>
        /// PAF Form-It is used to get the exisitng Employee Data in PAF Form.
        /// </summary>
        /// <returns></returns>
        public UserData GetPAFExistingEmployeeDetails(string otherEmpUserId)
        {
            UserData user = new UserData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetPAFExistingEmployeeDetails", con);
                cmd.Parameters.Add(new SqlParameter("@UserId", otherEmpUserId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        //user.UserId = Convert.ToInt32(ds.Tables[0].Rows[i]["Id"]);
                        user.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        user.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        user.Department = ds.Tables[0].Rows[i]["Department"].ToString();
                        user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        user.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        user.PhoneNumber = ds.Tables[0].Rows[i]["PhoneNumber"].ToString();
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return user;
        }

        #endregion

        #region Newly Added & Freq. Used Forms Section
        /// <summary>
        /// Dashboard-It is used to get the Newly Added Form Dashboard via Side Menu clicke.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetNewlyAddedForms(string deparment = "")
        {
            var result = new List<FormData>();
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id,ControllerName,UniqueFormName,Department,FormOwner,FormName,ReleaseDate&$orderby=ReleaseDate desc&$filter=IsComplete eq '1'");
                var responseText = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    if (modelResult != null && modelResult.Data != null && modelResult.Data.Forms != null && modelResult.Data.Forms.Count > 0)
                    {
                        //var groupList = modelResult.Data.Forms.Where(x=>x.UniqueFormName.Equals("PAF")).GroupBy(x => x.UniqueFormName);
                        var groupList = modelResult.Data.Forms.GroupBy(x => x.UniqueFormName);
                        foreach (var item in groupList)
                        {
                            var model = new FormData();
                            var firstItem = item.FirstOrDefault();
                            if (firstItem != null)
                            {
                                model.FormName = firstItem.FormName;
                                model.UniqueFormName = firstItem.UniqueFormName;
                                model.FormCount = item.Count();
                                model.FormParent = new FormParentModel();
                                model.FormParent.Id = firstItem.Id;
                                //model.FormParent.Id = firstItem.FormParent.Id;
                                model.Department = firstItem.Department;
                                model.FormOwner = firstItem.FormOwner;
                                model.ControllerName = firstItem.ControllerName;
                                result.Add(model);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        /// <summary>
        /// Dashboard-It is used to get the Freq. Used Form Dashboard via Side Menu clicke.
        /// </summary>
        /// <returns></returns>
        public async Task<List<FormData>> GetFreqAddedForms(string deparment = "")
        {
            var uniqueFormList = new List<FormData>();
            var freqForms = new List<FormData>();
            var result = new List<FormData>();
            try
            {
                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //new forms
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('FormParent')/items?$select=Id,ControllerName,UniqueFormName,Department,FormOwner,FormName,ReleaseDate&$orderby=ReleaseDate desc&$filter=IsComplete eq '1'");
                var responseTextNew = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseTextNew))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseTextNew);
                    uniqueFormList = modelResult.Data.Forms;
                }

                var responseFreq = await client.GetAsync("_api/web/lists/GetByTitle('Forms')/items?$select=Id,SubmitterUserName,ControllerName,Status,FormParentId/" +
                "Id,Created,Modified,Department,UniqueFormName,FormName&$filter=SubmitterUserName eq '" + user.UserName + "'&$expand=FormParentId&$top=1000");
                var responseText = await responseFreq.Content.ReadAsStringAsync();


                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    freqForms = modelResult.Data.Forms;

                    for (int i = 0; i < uniqueFormList.Count; i++)
                    {
                        uniqueFormList[i].FormCount = freqForms.Count(x => x.FormParent.Id == uniqueFormList[i].Id);
                    }
                }

                var freqFormList = uniqueFormList.Select(item => new FormData()
                {
                    FormName = item.FormName,
                    UniqueFormId = item.UniqueFormId,
                    FormCount = item.FormCount,
                    UniqueFormName = item.UniqueFormName,
                    Department = item.Department,
                    FormOwner = item.FormOwner,
                    ControllerName = item.ControllerName
                }).OrderByDescending(x => x.FormCount).ToList();

                var groupList = freqFormList.GroupBy(x => x.UniqueFormName);
                foreach (var item in groupList)
                {
                    var model = new FormData();
                    var firstItem = item.FirstOrDefault();
                    if (firstItem != null)
                    {
                        model.FormName = firstItem.FormName;
                        model.UniqueFormName = firstItem.UniqueFormName;
                        model.FormCount = item.Count();
                        model.FormParent = new FormParentModel();
                        model.FormParent.Id = firstItem.Id;
                        //model.FormParent.Id = firstItem.FormParent.Id;
                        model.Department = firstItem.Department;
                        model.FormOwner = firstItem.FormOwner;
                        model.ControllerName = firstItem.ControllerName;
                        result.Add(model);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        #endregion

        /// <summary>
        /// InternetAccess/SmartPhone/SoftwareRequisition-It is used to get the designations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        public List<DesignationModel> GetDesignations()
        {
            List<DesignationModel> designationList = new List<DesignationModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetDesignation", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DesignationModel dm = new DesignationModel();
                        dm.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["DesignationId"]);
                        dm.JobTitle = Convert.ToString(ds.Tables[0].Rows[i]["JobTitle"]).Trim();
                        dm.IsSmartPhoneApplicable = Convert.ToInt32(ds.Tables[0].Rows[i]["SmartPhoneApplicable"]);
                        //dm.IsActive = Convert.ToInt32(ds.Tables[0].Rows[i]["IsActive"]);
                        designationList.Add(dm);
                    }
                }
                return designationList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new List<DesignationModel>();
            }

        }


        /// <summary>
        /// Dashboard-It is used to Get Form Name List Dropdown.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetDashboardFormsList(string department)
        {
            DataModel IDCFData = new DataModel();
            dynamic result = IDCFData;
            DashboardModel dashboard = new DashboardModel();
            DataModel dataModel = new DataModel();
            List<FormData> formData = new List<FormData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetDashboardFormsList", con);
                cmd.Parameters.AddWithValue("@Department", department);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        FormData dm = new FormData();
                        dm.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["Id"]);
                        dm.UniqueFormName = Convert.ToString(ds.Tables[0].Rows[i]["UniqueFormName"]).Trim();
                        dm.FormName = Convert.ToString(ds.Tables[0].Rows[i]["FormName"]).Trim();
                        dm.ReleaseDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["ReleaseDate"]);
                        formData.Add(dm);
                    }
                }

                dataModel.Forms = formData;
                dashboard.Data = dataModel;
                result = dashboard.Data;

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }


        #region All AD Sync Methods

        public UserData GetCompanyNameFromAD(string emailId)
        {
            UserData user = new UserData();
            DirectoryEntry searchRoot = null;
            try
            {
                string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        search.Filter = "(mail=" + emailId + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            if (emailId.ToLower().Contains("extern"))
                            {
                                user.CompanyName = result.Properties["company"][0].ToString();
                            }
                            user.UserName = result.Properties["samaccountname"][0].ToString();

                            break;
                        }
                    }
                    catch
                    {

                    }
                }
                return user;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new UserData();
            }
        }
        public string GetUserIdByEmailId(string emailId)
        {
            string adname = "";
            string approvalId = "";
            DirectoryEntry searchRoot = null;
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            try
            {
                //string[] arrLDAP = LDAP.Split(',');
                //for (int i = 0; i < arrLDAP.Length; i++)
                //{
                //    try
                //    {
                //        adname = arrLDAP[i];
                //        //#if DEBUG
                //        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
                //        //#else
                //        //searchRoot = new DirectoryEntry(arrLDAP[i]);
                //        //#endif
                //        DirectorySearcher search = new DirectorySearcher(searchRoot);
                //        search.Filter = "(mail=" + emailId + ")";
                //        SearchResult result = search.FindOne();
                //        if (result != null)
                //        {
                //            string obVal = result.Properties["samaccountname"][0].ToString();
                //            approvalId = obVal;

                //            break;
                //        }
                //        else if (result == null)
                //        {
                //            Log.Error("For- " + emailId + " Result is getting NULL from " + adname);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Error("The- " + emailId + " Is Not Found in AD: " + adname + "Error: " + ex.Message, ex);
                //    }
                //}

                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();

                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_GetApproverthroughmail", con_form);
                cmd_form.Parameters.Add(new SqlParameter("@emailId", emailId));
                cmd_form.CommandType = CommandType.StoredProcedure;
                //adapter_form.SelectCommand = cmd_form;
                adapter_form.SelectCommand = cmd_form;
                con_form.Open();
                adapter_form.Fill(ds_form);
                con_form.Close();
                if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                {
                    for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                    {
                        approvalId = Convert.ToString(ds_form.Tables[0].Rows[i]["message"]);
                    }
                }
                return approvalId;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return approvalId;
            }
            //finally
            //{
            //    searchRoot.Close();
            //}
        }
        private string ConvertByteToStringSid(Byte[] sidBytes)
        {
            StringBuilder strSid = new StringBuilder();
            strSid.Append("S-");
            try
            {
                // Add SID revision.
                strSid.Append(sidBytes[0].ToString());
                // Next six bytes are SID authority value.
                if (sidBytes[6] != 0 || sidBytes[5] != 0)
                {
                    string strAuth = String.Format
                        ("0x{0:2x}{1:2x}{2:2x}{3:2x}{4:2x}{5:2x}",
                        (Int16)sidBytes[1],
                        (Int16)sidBytes[2],
                        (Int16)sidBytes[3],
                        (Int16)sidBytes[4],
                        (Int16)sidBytes[5],
                        (Int16)sidBytes[6]);
                    strSid.Append("-");
                    strSid.Append(strAuth);
                }
                else
                {
                    Int64 iVal = (Int32)(sidBytes[1]) +
                        (Int32)(sidBytes[2] << 8) +
                        (Int32)(sidBytes[3] << 16) +
                        (Int32)(sidBytes[4] << 24);
                    strSid.Append("-");
                    strSid.Append(iVal.ToString());
                }

                // Get sub authority count...
                int iSubCount = Convert.ToInt32(sidBytes[7]);
                int idxAuth = 0;
                for (int i = 0; i < iSubCount; i++)
                {
                    idxAuth = 8 + i * 4;
                    UInt32 iSubAuth = BitConverter.ToUInt32(sidBytes, idxAuth);
                    strSid.Append("-");
                    strSid.Append(iSubAuth.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return "";
            }
            return strSid.ToString();
        }
        public UserData GetUserDetailsFromUsernameAD(string username, string password)
        {
            DirectoryEntry searchRoot = null;
            UserData user = new UserData();
            string email = "";
            string cname = "";
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {

                        //#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], username, password);
                        //#else
                        //searchRoot = new DirectoryEntry(arrLDAP[i]);
                        //#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        search.Filter = "(sAMAccountName=" + username + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            object obVal = result.ContainsProperty("objectSid") ? result.Properties["objectSid"][0] : "0";
                            string str = this.ConvertByteToStringSid((Byte[])obVal);
                            string newObjectSid = "";
                            string objectSid = str;
                            string[] objectSidList = objectSid.Split('-');
                            var count = objectSidList.Count();
                            string SID = objectSidList[count - 1];
                            objectSidList = objectSidList.Take(objectSidList.Length - 1).ToArray();
                            foreach (var item in objectSidList)
                            {
                                newObjectSid += item.ToString() + '-';
                            }

                            //user.UserId = Convert.ToInt32(SID);
                            user.ObjectSid = newObjectSid;

                            user.UserName = result.Properties["samaccountname"][0].ToString();
                            email = result.ContainsProperty("mail") ? result.Properties["mail"][0].ToString() : "";
                            cname = result.ContainsProperty("company") ? result.Properties["company"][0].ToString() : "";
                            user.Email = email;
                            if (user.Email.ToLower().Contains("extern"))
                            {
                                user.CompanyName = cname;
                            }

                            var path = result.Path;
                            var domainName = GetDomainName(path);
                            user.DomainName = domainName;

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Not Found in AD", ex);
                    }
                }
                return user;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new UserData();
            }
            finally
            {
                searchRoot.Close();
            }
        }

        public UserData GetUserDetailsFromUsername(string username, string password)
        {
            DirectoryEntry searchRoot = null;
            UserData user = new UserData();
            string email = "";
            string cname = "";
            string passHash = "";
            string UserName = "";
            try
            {

                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();

                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_LoginDetails", con_form);
                cmd_form.Parameters.Add(new SqlParameter("@UserName", username));
                cmd_form.Parameters.Add(new SqlParameter("@Password", password));
                cmd_form.CommandType = CommandType.StoredProcedure;
                //adapter_form.SelectCommand = cmd_form;
                adapter_form.SelectCommand = cmd_form;
                con_form.Open();
                adapter_form.Fill(ds_form);
                con_form.Close();
                var passwordHash = new byte[36];
                if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                {
                    for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                    {
                        UserName = Convert.ToString(ds_form.Tables[0].Rows[i]["UserName"]);
                        email = Convert.ToString(ds_form.Tables[0].Rows[i]["EmailID"]);
                        passHash = Convert.ToString(ds_form.Tables[0].Rows[i]["Password"]);
                    }
                }

                if (!string.IsNullOrEmpty(passHash))
                {
                    passwordHash = Convert.FromBase64String(passHash);
                    PasswordHash pass = new PasswordHash(passwordHash);
                    var result = pass.Verify(password);

                    if (result)
                    {
                        email = email;
                        UserName = UserName;
                    }

                    else
                    {
                        email = "";
                        UserName = "";
                    }
                }
                user.Email = email;
                user.UserName = UserName;
                if (user.Email.ToLower().Contains("extern"))
                {
                    user.CompanyName = cname;
                }

                var path = "";
                //var domainName = GetDomainName(path);
                //user.DomainName = domainName;
                //user.UserName= username;


                return user;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new UserData();
            }

        }


        public string GetDomainName(string path)
        {
            var domainName = "";
            var dataArray = path.Split(',');
            var domainArray = dataArray.Where(x => x.StartsWith("DC"));
            domainName = string.Join(".", domainArray).Replace("DC=", "");

            return domainName;
        }
        public string GetApproverNameFromAD(string approverid, UserData currentUser = null)
        {
            DirectoryEntry searchRoot = null;
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            if (currentUser == null)
                currentUser = user;

            string approverName = "";
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], currentUser.UserName, currentUser.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);

                        search.Filter = "(samaccountname=" + approverid + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            approverName = result.Properties["displayname"][0].ToString();
                        }

                        break;
                    }
                    catch { }
                }
                return approverName;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return approverName;
            }
            finally
            {
                searchRoot.Close();
            }
        }

        public UserData GetApproverDetailsFromAD(string approverid, UserData currentUser = null)
        {
            DirectoryEntry searchRoot = null;
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            if (currentUser == null)
                currentUser = user;

            UserData userDetails = new UserData();

            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], currentUser.UserName, currentUser.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        //search.Filter = "(ObjectSid=" + approverid + ")";
                        search.Filter = "(samaccountname=" + approverid + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            userDetails.EmployeeName = result.Properties["displayname"][0].ToString();
                            userDetails.Email = result.Properties["mail"][0].ToString();
                        }

                        break;
                    }
                    catch { }
                }
                return userDetails;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
            finally
            {
                searchRoot.Close();
            }
        }

        public string GetApproverEmaiilFromAD(string approverid)
        {
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            DirectoryEntry searchRoot = null;
            string email = "";
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        //search.Filter = "(ObjectSid=" + approverid + ")";
                        search.Filter = "(samaccountname=" + approverid + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            email = result.Properties["mail"][0].ToString();
                        }
                        break;
                    }
                    catch { }
                }
                return email;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return email;
            }
            finally
            {
                searchRoot.Close();
            }
        }


        public UserData GetEmpDetByUserName(string username)
        {
            DirectoryEntry searchRoot = null;
            GlobalClass obj = new GlobalClass();
            user = obj.GetCurrentUser();
            var userDetails = new UserData();
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        search.Filter = "(sAMAccountName=" + username + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            //object obVal = result.ContainsProperty("objectSid") ? result.Properties["objectSid"][0] : "0";
                            //string str = this.ConvertByteToStringSid((Byte[])obVal);
                            //string newObjectSid = "";
                            //string objectSid = str;
                            //string[] objectSidList = objectSid.Split('-');
                            //var count = objectSidList.Count();
                            //string SID = objectSidList[count - 1];
                            //objectSidList = objectSidList.Take(objectSidList.Length - 1).ToArray();
                            //foreach (var item in objectSidList)
                            //{
                            //    newObjectSid += item.ToString() + '-';
                            //}

                            ////user.UserId = Convert.ToInt32(SID);
                            //userDetails.ObjectSid = SID;

                            userDetails.EmployeeName = result.Properties["displayname"][0].ToString();
                            userDetails.UserName = result.Properties["sAMAccountName"][0].ToString();
                            userDetails.Email = result.ContainsProperty("mail") ? result.Properties["mail"][0].ToString() : "";

                            var path = result.Path;
                            var domainName = GetDomainName(path);
                            userDetails.DomainName = domainName;

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Not Found in AD", ex);
                    }
                }
                return userDetails;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new UserData();
            }
            finally
            {
                searchRoot.Close();
            }
        }

        public UserData GetEmpDetByEmailId(string email)
        {
            DirectoryEntry searchRoot = null;
            GlobalClass obj = new GlobalClass();
            user = obj.GetCurrentUser();
            var userDetails = new UserData();
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        search.Filter = "(mail=" + email + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            //object obVal = result.ContainsProperty("objectSid") ? result.Properties["objectSid"][0] : "0";
                            //string str = this.ConvertByteToStringSid((Byte[])obVal);
                            //string newObjectSid = "";
                            //string objectSid = str;
                            //string[] objectSidList = objectSid.Split('-');
                            //var count = objectSidList.Count();
                            //string SID = objectSidList[count - 1];
                            //objectSidList = objectSidList.Take(objectSidList.Length - 1).ToArray();
                            //foreach (var item in objectSidList)
                            //{
                            //    newObjectSid += item.ToString() + '-';
                            //}

                            ////user.UserId = Convert.ToInt32(SID);
                            //userDetails.ObjectSid = SID;

                            userDetails.EmployeeName = result.Properties["displayname"][0].ToString();
                            userDetails.Email = result.Properties["mail"][0].ToString();
                            userDetails.UserName = result.ContainsProperty("sAMAccountName") ? result.Properties["sAMAccountName"][0].ToString() : "";

                            var path = result.Path;
                            var domainName = GetDomainName(path);
                            userDetails.DomainName = domainName;

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Not Found in AD", ex);
                    }
                }
                return userDetails;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new UserData();
            }
            finally
            {
                searchRoot.Close();
            }
        }

        public string GetApproverNameByEmailId(string emailId)
        {
            string adname = "";
            string approverName = "";
            DirectoryEntry searchRoot = null;
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                //                for (int i = 0; i < arrLDAP.Length; i++)
                //                {
                //                    try
                //                    {
                //                        adname = arrLDAP[i];
                //#if DEBUG
                //                        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
                //#else
                //                        searchRoot = new DirectoryEntry(arrLDAP[i]);
                //#endif
                //                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                //                        search.Filter = "(mail=" + emailId + ")";
                //                        SearchResult result = search.FindOne();
                //                        if (result != null)
                //                        {
                //                            approverName = result.Properties["displayname"][0].ToString();

                //                            break;
                //                        }
                //                        else if (result == null)
                //                        {
                //                            Log.Error("For- " + emailId + " Result is getting NULL from " + adname);
                //                        }
                //                    }
                //                    catch (Exception ex)
                //                    {
                //                        Log.Error("The- " + emailId + " Is Not Found in AD: " + adname + "Error: " + ex.Message, ex);
                //                    }
                //                }
                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter_form = new SqlDataAdapter();
                DataSet ds_form = new DataSet();

                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_GetApproverNamethroughmail", con_form);
                cmd_form.Parameters.Add(new SqlParameter("@emailId", emailId));
                cmd_form.CommandType = CommandType.StoredProcedure;
                //adapter_form.SelectCommand = cmd_form;
                adapter_form.SelectCommand = cmd_form;
                con_form.Open();
                adapter_form.Fill(ds_form);
                con_form.Close();
                if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                {
                    for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                    {
                        approverName = Convert.ToString(ds_form.Tables[0].Rows[i]["message"]);
                    }
                }
                return approverName;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return approverName;
            }
            //finally
            //{
            //    searchRoot.Close();
            //}
        }

        public string GetApproverNameByUserName(string userName)
        {
            string adname = "";
            string approverName = "";
            DirectoryEntry searchRoot = null;
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
                        adname = arrLDAP[i];
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], user.UserName, user.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        search.Filter = "(samaccountname=" + userName + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            approverName = result.Properties["displayname"][0].ToString();

                            break;
                        }
                        else if (result == null)
                        {
                            Log.Error("For- " + userName + " Result is getting NULL from " + adname);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("The- " + userName + " Is Not Found in AD: " + adname + "Error: " + ex.Message, ex);
                    }
                }
                return approverName;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return approverName;
            }
            finally
            {
                searchRoot.Close();
            }
        }
        #endregion

        public List<string> GetDomainIDs()
        {
            List<string> domainIDs = new List<string>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetDomainIDs", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        domainIDs.Add(Convert.ToString(ds.Tables[0].Rows[i]["DomainID"]).Trim());
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }

            return domainIDs;

        }
        public async Task<dynamic> GetDashBoardDept()
        {
            var newDept = new List<string>();
            dynamic result = new DataModel();
            try
            {

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_getDepartmentList", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        newDept.Add(Convert.ToString(ds.Tables[0].Rows[i]["Department"]));
                    }
                }

                return newDept;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return newDept;
            }
        }

        public async Task<int> CancelApprovalMaster(int formId)
        {
            try
            {
                #region Comment
                //ClientContext _context = new ClientContext(new Uri(conString));
                //Web web = _context.Web;
                //GlobalClass gc = new GlobalClass();
                //var user = gc.GetCurrentUser();
                //_context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ID,ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,Level,Logic,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(FormId eq '" + formId + "' and IsActive eq 1)&$expand=FormId,Author")).Result;
                //var responseText = await response.Content.ReadAsStringAsync();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText);
                //var result = modelData.Node.Data;
                //List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                #endregion
                DataTable dt = new DataTable();
                SqlDataAdapter adapter_form2 = new SqlDataAdapter();
                SqlCommand cmd2 = new SqlCommand();
                var con2 = new SqlConnection(sqlConString);
                cmd2 = new SqlCommand("sp_GetApprovalMasterByFormId", con2);
                cmd2.Parameters.Add(new SqlParameter("@FormId", formId));
                cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                cmd2.CommandType = CommandType.StoredProcedure;
                adapter_form2.SelectCommand = cmd2;
                con2.Open();
                adapter_form2.Fill(dt);
                con2.Close();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        SqlDataAdapter adapter1 = new SqlDataAdapter();
                        SqlCommand cmd1 = new SqlCommand();
                        DataSet ds3 = new DataSet();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("sp_UpdateApprovalMatrixStatusById", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", dt.Rows[i]["ID"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[i]["ID"])));
                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));

                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(ds3);
                        con.Close();
                        #region Comment
                        //var currentItem = approvalMasterlist.GetItemById(row.Id);
                        //currentItem["IsActive"] = 0;
                        //currentItem.Update();
                        //_context.Load(currentItem);
                        //_context.ExecuteQuery();
                        #endregion
                    }
                }
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task<List<POCRFormModel>> ViewPOCRExcelData()
        {
            List<POCRFormModel> routeList = new List<POCRFormModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetPOCRFReport", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        POCRFormModel route = new POCRFormModel();
                        route.FormIDId = Convert.ToString(ds.Tables[0].Rows[i]["FormID"]);
                        route.CompanyName = Convert.ToString(ds.Tables[0].Rows[i]["CompanyName"]);
                        route.EmployeeType = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeType"]);
                        route.OldEmployeeContactNo = Convert.ToString(ds.Tables[0].Rows[i]["ContactNo"]);
                        route.OldEmployeeNumber = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        route.EmployeeName = ds.Tables[0].Rows[i]["RequestFrom"].ToString();
                        route.BusinessNeed = ds.Tables[0].Rows[i]["BusinessNeeds"].ToString();
                        route.TSEName = ds.Tables[0].Rows[i]["TSEName"].ToString();
                        // route.POCRID = ds.Tables[0].Rows[i]["POCRID"];
                        route.POCRNo = ds.Tables[0].Rows[i]["POCRNO"].ToString();
                        route.ZSMName = ds.Tables[0].Rows[i]["ZSMName"].ToString();
                        route.ASMName = ds.Tables[0].Rows[i]["ASMName"].ToString();
                        route.TSELocation = ds.Tables[0].Rows[i]["TSELoction"].ToString();
                        route.DealerCode = ds.Tables[0].Rows[i]["DealerCode"].ToString();
                        route.DealerName = ds.Tables[0].Rows[i]["DealerName"].ToString();
                        route.DealerLocation = ds.Tables[0].Rows[i]["DealerLocation"].ToString();
                        route.FILDealership = ds.Tables[0].Rows[i]["FILDealership"].ToString();
                        route.DealerSalesLastYr = ds.Tables[0].Rows[i]["DealerSalesLY"].ToString();
                        route.DealerSalesTill = ds.Tables[0].Rows[i]["DealerSalesTD"].ToString();
                        route.Status = ds.Tables[0].Rows[i]["ExclusiveStatus"].ToString();
                        route.BuilderName = ds.Tables[0].Rows[i]["BuilderName"].ToString();
                        //    route.DealerLocation = ds.Tables[0].Rows[i]["LocationName"].ToString();
                        route.ProjectName = ds.Tables[0].Rows[i]["ProjectName"].ToString();
                        route.SiteName = ds.Tables[0].Rows[i]["SiteNameLoc"].ToString();
                        route.RERANumber = ds.Tables[0].Rows[i]["ReraNo"].ToString();
                        route.CustomerReference = ds.Tables[0].Rows[i]["ReferenceCustomer"].ToString();
                        // route.DateFrom = ds.Tables[0].Rows[i]["DeliveryFrom"].ToString();
                        //  route.DateTo = ds.Tables[0].Rows[i]["DeliveryTo"].ToString();
                        route.OrderValue = ds.Tables[0].Rows[i]["ApproxOrderValue"].ToString();
                        route.PreferredPlant = ds.Tables[0].Rows[i]["PreferredPlant"].ToString();
                        route.FirstLifting = ds.Tables[0].Rows[i]["FirstLiftingV"].ToString();
                        //  route.LiftingDate = ds.Tables[0].Rows[i]["FirstLiftingD"].ToString();
                        route.POCRRequest = ds.Tables[0].Rows[i]["POCRRequest"].ToString();
                        route.AddDisMaterial = ds.Tables[0].Rows[i]["AddDiscountPerMaterial"].ToString();
                        //route.ProDisValid = ds.Tables[0]jectDisc.Rows[i]["ProountValid"].ToString();
                        route.CreditReq = ds.Tables[0].Rows[i]["CreditRequired"].ToString();
                        //    route.DateofEnquiry = ds.Tables[0].Rows[i]["DateofEnquiry"].ToString();
                        route.CreditPer = ds.Tables[0].Rows[i]["CreditPeriod"].ToString();
                        route.InterstCost = ds.Tables[0].Rows[i]["InterestCost"].ToString();
                        route.CreditLimit = ds.Tables[0].Rows[i]["CreditLimit"].ToString();
                        route.CustOverview = ds.Tables[0].Rows[i]["BriefCustomer"].ToString();
                        route.WhyProject = ds.Tables[0].Rows[i]["WhyProject"].ToString();
                        route.CompName = ds.Tables[0].Rows[i]["CompetitorName"].ToString();
                        route.Invoice = ds.Tables[0].Rows[i]["Quotation"].ToString();
                        route.Freight = ds.Tables[0].Rows[i]["FrieghtValue"].ToString();
                        route.TruckVol = ds.Tables[0].Rows[i]["TruckVolume"].ToString();
                        route.Place = ds.Tables[0].Rows[i]["Place"].ToString();
                        route.VehValue = ds.Tables[0].Rows[i]["Vehiclevalue"].ToString();
                        route.EmployeeType = ds.Tables[0].Rows[i]["EmployeeType"].ToString();
                        //route.EmployeeCode = ds.Tables[0].Rows[i]["EmployeeCode"] ;
                        //route.EmployeeCCCode = ds.Tables[0].Rows[i]["EmployeeCCCode"].ToString();


                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }


        public async Task<List<CBRFData>> ViewCBRFFExcelData()
        {
            var CBRFDataList = new List<CBRFData>();
            var userDataList = new List<CabUsersDto>();
            var finalDataList = new List<CBRFData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('CabBookingRequestForm')/items?$select=ID,RequestSubmissionFor,ReportingPlaceWithAddress,AirportPickUpDrop,FlightNo,FlightTime,Location,OnBehalfLocation,CostCenterNumber,OnBehalfCostCenterNumber,Destination,CarRequiredFromDate,CarRequiredToDate,"
           + "FormID/ID,FormID/Created&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var CBRFResult = JsonConvert.DeserializeObject<CabBookingRequestModel>(dataText, settings);
                    CBRFDataList = CBRFResult.cbrfflist.cbrfData;
                }

                //Cab Booking User Details
                var clientCab = new HttpClient(handler);
                clientCab.BaseAddress = new Uri(conString);
                clientCab.DefaultRequestHeaders.Accept.Clear();
                clientCab.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseCabUser = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('CabBookingUserDetails')/items?$select=ID,CabUsersId,UserName,UserContactNumber,Destination,ReportingTime,ReportingPlaceWithAddress," +
                   "FormID/ID,CabUsersId/ID,FormID/Created&$expand=FormID,CabUsersId&$top=10000")).Result;

                var responseTextCabUser = await responseCabUser.Content.ReadAsStringAsync();
                var resultCab = JsonConvert.DeserializeObject<CabUsersModel>(responseTextCabUser);
                userDataList = resultCab.List.CabUsersList;

                foreach (var row in CBRFDataList)
                {
                    var cabUsersList = userDataList.Where(x => x.FormID.Id == row.FormID.Id && x.CabUsersId.Id == row.Id).ToList();
                    if (cabUsersList.Count == 0)
                    {
                        var model = new CBRFData();
                        model = row;
                        finalDataList.Add(model);
                    }
                    else
                    {
                        foreach (var dataItem in cabUsersList)
                        {
                            var model = new CBRFData();
                            model = row.Clone();
                            model.UserName = dataItem.UserName;
                            model.UserContactNumber = dataItem.UserContactNumber;
                            model.Destination = dataItem.Destination;
                            model.ReportingTime = dataItem.ReportingTime;
                            model.ReportingPlaceWithAddress = dataItem.ReportingPlaceWithAddress;
                            finalDataList.Add(model);
                        }
                    }
                }

                return finalDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return finalDataList;
            }
        }

        public async Task<List<BTFData>> ViewBTFExcelData()
        {
            var BTFDataList = new List<BTFData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('BusTransportationForm')/items?$select=*,FormID/ID"
                + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var BUSResult = JsonConvert.DeserializeObject<BusTransportationFormModel>(dataText, settings);
                    BTFDataList = BUSResult.btflist.btfData;
                }
                return BTFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return BTFDataList;
            }
        }

        public async Task<List<BTFData>> ViewBTFOldExcelData()
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetBusOldReport", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.CompanyName = Convert.ToString(ds.Tables[0].Rows[i]["CompanyName"]);
                        route.EmployeeType = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeType"]);
                        route.OldEmployeeContactNo = Convert.ToString(ds.Tables[0].Rows[i]["ContactNo"]);
                        route.OldEmployeeNumber = Convert.ToString(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        route.EmployeeName = ds.Tables[0].Rows[i]["RequestFrom"].ToString();
                        route.BusinessNeed = ds.Tables[0].Rows[i]["BusinessNeeds"].ToString();
                        route.Created_Date = Convert.ToDateTime(ds.Tables[0].Rows[i]["RecievedDate"]);
                        route.TransportationRequired = Convert.ToString(ds.Tables[0].Rows[i]["TransportRequired"]);
                        route.Gender = Convert.ToString(ds.Tables[0].Rows[i]["Gender"]);
                        route.BusShift = Convert.ToString(ds.Tables[0].Rows[i]["Shift"]);
                        route.BusRouteNumber = ds.Tables[0].Rows[i]["RouteNumber"].ToString();
                        route.BusRouteName = ds.Tables[0].Rows[i]["RouteName"].ToString();
                        route.PickupPoint = ds.Tables[0].Rows[i]["PickUpPoint"].ToString();
                        route.Distance = ds.Tables[0].Rows[i]["DistancefrmResidenceToPickupPoint"].ToString();
                        route.Address = ds.Tables[0].Rows[i]["Address"].ToString();
                        route.Slab = ds.Tables[0].Rows[i]["Slab"].ToString();
                        route.Amount = ds.Tables[0].Rows[i]["SlabAmount"].ToString();
                        route.BusLocationName = ds.Tables[0].Rows[i]["LocationName"].ToString();

                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }

        public async Task<List<FormData>> GetRegionData()
        {
            List<FormData> regionList = new List<FormData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetRegionDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        FormData region = new FormData();
                        region.Region = ds.Tables[0].Rows[i]["RegionName"].ToString();
                        regionList.Add(region);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return regionList;
        }

        public async Task<List<BTFData>> GetBusFromsOldEmployeeData()
        {
            List<BTFData> routeList = new List<BTFData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_BusTransportOldEmp", con);
                cmd.Parameters.Add(new SqlParameter("@Flag", "S"));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        BTFData route = new BTFData();
                        route.EmployeeCode = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        routeList.Add(route);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return routeList;
        }

        public async Task<List<MaterialRequestData>> MaterialRequestExcelData()
        {
            var MRFDataList = new List<MaterialRequestData>();
            var materialDetailsList = new List<MaterialDetailsData>();
            var finalDataList = new List<MaterialRequestData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('MaterialRequestForm')/items?$select=*,"
           + "FormID/ID,FormID/Created&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var MRFResult = JsonConvert.DeserializeObject<MaterialRequestModel>(dataText, settings);
                    MRFDataList = MRFResult.List.MaterialRequestList;
                }

                //Material Request Details
                var clientMRF = new HttpClient(handler);
                clientMRF.BaseAddress = new Uri(conString);
                clientMRF.DefaultRequestHeaders.Accept.Clear();
                clientMRF.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var responseMaterialDetails = Task.Run(() => clientMRF.GetAsync("_api/web/lists/GetByTitle('MaterialDetails')/items?$select=*," +
                   "FormID/ID,MaterialRequestID/ID,FormID/Created&$expand=FormID,MaterialRequestID&$top=10000")).Result;

                var responseTextMaterialDetails = await responseMaterialDetails.Content.ReadAsStringAsync();
                var resultMaterialDetails = JsonConvert.DeserializeObject<MaterialDetailsModel>(responseTextMaterialDetails, settings);
                materialDetailsList = resultMaterialDetails.List.MaterialDetailsList;

                foreach (var row in MRFDataList)
                {
                    //var materialDetailList = materialDetailsList.Where(x => x.FormID.Id == row.FormID.Id && x.MaterialRequestID == row.Id).ToList();
                    var materialDetailDataList = materialDetailsList.Where(x => x.FormID.Id == row.FormID.Id && x.MaterialRequestID.Id == row.Id).Select(x => x).ToList();

                    foreach (var dataItem in materialDetailDataList)
                    {

                        var model = new MaterialRequestData();
                        model = row.Clone();
                        //finalDataList.Add(model);

                        //var modelData = new MaterialRequestData();
                        model.PartNumber = dataItem.PartNumber;
                        model.PartDescription = dataItem.PartDescription;
                        model.Remarks = dataItem.Remarks;
                        model.Quantity = dataItem.Quantity;
                        finalDataList.Add(model);


                    }
                }

                return finalDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return finalDataList;
            }
        }

        public async Task<List<GiftsInvitationData>> ViewGAIFExcelData()
        {
            var GAIFDataList = new List<GiftsInvitationData>();
            var finalDataList = new List<GiftsInvitationData>();
            var questionDataList = new List<QuestionDto>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('GiftsInvitationForm')/items?$select=*,FormID/ID"
              + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var GAIFResult = JsonConvert.DeserializeObject<GAIFModel>(dataText, settings);
                    GAIFDataList = GAIFResult.list.data;
                }

                //Question Details
                var clientGift = new HttpClient(handler);
                clientGift.BaseAddress = new Uri(conString);
                clientGift.DefaultRequestHeaders.Accept.Clear();
                clientGift.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('GiftsInvitationFormQuestionList')/items?$select=ID,QuestionId,Question," +
              "FormID/ID,QuestionId/ID,FormID/Created&$expand=FormID,QuestionId&$top=10000")).Result;

                var responseTextUser = await response.Content.ReadAsStringAsync();
                var resultGift = JsonConvert.DeserializeObject<QuestionModel>(responseTextUser);
                questionDataList = resultGift.QuestionList.data;

                foreach (var row in GAIFDataList)
                {
                    var giftUsersList = questionDataList.Where(x => x.FormIDQuestion.Id == row.FormIDGift.Id && x.QuestionId.Id == row.Id).ToList();
                    if (giftUsersList.Count == 0)
                    {
                        var model = new GiftsInvitationData();
                        model = row;
                        finalDataList.Add(model);
                    }
                    else
                    {
                        foreach (var dataItem in giftUsersList)
                        {
                            var model = new GiftsInvitationData();
                            model = row.Clone();
                            model.Question = dataItem.Question;
                            finalDataList.Add(model);
                        }
                    }
                }

                return finalDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return finalDataList;
            }
        }

        public async Task<List<NewGlobalCodeData>> ViewNGCFExcelData()
        {
            var NGCFDataList = new List<NewGlobalCodeData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('NewGlobalCodeForm')/items?$select=*,FormID/ID"
              + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var NGCFResult = JsonConvert.DeserializeObject<NewGlobalCodeModel>(dataText, settings);
                    NGCFDataList = NGCFResult.NGCFResults.NewGlobalCodeData;
                }

                return NGCFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return NGCFDataList;
            }
        }

        public async Task<List<UserRequestData>> ViewURCFExcelData()
        {
            var URCFDataList = new List<UserRequestData>();
            var finalDataList = new List<UserRequestData>();
            var appCatDataList = new List<ApplicationCategoryData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('UserRequestForm')/items?$select=*,FormID/ID"
              + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var URCFResult = JsonConvert.DeserializeObject<UserRequestModel>(dataText, settings);
                    URCFDataList = URCFResult.URCFResults.UserRequestData;
                }

                //Application Details
                var clientApp = new HttpClient(handler);
                clientApp.BaseAddress = new Uri(conString);
                clientApp.DefaultRequestHeaders.Accept.Clear();
                clientApp.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('UserRequestApplicationList')/items?$select=ID,AppId,ServiceCategory,ServiceSubCategory,Role,AccessType,Brand,ApplicationUserID," +
              "FormID/ID,AppId/ID,FormID/Created&$expand=FormID,AppId&$top=20000")).Result;

                var responseTextUser = await response.Content.ReadAsStringAsync();
                var resultUser = JsonConvert.DeserializeObject<ApplicationCategoryModel>(responseTextUser);
                appCatDataList = resultUser.ApplicationCategoryResults.data;

                foreach (var row in URCFDataList)
                {
                    var appCatUsersList = appCatDataList.Where(x => x.FormIDAppCat.Id == row.FormIDURCF.Id && x.AppCatId.Id == row.Id).ToList();
                    if (appCatUsersList.Count == 0)
                    {
                        var model = new UserRequestData();
                        model = row;
                        finalDataList.Add(model);
                    }
                    else
                    {
                        foreach (var dataItem in appCatUsersList)
                        {
                            var model = new UserRequestData();
                            model = row.Clone();
                            model.ServiceCategory = dataItem.ServiceCategory;
                            model.ServiceSubCategory = dataItem.ServiceSubCategory;
                            model.Role = dataItem.Role;
                            model.AccessType = dataItem.AccessType;
                            model.BrandApp = dataItem.BrandApp;
                            model.ApplicationUserID = dataItem.ApplicationUserID;
                            finalDataList.Add(model);
                        }
                    }
                }

                return finalDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return finalDataList;
            }
        }

        //Temp Code Approval Master Update
        //public async Task<int> UpdateApprovalMaster()
        //{
        //    try
        //    {
        //        ClientContext _context = new ClientContext(new Uri(conString));
        //        Web web = _context.Web;
        //        GlobalClass gc = new GlobalClass();
        //        var user = gc.GetCurrentUser();
        //        _context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
        //        var handler = new HttpClientHandler();
        //        handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
        //        var client = new HttpClient(handler);
        //        client.BaseAddress = new Uri(conString);
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
        //        var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ID,ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,Level,Logic,"
        //        + "FormId/Id,FormId/Created,Author/Title&$expand=FormId,Author&$top=10000")).Result;
        //        var responseText = await response.Content.ReadAsStringAsync();

        //        var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText);
        //        var result = modelData.Node.Data;
        //        List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");

        //        var st = new System.Diagnostics.Stopwatch();
        //        st.Start();

        //        //Log.Error($"{DateTime.Now} : on started");

        //        //NOT CASE
        //        var notRecord = result.Where(x => x.Logic == "NOT" && x.ApproverStatus != "Pending");
        //        foreach (var row in notRecord)
        //        {
        //            var currentItem = approvalMasterlist.GetItemById(row.Id);
        //            currentItem["IsCompleted"] = 1;
        //            currentItem.Update();
        //            _context.Load(currentItem);
        //            _context.ExecuteQuery();
        //        }

        //        //OR CASE
        //        var orRecord = result.Where(x => x.Logic == "OR");
        //        var formidGroup = orRecord.GroupBy(x => x.FormId.Id);
        //        foreach (var t in formidGroup)
        //        {
        //            var lvlGroup = t.GroupBy(x => x.Level);
        //            foreach (var items in lvlGroup)
        //            {
        //                if (items.Any(x => x.ApproverStatus == "Approved" || x.ApproverStatus == "Rejected" || x.ApproverStatus == "Enquired"))
        //                {
        //                    foreach (var row in items)
        //                    {
        //                        var currentItem = approvalMasterlist.GetItemById(row.Id);
        //                        currentItem["IsCompleted"] = 1;
        //                        currentItem.Update();
        //                        _context.Load(currentItem);
        //                        _context.ExecuteQuery();
        //                    }
        //                }
        //            }
        //        }

        //        // Log.Error($"{DateTime.Now} : on ended");
        //        st.Stop();
        //        var time = st.ElapsedMilliseconds;
        //        Log.Error($"{time} : Total Time");

        //        return 1;
        //    }
        //    catch (Exception ex)
        //    {
        //        return 0;
        //        Log.Error(ex.Message, ex);
        //    }
        //}   

        public async Task<DataModel> GetFormParentDetailsByUniqueName(string uniqueFormName)
        {
            var result = new DataModel();
            try
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var url = string.Empty;
                url = "_api/web/lists/GetByTitle('FormParent')/items?$select=Id,Department,FormName,ControllerName,ReleaseDate&$filter=(UniqueFormName " +
                    "eq '" + uniqueFormName + "' and IsComplete eq '1')";
                var response = await client.GetAsync(url);
                var responseText = await response.Content.ReadAsStringAsync();
                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;
                if (!string.IsNullOrEmpty(responseText))
                {
                    var locResult = JsonConvert.DeserializeObject<DashboardModel>(responseText);
                    result = locResult.Data;
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }


        public string GetUserNameFromAD(string approverid, UserData currentUser = null)
        {
            DirectoryEntry searchRoot = null;
            string LDAP = ConfigurationManager.AppSettings["LDAPURL"];
            if (currentUser == null)
                currentUser = user;

            string userName = "";
            try
            {
                string[] arrLDAP = LDAP.Split(',');
                for (int i = 0; i < arrLDAP.Length; i++)
                {
                    try
                    {
#if DEBUG
                        searchRoot = new DirectoryEntry(arrLDAP[i], currentUser.UserName, currentUser.Password);
#else
                        searchRoot = new DirectoryEntry(arrLDAP[i]);
#endif
                        DirectorySearcher search = new DirectorySearcher(searchRoot);
                        search.Filter = "(ObjectSid=" + approverid + ")";
                        SearchResult result = search.FindOne();
                        if (result != null)
                        {
                            userName = result.Properties["samaccountname"][0].ToString();
                        }

                        break;
                    }
                    catch { }
                }
                return userName;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return userName;
            }
            finally
            {
                searchRoot.Close();
            }
        }



        private int GetCountOfPendingRelationApproval(IEnumerable<FormData> currentLevelApprovers, FormData approver)
        {
            try
            {
                if (approver.ApproverStatus.ToLower() == "approved")
                {
                    foreach (var item in currentLevelApprovers.Where(x => x.RelationWith == approver.RelationId && x.Logic.ToLower() == "or"))
                    {
                        item.ApproverStatus = "Approved";
                    }
                }
                int count = 0;
                var approversRelated = currentLevelApprovers.Where(x => x.RelationWith == approver.RelationId);
                if (approversRelated != null && approversRelated.Count() > 0 && approversRelated.All(x => x.RelationWith != 0))
                {
                    foreach (var a in approversRelated)
                    {
                        count += GetCountOfPendingRelationApproval(currentLevelApprovers, a);
                    }
                }
                int pendingCount = 0;
                if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "and"))
                {
                    var andCount = currentLevelApprovers.Count(x => x.Logic.ToLower() == "and");
                    pendingCount += currentLevelApprovers.Count(x =>
                        (
                            x.RelationWith == approver.RelationWith && (andCount == 1 ? x.RelationId != approver.RelationId : true)
                            && x.Logic.ToLower() == "and"
                            && x.ApproverStatus.ToLower() == "pending"
                        )
                        || (x.RelationId == approver.RelationWith && approver.Logic.ToLower() == "and" && x.ApproverStatus.ToLower() == "pending")
                        || (x.RelationWith == approver.RelationId && x.Logic.ToLower() == "and" && x.ApproverStatus.ToLower() == "pending")
                    );
                }
                if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "or"))
                {
                    var orCount = currentLevelApprovers.Count(x => x.Logic.ToLower() == "or");
                    pendingCount += currentLevelApprovers.Any(x =>
                        (
                            x.RelationWith == approver.RelationWith && (orCount == 1 ? x.RelationId != approver.RelationId : true)
                            && x.Logic.ToLower() == "or"
                            && x.ApproverStatus.ToLower() == "approved"
                        )
                        || (x.RelationId == approver.RelationWith && approver.Logic.ToLower() == "or" && x.ApproverStatus.ToLower() == "approved")
                        || (x.RelationWith == approver.RelationId && x.Logic.ToLower() == "or" && x.ApproverStatus.ToLower() == "approved")
                    ) ? 0 : 1;
                }
                currentLevelApprovers.Count(x => x.ApproverStatus.ToLower() == "pending");
                count += pendingCount;
                if (pendingCount == 0)
                    approver.ApproverStatus = "Approved";//Temp making it as approved as we will check it in top level..
                return count;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return -1;
            }
        }

        private int GetCountOfPendingRelationApproval(IEnumerable<ApprovalDataModel> currentLevelApprovers, ApprovalDataModel approver)
        {
            try
            {
                int count = 0;
                var approversRelated = currentLevelApprovers.Where(x => x.RelationWith == approver.RelationId);
                if (approversRelated != null && approversRelated.Count() > 0 && approversRelated.All(x => x.RelationWith != 0))
                {
                    foreach (var a in approversRelated)
                    {
                        count += GetCountOfPendingRelationApproval(currentLevelApprovers, a);
                    }
                }
                //if (approver.RelationWith == 0)
                //{
                //    var andApprovers = currentLevelApprovers.Where(x => x.Logic.ToLower() == "and" && x.RelationWith == 0);
                //    if (andApprovers.Count() > 1)
                //    {
                //        count += andApprovers.Count(x => x.ApproverStatus.ToLower() == "pending");
                //    }
                //    var orApprovers = currentLevelApprovers.Where(x => x.Logic.ToLower() == "or" && x.RelationWith == 0);
                //    if (orApprovers.Count() > 1)
                //    {
                //        count += orApprovers.Any(x => x.ApproverStatus.ToLower() == "pending") ? 1 : 0;
                //    }
                //}
                //var mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == approver.RelationWith);
                int pendingCount = 0;
                if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "and"))
                {
                    pendingCount += currentLevelApprovers.Count(x =>
                        (
                            x.RelationWith == approver.RelationWith && x.RelationId != approver.RelationId
                            && x.Logic.ToLower() == "and"
                            && x.ApproverStatus.ToLower() == "pending"
                        )
                        || (x.RelationId == approver.RelationWith && approver.Logic.ToLower() == "and" && x.ApproverStatus.ToLower() == "pending")
                        || (x.RelationWith == approver.RelationId && approver.Logic.ToLower() == "and" && x.ApproverStatus.ToLower() == "pending")
                    );
                }
                if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "or"))
                {
                    pendingCount += currentLevelApprovers.Any(x =>
                        (
                            x.RelationWith == approver.RelationWith && x.RelationId != approver.RelationId
                            && x.Logic.ToLower() == "or"
                            && x.ApproverStatus.ToLower() == "approved"
                        )
                        || (x.RelationId == approver.RelationWith && approver.Logic.ToLower() == "or" && x.ApproverStatus.ToLower() == "approved")
                        || (x.RelationWith == approver.RelationId && approver.Logic.ToLower() == "or" && x.ApproverStatus.ToLower() == "approved")
                    ) ? 0 : 1;
                }
                count += pendingCount;
                if (pendingCount == 0)
                    approver.ApproverStatus = "Approved";//Temp making it as approved as we will check it in top level..
                return count;
                //if (approver.Logic.ToLower() == "and")
                //{
                //    pendingCount += currentLevelApprovers.Where(x =>
                //        x.RelationWith == approver.RelationWith
                //        && x.Logic.ToLower() == "and"
                //        && x.ApproverStatus.ToLower() == "pending").Count();
                //    pendingCount += currentLevelApprovers.Any(x =>
                //        x.RelationWith == approver.RelationWith
                //        && x.Logic.ToLower() == "or"
                //        && x.ApproverStatus.ToLower() == "approved"
                //    ) ? 0 : 1;
                //    if (pendingCount == 0)
                //        approver.ApproverStatus = "Approved";//Temp making it as approved as we will check it in top level..
                //}
                //else if (approver.Logic.ToLower() == "or")
                //{
                //    pendingCount += currentLevelApprovers.Any(x =>
                //        x.RelationWith == approver.RelationWith
                //        && x.Logic.ToLower() == "or"
                //        && x.ApproverStatus.ToLower() == "approved"
                //    ) ? 0 : 1;
                //    pendingCount += currentLevelApprovers.Where(x =>
                //        x.RelationWith == approver.RelationWith
                //        && x.Logic.ToLower() == "and"
                //        && x.ApproverStatus.ToLower() == "pending").Count();
                //    if (pendingCount == 0)
                //        approver.ApproverStatus = "Approved";//Temp making it as approved as we will check it in top level..
                //}

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return -1;
            }
        }

        private IEnumerable<FormData> GetRelatedOrApprover(IEnumerable<FormData> currentLevelApprovers, FormData approver)
        {
            var tempList = new List<FormData>();
            var approversRelated = currentLevelApprovers.Where(x => x.RelationWith == approver.RelationId && x.Logic.ToLower() == "or");
            if (approversRelated != null && approversRelated.Count() > 0 && approversRelated.All(x => x.RelationWith != 0))
            {
                tempList.AddRange(approversRelated);
                foreach (var a in approversRelated)
                {
                    tempList.AddRange(GetRelatedOrApprover(currentLevelApprovers, a));
                }
            }
            return tempList;
        }
        private IEnumerable<ApprovalDataModel> GetRelatedOrApprover(IEnumerable<ApprovalDataModel> currentLevelApprovers, ApprovalDataModel approver)
        {
            var tempList = new List<ApprovalDataModel>();
            var approversRelated = currentLevelApprovers.Where(x => x.RelationWith == approver.RelationId && x.Logic.ToLower() == "or");
            if (approversRelated != null && approversRelated.Count() > 0 && approversRelated.All(x => x.RelationWith != 0))
            {
                tempList.AddRange(approversRelated);
                foreach (var a in approversRelated)
                {
                    tempList.AddRange(GetRelatedOrApprover(currentLevelApprovers, a));
                }
            }
            return tempList;
        }


        public async Task<List<IDCFData>> GetIDFormListData()
        {
            List<IDCFData> list = new List<IDCFData>();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                var response = await client.GetAsync("_api/web/lists/GetByTitle('IDCardForm')/items?$select=ID,EmployeeCode," +
                    "EmployeeUserId,EmployeeName," +
                    "OtherEmployeeCode,OtherEmployeeUserId,OtherEmployeeName,OtherExternalOtherOrgName," +
                    "ExternalOrganizationName,OtherExternalOrganizationName,OnBehalfOption,BusinessNeed," +
                    "DateOfJoining,TypeOfCard,DateofIssue,IDCardNumber,Chargable," +
                    "FormID/ID,FormID/Created&$expand=FormID&top10000");

                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                if (!string.IsNullOrEmpty(responseText))
                {
                    var IDCFResult = JsonConvert.DeserializeObject<IDCFModel>(responseText, settings);
                    list = IDCFResult.idcfflist.idcfData;
                    while (!string.IsNullOrEmpty(IDCFResult.idcfflist.nextDataURL))
                    {
                        response = await client.GetAsync(IDCFResult.idcfflist.nextDataURL);

                        responseText = await response.Content.ReadAsStringAsync();
                        settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            IDCFResult = JsonConvert.DeserializeObject<IDCFModel>(responseText, settings);
                            list.AddRange(IDCFResult.idcfflist.idcfData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return list;
        }

        public async Task<List<DesktopLaptopInstallationChecklistModel>> GetDLICListData()
        {
            List<DesktopLaptopInstallationChecklistModel> list = new List<DesktopLaptopInstallationChecklistModel>();
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

                var response = await client.GetAsync("_api/web/lists/GetByTitle('DesktopLaptopInstallationCheckListForm')/items?$select=*," +
                    "FormID/ID,FormID/Created&$expand=FormID&top10000");

                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                if (!string.IsNullOrEmpty(responseText))
                {
                    var IDCFResult = JsonConvert.DeserializeObject<DLICModel>(responseText, settings);
                    list = IDCFResult.data.list;
                    while (!string.IsNullOrEmpty(IDCFResult.data.nextDataURL))
                    {
                        response = await client.GetAsync(IDCFResult.data.nextDataURL);

                        responseText = await response.Content.ReadAsStringAsync();
                        settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            IDCFResult = JsonConvert.DeserializeObject<DLICModel>(responseText, settings);
                            list.AddRange(IDCFResult.data.list);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return list;
        }

        private string GetStatusOfApproval(List<FormData> currentLevelApprovers, FormData currentApprover = null)
        {
            if (currentApprover == null)
            {
                currentApprover = new FormData();
                currentApprover.RelationId = 0;
            }
            try
            {
                string status = "";
                if (currentApprover.RelationId == 0)
                {
                    if (currentLevelApprovers.Any(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and"))
                    {
                        var relatedAndApprovers = currentLevelApprovers.Where(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and");
                        foreach (var item in relatedAndApprovers)
                        {
                            //Prevent from infinite loop
                            if (relatedAndApprovers.All(x => x.RelationId == 0))
                            {
                                var r = relatedAndApprovers.Where(x => x.ApproverStatus.ToLower() != "approved");
                                status = r != null && r.Count() > 0 ? r.FirstOrDefault().ApproverStatus : "Approved";
                            }
                            else
                                status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() != "approved")
                                return status;
                        }
                    }
                    if (currentLevelApprovers.Any(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or"))
                    {
                        var relatedOrApprovers = currentLevelApprovers.Where(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or");
                        foreach (var item in relatedOrApprovers)
                        {
                            if (relatedOrApprovers.All(x => x.RelationId == 0))
                            {
                                var r = relatedOrApprovers.Where(x => x.ApproverStatus.ToLower() != "pending");
                                status = r != null && r.Count() > 0 ? r.FirstOrDefault().ApproverStatus : "Pending";
                            }
                            else
                                status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() == "approved" || status.ToLower() == "enquired" || status.ToLower() == "rejected")
                                return status;
                        }
                    }
                    return status;
                }
                if (!string.IsNullOrEmpty(currentApprover.ApproverStatus) && currentApprover.ApproverStatus.ToLower() == "approved")
                {
                    if (currentLevelApprovers.Any(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and"))
                    {
                        foreach (var item in currentLevelApprovers.Where(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and"))
                        {
                            status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() != "approved")
                                return status;
                        }
                    }
                    return currentApprover.ApproverStatus;//Approved
                }
                else
                {
                    if (currentApprover.ApproverStatus.ToLower() != "pending")
                        return currentApprover.ApproverStatus;
                    if (currentLevelApprovers.Any(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or"))
                    {
                        foreach (var item in currentLevelApprovers.Where(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or"))
                        {
                            status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() == "approved" || status.ToLower() == "enquired" || status.ToLower() == "rejected")
                                return status;
                        }
                    }
                    return currentApprover.ApproverStatus;//Pending/Enquire/Rejected/Null/Empty
                }
            }
            catch (Exception ex)
            {
                return "Error";
            }
        }

        private string GetStatusOfApproval(List<ApprovalDataModel> currentLevelApprovers, ApprovalDataModel currentApprover = null)
        {
            if (currentApprover == null)
            {
                currentApprover = new ApprovalDataModel();
                currentApprover.RelationId = 0;
            }
            try
            {
                string status = "";
                if (currentApprover.RelationId == 0)
                {
                    if (currentLevelApprovers.Any(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and"))
                    {
                        var relatedAndApprovers = currentLevelApprovers.Where(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and");
                        foreach (var item in relatedAndApprovers)
                        {
                            //Prevent from infinite loop
                            if (relatedAndApprovers.All(x => x.RelationId == 0))
                            {
                                var r = relatedAndApprovers.Where(x => x.ApproverStatus.ToLower() != "approved");
                                status = r != null && r.Count() > 0 ? r.FirstOrDefault().ApproverStatus : "Approved";
                            }
                            else
                                status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() != "approved")
                                return status;
                        }
                    }
                    if (currentLevelApprovers.Any(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or"))
                    {
                        var relatedOrApprovers = currentLevelApprovers.Where(x => x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or");
                        foreach (var item in relatedOrApprovers)
                        {
                            if (relatedOrApprovers.All(x => x.RelationId == 0))
                            {
                                var r = relatedOrApprovers.Where(x => x.ApproverStatus.ToLower() != "pending");
                                status = r != null && r.Count() > 0 ? r.FirstOrDefault().ApproverStatus : "Pending";
                            }
                            else
                                status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() == "approved" || status.ToLower() == "enquired" || status.ToLower() == "rejected")
                                return status;
                        }
                    }
                    return status;
                }
                if (!string.IsNullOrEmpty(currentApprover.ApproverStatus) && currentApprover.ApproverStatus.ToLower() == "approved")
                {
                    if (currentLevelApprovers.Any(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and"))
                    {
                        foreach (var item in currentLevelApprovers.Where(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "and"))
                        {
                            status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() != "approved")
                                return status;
                        }
                    }
                    return currentApprover.ApproverStatus;//Approved
                }
                else
                {
                    if (currentApprover.ApproverStatus.ToLower() != "pending")
                        return currentApprover.ApproverStatus;
                    if (currentLevelApprovers.Any(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or"))
                    {
                        foreach (var item in currentLevelApprovers.Where(x => x.RelationWith.HasValue && x.RelationWith != 0 && x.RelationWith == currentApprover.RelationId && x.Logic.ToLower() == "or"))
                        {
                            status = GetStatusOfApproval(currentLevelApprovers, item);
                            if (status.ToLower() == "approved" || status.ToLower() == "enquired" || status.ToLower() == "rejected")
                                return status;
                        }
                    }
                    return currentApprover.ApproverStatus;//Pending/Enquire/Rejected/Null/Empty
                }
            }
            catch (Exception ex)
            {
                return "Error";
            }
        }

        public async Task<List<QualityMeisterbockCubingData>> ViewQMCRExcelData()
        {
            var QMCRDataList = new List<QualityMeisterbockCubingData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('QualityMeisterbockCubingList')/items?$select=*,FormID/ID"
              + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var QMCRResult = JsonConvert.DeserializeObject<QualityMeisterbockCubingModel>(dataText, settings);
                    QMCRDataList = QMCRResult.QualityMeisterbockCubingResults.QualityMeisterbockCubingData;
                }

                return QMCRDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return QMCRDataList;
            }
        }

        public async Task<List<MMRData>> ViewMMRFExcelData()
        {
            var MMRFDataList = new List<MMRData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('MMRForm')/items?$select=*,FormID/ID"
              + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var MMRFResult = JsonConvert.DeserializeObject<MMRModel>(dataText, settings);
                    MMRFDataList = MMRFResult.MMRResults.MMRData;
                }

                return MMRFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return MMRFDataList;
            }
        }

        public async Task<List<AnalysisPartsFormPresentationData>> ViewAPFPExcelData()
        {
            var APFPDataItem = new List<AnalysisPartsFormPresentationData>();
            var APFPitemList = new List<AnalysisPartsFormPresentationData>();
            var finalDataList = new List<AnalysisPartsFormPresentationModel>();
            var appCatDataList = new List<AnalysisPartsFormPresentationModel>();
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


                var response1 = await client.GetAsync("_api/web/lists/GetByTitle('AnalysisPartsFormPresentationList')/items?$select=*"
                        + "&$expand=AttachmentFiles&$top=100000");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };






                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCFUserResult = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationModel>(responseText1, settings);
                    APFPDataItem = SUCFUserResult.AnalysisPartsFormPresentationResults.AnalysisPartsFormPresentationData;

                    APFPDataItem = APFPDataItem.OrderByDescending(x => x.Id).GroupBy(x => x.FormIDId).Select(x => x.First()).ToList();
                    //For table List

                    var handler2 = new HttpClientHandler();
                    handler2.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var client2 = new HttpClient(handler2);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");


                    var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('AnalysisPartsFormDataList')/items?$select=*" +
                        "&$top=100000")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {
                        var ListResult = JsonConvert.DeserializeObject<AnalysisPartsFormPresentationModel>(responseText2, settings);
                        APFPitemList = ListResult.AnalysisPartsFormPresentationResults.AnalysisPartsFormPresentationData;
                    }

                    APFPitemList.RemoveAll(a => !APFPDataItem.Exists(b => a.ListItemIdId == Convert.ToString(b.Id)));


                }
                foreach (var rcodeObj in APFPDataItem)
                {
                    foreach (var obj in (APFPitemList.Where(t => t.ListItemIdId == Convert.ToString(rcodeObj.Id))))
                    {
                        obj.WeekNo = rcodeObj.WeekNo;
                        obj.Topic = rcodeObj.Topic;
                        obj.Department = rcodeObj.Department;
                        obj.DetailDescription = rcodeObj.DetailDescription;
                        obj.EmployeeName = rcodeObj.EmployeeName;
                        obj.EmployeeCCCode = rcodeObj.EmployeeCCCode;
                        obj.EmployeeCode = rcodeObj.EmployeeCode;
                        obj.EmployeeUserId = rcodeObj.EmployeeUserId;
                        obj.Department = rcodeObj.Department;
                        obj.EmployeeDesignation = rcodeObj.EmployeeDesignation;
                        obj.EmployeeLocation = rcodeObj.EmployeeLocation;
                        obj.EmployeeContactNo = rcodeObj.EmployeeContactNo;
                        obj.RequestSubmissionFor = rcodeObj.RequestSubmissionFor;
                        obj.EmployeeType = rcodeObj.EmployeeType;
                        obj.EmployeeDepartment = rcodeObj.EmployeeDepartment;
                        obj.OtherEmployeeName = rcodeObj.OtherEmployeeName;
                        obj.OtherEmployeeCCCode = rcodeObj.OtherEmployeeCCCode;
                        obj.OtherEmployeeDesignation = rcodeObj.OtherEmployeeDesignation;
                        obj.OtherEmployeeDepartment = rcodeObj.OtherEmployeeDepartment;
                    }
                }


                return APFPitemList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return APFPitemList;
            }
        }

        public async Task<List<IMACFormModel>> ViewIMACExcelData()
        {
            var IMACDataItem = new List<IMACFormModel>();
            var IMACitemList = new List<IMACFormModel>();
            var finalDataList = new List<IMACFormModel>();
            var appCatDataList = new List<IMACFormModel>();
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


                var response1 = await client.GetAsync("_api/web/lists/GetByTitle('IMACForm')/items?$select=*"
                        + "&$expand=AttachmentFiles&$top=100000");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };






                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCESSResult = JsonConvert.DeserializeObject<IMACModel>(responseText1, settings);
                    IMACDataItem = SUCESSResult.IMACResults.IMACFormModel;

                    IMACDataItem = IMACDataItem.OrderByDescending(x => x.ID).GroupBy(x => x.FormIDId).Select(x => x.First()).ToList();
                    //For table List

                    var handler2 = new HttpClientHandler();
                    handler2.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var client2 = new HttpClient(handler2);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");


                    var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('IMACDataList')/items?$select=*" +
                        "&$top=100000")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {
                        var ListResult = JsonConvert.DeserializeObject<IMACModel>(responseText2, settings);
                        IMACitemList = ListResult.IMACResults.IMACFormModel;
                    }

                    //IMACitemList.RemoveAll(a => !IMACDataItem.Exists(b => a.ListItemIdId == Convert.ToString(b.Id)));
                    IMACitemList.RemoveAll(a => !IMACDataItem.Exists(b => Convert.ToString(a.FormId) == b.FormIDId));


                }
                foreach (var rcodeObj in IMACDataItem)
                {
                    foreach (var obj in (IMACitemList.Where(t => Convert.ToString(t.FormId) == rcodeObj.FormIDId)))
                    {
                        obj.IMACtype = rcodeObj.IMACtype;

                        obj.BusinessJustification = rcodeObj.BusinessJustification;

                        obj.EmployeeName = rcodeObj.EmployeeName;
                        obj.EmployeeCCCode = rcodeObj.EmployeeCCCode;
                        obj.EmployeeCode = rcodeObj.EmployeeCode;
                        obj.EmployeeUserId = rcodeObj.EmployeeUserId;

                        obj.EmployeeDesignation = rcodeObj.EmployeeDesignation;
                        obj.EmployeeLocation = rcodeObj.EmployeeLocation;
                        obj.EmployeeContactNo = rcodeObj.EmployeeContactNo;
                        obj.RequestSubmissionFor = rcodeObj.RequestSubmissionFor;
                        obj.EmployeeType = rcodeObj.EmployeeType;
                        obj.EmployeeDepartment = rcodeObj.EmployeeDepartment;
                        obj.OtherEmployeeName = rcodeObj.OtherEmployeeName;
                        obj.OtherEmployeeCCCode = rcodeObj.OtherEmployeeCCCode;
                        obj.OtherEmployeeDesignation = rcodeObj.OtherEmployeeDesignation;
                        obj.OtherEmployeeDepartment = rcodeObj.OtherEmployeeDepartment;
                    }
                }


                return IMACitemList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return IMACitemList;
            }
        }

        public async Task<List<EQSAccessmModelData>> ViewEQSAExcelData()
        {
            var IMACDataItem = new List<EQSAccessmModelData>();
            var IMACitemList = new List<EQSAccessmModelData>();
            var finalDataList = new List<EQSAccessmModelData>();
            var appCatDataList = new List<EQSAccessmModelData>();
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


                var response1 = await client.GetAsync("_api/web/lists/GetByTitle('EQSAccess')/items?$select=*"
                        + "&$expand=AttachmentFiles&$top=100000");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };






                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCESSResult = JsonConvert.DeserializeObject<EQSAccessmModel>(responseText1, settings);
                    IMACDataItem = SUCESSResult.EQSAccessmModelResults.EQSAccessmModelData;


                    IMACDataItem = IMACDataItem.OrderByDescending(x => x.Id).GroupBy(x => x.FormIDId).Select(x => x.First()).ToList();
                    //For table List

                    var handler2 = new HttpClientHandler();
                    handler2.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var client2 = new HttpClient(handler2);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");


                    var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('EQSAccessTableData')/items?$select=*" +
                        "&$top=100000")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {
                        var ListResult = JsonConvert.DeserializeObject<EQSAccessmModel>(responseText2, settings);
                        IMACitemList = ListResult.EQSAccessmModelResults.EQSAccessmModelData;

                    }

                    //IMACitemList.RemoveAll(a => !IMACDataItem.Exists(b => a.ListItemIdId == Convert.ToString(b.Id)));
                    IMACitemList.RemoveAll(a => !IMACDataItem.Exists(b => a.FormIDId == b.Id));


                }
                foreach (var rcodeObj in IMACDataItem)
                {
                    foreach (var obj in (IMACitemList.Where(t => t.FormIDId == rcodeObj.Id)))
                    {
                        obj.RequestType = rcodeObj.RequestType;
                        obj.BusinessJustification = rcodeObj.BusinessJustification;
                        //obj.EmployeeName = rcodeObj.EmployeeName;
                        //obj.EmployeeID = rcodeObj.EmployeeID;
                        //obj.LogicCardID = rcodeObj.LogicCardID;
                        //obj.StationName = rcodeObj.StationName;
                        //obj.Shop = rcodeObj.Shop;
                        //obj.AccessGroup = rcodeObj.AccessGroup;
                        obj.FormIDId = rcodeObj.FormIDId;

                        obj.EmployeeCCCode = rcodeObj.EmployeeCCCode;
                        obj.EmployeeCode = rcodeObj.EmployeeCode;
                        obj.EmployeeUserId = rcodeObj.EmployeeUserId;

                        obj.EmployeeDesignation = rcodeObj.EmployeeDesignation;
                        obj.EmployeeLocation = rcodeObj.EmployeeLocation;
                        obj.EmployeeContactNo = rcodeObj.EmployeeContactNo;
                        obj.RequestSubmissionFor = rcodeObj.RequestSubmissionFor;
                        obj.EmployeeType = rcodeObj.EmployeeType;
                        obj.EmployeeDepartment = rcodeObj.EmployeeDepartment;
                        obj.OtherEmployeeName = rcodeObj.OtherEmployeeName;
                        obj.OtherEmployeeCCCode = rcodeObj.OtherEmployeeCCCode;
                        obj.OtherEmployeeDesignation = rcodeObj.OtherEmployeeDesignation;
                        obj.OtherEmployeeDepartment = rcodeObj.OtherEmployeeDepartment;
                    }
                }


                return IMACitemList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return IMACitemList;
            }
        }
        public async Task<List<FixtureRequisitionData>> ViewQFRFExcelData()
        {
            var QFRFDataList = new List<FixtureRequisitionData>();
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

                var data = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('FixtureRequisitionForm')/items?$select=*,FormID/ID"
              + "&$expand=FormID&$top=10000")).Result;

                var dataText = await data.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(dataText))
                {
                    var QFRFResult = JsonConvert.DeserializeObject<FixtureRequisitionModel>(dataText, settings);
                    QFRFDataList = QFRFResult.FixtureRequisitionResults.FixtureRequisitionData;
                }

                return QFRFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return QFRFDataList;
            }
        }


        public async Task<List<IPAFData>> ViewIPAFExcelData()
        {
            var IPAFDataItem = new List<IPAFData>();
            var IPAFitemList = new List<IPAFData>();
            var finalDataList = new List<IPAFData>();
            var appCatDataList = new List<IPAFData>();
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


                var response1 = await client.GetAsync("_api/web/lists/GetByTitle('IPAFForm')/items?$select=*"
                        + "&$expand=AttachmentFiles&$top=100000");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };






                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCESSResult = JsonConvert.DeserializeObject<IPAFModel>(responseText1, settings);
                    IPAFDataItem = SUCESSResult.IPAFResults.IPAFData;

                    IPAFDataItem = IPAFDataItem.OrderByDescending(x => x.Id).GroupBy(x => x.FormIDId).Select(x => x.First()).ToList();
                    //For table List

                    var handler2 = new HttpClientHandler();
                    handler2.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var client2 = new HttpClient(handler2);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");


                    var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('IPAFFormDataList')/items?$select=*" +
                        "&$top=100000")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {
                        var ListResult = JsonConvert.DeserializeObject<IPAFModel>(responseText2, settings);
                        IPAFitemList = ListResult.IPAFResults.IPAFData;
                    }

                    //IMACitemList.RemoveAll(a => !IMACDataItem.Exists(b => a.ListItemIdId == Convert.ToString(b.Id)));
                    IPAFitemList.RemoveAll(a => !IPAFDataItem.Exists(b => a.Id == b.Id));


                }
                foreach (var rcodeObj in IPAFDataItem)
                {
                    foreach (var obj in (IPAFitemList.Where(t => t.Id == rcodeObj.Id)))
                    {
                        obj.FormIDId = rcodeObj.FormIDId;
                        obj.RequestType = rcodeObj.RequestType;
                        obj.Applicationname = rcodeObj.Applicationname;
                        obj.Applicationurl = rcodeObj.Applicationurl;
                        obj.Applicationaccess = rcodeObj.Applicationaccess;
                        obj.Accessgroup = rcodeObj.Accessgroup;
                        obj.RequestFromDate = rcodeObj.RequestFromDate;
                        obj.RequestToDate = rcodeObj.RequestToDate;
                        obj.BusinessJustification = rcodeObj.BusinessJustification;
                        obj.EmployeeName = rcodeObj.EmployeeName;
                        obj.EmployeeCCCode = rcodeObj.EmployeeCCCode;
                        obj.EmployeeCode = rcodeObj.EmployeeCode;
                        obj.EmployeeUserId = rcodeObj.EmployeeUserId;
                        obj.EmployeeDesignation = rcodeObj.EmployeeDesignation;
                        obj.EmployeeLocation = rcodeObj.EmployeeLocation;
                        obj.EmployeeContactNo = rcodeObj.EmployeeContactNo;
                        obj.RequestSubmissionFor = rcodeObj.RequestSubmissionFor;
                        obj.EmployeeType = rcodeObj.EmployeeType;
                        obj.EmployeeDepartment = rcodeObj.EmployeeDepartment;
                        obj.OtherEmployeeName = rcodeObj.OtherEmployeeName;
                        obj.OtherEmployeeCCCode = rcodeObj.OtherEmployeeCCCode;
                        obj.OtherEmployeeDesignation = rcodeObj.OtherEmployeeDesignation;
                        obj.OtherEmployeeDepartment = rcodeObj.OtherEmployeeDepartment;
                    }
                }


                return IPAFitemList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return IPAFitemList;
            }
        }
        public async Task<int> ResubmitUpdateForSQL(int formId)
        {
            try
            {
                #region Comment
                //  ClientContext _context = new ClientContext(new Uri(conString));
                //  Web web = _context.Web;
                //  GlobalClass gc = new GlobalClass();
                //  var user = gc.GetCurrentUser();
                //  _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //  var handler = new HttpClientHandler();
                //  handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //  var client = new HttpClient(handler);
                //  client.BaseAddress = new Uri(conString);
                //  client.DefaultRequestHeaders.Accept.Clear();
                //  client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //  var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ID,ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,Level,Logic,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(FormId eq '" + formId + "' and IsActive eq 1)&$expand=FormId,Author");
                //  var responseText = await response.Content.ReadAsStringAsync();
                //  var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText);
                //  var result = modelData.Node.Data;

                //  List approvalMasterlist = web.Lists.GetByTitle("ApprovalMaster");
                //  foreach (var row in result)
                //  {
                //      var currentItem = approvalMasterlist.GetItemById(row.Id);
                //      //currentItem.RefreshLoad();
                //      //_context.ExecuteQuery();
                //      currentItem["IsActive"] = 0;
                //      currentItem.Update();
                //      _context.Load(currentItem);
                //      _context.ExecuteQuery();
                //  }
                #endregion
                try
                {
                    int Status = 0;
                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataSet ds = new DataSet();
                    List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("sp_USPUpdateApprovalMasterStatus", con);
                    cmd.Parameters.Add(new SqlParameter("@formId", formId));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(ds);
                    con.Close();
                    if (ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            Status = Convert.ToInt32(ds.Tables[0].Rows[i]["Status"]);
                        }
                    }
                    return Status;
                }
                catch (Exception)
                {

                    return 0;
                }


            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public async Task<int> SaveResponseFromMail(string response, int appRowId, string comment, int approvalType, string UserName, string UserFullname)
        {

            int formId = 0;
            int approverLevel = 0;
            int rowId = 0;

            try
            {
                DashboardModel dashboardModel = new DashboardModel();
                DataModel dataModel = new DataModel();
                List<FormData> dataList = new List<FormData>();
                List<FormData> approverIdList = new List<FormData>();
                FormData formData = new FormData();

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();

                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_SaveResponseAppRowId", con);
                cmd.Parameters.Add(new SqlParameter("@appRowId", appRowId));
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
                        FormLookup item = new FormLookup();
                        item.Id = Convert.ToInt32(dt.Rows[i]["FormID"]);
                        formData.FormRelation = item;
                        formData.Id = Convert.ToInt32(dt.Rows[i]["ID"]);
                        formData.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                        formData.AuthorityToEdit = Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                        formData.ApproverId = Convert.ToInt32(dt.Rows[i]["ApproverId"]);
                        formData.IsActive = Convert.ToInt32(dt.Rows[i]["IsActive"]);
                        formData.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
                        formData.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                        formData.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                        formData.Department = Convert.ToString(dt.Rows[i]["Department"]);
                        formData.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                        formData.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                        formData.NextApproverId = Convert.ToInt32(dt.Rows[i]["Level"]);
                        formData.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Modified"]);
                        formData.ApproverUserName = Convert.ToString(dt.Rows[i]["ApproverUserName"]);
                        formData.RelationWith = Convert.ToInt32(dt.Rows[i]["RelationWith"]);
                        formData.RelationId = Convert.ToInt32(dt.Rows[i]["RelationId"]);
                        dataList.Add(formData);

                    }
                }

                dashboardModel.Data = dataModel;
                dataModel.Forms = dataList;

                if (dataList[0].RunWorkflow.ToLower() == "no")
                {
                    rowId = dataList[0].RowId;
                    formId = dataList[0].FormRelation.Id;


                    DashboardModel dashboardModels = new DashboardModel();
                    DataModel DataModel1 = new DataModel();
                    List<FormData> DataList = new List<FormData>();
                    List<FormData> ApproverIdList = new List<FormData>();


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();

                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_SaveResponseforAprroval", con);
                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData formData1 = new FormData();
                            formData1.Id = Convert.ToInt32(dt1.Rows[i]["ID"]);
                            formData1.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
                            formData1.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
                            formData1.ApproverId = Convert.ToInt32(dt1.Rows[i]["ApproverId"]);
                            formData1.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
                            formData1.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                            formData1.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
                            formData1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            formData1.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);
                            formData1.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
                            formData1.NextApproverId = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            formData1.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
                            formData1.RelationWith = Convert.ToInt32(dt1.Rows[i]["RelationWith"]);
                            formData1.RelationId = Convert.ToInt32(dt1.Rows[i]["RelationId"]);
                            formData1.Comment = Convert.ToString(dt1.Rows[i]["Comment"]);
                            ApproverIdList.Add(formData1);

                        }
                    }
                    dashboardModels.Data = DataModel1;
                    DataModel1.Forms = ApproverIdList;

                    var currentLevelApprovers = ApproverIdList.Where(x => x.IsActive == 1); // Add OR Condn 
                    var currentApprover = currentLevelApprovers.Where(x => x.ApproverUserName == UserName).FirstOrDefault();

                    var currentLevel = currentApprover.Level;
                    approverLevel = currentLevel.Value;
                    var minLevel = ApproverIdList.Min(x => x.Level);
                    var maxLevel = ApproverIdList.Max(x => x.Level);
                    var nextLevelApprovers = ApproverIdList.Where(x => x.IsActive == 0 && x.Level == currentLevel + 1);

                    switch (currentApprover.Logic)
                    {
                        case "NOT":
                            {
                                if (response == "Approved")
                                {
                                    SqlCommand cmd_A = new SqlCommand();
                                    SqlDataAdapter adapter_A = new SqlDataAdapter();
                                    con = new SqlConnection(sqlConString);
                                    cmd_A = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    cmd_A.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
                                    cmd_A.Parameters.Add(new SqlParameter("@IsActive", 10));
                                    cmd_A.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                    cmd_A.Parameters.Add(new SqlParameter("@Comment", comment));
                                    cmd_A.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                    cmd_A.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                    cmd_A.CommandType = CommandType.StoredProcedure;
                                    adapter_A.SelectCommand = cmd_A;
                                    con.Open();
                                    adapter_A.Fill(dt1);
                                    con.Close();



                                    foreach (var nextApprover in nextLevelApprovers)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }

                                    //Forms List Update
                                    if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
                                    {
                                        //update in formstatus as approved

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();


                                    }
                                    else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
                                    {
                                        //update in formstatus as approved
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && approverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                    {
                                        //update in formstatus as approved
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                    {
                                        //update in formstatus as approved
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    else if (currentApprover.Level == minLevel)
                                    {
                                        //update in formstatus as initiliazed
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();

                                    }
                                }
                                if (response == "Enquired")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == UserName && x.IsActive == 1);
                                    //foreach (var ail in ApproverIdList)
                                    //{

                                    //    con = new SqlConnection(sqlConString);
                                    //    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    //    cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                    //    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                    //    if (ail.ApproverUserName == user.UserName && ail.IsActive == 1)
                                    //    {
                                    //        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", currentUser.Id != ail.Id ? "Pending" : response));
                                    //        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //    }
                                    //    else
                                    //    {
                                    //        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", currentUser.Id != ail.Id ? "Pending" : response));
                                    //        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //    }

                                    //    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                                    //    cmd1.CommandType = CommandType.StoredProcedure;
                                    //    adapter1.SelectCommand = cmd1;
                                    //    con.Open();
                                    //    adapter1.Fill(dt1);
                                    //    con.Close();

                                    //}

                                    if (currentUser != null)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }

                                    foreach (var ail in ApproverIdList)
                                    {

                                        if (currentUser.Id != ail.Id)
                                        {
                                            object Comment = DBNull.Value;
                                            if (ail.Comment != null)
                                                Comment = ail.Comment;

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }

                                    }
                                    //con = new SqlConnection(sqlConString);
                                    //cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    //cmd1.Parameters.Add(new SqlParameter("@Id", ApproverIdList[0].Id));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(1)));
                                    //cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ApproverIdList[0].ApproverStatus));
                                    //cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                                    //cmd1.CommandType = CommandType.StoredProcedure;
                                    //adapter1.SelectCommand = cmd1;
                                    //con.Open();
                                    //adapter1.Fill(dt1);
                                    //con.Close();

                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                    //Approval Master New Record added after Enquired
                                    object submitterUserName = DBNull.Value;
                                    object businessNeed = DBNull.Value;
                                    DataTable DT = new DataTable();
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("GetFormDataByFormId", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(DT);
                                    con.Close();

                                    if (DT.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < DT.Rows.Count; i++)
                                        {

                                            submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                            businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                                        }
                                    }

                                    int submitterId = 0;

                                    //if (submitterUserName != null)
                                    //    submitterUserName = dataList[0].SubmitterUserName;
                                    //if (businessNeed != null)
                                    //    businessNeed = dataList[0].BusinessNeed;//submitterUserName = dataList[0].SubmitterUserName;
                                    //businessNeed = dataList[0].BusinessNeed;

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                                    cmd1.Parameters.Add(new SqlParameter("@Department", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                                    cmd1.Parameters.Add(new SqlParameter("@Email", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                                    cmd1.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();




                                    //List approvalMasterlist = _context.Web.Lists.GetByTitle("ApprovalMaster");
                                    //approvalMasterlist.RefreshLoad();
                                    //_context.ExecuteQuery();
                                    //ListItemCreationInformation approvalMasteritemCreated = new ListItemCreationInformation();
                                    //ListItem approvalMasteritem = approvalMasterlist.AddItem(approvalMasteritemCreated);
                                    //approvalMasteritem["FormId"] = formId;
                                    //approvalMasteritem["RowId"] = rowId;
                                    //approvalMasteritem["ApproverUserName"] = submitterUserName;
                                    //approvalMasteritem["IsActive"] = 1;
                                    //approvalMasteritem["NextApproverId"] = 0;
                                    //approvalMasteritem["ApproverStatus"] = "Enquired";
                                    //approvalMasteritem["RunWorkflow"] = "No";
                                    //approvalMasteritem["BusinessNeed"] = businessNeed;
                                    //approvalMasteritem.Update();
                                    //_context.Load(approvalMasteritem);
                                    //_context.ExecuteQuery();

                                }
                                if (response == "Rejected")
                                {
                                    foreach (var ail in ApproverIdList)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        if (ail.ApproverUserName == UserName && ail.IsActive == 1)
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        }
                                        else
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus == "Pending" ? "Rejected by " + UserFullname : ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", ail.ApproverStatus == "Pending" ? "" : ail.Comment));
                                        }

                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();


                                    }
                                    //Forms List Update
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                break;
                            }
                        case "OR":
                            {
                                if (response == "Approved")
                                {
                                    //0 means No Assistant
                                    List<FormData> groupedList = new List<FormData>();
                                    int countPendingApprovers = 0;
                                    var mainApprover = currentApprover;
                                    currentApprover.ApproverStatus = "Approved";
                                    while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
                                    {
                                        mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
                                    }
                                    if (mainApprover.RelationWith != null)
                                    {
                                        countPendingApprovers += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
                                    }
                                    if (countPendingApprovers == -1)
                                    {
                                        return -1;
                                    }
                                    if (currentLevelApprovers.Any(x => x.Logic.ToLower() == "and"))
                                    {

                                        currentApprover.ApproverStatus = response;
                                        groupedList.AddRange(
                                            GetRelatedOrApprover(currentLevelApprovers, mainApprover)
                                        );
                                        groupedList.Add(mainApprover);

                                    }
                                    else
                                    {
                                        groupedList.AddRange(currentLevelApprovers);
                                    }

                                    foreach (var cla in groupedList)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", cla.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        if (cla.ApproverUserName == UserName && cla.IsActive == 1)
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        }
                                        else
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Approved by " + UserFullname));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        }

                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();

                                    }

                                    if (countPendingApprovers < 1)
                                    {
                                        foreach (var nextApprover in nextLevelApprovers)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                        //Forms List Update
                                        if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
                                        {
                                            //update in formstatus as approved

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        //I guess yeh condn yaha nahi aayega?
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        //I guess yeh condn yaha nahi aayega?
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == minLevel)
                                        {
                                            //update in formstatus as initiliazed
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                    }
                                }
                                if (response == "Enquired")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();

                                        //ListItem currentItem = list.GetItemById(currentUser.Id);
                                        //currentItem["ApproverStatus"] = response;
                                        //currentItem["Comment"] = comment;
                                        //currentItem.Update();
                                        //_context.Load(currentItem);
                                        //_context.ExecuteQuery();
                                    }
                                    foreach (var ail in ApproverIdList)
                                    {

                                        if (currentUser.Id != ail.Id)
                                        {
                                            object Comment = DBNull.Value;
                                            if (ail.Comment != null)
                                                Comment = ail.Comment;

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }

                                    }
                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                    //Approval Master New Record added after Enquired
                                    int submitterId = 0;
                                    object submitterUserName = DBNull.Value;
                                    object businessNeed = DBNull.Value;
                                    DataTable DT = new DataTable();
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("GetFormDataByFormId", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(DT);
                                    con.Close();

                                    if (DT.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < DT.Rows.Count; i++)
                                        {

                                            submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                            businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                                        }
                                    }
                                    //if (submitterUserName != null)
                                    //    submitterUserName = dataList[0].SubmitterUserName;
                                    //if (businessNeed != null)
                                    //    businessNeed = dataList[0].BusinessNeed;//submitterUserName = dataList[0].SubmitterUserName;
                                    //businessNeed = dataList[0].BusinessNeed;

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                                    cmd1.Parameters.Add(new SqlParameter("@Department", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                                    cmd1.Parameters.Add(new SqlParameter("@Email", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                                    cmd1.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();


                                }
                                if (response == "Rejected")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        if (currentUser.ApproverUserName != UserName)
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Rejected by " + UserFullname));
                                        }
                                        else
                                        {
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        }

                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }
                                    foreach (var ail in ApproverIdList)
                                    {
                                        if (currentUser.Id != ail.Id)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            if (ail.ApproverUserName != UserName)
                                            {
                                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus == "Pending" ? "Rejected by " + UserFullname : ail.ApproverStatus));
                                            }
                                            else
                                            {
                                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            }

                                            cmd1.Parameters.Add(new SqlParameter("@Comment", ail.ApproverStatus == "Pending" ? "" : ail.Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();

                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                    }
                                    //Forms List Update
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                break;
                            }

                        case "AND":
                            {
                                if (response == "Approved")
                                {
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                    cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();



                                    //var pendingStatusCount = currentLevelApprovers.ToList().Count();
                                    var mainApprover = currentApprover;
                                    currentApprover.ApproverStatus = "Approved";
                                    while (mainApprover.RelationWith != null && mainApprover.RelationWith != 0)
                                    {
                                        mainApprover = currentLevelApprovers.FirstOrDefault(x => x.RelationId == mainApprover.RelationWith);
                                    }
                                    int pendingStatusCount = 0;
                                    if (mainApprover.RelationWith != null)
                                    {
                                        //pendingStatusCount = GetCountOfPendingRelationApproval(currentLevelApprovers, mainApprover);
                                        pendingStatusCount += GetStatusOfApproval(currentLevelApprovers.ToList(), mainApprover).ToLower() == "pending" ? 1 : 0;
                                    }

                                    //OR Condn code
                                    var orCondnApprovers = currentLevelApprovers.Where(x => x.Logic.ToLower() == "or").ToList();
                                    if (orCondnApprovers != null && orCondnApprovers.Count() > 0)
                                    {
                                        //Assuming AND case will not be a assist. but can be approver
                                        List<FormData> groupedList = new List<FormData>();
                                        var AppAssistList = GetRelatedOrApprover(currentLevelApprovers, mainApprover);
                                        if (AppAssistList != null && AppAssistList.Count() > 0)
                                        {
                                            groupedList.AddRange(AppAssistList);
                                        }
                                        foreach (var item in AppAssistList)
                                        {
                                            item.ApproverStatus = "Approved";//Temp Making all current approver assist as approved
                                        }
                                        foreach (var cla in groupedList)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", cla.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();


                                            //Remove Current approver and Its Assist as it is currently approved...
                                            orCondnApprovers.Remove(cla);
                                        }

                                        if (pendingStatusCount == -1)
                                        {
                                            return -1;
                                        }

                                    }

                                    if (pendingStatusCount == 0)
                                    {
                                        foreach (var nextApprover in nextLevelApprovers)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrixNextApprover", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();


                                        }
                                    }

                                    if (pendingStatusCount == 0)
                                    {
                                        //Forms List Update
                                        if (currentApprover.Level == maxLevel && ApproverIdList.Where(x => x.Level == maxLevel).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "OR")
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "AND" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == maxLevel && currentApprover.Logic == "NOT" && ApproverIdList.Where(x => x.IsActive == 1).Count() == 1)
                                        {
                                            //update in formstatus as approved
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }
                                        else if (currentApprover.Level == minLevel)
                                        {
                                            //update in formstatus as initiliazed
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                    }
                                }
                                if (response == "Enquired")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {
                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }


                                    //con = new SqlConnection(sqlConString);
                                    //cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                    //cmd1.Parameters.Add(new SqlParameter("@Id", ApproverIdList[0].Id));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(1)));
                                    //cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ApproverIdList[0].ApproverStatus));
                                    //cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                    //cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));

                                    //cmd1.CommandType = CommandType.StoredProcedure;
                                    //adapter1.SelectCommand = cmd1;
                                    //con.Open();
                                    //adapter1.Fill(dt1);
                                    //con.Close();

                                    foreach (var ail in ApproverIdList)
                                    {

                                        if (currentUser.Id != ail.Id)
                                        {
                                            object Comment = DBNull.Value;
                                            if (ail.Comment != null)
                                                Comment = ail.Comment;

                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();
                                        }

                                    }
                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();



                                    //Approval Master New Record added after Enquired
                                    int submitterId = 0;

                                    object submitterUserName = DBNull.Value;
                                    object businessNeed = DBNull.Value;
                                    //if (dataList[0].SubmitterUserName != null)
                                    //    submitterUserName = dataList[0].SubmitterUserName;
                                    //if (dataList[0].BusinessNeed != null)
                                    //    businessNeed = dataList[0].BusinessNeed;
                                    DataTable DT = new DataTable();
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("GetFormDataByFormId", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(DT);
                                    con.Close();

                                    if (DT.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < DT.Rows.Count; i++)
                                        {

                                            submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                            businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                                        }
                                    }
                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_SaveApproverDetails", con);
                                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                                    cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                                    cmd1.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                                    cmd1.Parameters.Add(new SqlParameter("@Department", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ControllerName", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                                    cmd1.Parameters.Add(new SqlParameter("@Email", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                                    cmd1.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@Logic", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@Designation", ""));
                                    cmd1.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                                    cmd1.Parameters.Add(new SqlParameter("@RunWorkflow", "No"));
                                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                if (response == "Rejected")
                                {
                                    var currentUser = ApproverIdList.Find(x => x.ApproverUserName == UserName && x.IsActive == 1);
                                    if (currentUser != null)
                                    {

                                        con = new SqlConnection(sqlConString);
                                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                        cmd1.Parameters.Add(new SqlParameter("@Id", currentUser.Id));
                                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        adapter1.SelectCommand = cmd1;
                                        con.Open();
                                        adapter1.Fill(dt1);
                                        con.Close();
                                    }

                                    foreach (var ail in ApproverIdList)
                                    {
                                        if (currentUser.Id != ail.Id)
                                        {
                                            con = new SqlConnection(sqlConString);
                                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", ail.ApproverStatus == "Pending" ? response : ail.ApproverStatus));
                                            cmd1.Parameters.Add(new SqlParameter("@Comment", ail.ApproverStatus == "Pending" ? "" : ail.Comment));
                                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                                            cmd1.CommandType = CommandType.StoredProcedure;
                                            adapter1.SelectCommand = cmd1;
                                            con.Open();
                                            adapter1.Fill(dt1);
                                            con.Close();

                                        }
                                    }

                                    //Forms List Update

                                    con = new SqlConnection(sqlConString);
                                    cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                                    cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                                    cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    adapter1.SelectCommand = cmd1;
                                    con.Open();
                                    adapter1.Fill(dt1);
                                    con.Close();

                                }
                                break;
                            }
                    }
                }
                else if (dataList[0].RunWorkflow.ToLower() == "yes")
                {

                    DashboardModel dashboardModels = new DashboardModel();
                    DataModel DataModel1 = new DataModel();
                    List<FormData> DataList = new List<FormData>();
                    List<FormData> ApproverIdList = new List<FormData>();


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();

                    rowId = dataList[0].RowId;
                    formId = dataList[0].FormRelation.Id;

                    DataTable dt1 = new DataTable();
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_SaveResponseforAprroval", con);
                    cmd1.Parameters.Add(new SqlParameter("@RowId", rowId));
                    cmd1.Parameters.Add(new SqlParameter("@FormID", formId));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData formData1 = new FormData();
                            formData1.Id = Convert.ToInt32(dt1.Rows[i]["ID"]);
                            formData1.ApprovalType = Convert.ToString(dt1.Rows[i]["ApprovalType"]);
                            formData1.AuthorityToEdit = Convert.ToInt32(dt1.Rows[i]["AuthorityToEdit"]);
                            formData1.ApproverId = Convert.ToInt32(dt1.Rows[i]["ApproverId"]);
                            formData1.IsActive = Convert.ToInt32(dt1.Rows[i]["IsActive"]);
                            formData1.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                            formData1.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
                            formData1.Department = Convert.ToString(dt1.Rows[i]["Department"]);
                            formData1.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);
                            formData1.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
                            formData1.NextApproverId = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            formData1.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Modified"]);
                            formData1.ApproverUserName = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);
                            formData1.RelationWith = Convert.ToInt32(dt1.Rows[i]["RelationWith"]);
                            formData1.RelationId = Convert.ToInt32(dt1.Rows[i]["RelationId"]);
                            approverIdList.Add(formData1);

                        }
                    }
                    dashboardModels.Data = DataModel1;
                    DataModel1.Forms = ApproverIdList;

                    var currentLevelApprovers = approverIdList.Where(x => x.IsActive == 1);
                    var currentApprover = currentLevelApprovers.Where(x => x.ApproverUserName == UserName).FirstOrDefault();
                    var currentLevel = currentApprover.Level;
                    approverLevel = currentLevel.Value;
                    var minLevel = approverIdList.Min(x => x.Level);
                    var maxLevel = approverIdList.Max(x => x.Level);
                    var nextLevelApprovers = approverIdList.Where(x => x.IsActive == 0 && x.Level == currentLevel + 1);

                    if (response == "Approved")
                    {
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", currentApprover.Id));
                        cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                        cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                        cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                        cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                        cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(dt1);
                        con.Close();



                        foreach (var nextApprover in nextLevelApprovers)
                        {
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", nextApprover.Id));
                            cmd1.Parameters.Add(new SqlParameter("@IsActive", 1));
                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }

                        //Forms List Update
                        if (currentApprover.Level == maxLevel)
                        {
                            //update in formstatus as approved
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Approved"));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();
                        }
                        else if (currentApprover.Level == minLevel)
                        {
                            //update in formstatus as initiliazed

                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                            cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Initiated"));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }
                    }

                    if (response == "Enquired")
                    {
                        foreach (var ail in approverIdList)
                        {

                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                            cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                            cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }
                        //Forms List Update

                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Enquired"));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(dt1);
                        con.Close();

                        //Approval Master New Record added after Enquired
                        int submitterId = 0;

                        object submitterUserName = DBNull.Value;
                        object businessNeed = DBNull.Value;
                        //if (dataList[0].SubmitterUserName != null)
                        //    submitterUserName = dataList[0].SubmitterUserName;
                        //if (dataList[0].BusinessNeed != null)
                        //    businessNeed = dataList[0].BusinessNeed;

                        DataTable DT = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("GetFormDataByFormId", con);
                        cmd1.Parameters.Add(new SqlParameter("@FormId", Convert.ToInt64(formId)));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(DT);
                        con.Close();

                        if (DT.Rows.Count > 0)
                        {
                            for (int i = 0; i < DT.Rows.Count; i++)
                            {

                                submitterUserName = Convert.ToString(DT.Rows[i]["SubmitterUserName"]);
                                businessNeed = Convert.ToString(DT.Rows[i]["BusinessNeed"]);

                            }
                        }

                        DataTable dtResult = new DataTable();
                        SqlCommand cmd2 = new SqlCommand();
                        SqlDataAdapter adapter2 = new SqlDataAdapter();
                        con = new SqlConnection(sqlConString);
                        cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
                        cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
                        cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
                        cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                        cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                        cmd2.Parameters.Add(new SqlParameter("@Department", ""));
                        cmd2.Parameters.Add(new SqlParameter("@FormParentId", 0));
                        cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@CreatedBy", submitterUserName));
                        cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                        cmd2.Parameters.Add(new SqlParameter("@Email", ""));
                        cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", businessNeed));
                        cmd2.Parameters.Add(new SqlParameter("@Level", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
                        cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", submitterUserName));
                        cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverName", submitterUserName));


                        cmd2.CommandType = CommandType.StoredProcedure;
                        adapter2.SelectCommand = cmd2;
                        con.Open();
                        adapter2.Fill(dtResult);
                        con.Close();


                    }
                    if (response == "Rejected")
                    {
                        foreach (var ail in approverIdList)
                        {
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_UpdateApprovalMatrix", con);
                            cmd1.Parameters.Add(new SqlParameter("@Id", ail.Id));
                            cmd1.Parameters.Add(new SqlParameter("@IsActive", Convert.ToInt32(0)));
                            if (ail.ApproverUserName == UserName && ail.IsActive == 1)
                            {
                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            }
                            else
                            {
                                cmd1.Parameters.Add(new SqlParameter("@ApproverStatus", response));
                                cmd1.Parameters.Add(new SqlParameter("@Comment", comment));
                            }

                            cmd1.Parameters.Add(new SqlParameter("@IsCompleted", 1));
                            cmd1.Parameters.Add(new SqlParameter("@EmailApproverStatus", 1));

                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter1.SelectCommand = cmd1;
                            con.Open();
                            adapter1.Fill(dt1);
                            con.Close();

                        }
                        //Forms List Update

                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_UpdateApprovalStatusInForms", con);
                        cmd1.Parameters.Add(new SqlParameter("@Id", formId));
                        cmd1.Parameters.Add(new SqlParameter("@FormStatus", "Rejected"));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter1.SelectCommand = cmd1;
                        con.Open();
                        adapter1.Fill(dt1);
                        con.Close();

                    }
                }
                else
                {
                    if (approvalType == 1 || approvalType == 0)
                    {
                        DataTable dtResult = new DataTable();
                        SqlCommand cmd2 = new SqlCommand();
                        SqlDataAdapter adapter2 = new SqlDataAdapter();
                        con = new SqlConnection(sqlConString);
                        cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
                        cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
                        cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
                        cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                        cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                        cmd2.Parameters.Add(new SqlParameter("@Department", ""));
                        cmd2.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@CreatedBy", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                        cmd2.Parameters.Add(new SqlParameter("@Email", ""));
                        cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Level", 1));
                        cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
                        cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverName", UserName));


                        cmd2.CommandType = CommandType.StoredProcedure;
                        adapter2.SelectCommand = cmd2;
                        con.Open();
                        adapter2.Fill(dtResult);
                        con.Close();

                    }
                    else if (approvalType == 2)
                    {
                        DataTable dtResult = new DataTable();
                        SqlCommand cmd2 = new SqlCommand();
                        SqlDataAdapter adapter2 = new SqlDataAdapter();
                        con = new SqlConnection(sqlConString);
                        cmd2 = new SqlCommand("USP_SaveApproverDetails", con);
                        cmd2.Parameters.Add(new SqlParameter("@FormID", formId));
                        cmd2.Parameters.Add(new SqlParameter("@RowId", rowId));
                        cmd2.Parameters.Add(new SqlParameter("@IsActive", 1));
                        cmd2.Parameters.Add(new SqlParameter("@NextAppId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverStatus", "Enquired"));
                        cmd2.Parameters.Add(new SqlParameter("@Department", ""));
                        cmd2.Parameters.Add(new SqlParameter("@FormParentId", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ControllerName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@CreatedBy", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Created", DateTime.Now));
                        cmd2.Parameters.Add(new SqlParameter("@Email", ""));
                        cmd2.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Level", 1));
                        cmd2.Parameters.Add(new SqlParameter("@Logic", ""));
                        cmd2.Parameters.Add(new SqlParameter("@Designation", ""));
                        cmd2.Parameters.Add(new SqlParameter("@DelegatedByEmpNo", Convert.ToInt64(0)));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverUserName", ""));
                        cmd2.Parameters.Add(new SqlParameter("@RunWorkflow", "Yes"));
                        cmd2.Parameters.Add(new SqlParameter("@ApproverName", UserName));


                        cmd2.CommandType = CommandType.StoredProcedure;
                        adapter2.SelectCommand = cmd2;
                        con.Open();
                        adapter2.Fill(dtResult);
                        con.Close();
                    }
                }


                var EmailId = "";
                SqlCommand cmdUN = new SqlCommand();
                SqlDataAdapter UNadapter = new SqlDataAdapter();
                DataTable dt2 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmdUN = new SqlCommand("sp_GetUserByUserName", con);
                cmdUN.Parameters.Add(new SqlParameter("@UserName", UserName));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmdUN.CommandType = CommandType.StoredProcedure;
                UNadapter.SelectCommand = cmdUN;
                con.Open();
                UNadapter.Fill(dt2);
                con.Close();
                if (dt2.Rows.Count > 0)
                {

                    for (int i = 0; i < dt2.Rows.Count; i++)
                    {
                        EmailId = Convert.ToString(dt2.Rows[i]["EmailID"]);
                    }

                }

                var userList = new UserData()
                {
                    UserName = UserName,
                    IsFinalMailTriggeredManually = false,
                    Email = EmailId,
                    EmployeeName = UserFullname
                };

                SendEmail(formId, userList, response, comment, approverLevel, rowId, appRowId);

                return 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
        }

        //public async Task<List<User>>  GetCCNameByFilter(string name)
        //{
        //    List<User> ccList = new List<User>(); 
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand();
        //        SqlDataAdapter adapter = new SqlDataAdapter();
        //        DataSet ds = new DataSet();

        //        con = new SqlConnection(sqlConString);
        //        cmd = new SqlCommand("USP_getCCDetails", con);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = cmd;
        //        con.Open();
        //        adapter.Fill(ds);
        //        con.Close();

        //        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
        //        {
        //            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //            {
        //                domainIDs.Add(Convert.ToString(ds.Tables[0].Rows[i]["DomainID"]).Trim());
        //            }
        //        }
        //    }
        //    catch (Exception ex) { 
        //        Log.Error(ex.Message, ex);
        //    }
        //    return ccList;
        //}


        public async Task<UserInsertStatusModel> ChangePassword(int ID, string CurrentPassword, string NewPassword)
        {
            var HassPass = PasswordHasher(NewPassword);
            UserInsertStatusModel ccList = new UserInsertStatusModel();
            int domainIDs = 0;
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_ChangePassword", con);
                cmd.Parameters.Add(new SqlParameter("@ID", ID));
                //cmd.Parameters.Add(new SqlParameter("@CurrentPassword", CurrentPassword));
                cmd.Parameters.Add(new SqlParameter("@HassPass", HassPass));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();


                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        domainIDs = Convert.ToInt32(ds.Tables[0].Rows[0]["id"]);
                        ccList.Status = 200;
                        ccList.Message = "Password save successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                ccList.Status = 300;
                ccList.Message = "Password not change";
                ccList.ErrorMessage = ex.Message;
                Log.Error(ex.Message, ex);
            }
            return ccList;
        }

        public string PasswordHasher(string password)
        {
            PasswordHash hasher = new PasswordHash(password);
            var hashedArray = hasher.ToArray();
            return Convert.ToBase64String(hashedArray);

        }


        public async Task<MobileModel> GetAllMobileTask(string email)
        {

            MobileModel ccList = new MobileModel();
            int domainIDs = 0;
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_GetAllMobileTask", con);
                cmd.Parameters.Add(new SqlParameter("@email", email));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();


                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        ccList.Approvercount = Convert.ToInt32(ds.Tables[0].Rows[0]["Approvercount"]);
                        ccList.Cancelcount = Convert.ToInt32(ds.Tables[0].Rows[0]["Cancelcount"]);
                        ccList.Enquirecount = Convert.ToInt32(ds.Tables[0].Rows[0]["Enquirecount"]);
                        ccList.Pendingcount = Convert.ToInt32(ds.Tables[0].Rows[0]["Pendingcount"]);
                        ccList.Totalcount = Convert.ToInt32(ds.Tables[0].Rows[0]["Totalcount"]);
                        ccList.Rejectcount = Convert.ToInt32(ds.Tables[0].Rows[0]["Rejectcount"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ccList.Approvercount = 0;
                ccList.Cancelcount = 0;
                ccList.Enquirecount = 0;
                ccList.Pendingcount = 0;
                ccList.Totalcount = 0;
                ccList.Rejectcount = 0;
                Log.Error(ex.Message, ex);
            }
            return ccList;
        }

        public async Task<List<FormData>> GetApprovedForms(string Checked, string Filter)
        {
            CommonDAL obj = new CommonDAL();
            var handler = obj.GetHttpClientHandler();
            if (user == null)
                return new List<FormData>();

            List<FormData> result = new List<FormData>();
            List<FormData> resultNew = new List<FormData>();
            DashboardModel modelResult1 = new DashboardModel();
            DataModel dataModel = new DataModel();
            List<FormData> formDataList = new List<FormData>();


            try
            {
                //var handler = new HttpClientHandler();
                //handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                //var client = new HttpClient(handler);
                //client.BaseAddress = new Uri(conString);
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                if (Checked == "1")
                {
                    SqlCommand cmd = new SqlCommand();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    DataTable dt = new DataTable();

                    con = new SqlConnection(sqlConString);
                    cmd = new SqlCommand("USP_getMyApprovedTask", con);
                    cmd.Parameters.Add(new SqlParameter("@ApproverName", user.UserName));
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(dt);
                    con.Close();

                    //var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,BusinessNeed,ApproverUserName,Level,Logic,RunWorkflow,Department,ApproverStatus,RowId,NextApproverId,Modified,Author/Title,FormId/FormName," +
                    //                  "FormId/Id,FormId/Created,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/Status&$filter=(ApproverUserName eq '" + user.UserName + "' and IsActive eq '1')&$expand=FormId,Author&$top=10000");
                    //var responseText = await response.Content.ReadAsStringAsync();
                    //var settings = new JsonSerializerSettings
                    //{
                    //    NullValueHandling = NullValueHandling.Ignore
                    //};
                    //var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText, settings);

                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            FormData FormDataitem = new FormData();
                            FormLookup formLookup = new FormLookup();
                            Author author = new Author();
                            FormDataitem.Id = Convert.ToInt32(dt.Rows[i]["Id"]);
                            FormDataitem.Logic = Convert.ToString(dt.Rows[i]["Logic"]);
                            FormDataitem.Level = Convert.ToInt32(dt.Rows[i]["Level"]);
                            FormDataitem.RecievedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            FormDataitem.RowId = Convert.ToInt32(dt.Rows[i]["RowId"]);
                            FormDataitem.RunWorkflow = Convert.ToString(dt.Rows[i]["RunWorkflow"]);
                            FormDataitem.UniqueFormId = Convert.ToInt32(dt.Rows[i]["UniqueID"]);
                            FormDataitem.BusinessNeed = Convert.ToString(dt.Rows[i]["BusinessNeed"]);
                            FormDataitem.AuthorityToEdit = Convert.ToInt32(dt.Rows[i]["AuthorityToEdit"]);
                            FormDataitem.ApprovalType = Convert.ToString(dt.Rows[i]["ApprovalType"]);
                            FormDataitem.Department = Convert.ToString(dt.Rows[i]["Department"]);
                            FormDataitem.NextApproverId = Convert.ToInt32(dt.Rows[i]["NextApproverId"]);
                            FormDataitem.ApproverStatus = Convert.ToString(dt.Rows[i]["ApproverStatus"]);
                            author.Submitter = Convert.ToString(dt.Rows[i]["ApproverUserName"]);

                            FormDataitem.Author = author;
                            formLookup.ControllerName = Convert.ToString(dt.Rows[i]["ControllerName"]);
                            formLookup.CreatedDate = Convert.ToDateTime(dt.Rows[i]["Created"]);
                            formLookup.FormName = Convert.ToString(dt.Rows[i]["FormName"]);
                            formLookup.FormStatus = Convert.ToString(dt.Rows[i]["Status"]);
                            formLookup.Id = Convert.ToInt32(dt.Rows[i]["Id"]);
                            formLookup.ListName = Convert.ToString(dt.Rows[i]["ListName"]);
                            formLookup.UniqueFormName = Convert.ToString(dt.Rows[i]["UniqueFormName"]);
                            formLookup.StringRecievedDate = Convert.ToString(dt.Rows[i]["StringCreated"]);
                            FormDataitem.FormRelation = formLookup;
                            formDataList.Add(FormDataitem);
                        }
                    }
                    DashboardModel dataModel1 = new DashboardModel();
                    dataModel.Forms = formDataList;
                    modelResult1.Data = dataModel;
                    var modelResult = modelResult1;
                    result = modelResult.Data.Forms;

                    if (Filter == "2")
                    {
                        foreach (var word in modelResult.Data.Forms.ToList())
                        {
                            System.DateTime RecievedDate = word.RecievedDate;
                            System.DateTime TodayDate = DateTime.Now;
                            System.TimeSpan diffResult = TodayDate.Subtract(RecievedDate);

                            DateTime date_1 = (RecievedDate).Date;
                            DateTime date_2 = DateTime.Now.Date;
                            var numberOfDays = (date_2 - date_1).Days;
                            if (numberOfDays <= 2)
                            {
                                resultNew.Add(word);
                            }
                        }
                        result = resultNew;
                    }
                    else if (Filter == "4")
                    {
                        foreach (var word in modelResult.Data.Forms.ToList())
                        {
                            System.DateTime RecievedDate = word.RecievedDate;
                            System.DateTime TodayDate = DateTime.Now;
                            System.TimeSpan diffResult = TodayDate.Subtract(RecievedDate);

                            DateTime date_1 = (RecievedDate).Date;
                            DateTime date_2 = DateTime.Now.Date;
                            var numberOfDays = (date_2 - date_1).Days;
                            if (numberOfDays > 2 && numberOfDays <= 5)
                            {
                                resultNew.Add(word);
                            }
                        }
                        result = resultNew;
                    }
                    else if (Filter == "5")
                    {
                        foreach (var word in modelResult.Data.Forms.ToList())
                        {

                            System.DateTime RecievedDate = word.RecievedDate;
                            System.DateTime TodayDate = DateTime.Now;
                            System.TimeSpan diffResult = TodayDate.Subtract(RecievedDate);
                            DateTime date_1 = (RecievedDate).Date;
                            DateTime date_2 = DateTime.Now.Date;
                            var numberOfDays = (date_2 - date_1).Days;
                            if (numberOfDays > 5)
                            {
                                resultNew.Add(word);
                            }
                        }
                        result = resultNew;
                    }
                    else
                    {
                        modelResult = modelResult1;
                        result = modelResult.Data.Forms;
                    }
                }
                else
                {


                    SqlCommand cmd1 = new SqlCommand();
                    SqlDataAdapter adapter1 = new SqlDataAdapter();
                    DataTable dt1 = new DataTable();
                    var username = user.UserName;
                    con = new SqlConnection(sqlConString);
                    cmd1 = new SqlCommand("USP_getMyApprovedTask", con);
                    cmd1.Parameters.Add(new SqlParameter("@ApproverName", username));
                    // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                    cmd1.CommandType = CommandType.StoredProcedure;
                    adapter1.SelectCommand = cmd1;
                    con.Open();
                    adapter1.Fill(dt1);
                    con.Close();
                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            FormData FormDataitem = new FormData();
                            FormLookup formLookup = new FormLookup();
                            Author author = new Author();
                            FormDataitem.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            FormDataitem.Logic = Convert.ToString(dt1.Rows[i]["Logic"]);
                            FormDataitem.Level = Convert.ToInt32(dt1.Rows[i]["Level"]);
                            FormDataitem.RecievedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            FormDataitem.RowId = Convert.ToInt32(dt1.Rows[i]["RowId"]);
                            FormDataitem.RunWorkflow = Convert.ToString(dt1.Rows[i]["RunWorkflow"]);
                            FormDataitem.UniqueFormId = Convert.ToInt32(dt1.Rows[i]["UniqueID"]);
                            FormDataitem.BusinessNeed = Convert.ToString(dt1.Rows[i]["BusinessNeed"]);
                            FormDataitem.ApproverStatus = Convert.ToString(dt1.Rows[i]["ApproverStatus"]);

                            author.Submitter = Convert.ToString(dt1.Rows[i]["ApproverUserName"]);

                            FormDataitem.Author = author;

                            formLookup.CreatedDate = Convert.ToDateTime(dt1.Rows[i]["Created"]);
                            formLookup.FormName = Convert.ToString(dt1.Rows[i]["FormName"]);
                            formLookup.FormStatus = Convert.ToString(dt1.Rows[i]["Status"]);
                            //formLookup.Id = Convert.ToInt32(dt1.Rows[i]["Id"]);
                            formLookup.Id = Convert.ToInt32(dt1.Rows[i]["UniqueID"]);
                            formLookup.ListName = Convert.ToString(dt1.Rows[i]["ListName"]);
                            formLookup.UniqueFormName = Convert.ToString(dt1.Rows[i]["UniqueFormName"]);
                            formLookup.StringRecievedDate = Convert.ToString(dt1.Rows[i]["StringCreated"]);
                            FormDataitem.FormRelation = formLookup;
                            formDataList.Add(FormDataitem);
                        }
                    }
                    DashboardModel dataModel1 = new DashboardModel();
                    dataModel.Forms = formDataList;
                    modelResult1.Data = dataModel;
                    var modelResult = modelResult1;
                    result = modelResult.Data.Forms;
                    //var response = await client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=Id,ApprovalType,AuthorityToEdit,BusinessNeed,Level,ApproverUserName,Logic,RunWorkflow,Department,ApproverStatus,RowId,NextApproverId,Modified,Author/Title,FormId/FormName," +
                    //    "FormId/Id,FormId/Created,FormId/ControllerName,FormId/ListName,FormId/UniqueFormName,FormID/Status&$filter=(ApproverUserName eq '" + user.UserName + "' and IsActive eq '1')&$expand=FormId,Author&$top=10000");
                    //var responseText = await response.Content.ReadAsStringAsync();
                    //var settings = new JsonSerializerSettings
                    //{
                    //    NullValueHandling = NullValueHandling.Ignore
                    //};
                    //if (!string.IsNullOrEmpty(responseText))
                    //{
                    //    var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText, settings);
                    //    result = modelResult.Data.Forms;
                    //}

                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        public ResponseData SaveDesignationData(string Designation)
        {
            ResponseData SAFData = new ResponseData();
            try
            {
                object username = DBNull.Value;
                if (user.UserName != null)
                    username = user.UserName;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SaveDesignationData", con);
                cmd.Parameters.Add(new SqlParameter("@Designation", Convert.ToString(Designation)));
                cmd.Parameters.Add(new SqlParameter("@ActionBy", username));
                cmd.Parameters.Add(new SqlParameter("@ActionDate", DateTime.Now));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        SAFData.Status = Convert.ToInt32(ds.Rows[i]["Status"]);
                        SAFData.Message = Convert.ToString(ds.Rows[i]["Message"]);
                    }
                }
            }
            catch (Exception ex)
            {
                SAFData.Status = 600;
                SAFData.Message = ex.Message;
            }
            return SAFData;
        }

        public List<Tasklist> GetTotalTask(string values, string email)
        {

            try
            {
                List<Tasklist> list = new List<Tasklist>();
                object username = DBNull.Value;

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_GetStatuswiseTask", con);
                cmd.Parameters.Add(new SqlParameter("@values", values));
                cmd.Parameters.Add(new SqlParameter("@email", email));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        Tasklist data = new Tasklist();

                        data.FormID = Convert.ToString(ds.Rows[i]["FormID"]);
                        data.SrNo = i;
                        data.FormName = Convert.ToString(ds.Rows[i]["FormName"]);
                        data.RequestedBy = Convert.ToString(ds.Rows[i]["RequestedBy"]);
                        data.ReceivedDate = Convert.ToString(ds.Rows[i]["ReceivedDate"]);


                        list.Add(data);
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }



    }
}



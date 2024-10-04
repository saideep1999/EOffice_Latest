using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
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

namespace Skoda_DCMS.DAL
{
    public class ITClearanceDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;

        /// <summary>
        /// IT Clearance Form-It is used to save data in sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> CreateITClearanceRequest(System.Web.Mvc.FormCollection form, UserData user)
        {
            dynamic result = new ExpandoObject();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential(spUsername, spPass);

            int RowId = 0;
            Web web = _context.Web;

            var listName = GlobalClass.ListNames.ContainsKey("ITCF") ? GlobalClass.ListNames["ITCF"] : "";
            if (listName == "")
            {
                result.one = 0;
                result.two = 0;
                return result;
            }

            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            int FormId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            try
            {
                var dataToBeHanded = Convert.ToInt64(form["txtHandedDataToEmpNumber"]);
                var approverIdList = await GetApprovalForITClearance(user, dataToBeHanded);
                if (approverIdList == null || approverIdList.Count == 0)
                {
                    result.one = 0;
                    result.two = 0;
                    return result;
                }
                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "IT Clearance Form";
                    item["UniqueFormName"] = "ITCF";
                    item["FormParentId"] = 15;
                    item["ListName"] = listName;
                    item["SubmitterId"] = user.UserId;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(FormId);
                    item["FormName"] = "IT Clearance Form";
                    item["UniqueFormName"] = "ITCF";
                    item["FormParentId"] = 15;
                    item["ListName"] = listName;
                    item["SubmitterId"] = user.UserId;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();
                    formId = item.Id;

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
                List FormList = web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                ListItem newRow = FormList.AddItem(itemInfo);
                if (FormId == 0)
                {
                    newRow["TriggerCreateWorkflow"] = "Yes";
                }
                else
                {
                    newRow["TriggerCreateWorkflow"] = "No";
                }
                newRow["RequestSubmissionFor"] = form["drpRequestSubmissionFor"];
                newRow["EmployeeType"] = form["chkEmployeeType"];
                newRow["ExternalOrganizationName"] = form["ddExternalOrganizationName"] ?? "";
                newRow["ExternalOtherOrganizationName"] = form["txtExternalOtherOrganizationName"] ?? "";
                newRow["EmployeeCode"] = form["txtEmployeeCode"];
                newRow["EmployeeCCCode"] = form["txtCostcenterCode"]; //
                newRow["EmployeeUserId"] = form["txtUserId"]; //SharePoint user Id
                newRow["EmployeeName"] = form["txtEmployeeName"];
                newRow["EmployeeDepartment"] = form["txtDepartment"];
                newRow["EmployeeContactNo"] = form["txtContactNo"];
                newRow["EmployeeDesignation"] = form["ddEmpDesignation"];// DropDown selection
                newRow["EmployeeLocation"] = form["ddEmpLocation"];//Dropdown selection
                newRow["LastWorkingDay"] = form["txtLastWorkingDay"];
                newRow["HandedDataToName"] = form["txtHandedDataToName"];
                newRow["HandedDataToEmpNumber"] = form["txtHandedDataToEmpNumber"];
                for (int i = 0; i < approverIdList.Count; i++)
                {
                    newRow["Approver" + (i + 1)] = approverIdList[i].UserId;
                }
                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();

                result.one = 1;
                result.two = formId;

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                result.one = 0;
                result.two = 0;
                return result;
            }


        }
        /// <summary>
        /// IT Clearance Form-It is used for viewing software requisition form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetITClearanceDetails(int rowId, int formId)
        {
            dynamic itClearance = new ExpandoObject();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential(user.UserName, user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response = await client.GetAsync("_api/web/lists/GetByTitle('ITClearance')/items?$select=ID,EmployeeType,ExternalOrganizationName,ExternalOtherOrganizationName,EmployeeCode," +
                    "EmployeeCCCode,EmployeeUserId,EmployeeName,EmployeeContactNo,EmployeeDepartment,EmployeeDesignation,EmployeeLocation,LastWorkingDay,HandedDataToName,HandedDataToEmpNumber,RequestSubmissionFor,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");

                var responseText = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var result = JsonConvert.DeserializeObject<ITClearanceModel>(responseText, settings);
                    itClearance.one = result.List.ITClearanceList;
                }


                //approval start
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,"
                + "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                var responseText2 = await response2.Content.ReadAsStringAsync();
                var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                if (modelData.Node.Data.Count > 0)
                {
                    var client3 = new HttpClient(handler);
                    client3.BaseAddress = new Uri(conString);
                    client3.DefaultRequestHeaders.Accept.Clear();
                    client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var items = modelData.Node.Data;
                    var idString = "";
                    for (int i = 0; i < items.Count; i++)
                    {
                        var id = items[i];//
                        idString += $"Id eq '{id.ApproverId}' {(i != items.Count - 1 ? "or " : "")}";
                        items[i].UserLevel = i + 1;//
                    }
                    var response3 = await client3.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + idString + ")");
                    var responseText3 = await response3.Content.ReadAsStringAsync();

                    dynamic data4 = Json.Decode(responseText3);

                    var names = new List<string>();
                    foreach (var name in data4.d.results)
                    {
                        names.Add(name.Title as string);
                    }
                    items = items.OrderBy(x => x.ApproverId).ToList();
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
                        dynamic data2 = Json.Decode(responseText2);
                        itClearance.two = data2.d.results;
                        itClearance.three = items;
                    }

                }
                else
                {
                    itClearance.two = new List<string>();
                    itClearance.three = new List<string>();
                }

                //approval end

                //approval end

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return itClearance;
        }
        /// <summary>
        /// Data Backup Restore Form-It is used for getting approvers from sql db.
        /// </summary>
        /// <returns></returns>
        public async Task<List<ApprovalMatrix>> GetApprovalForITClearance(UserData user, long dataToBeHanded)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ITClearanceApproval", con);
                cmd.Parameters.Add(new SqlParameter("@emailId", user.Email));
                cmd.Parameters.Add(new SqlParameter("@dataToBeHandedOverTo", dataToBeHanded));
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
                        appList.Add(app);
                    }
                }

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential(spUsername, spPass);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.Timeout = TimeSpan.FromSeconds(10);
                var emailString = "";
                var count = appList.Count;
                for (int i = 0; i < count; i++)
                {
                    var email = appList[i];
                    emailString += $"EMail eq '{email.EmailId}' {(i != count - 1 ? "or " : "")}";
                }

                var response = client.GetAsync($"_api/web/SiteUserInfoList/items?$select=Id,Title,JobTitle,EMail&$filter=({emailString})").Result;
                if (response == null)
                {
                    List<ApprovalMatrix> app = new List<ApprovalMatrix>();
                }
                var responseText = response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                var responseString = responseText.Result.ToString();
                doc.LoadXml(responseString);
                //doc.Load(responseText.ToString());
                var title = doc.GetElementsByTagName("d:Title");
                var id = doc.GetElementsByTagName("d:Id");
                var emails = doc.GetElementsByTagName("d:EMail");
                var jobtitle = doc.GetElementsByTagName("d:JobTitle");

                for (int i = 0; i < id.Count; i++)
                {
                    var currentEmail = emails[i].InnerXml;
                    var currentId = Convert.ToInt32(id[i].InnerXml);
                    var matchingUser = appList.Where(x => x.EmailId == currentEmail).FirstOrDefault();
                    if (matchingUser != null)
                        matchingUser.UserId = currentId;
                }

                return appList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }

        }

    }
}
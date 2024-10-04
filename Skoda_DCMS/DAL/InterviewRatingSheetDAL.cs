using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;

namespace Skoda_DCMS.DAL
{
    public class InterviewRatingSheetDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        public async Task<dynamic> SaveInterviewRatingSheet(System.Web.Mvc.FormCollection form, UserData user)
        {
            dynamic result = new ExpandoObject();
            ClientContext _context = new ClientContext(new Uri(conString));
            //_context.Credentials = new NetworkCredential(spUsername, spPass);
            _context.Credentials =  new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;

            var listName = GlobalClass.ListNames.ContainsKey("IRSF") ? GlobalClass.ListNames["IRSF"] : "";
            if (listName == "")
            {
                result.one = 0;
                result.two = 0;
                return result;
            }

            int formId = 0;
            int FormId = Convert.ToInt32(form["FormId"]);
            int AppRowId = Convert.ToInt32(form["AppRowId"]);
            try
            {
                //var selfOnBehalf = form["drpRequestSubmissionFor"];
                //var onBehalfEmail = form["hiddentxtEmail"];
                //var @RCEmailid = form["hiddenRCApprover"];
                //var @HRCEmailid = form["hiddenHRCApprover"];
                //var approverIdList = await GetApprovalIJPF(user, selfOnBehalf, onBehalfEmail, @RCEmailid, @HRCEmailid);
                //if (approverIdList == null || approverIdList.Count == 0)
                //{
                //    result.one = 0;
                //    result.two = 0;
                //    return result;
                //}
                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "Interview Rating Sheet Form";
                    item["UniqueFormName"] = "IRSF";
                    item["FormParentId"] = 26;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "InterviewRatingSheet";
                    item.Update();
                    _context.Load(item);
                    _context.ExecuteQuery();

                    formId = item.Id;
                }
                else
                {
                    List list = _context.Web.Lists.GetByTitle("Forms");
                    ListItem item = list.GetItemById(FormId);
                    item["FormName"] = "Interview Rating Sheet Form";
                    item["UniqueFormName"] = "IRSF";
                    item["FormParentId"] = 26;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "InterviewRatingSheet";
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
                    newRow["TriggerCreateWorkflow"] = "No";
                }
                else
                {
                    newRow["TriggerCreateWorkflow"] = "Yes";
                }
                newRow["Name"] = form["txtName"];
                newRow["JobTitle"] = form["txtJobTitle"];
                newRow["InterviewDate"] = form["txtInterviewDate"];
                newRow["Department"] = form["txtDepartment"];
                newRow["Section"] = form["txtSection"];
                newRow["InterviewPlace"] = form["txtInterviewPlace"];
                newRow["CompetenceGeneral"] = form["txtCompetenceGeneral"];
                newRow["CompetenceTechnical"] = form["txtCompetenceTechnical"];
                newRow["LivingIntegrityandResponsibility"] = form["txtLivingIntegrityandResponsibility"];
                newRow["OtherComments"] = form["txtOtherComments"];

                //for (int i = 0; i < approverIdList.Count; i++)
                //{
                //    newRow["Approver" + (i + 1)] = approverIdList[i].UserId;
                //}

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
                result.one = 0;
                result.two = 0;
                Log.Error(ex.Message, ex);
                return result;
            }
        }

        public async Task<dynamic> ViewIRSFFData(int rowId, int formId)
        {
            dynamic IRSFDataList = new ExpandoObject();

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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('InterviewRatingSheetForm')/items?$select=ID,Name,JobTitle,InterviewDate,Department,Section,InterviewPlace,CompetenceGeneral,CompetenceTechnical,LivingIntegrityandResponsibility,OtherComments"
                + "&$filter=(ID eq '" + rowId + "')");
                var responseText = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var IRSFResult = JsonConvert.DeserializeObject<InterviewRatingSheetModel>(responseText, settings);
                    IRSFDataList.one = IRSFResult.irsflist.irsfData;
                }

                //var client2 = new HttpClient(handler);
                //client2.BaseAddress = new Uri(conString);
                //client2.DefaultRequestHeaders.Accept.Clear();
                //client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?$select=ApproverId,ApproverStatus,Modified,IsActive,Comment,NextApproverId,"
                //+ "FormId/Id,FormId/Created,Author/Title&$filter=(RowId eq '" + rowId + "' and FormId eq '" + formId + "')&$expand=FormId,Author");
                //var responseText2 = await response2.Content.ReadAsStringAsync();
                //var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText2, settings);

                //var client3 = new HttpClient(handler);
                //client3.BaseAddress = new Uri(conString);
                //client3.DefaultRequestHeaders.Accept.Clear();
                //client3.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                //var items = modelData.Node.Data;
                //var names = new List<string>();
                //var idString = "";
                //var responseText3 = "";

                //for (int i = 0; i < items.Count; i++)
                //{
                //    var id = items[i];
                //    idString = $"Id eq '{id.ApproverId}'";
                //    items[i].UserLevel = i + 1;//
                //    var response3 = await client3.GetAsync("_api/web/SiteUserInfoList/items?$select=Title&$filter=(" + idString + ")");
                //    responseText3 = await response3.Content.ReadAsStringAsync();
                //    dynamic Paralleldata4 = Json.Decode(responseText3);

                //    if (Paralleldata4.Count != 0)
                //    {
                //        foreach (var name in Paralleldata4.d.results)
                //        {
                //            names.Add(name.Title as string);
                //        }
                //    }
                //}


                //if (items.Count == names.Count)
                //{
                //    for (int i = 0; i < items.Count; i++)
                //    {
                //        items[i].UserName = names[i];
                //    }
                //}
                //items = items.OrderBy(x => x.UserLevel).ToList();

                //if (!string.IsNullOrEmpty(responseText2))
                //{
                //    dynamic data2 = Json.Decode(responseText2);
                //    IRSFDataList.two = data2.d.results;
                //    IRSFDataList.three = items;
                //}

                return IRSFDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return IRSFDataList;
            }
        }

    }
}
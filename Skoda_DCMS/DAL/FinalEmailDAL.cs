using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class FinalEmailDAL
    {
        public UserData user;
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        public FinalEmailDAL()
        {
            GlobalClass obj = new GlobalClass();
            user = obj.GetCurrentUser();
        }

        public async Task<int> SendFinalEmail(FormCollection form)
        {
            int result = 0;
            try
            {
                string comment = "";
                ListDAL listDAL = new ListDAL();
                var formIds = form["txtFormId"];
                if (formIds.Contains(",")) {
                    var formsIdList = formIds.Split(',').ToList();

                    var handler = new HttpClientHandler();
                    handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                    var client = new HttpClient(handler);
                    client.BaseAddress = new Uri(conString);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                    string filterText = "";
                    for (int i = 0; i < formsIdList.Count; i++)
                    {
                        var item = formsIdList[i];
                        filterText += $"(ID eq {item})";
                        if (i != formsIdList.Count - 1)
                            filterText += " or ";
                    }
                    var response = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('Forms')/items?"
                       + "$select=DataRowId,Id,Status&$filter=(("
                       + filterText + ") and Status eq 'Approved')")).Result;
                    var responseText = await response.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    var formResult = new List<FormData>();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var modelResult = JsonConvert.DeserializeObject<DashboardModel>(responseText, settings);
                        formResult = modelResult.Data.Forms;
                    }

                    foreach (var item in formResult)
                    {
                        var response1 = Task.Run(() => client.GetAsync("_api/web/lists/GetByTitle('ApprovalMaster')/items?"
                            + "$select=Id,RowId,Level,ApproverStatus,IsActive,Modified,Logic,ApproverUserName&$filter=(FormId eq "
                            + item.Id + " and RowId eq " + item.DataRowId + ")")).Result;
                        var responseText1 = await response1.Content.ReadAsStringAsync();

                        List<ApprovalDataModel> approvalDataList = new List<ApprovalDataModel>();

                        if (!string.IsNullOrEmpty(responseText1))
                        {
                            var modelData = JsonConvert.DeserializeObject<ApprovalMasterModel>(responseText1, settings);
                            approvalDataList = modelData.Node.Data;
                        }

                        ApprovalDataModel finalApprover = new ApprovalDataModel();

                        finalApprover = approvalDataList.Where(x => !string.IsNullOrEmpty(x.ApproverStatus) && x.ApproverStatus.ToLower() == "approved")
                            .OrderByDescending(x => x.Level).FirstOrDefault();

                        if (finalApprover != null)
                        {
                            user.FinalMailApproverUserName = finalApprover.ApproverUserName;
                            user.IsFinalMailTriggeredManually = true;
                            await listDAL.SendEmail(item.Id, user, finalApprover.ApproverStatus, comment, finalApprover.Level, item.DataRowId.GetValueOrDefault(), finalApprover.Id);
                        }
                    }
                }
                else
                {
                    var formId = Convert.ToInt32(form["txtFormId"]);
                    var rowId = Convert.ToInt32(form["txtRowId"]);
                    string response = form["txtApproverResponse"];
                    var level = Convert.ToInt32(form["txtLevel"]);
                    var appRowId = Convert.ToInt32(form["txtAppRowId"]);

                    await listDAL.SendEmail(formId, user, response, comment, level, rowId, appRowId);
                }
                result = 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        public async Task<int> SendFinalEmailWithoutApprover(FormCollection form)
        {
            int result = 0;
            try
            {
                var formId = Convert.ToInt32(form["txtFormId"]);
                var rowId = Convert.ToInt32(form["txtRowId"]);
                string formShortName = form["txtformShortName"];
                string formName = form["txtformName"];

                ListDAL listDAL = new ListDAL();
                var userList = await listDAL.GetSubmitterDetails(formId, formShortName, rowId);
                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = FormStates.FinalApproval,
                    Recipients = null,
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                    FormName = formName,
                    CurrentUser = user,
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);


                result = 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
    }
}
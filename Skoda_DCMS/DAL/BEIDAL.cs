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
using System.Web.Mvc;
using Skoda_DCMS.Models.CommonModels;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class BEIDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        //UserData _CurrentUser;
        dynamic approverEmailIds;

        /// <summary>
        /// BEI-It is used to get the part list for BEI form from sharepoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> GetBei()
        {
            List<BeiPartData> beiPartData = new List<BeiPartData>();
            dynamic result = beiPartData;
            try
            {
                /*var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);*/

                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");//we want to receive JSON rather than XML
                var response = await client.GetAsync("_api/web/lists/GetByTitle('BeiMaster')/items?$select=PartDesc,Quantity,Id");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.Contains("401 UNAUTHORIZED"))
                    GlobalClass.IsUserLoggedOut = true;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var modelResult = JsonConvert.DeserializeObject<BeiDataModel>(responseText);
                    beiPartData = modelResult.beiDatalist.beiPartData;
                }
                result = beiPartData;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return result;
            }
        }
        /// <summary>
        /// BEI-It is used for Saving data.
        /// </summary>
        /// <returns></returns>
        public async Task<ResponseModel<object>> SaveBei(System.Web.Mvc.FormCollection form)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential(spUsername, spPass);
            Web web = _context.Web;
            var Remark = form["txtRemark"];
            var Avail = form["txtAvail"];
            var part = form["txtpartName"];
            var quant = form["txtquantity"];

            var remarkList = Remark.Split(',').ToList();
            var availList = Avail.Split(',').ToList();
            var partList = part.Split(',').ToList();
            var quantList = quant.Split(',').ToList();

            int RowId = 0;
            string formShortName = "BEI";
            string formName = "BEI Form";
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
            bool IsResubmit = FormId == 0 ? false : true;

            try
            {
                var requestSubmissionFor = form["drpRequestSubmissionFor"];
                var otherEmpType = form["rdOnBehalfOptionSelected"] ?? "";
                if (FormId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");//this complete block is a different function in new code=func(formname, uniqueformname,listname)
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newItem = FormsList.AddItem(itemCreateInfo);
                    newItem["FormName"] = "BEI";
                    newItem["UniqueFormName"] = "BEI";
                    newItem["ListName"] = listName;
                    newItem["SubmitterUserName"] = user.UserName;
                    newItem["Status"] = "Submitted";
                    newItem["FormParentId"] = 2;
                    newItem["Department"] = user.Department;
                    newItem["ControllerName"] = "BEI";
                    newItem["BusinessNeed"] = form["txtBusinessNeed"].Trim(); if (requestSubmissionFor == "Self")
                    {
                        newItem["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            newItem["Location"] = form["ddOtherEmpLocation"];
                        }
                        else
                        {
                            newItem["Location"] = form["ddOtherNewEmpLocation"];
                        }
                    }

                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();

                    formId = newItem.Id;
                }
                else
                {
                    List FormsList = web.Lists.GetByTitle("Forms");//this complete block is a different function in new code=func(formname, uniqueformname,listname)
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newItem = FormsList.AddItem(itemCreateInfo);
                    newItem["FormName"] = "BEI";
                    newItem["UniqueFormName"] = "BEI";
                    newItem["ListName"] = listName;
                    newItem["SubmitterUserName"] = user.UserName;
                    newItem["Status"] = "ReissueIDCard";
                    newItem["FormParentId"] = 2;
                    newItem["Department"] = user.Department;
                    newItem["ControllerName"] = "BEI";
                    newItem["BusinessNeed"] = form["txtBusinessNeed"].Trim(); 
                    if (requestSubmissionFor == "Self")
                    {
                        newItem["Location"] = form["ddEmpLocation"];
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            newItem["Location"] = form["ddOtherEmpLocation"];
                        }
                        else
                        {
                            newItem["Location"] = form["ddOtherNewEmpLocation"];
                        }
                    }

                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();

                    formId = newItem.Id;
                    ListDAL dal = new ListDAL();
                    var resubmitResult = await dal.ResubmitUpdate(formId);
                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetails(web, form, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                var newRow = userDetailsResponse.Model;
                newRow["FormID"] = formId;
                newRow["Vin"] = form["txtVin"];
                newRow["BusinessNeed"] = form["txtBusinessNeed"].Trim();

                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                result.Status = 200;
                result.Message = formId.ToString();

                RowId = newRow.Id;

                List BeiList = web.Lists.GetByTitle("BeiData");
                for (int i = 0; i < availList.Count; i++)
                {
                    ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                    ListItem newTBLRow = BeiList.AddItem(itemInfo);
                    newTBLRow["BeiRowID"] = RowId;
                    newTBLRow["PartDesc"] = partList[i];
                    newTBLRow["Quantity"] = quantList[i];
                    newTBLRow["Remark"] = remarkList[i];
                    newTBLRow["Availability"] = availList[i];
                    newTBLRow["FormID"] = formId;
                    newTBLRow.Update();
                    _context.Load(newTBLRow);
                    _context.ExecuteQuery();
                }

                    //Data Row ID Update in Forms List
                    var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                    if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                    {
                        return updateRowResponse;
                    }

                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, RowId);
                bool ifApproverExists = false;

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = ifApproverExists ? (IsResubmit ? FormStates.ReSubmit : FormStates.Submit) : FormStates.FinalApproval,
                    Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
                    FormName = formName,
                    CurrentUser = user,
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Log.Error(ex.Message, ex);
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
            }
            return result;
        }
        /// <summary>
        /// BEI-It is used for viewing the BEI form.
        /// </summary>
        /// <returns></returns>
        public async Task<dynamic> ViewBeiData(int rowId, int formId)
        {
            dynamic beiDataList = new ExpandoObject();//constructor called, always expandoobject for dynamic dataType
            List<BeiData> MainList = new List<BeiData>();
            List<BeiPartData> OtherList = new List<BeiPartData>();
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
                var response = await client.GetAsync("_api/web/lists/GetByTitle('BEIForm')/items?$select=*,FormID/ID,FormID/Created&$filter=(ID eq '" + rowId + "')&$expand=FormID");
                var responseText = await response.Content.ReadAsStringAsync();


                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText))
                {
                    var beiResult = JsonConvert.DeserializeObject<BeiModel>(responseText, settings);
                    MainList = beiResult.list.beiData;
                }

                beiDataList.one = MainList;
                var client2 = new HttpClient(handler);
                client2.BaseAddress = new Uri(conString);
                client2.DefaultRequestHeaders.Accept.Clear();
                client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                var response2 = await client2.GetAsync("_api/web/lists/GetByTitle('BeiData')/items?$select=ID,PartDesc,Quantity,Availability,"
                    + "Remark,Created&$filter=(BeiRowID eq '" + rowId + "')");
                var responseText2 = await response2.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseText2))
                {
                    var beiResult = JsonConvert.DeserializeObject<BeiDataModel>(responseText2);
                    OtherList = beiResult.beiDatalist.beiPartData;
                }
                beiDataList.two = OtherList;
                return beiDataList;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

        }
    }
}
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.WebControls;
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
    public class EQSAccessDAL : CommonDAL
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
        public async Task<dynamic> ViewEQSAccessFormData(int rowId, int formId)
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
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('EQSAccess')/items?$select=*"
  + "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCFUserResult = JsonConvert.DeserializeObject<EQSAccessmModel>(responseText1, settings);
                    URCFData.one = SUCFUserResult.EQSAccessmModelResults.EQSAccessmModelData;

                    //For table List

                    var handler2 = new HttpClientHandler();
                    handler2.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                    var client2 = new HttpClient(handler2);
                    client2.BaseAddress = new Uri(conString);
                    client2.DefaultRequestHeaders.Accept.Clear();
                    client2.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response2 = Task.Run(() => client2.GetAsync("_api/web/lists/GetByTitle('EQSAccessTableData')/items?$select=*" +
                        "&$filter=(FormId eq '" + rowId + "')")).Result;
                    var responseText2 = await response2.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText2))
                    {

                        var ListResult = JsonConvert.DeserializeObject<EQSAccessmModel>(responseText2, settings);
                        URCFData.Four = ListResult.EQSAccessmModelResults.EQSAccessmModelData;
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

        public async Task<ResponseModel<object>> SaveData(EQSAccessmModelData model, UserData userData)
        {

            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "EQSA";
            string formName = "EQSAccessForm";
            var listName = "EQSAccess";
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
            long ccNum = user.CostCenter;
            long empNum = user.EmpNumber;
            string empDes = model.EmployeeDesignation;
            var empLocName = isSelf ? Convert.ToString(model.EmployeeLocation) : (isSAVWIPL ? Convert.ToString(model.OtherEmployeeLocation) : Convert.ToString(model.OtherNewEmpLocation));
            var locations = await new ListDAL().GetLocations();
            if (locations == null && locations.Count <= 0)
            {
                result.Status = 500;
                result.Message = "There were some issue fetching Location data.";
                return result;
            }
            var locObj = locations.Find(x => x.LocationName == empLocName);
            if (locObj == null)
            {
                result.Status = 500;
                result.Message = "Could not found location data.";
                return result;
            }

            long empLoc = locObj.LocationId == 1 || locObj.LocationId == 3 ? locObj.LocationId : 2;
            var response = await GetEQSAApprovers(empNum, ccNum, empLoc);
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
                    item["FormParentId"] = 47;
                    item["ListName"] = listName;

                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "EQSAccess";
                    item["BusinessNeed"] = "";
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
                    item["FormParentId"] = 47;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "EQSAccess";
                    item["BusinessNeed"] = "";
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
                newRow["RequestType"] = model.RequestType;
                newRow["BusinessJustification"] = model.BusinessJustification;
                newRow["FormID"] = formId;
                if (otherEmpType == "SAVWIPLEmployee")
                {
                    newRow["OtherExternalOtherOrgName"] = model.OtherExternalOrganizationName;
                }
                else
                {
                    newRow["OtherExternalOtherOrgName"] = model.OtherNewExternalOrganizationName;
                }
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                RowId = newRow.Id;

                result.Status = 200;
                result.Message = formId.ToString();

                int SrNo = 1;
                foreach (var item in model.EQSAccessTableData)
                {
                    List List = web.Lists.GetByTitle("EQSAccessTableData");
                    List.Update();
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newItem = List.AddItem(itemCreateInfo);

                    newItem["SrNo"] = SrNo++;
                    newItem["EmployeeName"] = item.EmployeeName;
                    newItem["EmployeeID"] = item.EmployeeID;
                    newItem["LogicCardID"] = item.LogicCardID;
                    newItem["StationName"] = item.StationName;
                    newItem["Shop"] = item.Shop;
                    newItem["AccessGroup"] = item.AccessGroup;
                    newItem["FormId"] = RowId;
                    //newItem["RowId"] = RowId;


                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();
                }



                var approverIdList = response.Model;
                var approvalResponse = await SaveApprovalMasterData(approverIdList, "", RowId, formId);


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

                //status Approved
                //List formslist = _context.Web.Lists.GetByTitle("Forms");
                //ListItem newItemApprove = formslist.GetItemById(formId);
                //newItemApprove.RefreshLoad();
                //_context.ExecuteQuery();
                //newItemApprove["Status"] = "Approved";
                //newItemApprove.Update();
                //_context.Load(newItemApprove);
                //_context.ExecuteQuery();

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,                   
                    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
                    FormName = formName,
                    CurrentUser = user
                };


                var emailService = new EmailService();
                emailService.SendMail(emailData);

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

        private async Task<ResponseModel<List<ApprovalMatrix>>> GetEQSAApprovers(long empNum, long filledForEmpNum, long empLoc)
        {
            List<ApprovalMatrix> list = new List<ApprovalMatrix>();
            try
            {
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                SqlConnection con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetEQSAApprovers", con);
                sqlCommand.Parameters.Add(new SqlParameter("@empNum", empNum));
                sqlCommand.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = sqlCommand;
                con.Open();
                adapter.Fill(data);
                con.Close();

                if (data.Tables[0].Rows.Count > 0 && data.Tables[0] != null)
                {
                    for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                    {
                        ApprovalMatrix app = new ApprovalMatrix();
                        app.EmpNumber = Convert.ToInt32(data.Tables[0].Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(data.Tables[0].Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(data.Tables[0].Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(data.Tables[0].Rows[i]["EmailId"]);
                        app.Designation = Convert.ToString(data.Tables[0].Rows[i]["desg"]);
                        app.ApprovalLevel = (int)data.Tables[0].Rows[i]["approvalLevel"];
                        app.Logic = Convert.ToString(data.Tables[0].Rows[i]["logic"]);
                        if (data.Tables[0].Columns.Contains("Contents"))
                            app.ExtraDetails = Convert.ToString(data.Tables[0].Rows[i]["Contents"]);
                        if (data.Tables[0].Columns.Contains("LogicId"))
                            app.LogicId = Convert.ToInt64(data.Tables[0].Rows[i]["LogicId"]);
                        if (data.Tables[0].Columns.Contains("LogicWith"))
                            app.LogicWith = Convert.ToInt64(data.Tables[0].Rows[i]["LogicWith"]);
                        if (data.Tables[0].Columns.Contains("RelationId"))
                            app.RelationId = Convert.ToInt64(data.Tables[0].Rows[i]["RelationId"]);
                        if (data.Tables[0].Columns.Contains("RelationWith"))
                            app.RelationWith = Convert.ToInt64(data.Tables[0].Rows[i]["RelationWith"]);
                        appList.Add(app);
                    }
                }
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }
    }
}
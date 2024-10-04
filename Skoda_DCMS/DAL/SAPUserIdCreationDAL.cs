using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Utilities;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.DAL
{
    public class SAPUserIdCreationDAL: CommonDAL
    {
        public async Task<ResponseModel<object>> SaveData(SAPUserIdCreationModel data)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            try
            {
                ClientContext _context = new ClientContext(new Uri(conString));
                _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                Web web = _context.Web;
                int rowId = 0; //User to store newly inserted data id
                string formShortName = "SUCF";
                string formName = "SAP User Id Creation";
                string listName = GlobalClass.ListNames.ContainsKey("SUCF") ? GlobalClass.ListNames["SUCF"] : "";
                if (listName == "")
                {
                    result.Status = 500;
                    result.Message = "List not found.";
                    return result;
                }
                //int formId = 0;
                int formId = Convert.ToInt32(data.FormId);
                bool IsResubmit = formId == 0 ? false : true;
                int AppRowId = Convert.ToInt32(data.AppRowId);
                var requestSubmissionFor = data.RequestSubmissionFor;
                var otherEmpType = data.OnBehalfOption ?? "";
                bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                long ccNum = isSelf ? user.CostCenter : (isSAVWIPL ? Convert.ToInt64(data.OtherEmployeeCCCode) : Convert.ToInt64(data.OtherNewCostcenterCode));
                long empNum = isSelf ? user.EmpNumber : (isSAVWIPL ? Convert.ToInt64(data.OtherEmployeeCode) : Convert.ToInt64(data.OtherNewEmployeeCode));
                //bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
                var response = await GetSAPUserApprovers(empNum, ccNum);
                if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
                {
                    result.Status = 500;
                    result.Message = response.Message;
                    return result;
                }
                var approvers = response.Model;
                if (formId == 0)
                {
                    List FormsList = web.Lists.GetByTitle("Forms");
                    ListItemCreationInformation itemCreated = new ListItemCreationInformation();
                    ListItem item = FormsList.AddItem(itemCreated);
                    item["FormName"] = "SAP UserId Creation Form";
                    item["UniqueFormName"] = "SUCF";
                    item["FormParentId"] = 36;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SAPUserIdCreation";
                    item["BusinessNeed"] = data.BusinessNeed ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = data.EmployeeLocation;
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = data.OtherEmployeeLocation;
                        }
                        else
                        {
                            item["Location"] = data.OtherNewEmpLocation;
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
                    item["FormName"] = "SAP UserId Creation Form";
                    item["UniqueFormName"] = "SUCF";
                    item["FormParentId"] = 36;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "SAPUserIdCreation";
                    item["BusinessNeed"] = data.BusinessNeed ?? "";
                    if (requestSubmissionFor == "Self")
                    {
                        item["Location"] = data.EmployeeLocation;
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            item["Location"] = data.OtherEmployeeLocation;
                        }
                        else
                        {
                            item["Location"] = data.OtherNewEmpLocation;
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

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(web, data, listName, formId);
                if (userDetailsResponse.Status != 200 && userDetailsResponse.Model == null)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                var newRow = userDetailsResponse.Model;
                newRow["FormID"] = formId;
                newRow["BusinessNeed"] = data.BusinessNeed;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                int RowId = newRow.Id;
                result.Status = 200;
                result.Message = formId.ToString();

                //<Form Transaction Fields>
                int SrNo = 1;
                foreach (var item in data.UserData)
                {
                    List List = web.Lists.GetByTitle("SAPUserDataList");
                    List.Update();
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem newItem = List.AddItem(itemCreateInfo);
                    newItem["ListItemId"] = RowId;
                    newItem["SrNo"] = SrNo++;
                    newItem["System"] = item.System;
                    newItem["Client"] = item.Client;
                    newItem["TypeOfUser"] = item.Type;
                    newItem["Reason"] = item.Reason;
                    newItem["Module"] = item.Module;
                    newItem["ModuleDescription"] = item.Module.Split(new char[] { ',' }).Any(x => x.ToLower() == "others") ? item.ModuleDescription : "";
                    newItem["SubModule"] = item.SubModule;
                    newItem["RequestType"] = item.RequestType;
                    if (item.RequestType.ToLower() == "temporary")
                    {
                        newItem["TempFrom"] = item.TempFrom;
                        newItem["TempTo"] = item.TempTo;
                    }
                    newItem["FormID"] = formId;
                    //newItem["ListItemId"] = RowId;//RowId of currently added item
                    newItem.Update();
                    _context.Load(newItem);
                    _context.ExecuteQuery();
                }
                //</Form Transaction Fields>

                var approvalResponse = await SaveApprovalMasterData(approvers, data.BusinessNeed ?? "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }

                //Data Row ID Update in Forms List
                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, RowId);
                if (userList == null || userList.Count == 0)
                {
                    result.Status = 500;
                    result.Message = "There were some issue fetching Submitter Details.";
                    return result;
                }
                foreach (var approver in approvers)
                {
                    var userData = new UserData()
                    {
                        EmployeeName = approver.FName + " " + approver.LName,
                        Email = approver.EmailId,
                        ApprovalLevel = approver.ApprovalLevel,
                        IsApprover = true
                    };
                    userList.Add(userData);
                }

                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    Recipients = userList.Where(x => x.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(x => !x.IsOnBehalf && !x.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(x => x.IsOnBehalf).FirstOrDefault(),
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
                return result;
            }
            return result;
        }

        public async Task<dynamic> GetSAPUserCreationDetails(int rowId, int formId)
        {
            dynamic SUCFData = new ExpandoObject();
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
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('SAPUserIDForm')/items?$select=*" +
                    "&$filter=(ID eq '" + rowId + "')");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCFResult = JsonConvert.DeserializeObject<SUCFModel>(responseText1, settings);
                    SUCFData.one = SUCFResult.list.data;
                    client1.DefaultRequestHeaders.Accept.Clear();
                    client1.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                    var response = await client1.GetAsync("_api/web/lists/GetByTitle('SAPUserDataList')/items?$select=*,ListItemId/ID" +
                        "&$filter=(ListItemId/ID eq '" + rowId + "')&$expand=ListItemId");
                    var responseText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        var SUCFUserResult = JsonConvert.DeserializeObject<SUCFUserDataModel>(responseText, settings);
                        SUCFData.one[0].UserData = SUCFUserResult.list.data;
                    }
                }
                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                SUCFData.two = r1.Model;
                SUCFData.three = r2.Model;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return SUCFData;
        }

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetSAPUserApprovers(long empNum, long ccNum)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                SqlConnection con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetSAPUserCreationFormApprovers", con);
                sqlCommand.Parameters.Add(new SqlParameter("@EmpNum", empNum));
                sqlCommand.Parameters.Add(new SqlParameter("@CCNum", ccNum));
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
                        app.ExtraDetails = Convert.ToString(data.Tables[0].Rows[i]["Contents"]);
                        app.Logic = Convert.ToString(data.Tables[0].Rows[i]["logic"]);
                        app.LogicId = Convert.ToInt64(data.Tables[0].Rows[i]["LogicId"]);
                        app.LogicWith = Convert.ToInt64(data.Tables[0].Rows[i]["LogicWith"]);
                        app.RelationId = Convert.ToInt64(data.Tables[0].Rows[i]["RelationId"]);
                        app.RelationWith = Convert.ToInt64(data.Tables[0].Rows[i]["RelationWith"]);
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
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
    public class FixtureRequisitionDAL : CommonDAL
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

        public async Task<ResponseModel<object>> SaveQFRFForm(FixtureRequisitionData model, UserData user, HttpPostedFileBase file, HttpPostedFileBase file1)
        {

            ResponseModel<object> result = new ResponseModel<object>();
            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);

            int RowId = 0;
            Web web = _context.Web;
            string formShortName = "QFRF";
            string formName = "Fixture Requisition Form";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            //var listName = "FixtureRequisitionForm";
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


            var response = await GetApprovalQFRFForm(empNum, ccNum, empDes, model);
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
                    item["FormParentId"] = 44;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Submitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "FixtureRequisition";
                    item["BusinessNeed"] = model.BusinessNeed ?? "";
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
                    item["FormParentId"] = 41;
                    item["ListName"] = listName;
                    item["SubmitterUserName"] = user.UserName;
                    item["Status"] = "Resubmitted";
                    item["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
                    item["Department"] = user.Department;
                    item["ControllerName"] = "FixtureRequisition";
                    item["BusinessNeed"] = model.BusinessNeed ?? "";
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
                newRow["FixtureName"] = model.FixtureName;
                newRow["FixtureNo"] = model.FixtureNo;
                newRow["ProjectName"] = model.ProjectName;
                newRow["FromDate"] = model.FromDate;
                newRow["ToDate"] = model.ToDate;
                newRow["Reason"] = model.Reason;
                newRow["RpsPin"] = model.RpsPin;
                newRow["RpsPinRemark"] = model.RpsPinRemark;
                newRow["Clamps"] = model.Clamps;
                newRow["ClampsRemark"] = model.ClampsRemark;
                newRow["Wheels"] = model.Wheels;
                newRow["WheelsRemark"] = model.WheelsRemark;
                newRow["RpsStick"] = model.RpsStick;
                newRow["RpsStickRemark"] = model.RpsStickRemark;
                newRow["LoseElement"] = model.LoseElement;
                newRow["LoseRemark"] = model.LoseRemark;
                newRow["Mylers"] = model.Mylers;
                newRow["MylerRemark"] = model.MylerRemark;
                newRow["PinThreads"] = model.PinThreads;
                newRow["PinRemark"] = model.PinRemark;
                newRow["RestingPads"] = model.RestingPads;
                newRow["PadsRemark"] = model.PadsRemark;
                newRow["SlidersRemark"] = model.SlidersRemark;
                newRow["Sliders"] = model.Sliders;
                newRow["Kugel"] = model.Kugel;
                newRow["KugelRemark"] = model.KugelRemark;
                newRow["BusinessNeed"] = model.BusinessNeed;

                newRow["FormID"] = formId;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                RowId = newRow.Id;

                result.Status = 200;
                result.Message = formId.ToString();

                var approverIdList = response.Model;

               
                //Task Entry in Approval Master List
                var approvalResponse = await SaveApprovalMasterData(approverIdList, model.BusinessNeed ?? "", RowId, formId);

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

        public async Task<ResponseModel<List<ApprovalMatrix>>> GetApprovalQFRFForm(long empNum, long ccNum, string empDes, FixtureRequisitionData model)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_QFRFForm", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNo", empNum));
                cmd.Parameters.Add(new SqlParameter("@ccnum", ccNum));
                cmd.Parameters.Add(new SqlParameter("@empDes", empDes));
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

                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
                var client = new HttpClient(handler);
                client.BaseAddress = new Uri(conString);
                client.DefaultRequestHeaders.Accept.Clear();
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

        public async Task<dynamic> ViewQFRFFormData(int rowId, int formId)
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
                var response1 = await client1.GetAsync("_api/web/lists/GetByTitle('FixtureRequisitionForm')/items?$select=*"
  + "&$filter=(ID eq '" + rowId + "')&$expand=AttachmentFiles");
                var responseText1 = await response1.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                if (!string.IsNullOrEmpty(responseText1))
                {
                    var SUCFUserResult = JsonConvert.DeserializeObject<FixtureRequisitionModel>(responseText1, settings);
                    URCFData.one = SUCFUserResult.FixtureRequisitionResults.FixtureRequisitionData;
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

        public async Task<int> UpdateData(FixtureRequisitionData model, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("QFRF") ? GlobalClass.ListNames["QFRF"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {
                int RowId = 0;

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newRow = list.GetItemById(formId);

                newRow["ARpsPin"] = model.ARpsPin;
                newRow["ARpsPinRemark"] = model.ARpsPinRemark;
                newRow["AClamps"] = model.AClamps;
                newRow["AClampsRemark"] = model.AClampsRemark;
                newRow["AWheels"] = model.AWheels;
                newRow["AWheelsRemark"] = model.AWheelsRemark;
                newRow["ARpsStick"] = model.ARpsStick;
                newRow["ARpsStickRemark"] = model.ARpsStickRemark;
                newRow["ALoseElement"] = model.ALoseElement;
                newRow["ALoseRemark"] = model.ALoseRemark;
                newRow["AMylers"] = model.AMylers;
                newRow["AMylerRemark"] = model.AMylerRemark;
                newRow["APinThreads"] = model.APinThreads;
                newRow["APinRemark"] = model.APinRemark;
                newRow["ARestingPads"] = model.ARestingPads;
                newRow["APadsRemark"] = model.APadsRemark;
                newRow["ASlidersRemark"] = model.ASlidersRemark;
                newRow["ASliders"] = model.ASliders;
                newRow["AKugel"] = model.AKugel;
                newRow["AKugelRemark"] = model.AKugelRemark;
                newRow.Update();
                _context.Load(newRow);
                _context.ExecuteQuery();
                RowId = newRow.Id;

            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

        public async Task<int> UpdateCondition(QualityMeisterbockCubingData model, UserData user)
        {

            ClientContext _context = new ClientContext(new Uri(conString));
            _context.Credentials = new NetworkCredential($"{user.DomainName}\\{user.UserName}", user.Password);
            Web web = _context.Web;
            var listName = GlobalClass.ListNames.ContainsKey("QMCR") ? GlobalClass.ListNames["QMCR"] : "";
            if (listName == "")
                return 0;
            int formId = Convert.ToInt32(model.FormSrId);
            try
            {
                int RowId = 0;

                List list = _context.Web.Lists.GetByTitle(listName);
                ListItem newItem = list.GetItemById(formId);

                newItem["ConditionPostTrial"] = model.ConditionPostTrial;

                newItem.Update();
                _context.Load(newItem);
                _context.ExecuteQuery();
                RowId = newItem.Id;

            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }

            return 1;
        }

    }
}
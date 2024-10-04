using Skoda_DCMS.App_Start;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.Helpers
{
    public class EmailBodyBuilder
    {
        SqlConnection con;
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public readonly string commonSubjectCode = ConfigurationManager.AppSettings["CommonSubject"];
        public async Task<List<EmailDataModel>> CreateEmails(EmailDataModel emailData)
        {
            List<EmailDataModel> emails = new List<EmailDataModel>();
            try
            {
                if (emailData.Action == FormStates.Submit)
                {
                    //mail for on behalf
                    if (emailData.OnBehalfSender != null && !string.IsNullOrEmpty(emailData.OnBehalfSender.Email))
                    {
                        var onBehalfEmailBody = GetEmailBodyForRequestSubmissionOnBehalf(emailData.OnBehalfSender.FullName, emailData.RequestId, emailData.Sender.FullName);
                        emails.Add(new EmailDataModel($"{emailData.RequestId} request submitted on your behalf - {emailData.FormName}", onBehalfEmailBody, new List<UserData>() { emailData.OnBehalfSender }));
                    }
                    //mail for submitter
                    //var submitterEmailBody = GetEmailBodyForRequestSubmission(emailData.Sender.FullName, emailData.RequestId);
                    //emails.Add(new EmailDataModel($"{emailData.RequestId} Request Submitted - {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mails for approvers
                    if (emailData.Recipients.Count() != 0)
                    {
                        foreach (var receiver in emailData.Recipients)
                        {
                            var UserName = "";
                            var Id = "";
                            var Designation = "";
                            var IsAction = "0";
                            var ActionQuery = "";
                            var Level = "";
                            SqlDataAdapter adapter = new SqlDataAdapter();
                            DataTable ds = new DataTable();
                            SqlCommand cmd = new SqlCommand();
                            con = new SqlConnection(sqlConString);
                            cmd = new SqlCommand("USP_GetEmployeeDataByEmailId", con);
                            cmd.Parameters.Add(new SqlParameter("@Email", receiver.Email));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd;
                            con.Open();
                            adapter.Fill(ds);
                            con.Close();
                            if (ds.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds.Rows.Count; i++)
                                {
                                    UserName = ds.Rows[0]["UserName"] != DBNull.Value && ds.Rows[0]["UserName"] != "" ? Convert.ToString(ds.Rows[0]["UserName"]) : "";
                                }
                            }
                            SqlCommand cmd1 = new SqlCommand();
                            SqlDataAdapter adapter1 = new SqlDataAdapter();
                            DataTable ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_GetApprovalMasterByFormId", con);
                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", UserName));
                            cmd1.Parameters.Add(new SqlParameter("@FormId", emailData.FormId));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    Id = ds1.Rows[0]["Id"] != DBNull.Value && ds1.Rows[0]["Id"] != "" ? Convert.ToString(ds1.Rows[0]["Id"]) : "";
                                    Designation = ds1.Rows[0]["Designation"] != DBNull.Value && ds1.Rows[0]["Designation"] != "" ? Convert.ToString(ds1.Rows[0]["Designation"]) : "";
                                    Level = ds1.Rows[0]["Level"] != DBNull.Value && ds1.Rows[0]["Level"] != "" ? Convert.ToString(ds1.Rows[0]["Level"]) : "";
                                }
                            }
                            cmd1 = new SqlCommand();
                            adapter1 = new SqlDataAdapter();
                            ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                            cmd1.Parameters.Add(new SqlParameter("@UniqueFormName", emailData.UniqueFormName));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    IsAction = ds1.Rows[0]["IsAction"] != DBNull.Value && ds1.Rows[0]["IsAction"] != "" ? Convert.ToString(ds1.Rows[0]["IsAction"]) : "0";
                                    ActionQuery = ds1.Rows[0]["ActionQuery"] != DBNull.Value && ds1.Rows[0]["ActionQuery"] != "" ? Convert.ToString(ds1.Rows[0]["ActionQuery"]) : "";
                                }
                            }
                            if (IsAction == "1")
                            {
                                List<string> AQ = ActionQuery.Split(',').ToList<string>();

                                for (int i = 0; i < AQ.Count; i++)
                                {
                                    if (AQ[i] == Designation)
                                    {
                                        IsAction = "1";
                                    }
                                    else if (AQ[i] == Level)
                                    {
                                        IsAction = "1";
                                    }
                                }
                            }
                            string body = await GetEmailBodyForRequestApproval(receiver.FullName,
                                emailData.RequestId, emailData.FormName, emailData.Sender == null ? null : emailData.Sender.FullName, UserName, Id, IsAction, emailData == null ? null : emailData);
                            emailData.Subject = $"Request {emailData.RequestId} received for Approval- {emailData.FormName}";
                            var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                            emails.Add(emailModel);
                        }
                    }
                }

                else if (emailData.Action == FormStates.ReSubmit)
                {
                    //mail for on behalf
                    if (emailData.OnBehalfSender != null && !string.IsNullOrEmpty(emailData.OnBehalfSender.Email))
                    {
                        var onBehalfEmailBody = GetEmailBodyForRequestSubmissionOnBehalf(emailData.OnBehalfSender.FullName, emailData.RequestId, emailData.Sender.FullName, true);
                        emails.Add(new EmailDataModel($"{emailData.RequestId} request Re-Submitted on your behalf - {emailData.FormName}", onBehalfEmailBody, new List<UserData>() { emailData.OnBehalfSender }));
                    }
                    //mail for submitter
                    // var submitterEmailBody = GetEmailBodyForRequestSubmission(emailData.Sender.FullName, emailData.RequestId, true);
                    // emails.Add(new EmailDataModel($"{emailData.RequestId} Request Re-Submitted - {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mails for approvers
                    if (emailData.Recipients.Count() != 0)
                    {
                        foreach (var receiver in emailData.Recipients)
                        {
                            var UserName = "";
                            var Id = "";
                            var Designation = "";
                            var IsAction = "0";
                            var ActionQuery = "";
                            var Level = "";
                            SqlDataAdapter adapter = new SqlDataAdapter();
                            DataTable ds = new DataTable();
                            SqlCommand cmd = new SqlCommand();
                            con = new SqlConnection(sqlConString);
                            cmd = new SqlCommand("USP_GetEmployeeDataByEmailId", con);
                            cmd.Parameters.Add(new SqlParameter("@Email", receiver.Email));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd;
                            con.Open();
                            adapter.Fill(ds);
                            con.Close();
                            if (ds.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds.Rows.Count; i++)
                                {
                                    UserName = ds.Rows[0]["UserName"] != DBNull.Value && ds.Rows[0]["UserName"] != "" ? Convert.ToString(ds.Rows[0]["UserName"]) : "";
                                }
                            }
                            SqlCommand cmd1 = new SqlCommand();
                            SqlDataAdapter adapter1 = new SqlDataAdapter();
                            DataTable ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_GetApprovalMasterByFormId", con);
                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", UserName));
                            cmd1.Parameters.Add(new SqlParameter("@FormId", emailData.FormId));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    Id = ds1.Rows[0]["Id"] != DBNull.Value && ds1.Rows[0]["Id"] != "" ? Convert.ToString(ds1.Rows[0]["Id"]) : "";
                                    Designation = ds1.Rows[0]["Designation"] != DBNull.Value && ds1.Rows[0]["Designation"] != "" ? Convert.ToString(ds1.Rows[0]["Designation"]) : "";
                                    Level = ds1.Rows[0]["Level"] != DBNull.Value && ds1.Rows[0]["Level"] != "" ? Convert.ToString(ds1.Rows[0]["Level"]) : "";

                                }
                            }
                            cmd1 = new SqlCommand();
                            adapter1 = new SqlDataAdapter();
                            ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                            cmd1.Parameters.Add(new SqlParameter("@UniqueFormName", emailData.UniqueFormName));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    IsAction = ds1.Rows[0]["IsAction"] != DBNull.Value && ds1.Rows[0]["IsAction"] != "" ? Convert.ToString(ds1.Rows[0]["IsAction"]) : "0";
                                    ActionQuery = ds1.Rows[0]["ActionQuery"] != DBNull.Value && ds1.Rows[0]["ActionQuery"] != "" ? Convert.ToString(ds1.Rows[0]["ActionQuery"]) : "";
                                }
                            }
                            if (IsAction == "1")
                            {
                                List<string> AQ = ActionQuery.Split(',').ToList<string>();

                                for (int i = 0; i < AQ.Count; i++)
                                {
                                    if (AQ[i] == Designation)
                                    {
                                        IsAction = "1";
                                    }
                                    else if (AQ[i] == Level)
                                    {
                                        IsAction = "1";
                                    }
                                }
                            }
                            string body = await GetEmailBodyForRequestApproval(receiver.FullName,
                                emailData.RequestId, emailData.FormName, emailData.Sender.FullName, UserName, Id, IsAction, emailData == null ? null : emailData);
                            emailData.Subject = $"Request {emailData.RequestId} received for Approval- {emailData.FormName}";
                            var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                            emails.Add(emailModel);
                        }
                    }
                }

                else if (emailData.Action == FormStates.PartialApproval)
                {
                    var submitterEmailBody = GetEmailBodyForPartialApprovalToSubmitter(
                            emailData.Sender.FullName,
                            emailData.RequestId,
                            emailData.Recipients.Any(x => x.IsCurrentApprover) ? emailData.Recipients.Where(x => x.IsCurrentApprover).FirstOrDefault().EmployeeName : ""
                            );

                    emails.Add(new EmailDataModel($"{emailData.RequestId} Request Status Update- {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mail for next approvers
                    foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsNextApprover))
                    {
                        var UserName = "";
                        var Id = "";
                        var Designation = "";
                        var IsAction = "0";
                        var ActionQuery = "";
                        var Level = "";
                        SqlDataAdapter adapter = new SqlDataAdapter();
                        DataTable ds = new DataTable();
                        SqlCommand cmd = new SqlCommand();
                        con = new SqlConnection(sqlConString);
                        cmd = new SqlCommand("USP_GetEmployeeDataByEmailId", con);
                        cmd.Parameters.Add(new SqlParameter("@Email", receiver.Email));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmd.CommandType = CommandType.StoredProcedure;
                        adapter.SelectCommand = cmd;
                        con.Open();
                        adapter.Fill(ds);
                        con.Close();
                        if (ds.Rows.Count > 0)
                        {
                            for (int i = 0; i < ds.Rows.Count; i++)
                            {
                                UserName = ds.Rows[0]["UserName"] != DBNull.Value && ds.Rows[0]["UserName"] != "" ? Convert.ToString(ds.Rows[0]["UserName"]) : "";
                            }
                        }
                        SqlCommand cmd1 = new SqlCommand();
                        SqlDataAdapter adapter1 = new SqlDataAdapter();
                        DataTable ds1 = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_GetApprovalMasterByFormId", con);
                        cmd1.Parameters.Add(new SqlParameter("@ApproverName", UserName));
                        cmd1.Parameters.Add(new SqlParameter("@FormId", emailData.FormId));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter.SelectCommand = cmd1;
                        con.Open();
                        adapter.Fill(ds1);
                        con.Close();
                        if (ds1.Rows.Count > 0)
                        {
                            for (int i = 0; i < ds1.Rows.Count; i++)
                            {
                                Id = ds1.Rows[0]["Id"] != DBNull.Value && ds1.Rows[0]["Id"] != "" ? Convert.ToString(ds1.Rows[0]["Id"]) : "";
                                Designation = ds1.Rows[0]["Designation"] != DBNull.Value && ds1.Rows[0]["Designation"] != "" ? Convert.ToString(ds1.Rows[0]["Designation"]) : "";
                                Level = ds1.Rows[0]["Level"] != DBNull.Value && ds1.Rows[0]["Level"] != "" ? Convert.ToString(ds1.Rows[0]["Level"]) : "";
                            }
                        }
                        cmd1 = new SqlCommand();
                        adapter1 = new SqlDataAdapter();
                        ds1 = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                        cmd1.Parameters.Add(new SqlParameter("@UniqueFormName", emailData.UniqueFormName));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter.SelectCommand = cmd1;
                        con.Open();
                        adapter.Fill(ds1);
                        con.Close();
                        if (ds1.Rows.Count > 0)
                        {
                            for (int i = 0; i < ds1.Rows.Count; i++)
                            {
                                IsAction = ds1.Rows[0]["IsAction"] != DBNull.Value && ds1.Rows[0]["IsAction"] != "" ? Convert.ToString(ds1.Rows[0]["IsAction"]) : "0";
                                ActionQuery = ds1.Rows[0]["ActionQuery"] != DBNull.Value && ds1.Rows[0]["ActionQuery"] != "" ? Convert.ToString(ds1.Rows[0]["ActionQuery"]) : "";
                            }
                        }
                        if (IsAction == "1")
                        {
                            List<string> AQ = ActionQuery.Split(',').ToList<string>();

                            for (int i = 0; i < AQ.Count; i++)
                            {
                                if (AQ[i] == Designation)
                                {
                                    IsAction = "1";
                                }
                                else if (AQ[i] == Level)
                                {
                                    IsAction = "1";
                                }
                            }
                        }
                        string body = await GetEmailBodyForPartialApprovalToNextApprover(receiver.FullName,
                            emailData.RequestId, emailData.Sender.FullName, UserName, Id, IsAction, emailData);

                        emailData.Subject = $"Request {emailData.RequestId} received for Approval- {emailData.FormName}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                        emails.Add(emailModel);
                    }

                    //mail for current approver
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsCurrentApprover))
                    //{
                    //    string body = GetEmailBodyForPartialApprovalToCurrentApprover(receiver.FullName,
                    //        emailData.RequestId);

                    //    emailData.Subject = $"Request {emailData.RequestId} successfully Approved- {emailData.FormName}";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail sent for Parallel approvers(OR condition)
                    foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                    {
                        string status = "Approved";

                        string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                            emailData.RequestId, receiverParallel.FullName, status);

                        emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                        emails.Add(emailModel);
                    }
                    //mail sent for Parallel approvers(OR condition)
                }
                else if (emailData.Action == FormStates.Reject)
                {
                    var submitterEmailBody = GetEmailBodyForRejectionToSubmitter(
                            emailData.Sender.FullName,
                            emailData.RequestId,
                            emailData.Recipients.Any(x => x.IsCurrentApprover) ? emailData.Recipients.Where(x => x.IsCurrentApprover).FirstOrDefault().FullName : "",
                            emailData.Comment
                            );

                    emails.Add(new EmailDataModel($"{emailData.RequestId} Request Status Update- {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    ////mail for next approvers
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsNextApprover))
                    //{
                    //    string body = GetEmailBodyForPartialApprovalToNextApprover(receiver.FullName,
                    //        emailData.RequestId, emailData.Sender.FullName);

                    //    emailData.Subject = $"Request {emailData.RequestId} received for Approval";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail for current approver
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsCurrentApprover))
                    //{
                    //    string body = GetEmailBodyForRejectionToRejector(receiver.FullName,
                    //        emailData.RequestId);

                    //    emailData.Subject = $"Request {emailData.RequestId} successfully rejected- {emailData.FormName}";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail sent for Parallel approvers(OR condition)
                    foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                    {
                        string status = "Rejected";

                        string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                            emailData.RequestId, receiverParallel.FullName, status);

                        emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                        emails.Add(emailModel);
                    }
                    //mail sent for Parallel approvers(OR condition)

                }
                else if (emailData.Action == FormStates.Enquire)
                {
                    var submitterEmailBody = GetEmailBodyForEnquireToSubmitter(
                            emailData.Sender.FullName,
                            emailData.RequestId,
                            emailData.Recipients.Any(x => x.IsCurrentApprover) ? emailData.Recipients.Where(x => x.IsCurrentApprover).FirstOrDefault().FullName : "",
                            emailData.Comment
                            );

                    emails.Add(new EmailDataModel($"{emailData.RequestId} Request Status Update- {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    ////mail for next approvers
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsNextApprover))
                    //{
                    //    string body = GetEmailBodyForPartialApprovalToNextApprover(receiver.FullName,
                    //        emailData.RequestId, emailData.Sender.FullName);

                    //    emailData.Subject = $"Request {emailData.RequestId} received for Approval";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail for current approver
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsCurrentApprover))
                    //{
                    //    string body = GetEmailBodyForEnquireToEnquirer(receiver.FullName,
                    //        emailData.RequestId);

                    //    emailData.Subject = $"Enquiry for request {emailData.RequestId} successfully submitted- {emailData.FormName}";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail sent for Parallel approvers(OR condition)
                    foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                    {
                        string status = "Enquired";

                        string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                            emailData.RequestId, receiverParallel.FullName, status);

                        emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                        emails.Add(emailModel);
                    }
                    //mail sent for Parallel approvers(OR condition)
                }
                else if (emailData.Action == FormStates.FinalApproval)
                {
                    var commonSubject = "";
                    var finalBodyForSubmitter = "";

                    if (commonSubjectCode.ToLower() == "uat")
                    {
                        commonSubject = $"*********eforms – Test Email - Please ignore - Request Id : {emailData.RequestId} - {emailData.FormName}" + "*******************";
                    }
                    else if (commonSubjectCode.ToLower() == "prod")
                    {
                        commonSubject = $"*********eforms – Request Id : {emailData.RequestId} - {emailData.FormName}" + "*******************";
                    }
                    else
                    {
                        commonSubject = $"*********eforms – Test Email - Please ignore - Request Id : {emailData.RequestId} - {emailData.FormName}" + "*******************";
                    }
                    //mail for current approver
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsCurrentApprover))
                    //{
                    //    string body = GetEmailBodyForPartialApprovalToCurrentApprover(receiver.FullName,
                    //        emailData.RequestId);
                    //    emailData.Subject = $"Request {emailData.RequestId} successfully Approved- {emailData.FormName}";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}
                    //mail sent for Parallel approvers(OR condition)
                    if (emailData.ParallelRecipients != null)
                    {
                        foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                        {
                            string status = "Approved";

                            string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                                emailData.RequestId, receiverParallel.FullName, status);

                            emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName}";
                            var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                            emails.Add(emailModel);
                        }
                    }
                    //mail sent for Parallel approvers(OR condition)
                    var SenderFullName = "Sir/Mam";
                    if (emailData.Sender != null)
                    {
                        SenderFullName = emailData.Sender.FullName;
                    }
                    finalBodyForSubmitter = "<span>Dear " + SenderFullName + ",</span> <br/>";
                    finalBodyForSubmitter += "<span>Your Request " + emailData.UniqueFormName + emailData.FormId + " is approved successfully.</span> <br/>";

                    switch (emailData.UniqueFormName)
                    {
                        case "ITCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetITClearanceFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "COIF":
                            {
                                var finalTeamEmailIds = new List<string>() { "compliance@skoda-vw.co.in", "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                //  var finalBody = await commonBAL.GetConflictOfInterestFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                //  finalBodyForSubmitter += finalBody;
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetConflictOfInterestFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "KSRMUICF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetKSRMUserIdCreationFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                // emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "RADLF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetResourceAccountDLFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                // emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "SRCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetSRCFFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "GUICF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetGaneshIdFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                //finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "SPRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                //var toList = new List<string>() { "mumbaiassetadmin@skoda-vw.co.in", "servicedeskmanager.nscindia@skoda-vw.co.in" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetSmartPhoneFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);


                                //   emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));
                                //if (emailData.ToIds != null)
                                //    emailData.ToIds.AddRange(emailData.ToIds);
                                //else
                                //    emailData.ToIds = emailData.ToIds;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });

                                break;
                            }
                        case "DBRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetDataBackupRestoreFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                //finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "IA":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                //var tempEmailData = await commonBAL.GetInternetAccessFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                var tempEmailData = await commonBAL.GetInternetAccessFAM_SQL(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                //if (emailData.Sender == null)
                                //{
                                //    emailData.Sender.Email = "eform.notifications@mobinexttech.com";
                                //}
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));
                                //  emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds.ToList(),
                                    CCIds = emailData.CCIds.ToList(),
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x }).ToList()
                                });
                                //emails = emails.Select(x => new EmailDataModel() {  }).ToList();
                                //for (int i = 0; i < emails.Count; i++)
                                //{
                                //    if(emails[i].OnBehalfSender == null)
                                //    {
                                //        UserData user = new UserData();
                                //        user.Email = "eform.notifications@mobinexttech.com";
                                //        emails[i].OnBehalfSender = user;
                                //    }

                                //}
                                break;
                            }
                        case "SRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetSoftwareRequisitionFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                // emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "SFF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetSharedFolderFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                //finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "ITARF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                //var toList = new List<string>() { "mumbaiassetadmin@skoda-vw.co.in", "servicedeskmanager.nscindia@skoda-vw.co.in" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetITAssetRequisitionFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);

                                // emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));
                                //if (emailData.ToIds != null)
                                //    emailData.ToIds.AddRange(emailData.ToIds);
                                //else
                                //    emailData.ToIds = emailData.ToIds;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "CBRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com",
                                "extern.ajay.mohite@skoda-vw.co.in",
                                "extern.vitthal.lambhade@skoda-vw.co.in",
                                "extern.shrirang.kulkarni@skoda-vw.co.in",
                                "extern.pandurang.khetmalis@skoda-vw.co.in",
                                "extern.kaustubh.gole@skoda-vw.co.in",
                                "Volkswagen-Pune-Transport@skoda-vw.co.in" };

                                var commonBAL = new CommonBAL();
                                var tempEmailData = await commonBAL.GetCabBookingFormBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                //emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);

                                emailData.CCIds = new List<string>() { "prashant.bhosale@skoda-vw.co.in" };

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "SFO":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetSuggestionForOrderFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = new List<string>();
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    //Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "DNF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetDeviationNoteFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = new List<string>();
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "IDCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetIDCardFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                if (emailData.ToIds != null)
                                    emailData.ToIds.AddRange(tempEmailData.ToIds);
                                else
                                    emailData.ToIds = tempEmailData.ToIds;
                                // finalBodyForSubmitter += emailData.Body;
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));
                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                var ids = new List<string>();
                                if (emailData.ToIds != null && emailData.ToIds.Count > 0)
                                    ids.AddRange(emailData.ToIds);
                                if (tempEmailData.ToIds != null && tempEmailData.ToIds.Count > 0)
                                    ids.AddRange(tempEmailData.ToIds);
                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = ids,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "RIDCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetReissueIDCardFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                if (emailData.ToIds != null)
                                    emailData.ToIds.AddRange(tempEmailData.ToIds);
                                else
                                    emailData.ToIds = tempEmailData.ToIds;
                                // finalBodyForSubmitter += emailData.Body;
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "IJPF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetInternalJobPostingFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                if (emailData.ToIds != null)
                                    emailData.ToIds.AddRange(tempEmailData.ToIds);
                                else
                                    emailData.ToIds = tempEmailData.ToIds;

                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "BTF":
                            {
                                //var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com",
                                //"extern.ajay.mohite@skoda-vw.co.in",
                                //"extern.vitthal.lambhade@skoda-vw.co.in",
                                //"extern.shrirang.kulkarni@skoda-vw.co.in",
                                //"extern.pandurang.khetmalis@skoda-vw.co.in",
                                //"extern.kaustubh.gole@skoda-vw.co.in",
                                //"Volkswagen-Pune-Transport@skoda-vw.co.in" };

                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetBusTranFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                if (emailData.ToIds != null)
                                    emailData.ToIds.AddRange(tempEmailData.ToIds);
                                else
                                    emailData.ToIds = tempEmailData.ToIds;


                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "OCRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetOCRFFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "ECF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetEmployeeClearanceFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));
                                var ids = new List<string>();
                                if (emailData.ToIds != null && emailData.ToIds.Count > 0)
                                    ids.AddRange(emailData.ToIds);
                                if (tempEmailData.ToIds != null && tempEmailData.ToIds.Count > 0)
                                    ids.AddRange(tempEmailData.ToIds);
                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = ids,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "DAF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetDrivingAuthorizationFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                // finalBodyForSubmitter += emailData.Body;
                                GetAllRelevantEmails(ref emailData);
                                GetEmailsFromAssetLocation(ref emailData);
                                //emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "CRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com", "courier.support@skoda-vw.co.in" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetCourierRequestFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "DARF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                var tempEmailData = await commonBAL.GetDoorAccessFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser, 0);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.IDList = tempEmailData.IDList;
                                GetIDCardOfficeApprovers(ref emailData);
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "PAF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetPAFFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;
                                var pafLocation = tempEmailData.PAFLocation;

                                if (emailData.ToIds == null)
                                {
                                    if (pafLocation.Contains("Pune"))
                                    {
                                        emailData.ToIds = new List<string> { "extern.ashitosh.takale@skoda-vw.co.in" };
                                    }
                                    else
                                    {
                                        emailData.ToIds = new List<string> { "sonesh.choudhary@skoda-vw.co.in" };
                                    }
                                }
                                else
                                {
                                    if (pafLocation.Contains("Pune"))
                                    {
                                        emailData.ToIds.Add("extern.ashitosh.takale@skoda-vw.co.in");
                                    }
                                    else
                                    {
                                        emailData.ToIds.Add("sonesh.choudhary@skoda-vw.co.in");
                                    }
                                }

                                List<KeyValuePair<string, Object>> formData = tempEmailData.ExtraFormData;
                                var isExceptionalPhoto = (bool)formData.Where(x => x.Key == "IsExceptionalPhoto").FirstOrDefault().Value;

                                if (isExceptionalPhoto)
                                {
                                    if (emailData != null)
                                    {
                                        emailData.ToIds.Add("mithin.selven@skoda-vw.co.in");
                                        emailData.ToIds.Add("vaishali.joshi@skoda-vw.co.in");
                                        emailData.ToIds.Add("urmimala.dutta@skoda-vw.co.in");
                                        emailData.ToIds.Add("metabelle.lobo@skoda-vw.co.in");
                                    }
                                    else
                                    {
                                        emailData.ToIds = new List<string> { "mithin.selven@skoda-vw.co.in", "vaishali.joshi@skoda-vw.co.in", "urmimala.dutta@skoda-vw.co.in", "metabelle.lobo@skoda-vw.co.in" };
                                    }
                                }

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "BEI":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();

                                var tempEmailData = await commonBAL.GetBEIFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "MRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                //"yogesh.oza@skoda-vw.co.in",
                                //"veenit.nikam@skoda-vw.co.in",
                                //"shivlal.kasode@skoda-vw.co.in",
                                //"vikram.kshire@skoda-vw.co.in",
                                //"amol.pimple@skoda-vw.co.in",
                                //"ajit.awati@skoda-vw.co.in",
                                //"rajesh.andhale@skoda-vw.co.in",
                                //"subhash.mangate@skoda-vw.co.in",
                                //"rahul.gopale@skoda-vw.co.in",
                                //"gopal.gawai@skoda-vw.co.in",
                                //"bansi.gapat@skoda-vw.co.in",
                                //"motiram.sonawane@skoda-vw.co.in",
                                //"dilip.sangle@skoda-vw.co.in",
                                //"hukam.singh.panwar@skoda-vw.co.in",
                                //"amol.pawar2@skoda-vw.co.in",
                                //"bhalchandra.kothekar@skoda-vw.co.in",
                                //"atul.kulkarni1@skoda-vw.co.in",
                                //"basudev.swain@skoda-vw.co.in" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetMaterialRequestFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "SUCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com", "ServiceDeskIndia@skoda-vw.co.in" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();

                                var tempEmailData = await commonBAL.GetSUCFFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                //emailData.ToIds = tempEmailData.ToIds;
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);
                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "DLIC":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();

                                var tempEmailData = await commonBAL.GetDLICFormFAM(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;

                                GetFinalEmailReceipient(ref emailData, "DLIC");
                                GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "GAIF":
                            {
                                var finalTeamEmailIds = new List<string>() { "compliance@skoda-vw.co.in", "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetGiftsInvitationBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "NGCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetNewGlobalCodeBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "URCF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetUserRequestBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "ISLS":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetISLSBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                //emailData.ToIds = tempEmailData.ToIds;
                                var ISLSSubject = "";
                                if (commonSubjectCode.ToLower() == "uat")
                                {
                                    ISLSSubject = $"*********eforms – Test Email - Please ignore - Request Id : {emailData.RequestId} - {emailData.FormName} & Global Process No : {tempEmailData.Comment}" + "*******************";
                                }
                                else if (commonSubjectCode.ToLower() == "prod")
                                {
                                    ISLSSubject = $"*********eforms – Request Id : {emailData.RequestId} - {emailData.FormName} & Global Process No : {tempEmailData.Comment}" + "*******************";
                                }
                                else
                                {
                                    ISLSSubject = $"*********eforms – Test Email - Please ignore - Request Id : {emailData.RequestId} - {emailData.FormName} & Global Process No : {tempEmailData.Comment}" + "*******************";
                                }
                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = ISLSSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "APFP":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetAPFPBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;
                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "QMCR":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetQMCRBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "IMAC":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetIMACBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.ToIds = tempEmailData.ToIds;
                                emailData.CCIds = tempEmailData.CCIds;
                                emailData.Recipients = tempEmailData.Recipients;
                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "MMRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetMMRFBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "QFRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetQFRFBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "EQSA":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetEQSABody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "IPAF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                emailData.ToIds = new List<string>();
                                var tempEmailData = await commonBAL.GetIPAFFBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                        case "POCRF":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };
                                var commonBAL = new CommonBAL();
                                var tempEmailData = await commonBAL.GetPOCRFBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser, 0);
                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                emailData.IDList = tempEmailData.IDList;
                                GetIDCardOfficeApprovers(ref emailData);
                                //GetAllRelevantEmails(ref emailData);
                                //GetEmailsFromAssetLocation(ref emailData);

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                    }
                }
                return emails;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return emails;
            }
        }

        public string GetEmailBodyForRequestSubmission(string empName, string requestId, bool IsResubmit = false)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + empName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";

            body = body + "<br>";

            body = body + $"Your Request <b> {requestId} </b> is {(IsResubmit ? "re-submitted" : "submitted")} successfully.";

            body = body + "<br><br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";
            return body;
        }

        public string GetEmailBodyForRequestSubmissionOnBehalf(string empName, string requestId, string onBehalfSender, bool IsResubmit = false)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + empName + ",</span> <br/>";
            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body = body + $"A Request with Id <b> {requestId} </b> was {(IsResubmit ? "re-submitted" : "submitted")} on your behalf by {onBehalfSender}";

            body = body + "<br><br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public async Task<string> GetEmailBodyForRequestApproval(string empName, string requestId, string requestType, string submitterName, string UserName, string AppRowId, string IsAction, EmailDataModel EmailData)
        {
            string body = string.Empty;
            string attbody = "_blank";
            string userName = UserName;//.Replace(" ", "%20");
            string EmpName = empName.Replace(" ", "%20");
            string appRowId = AppRowId;
            string AppHostUrl = ConfigurationManager.AppSettings["AppHostUrl"];
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            var TDBody = await TransactionBody(EmailData.UniqueFormName, EmailData.FormId, EmailData.CurrentUser);
            body = "<span>Dear " + empName + ",</span> <br/>";

            //body += "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body += "<br>";

            body += "<br> <table width=\"100%\">";

            if (!string.IsNullOrEmpty(requestId))
            {
                MatchCollection mc = Regex.Matches(requestId, @"^[a-z]*", RegexOptions.IgnoreCase);
                foreach (Match m in mc)
                {
                    switch (m.ToString().ToLower())
                    {
                        case "dlic":
                            body += submitterName + " has sent you a request to review and approve. Please check if the selected applications are installed on your machines and provide confirmation for process closure.<br><br> <table width=\"100%\">";
                            break;
                        default:
                            body += submitterName + " has sent you a request to review and approve. Kindly click on the Application URL to take the necessary action.<br><br> <table width=\"100%\">";
                            break;
                    }
                }
            }
            else
            {
                body += submitterName + " has sent you a request to review and approve. Kindly click on the Application URL to take the necessary action.<br><br>";// <table width=\"100%\">";
            }
            //body += "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\">Request Information</th></tr>";
            //body += "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            //body += "<tr><td>" + "Request Description: " + requestType + "</td></tr>";
            //body += "<tr><td>" + "Application URL: " + applicationURL + "</td></tr>";

            body += TDBody;
            body += "</table><br><br>";

            if (IsAction == "0")
            {
                body += "<div style='display: inline-block;'><a target='_blank'  href='" + AppHostUrl + "Login/SaveResponse?response=Approved&appRowId=" + AppRowId + "&UserName=" + UserName + "&UserFullname=" + EmpName + "' style='background: #0C6980;border-radius: 5px;border: 1px solid #0C6980;font-size: 1rem;line-height: 173.7%;position: relative;padding: 0 1rem;height: 44px;display: flex;align-items: center;text-align: center;justify-content: center;text-transform: uppercase;color: #000; margin-right: 5px;' >Approved</a></div>" +
                       "<div style ='display: inline-block;' ><a target='_blank' href='" + AppHostUrl + "Login/SaveResponse?response=Rejected&appRowId=" + AppRowId + "&UserName=" + UserName + "&UserFullname=" + EmpName + "' style ='background: rgb(210,38,48);border-radius: 5px;border: 1px solid rgb(210,38,48);font-size: 1rem;line-height: 173.7%;position: relative;padding: 0 1rem;height: 44px;display: flex;align-items: center;text-align: center;justify-content: center;text-transform: uppercase;color: #000;'>Rejected</a></div> " +
                       "<div style ='display: inline-block;' ><a target='_blank' href='" + AppHostUrl + "Login/SaveResponse?response=Enquired&appRowId=" + AppRowId + "&UserName=" + UserName + "&UserFullname=" + EmpName + "' style ='background: #bebebe;border-radius: 5px;border: 1px solid #bebebe;font-size: 1rem;line-height: 173.7%;position: relative;padding: 0 1rem;height: 44px;display: flex;align-items: center;text-align: center;justify-content: center;text-transform: uppercase;color: #000;' >Enquired</a></div> ";
                body += "<br><br>";
               
            }
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForPartialApprovalToSubmitter(string submitter, string requestId, string approver)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + submitter + ",</span> <br/>";

            body = body + "Your request with Id : <b>" + requestId + "</b> has been approved by " + approver;

            body = body + "<br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public async Task<string> GetEmailBodyForPartialApprovalToNextApprover(string approver, string requestId, string submitterName, string UserName, string AppRowId, string IsAction, EmailDataModel EmailData)
        {
            string body = string.Empty;
            string attbody = "_blank";
            string userName = UserName.Replace(" ", "%20");
            string EmpName = approver.Replace(" ", "%20");
            string appRowId = AppRowId;
            string AppHostUrl = ConfigurationManager.AppSettings["AppHostUrl"];
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            var TDBody = await TransactionBody(EmailData.UniqueFormName, EmailData.FormId, EmailData.CurrentUser);
            body = "<span>Dear " + approver + ",</span> <br/>";

            //body += "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body += "<br>";

            body += "<br> <table width=\"100%\">";
            body += submitterName + " has sent you a request to review and approve. Kindly click on the Application URL to take the necessary action.<br><br> <table width=\"100%\">";
            //body += "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\">Request Information</th></tr>";
            //body += "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            //body += "<tr><td>" + "Application URL: " + applicationURL + "</td></tr>";

            body += "</table><br><br>";
            body += TDBody;
            if (IsAction == "0")
            {
                body += "<div style='display: inline-block;'><a target='_blank'  href='" + AppHostUrl + "Login/SaveResponse?response=Approved&appRowId=" + AppRowId + "&UserName=" + UserName + "&UserFullname=" + EmpName + "' style='background: #0C6980;border-radius: 5px;border: 1px solid #0C6980;font-size: 1rem;line-height: 173.7%;position: relative;padding: 0 1rem;height: 44px;display: flex;align-items: center;text-align: center;justify-content: center;text-transform: uppercase;color: #000; margin-right: 5px;' >Approved</a></div>" +
                       "<div style ='display: inline-block;' ><a target='_blank' href='" + AppHostUrl + "Login/SaveResponse?response=Rejected&appRowId=" + AppRowId + "&UserName=" + UserName + "&UserFullname=" + EmpName + "' style ='background: rgb(210,38,48);border-radius: 5px;border: 1px solid rgb(210,38,48);font-size: 1rem;line-height: 173.7%;position: relative;padding: 0 1rem;height: 44px;display: flex;align-items: center;text-align: center;justify-content: center;text-transform: uppercase;color: #000;'>Rejected</a></div> " +
                       "<div style ='display: inline-block;' ><a target='_blank' href='" + AppHostUrl + "Login/SaveResponse?response=Enquired&appRowId=" + AppRowId + "&UserName=" + UserName + "&UserFullname=" + EmpName + "' style ='background: #bebebe;border-radius: 5px;border: 1px solid #bebebe;font-size: 1rem;line-height: 173.7%;position: relative;padding: 0 1rem;height: 44px;display: flex;align-items: center;text-align: center;justify-content: center;text-transform: uppercase;color: #000;' >Enquired</a></div> ";
                body += "<br><br>";
            }
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForPartialApprovalToCurrentApprover(string approver, string requestId)
        {
            string body = string.Empty;
            string applicationURL = ConfigurationManager.AppSettings["MyWorkPage"];
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            string hostName = ConfigurationManager.AppSettings["hostName"];

            body = "<span>Dear " + approver + ",</span> <br/>";

            //body += "<img src=\"" + hostName + eformsLogoPath.Replace("~", "") + "\" alt=\"\"></img>";
            body += "<br>";

            body += "<br>";
            body += $"You have successfully approved the request {requestId} <br>";

            body = body + "<br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForCancelledRequest(string empName, string requestId, string requestType, string submitterName, string cancelReason)
        {
            string body = string.Empty;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + empName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";

            body = "<br> <table width=\"100%\">";
            body = body + "Request Id: " + requestId + " has been cancelled by requester.<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\">Request Information</th></tr>";

            body = body + "<tr><td>" + "Request Description: " + requestType + "</td></tr>";
            body = body + "<tr><td>" + "Requester: " + submitterName + "</td></tr>";
            body = body + "<tr><td>" + "Reason for cancellation: " + cancelReason + "</td></tr>";
            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForApprovedRequest(string empName, string requestId, string approverName, string requestType)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + empName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body = body + "<br><br> <table width=\"100%\">";

            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Information</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + requestType + "</td></tr>";
            body = body + "<tr><td>" + "Status: " + "Processed Successfully in eforms." + "</td></tr>";
            body = body + "<tr><td>" + "For more details please visit the below link: " + applicationURL + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForRejectionToSubmitter(string submitter, string requestId, string approverName, string rejectionComments)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + submitter + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body += $"Your request <b> {requestId} </b> was Rejected. Following are the details <br>";
            body = body + "<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Rejected By: " + approverName + "</td></tr>";
            body = body + "<tr><td>" + "Status: " + "Rejected" + "</td></tr>";
            body = body + "<tr><td>" + "Comments: " + rejectionComments + "</td></tr>";
            body = body + "<tr><td>" + "For more details please visit the below link: " + applicationURL + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }
        public string GetEmailBodyForRejectionToRejector(string approverName, string requestId)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + approverName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body += $"You have successfully rejected the request {requestId} <br>";

            body = body + "<br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }
        public string GetEmailBodyForEnquireToSubmitter(string submitter, string requestId, string approverName, string enquireComments)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + submitter + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";
            body += $"Your request <b> {requestId} </b> was Enquired. Following are the details <br>";
            body = body + "<br><br> <table width=\"100%\">";

            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Details</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Enquired By: " + approverName + "</td></tr>";
            body = body + "<tr><td>" + "Status: " + "Enquired" + "</td></tr>";
            body = body + "<tr><td>" + "Comments: " + enquireComments + "</td></tr>";
            body = body + "<tr><td>" + "For more details please visit the below link: " + applicationURL + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }
        public string GetEmailBodyForEnquireToEnquirer(string approverName, string requestId)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + approverName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body += $"Your enquiry has been successfully submitted for the request {requestId} <br>";

            body = body + "<br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForOtherApproverColleagues(string empName, string requestId, string approverName, string status)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + empName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body = body + "<br><br> <table width=\"100%\">";

            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Information</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Action By: " + approverName + "</td></tr>";
            body = body + "<tr><td>" + "Status: " + status + "</td></tr>";
            body = body + "<tr><td>" + "Note: " + "Your Colleague " + approverName + " has taken the action on the above request." + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForForwardedRequest(string toEmpName, string requestId, string approverName, string requestType, string comments)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];
            body = "<span>Dear " + toEmpName + ",</span> <br/>";

            body = "<span>Your colleague " + approverName + " has sent a request to you for your approval.</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body = body + "<br><br> <table width=\"100%\">";

            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\"><b>Request Information</b></th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + requestType + "</td></tr>";
            body = body + "<tr><td>" + "Forwarded By: " + approverName + "</td></tr>";
            body = body + "<tr><td>" + "Status: " + "Forwarded" + "</td></tr>";
            body = body + "<tr><td>" + "Comments: " + comments + "</td></tr>";
            body = body + "<tr><td>" + "For more details please visit the below link: " + applicationURL + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForConflictOfInterestReview(string empName, string requestId, string requestType, string submitterName)
        {
            string body = string.Empty;
            string applicationURL = ConfigurationManager.AppSettings["MyWorkPage"];
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + empName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body = "<br> <table width=\"100%\">";
            body = body + submitterName + " has sent you a Conflict of Interest form for your review. Kindly click on the Application URL to take the necessary action.<br><br> <table width=\"100%\">";
            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\">Request Information</th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + requestType + "</td></tr>";
            body = body + "<tr><td>" + "Application URL: " + applicationURL + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        public string GetEmailBodyForConflictOfInterestHOD(string hodEmpName, string requestId, string requestType, string submitterName)
        {
            string body = string.Empty;
            string applicationURL = ConfigurationManager.AppSettings["MyWorkPage"];
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + hodEmpName + ",</span> <br/>";

            //body = body + "<img src=\"" + eformsLogoPath + "\" alt=\"\"></img>";
            body = body + "<br>";

            body = "<br> <table width=\"100%\">";
            body = body + "Conflict of Interest form for " + submitterName + " is reviewed and updated successfully." + "<br><br> <table width=\"100%\">";

            body = body + "<tr><th style=\"padding-top: 12px;padding-bottom: 12px;text-align: left;background-color: #4CAF50;color: white; border: 1px solid #ddd;\">Request Information</th></tr>";
            body = body + "<tr><td>" + "Request Id: " + requestId + "</td></tr>";
            body = body + "<tr><td>" + "Request Description: " + requestType + "</td></tr>";

            body = body + "</table><br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }

        /// <summary>
        /// Extension method - To distinct the records based on some field value.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>

        //public  IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        //{
        //    HashSet<TKey> seenKeys = new HashSet<TKey>();
        //    foreach (TSource element in source)
        //    {
        //        if (seenKeys.Add(keySelector(element)))
        //        {
        //            yield return element;
        //        }
        //    }
        //}

        public void GetAllRelevantEmails(ref EmailDataModel emailData)
        {
            CommonBAL comBAL = new CommonBAL();
            var dataList = comBAL.GetEmailIdByLocation(emailData.Location);
            emailData.ToIds = dataList.Where(x => x.IsManager == 0).Select(x => x.Email).ToList();
            emailData.CCIds = dataList.Where(x => x.IsManager == 1).Select(x => x.Email).ToList();
        }

        public void GetIDCardOfficeApprovers(ref EmailDataModel emailData)
        {
            CommonBAL comBAL = new CommonBAL();
            string commaString = string.Join(",", emailData.IDList);
            var dataList = comBAL.GetIDCardOfficeEmailId(commaString);
            emailData.ToIds = dataList.Select(x => x).ToList();
            //emailData.CCIds = dataList.Where(x => x.IsManager == 1).Select(x => x.Email).ToList();
        }

        public string GetEmailBodyForParallelApproval(string approver, string requestId, string parallelApprover, string status)
        {
            string body = string.Empty;
            string applicationURL = GlobalClass.ApplicationUrl;
            string eformsLogoPath = ConfigurationManager.AppSettings["eformsLogoPath"];

            body = "<span>Dear " + parallelApprover + ",</span> <br/>";

            body = body + "Request Id : <b>" + requestId + "</b> has been " + status + " by " + approver;

            body = body + "<br>";
            body = body + "Thanks & Regards, ";
            body = body + "<br>";
            body = body + "e|forms Team <br><br>";
            body = body + "<img src=cid:LogoImage alt=\"\"></img>";

            return body;
        }
        public void GetEmailsFromAssetLocation(ref EmailDataModel emailData)
        {
            CommonBAL comBAL = new CommonBAL();
            var dataList = comBAL.GetEmailIdByAssetLocation(emailData.Location);
            var assetEmailId = dataList.FirstOrDefault() == null ? "" : dataList.FirstOrDefault().AssetEmailId;
            if (!string.IsNullOrEmpty(assetEmailId))
            {
                var eSplit = assetEmailId.Split(',');
                if (emailData.ToIds == null)
                    emailData.ToIds = eSplit.ToList();
                else
                    emailData.ToIds.AddRange(eSplit.ToList());
            }
        }

        public void GetFinalEmailReceipient(ref EmailDataModel emailData, string uniqueFormName)
        {
            CommonBAL comBAL = new CommonBAL();
            var dataList = comBAL.GetFinalEmailReceipient(emailData.Location, uniqueFormName);
            if (dataList != null && dataList.Count > 0)
            {
                if (emailData.ToIds == null)
                    emailData.ToIds = dataList.ToList();
                else
                    emailData.ToIds.AddRange(dataList.ToList());
            }
        }

        public async Task<List<EmailDataModel>> CreateEmailForISLS(EmailDataModel emailData, string GPNo)
        {
            List<EmailDataModel> emails = new List<EmailDataModel>();
            try
            {
                if (emailData.CurrentUser != null)
                {
                    var commonBAL = new CommonBAL();
                    var tempEmailData = await commonBAL.GetISLSBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);
                    GPNo = tempEmailData.Comment;
                }

                if (emailData.Action == FormStates.Submit)
                {
                    //mail for on behalf
                    if (emailData.OnBehalfSender != null && !string.IsNullOrEmpty(emailData.OnBehalfSender.Email))
                    {
                        var onBehalfEmailBody = GetEmailBodyForRequestSubmissionOnBehalf(emailData.OnBehalfSender.FullName, emailData.RequestId, emailData.Sender.FullName);
                        emails.Add(new EmailDataModel($"{emailData.RequestId} request submitted on your behalf - {emailData.FormName} & Global Process No : {GPNo} ", onBehalfEmailBody, new List<UserData>() { emailData.OnBehalfSender }));
                    }
                    //mail for submitter
                    //var submitterEmailBody = GetEmailBodyForRequestSubmission(emailData.Sender.FullName, emailData.RequestId);
                    //emails.Add(new EmailDataModel($"{emailData.RequestId} Request Submitted - {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mails for approvers
                    if (emailData.Recipients.Count() != 0)
                    {
                        foreach (var receiver in emailData.Recipients)
                        {
                            var UserName = "";
                            var Id = "";
                            var Designation = "";
                            var IsAction = "0";
                            var ActionQuery = "";
                            var Level = "";
                            SqlDataAdapter adapter = new SqlDataAdapter();
                            DataTable ds = new DataTable();
                            SqlCommand cmd = new SqlCommand();
                            con = new SqlConnection(sqlConString);
                            cmd = new SqlCommand("USP_GetEmployeeDataByEmailId", con);
                            cmd.Parameters.Add(new SqlParameter("@Email", receiver.Email));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd;
                            con.Open();
                            adapter.Fill(ds);
                            con.Close();
                            if (ds.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds.Rows.Count; i++)
                                {
                                    UserName = ds.Rows[0]["UserName"] != DBNull.Value && ds.Rows[0]["UserName"] != "" ? Convert.ToString(ds.Rows[0]["UserName"]) : "";
                                }
                            }
                            SqlCommand cmd1 = new SqlCommand();
                            SqlDataAdapter adapter1 = new SqlDataAdapter();
                            DataTable ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_GetApprovalMasterByFormId", con);
                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", UserName));
                            cmd1.Parameters.Add(new SqlParameter("@FormId", emailData.FormId));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    Id = ds1.Rows[0]["Id"] != DBNull.Value && ds1.Rows[0]["Id"] != "" ? Convert.ToString(ds1.Rows[0]["Id"]) : "";
                                    Designation = ds1.Rows[0]["Designation"] != DBNull.Value && ds1.Rows[0]["Designation"] != "" ? Convert.ToString(ds1.Rows[0]["Designation"]) : "";
                                    Level = ds1.Rows[0]["Level"] != DBNull.Value && ds1.Rows[0]["Level"] != "" ? Convert.ToString(ds1.Rows[0]["Level"]) : "";
                                }
                            }
                            cmd1 = new SqlCommand();
                            adapter1 = new SqlDataAdapter();
                            ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                            cmd1.Parameters.Add(new SqlParameter("@UniqueFormName", emailData.UniqueFormName));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    IsAction = ds1.Rows[0]["IsAction"] != DBNull.Value && ds1.Rows[0]["IsAction"] != "" ? Convert.ToString(ds1.Rows[0]["IsAction"]) : "0";
                                    ActionQuery = ds1.Rows[0]["ActionQuery"] != DBNull.Value && ds1.Rows[0]["ActionQuery"] != "" ? Convert.ToString(ds1.Rows[0]["ActionQuery"]) : "";
                                }
                            }
                            if (IsAction == "1")
                            {
                                List<string> AQ = ActionQuery.Split(',').ToList<string>();

                                for (int i = 0; i < AQ.Count; i++)
                                {
                                    if (AQ[i] == Designation)
                                    {
                                        IsAction = "1";
                                    }
                                    else if (AQ[i] == Level)
                                    {
                                        IsAction = "1";
                                    }
                                }
                            }
                            string body = await GetEmailBodyForRequestApproval(receiver.FullName,
                                emailData.RequestId, emailData.FormName, emailData.Sender.FullName, UserName, Id, IsAction, emailData == null ? null : emailData);
                            emailData.Subject = $"Request {emailData.RequestId} received for Approval- {emailData.FormName} & Global Process No : {GPNo}";
                            var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                            emails.Add(emailModel);
                        }
                    }
                }

                else if (emailData.Action == FormStates.ReSubmit)
                {
                    //mail for on behalf
                    if (emailData.OnBehalfSender != null && !string.IsNullOrEmpty(emailData.OnBehalfSender.Email))
                    {
                        var onBehalfEmailBody = GetEmailBodyForRequestSubmissionOnBehalf(emailData.OnBehalfSender.FullName, emailData.RequestId, emailData.Sender.FullName, true);
                        emails.Add(new EmailDataModel($"{emailData.RequestId} request Re-Submitted on your behalf - {emailData.FormName} & Global Process No : {GPNo}", onBehalfEmailBody, new List<UserData>() { emailData.OnBehalfSender }));
                    }
                    //mail for submitter
                    // var submitterEmailBody = GetEmailBodyForRequestSubmission(emailData.Sender.FullName, emailData.RequestId, true);
                    // emails.Add(new EmailDataModel($"{emailData.RequestId} Request Re-Submitted - {emailData.FormName}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mails for approvers
                    if (emailData.Recipients.Count() != 0)
                    {
                        foreach (var receiver in emailData.Recipients)
                        {
                            var UserName = "";
                            var Id = "";
                            var Designation = "";
                            var IsAction = "0";
                            var ActionQuery = "";
                            var Level = "";
                            SqlDataAdapter adapter = new SqlDataAdapter();
                            DataTable ds = new DataTable();
                            SqlCommand cmd = new SqlCommand();
                            con = new SqlConnection(sqlConString);
                            cmd = new SqlCommand("USP_GetEmployeeDataByEmailId", con);
                            cmd.Parameters.Add(new SqlParameter("@Email", receiver.Email));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd;
                            con.Open();
                            adapter.Fill(ds);
                            con.Close();
                            if (ds.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds.Rows.Count; i++)
                                {
                                    UserName = ds.Rows[0]["UserName"] != DBNull.Value && ds.Rows[0]["UserName"] != "" ? Convert.ToString(ds.Rows[0]["UserName"]) : "";
                                }
                            }
                            SqlCommand cmd1 = new SqlCommand();
                            SqlDataAdapter adapter1 = new SqlDataAdapter();
                            DataTable ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("USP_GetApprovalMasterByFormId", con);
                            cmd1.Parameters.Add(new SqlParameter("@ApproverName", UserName));
                            cmd1.Parameters.Add(new SqlParameter("@FormId", emailData.FormId));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    Id = ds1.Rows[0]["Id"] != DBNull.Value && ds1.Rows[0]["Id"] != "" ? Convert.ToString(ds1.Rows[0]["Id"]) : "";
                                    Designation = ds1.Rows[0]["Designation"] != DBNull.Value && ds1.Rows[0]["Designation"] != "" ? Convert.ToString(ds1.Rows[0]["Designation"]) : "";
                                    Level = ds1.Rows[0]["Level"] != DBNull.Value && ds1.Rows[0]["Level"] != "" ? Convert.ToString(ds1.Rows[0]["Level"]) : "";
                                }
                            }
                            cmd1 = new SqlCommand();
                            adapter1 = new SqlDataAdapter();
                            ds1 = new DataTable();
                            con = new SqlConnection(sqlConString);
                            cmd1 = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                            cmd1.Parameters.Add(new SqlParameter("@UniqueFormName", emailData.UniqueFormName));
                            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                            cmd1.CommandType = CommandType.StoredProcedure;
                            adapter.SelectCommand = cmd1;
                            con.Open();
                            adapter.Fill(ds1);
                            con.Close();
                            if (ds1.Rows.Count > 0)
                            {
                                for (int i = 0; i < ds1.Rows.Count; i++)
                                {
                                    IsAction = ds1.Rows[0]["IsAction"] != DBNull.Value && ds1.Rows[0]["IsAction"] != "" ? Convert.ToString(ds1.Rows[0]["IsAction"]) : "0";
                                    ActionQuery = ds1.Rows[0]["ActionQuery"] != DBNull.Value && ds1.Rows[0]["ActionQuery"] != "" ? Convert.ToString(ds1.Rows[0]["ActionQuery"]) : "";
                                }
                            }
                            if (IsAction == "1")
                            {
                                List<string> AQ = ActionQuery.Split(',').ToList<string>();

                                for (int i = 0; i < AQ.Count; i++)
                                {
                                    if (AQ[i] == Designation)
                                    {
                                        IsAction = "1";
                                    }
                                    else if (AQ[i] == Level)
                                    {
                                        IsAction = "1";
                                    }
                                }
                            }
                            string body = await GetEmailBodyForRequestApproval(receiver.FullName,
                                emailData.RequestId, emailData.FormName, emailData.Sender.FullName, UserName, Id, IsAction, emailData == null ? null : emailData);
                            emailData.Subject = $"Request {emailData.RequestId} received for Approval- {emailData.FormName} & Global Process No : {GPNo}";
                            var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                            emails.Add(emailModel);
                        }
                    }
                }

                else if (emailData.Action == FormStates.PartialApproval)
                {
                    var submitterEmailBody = GetEmailBodyForPartialApprovalToSubmitter(
                            emailData.Sender.FullName,
                            emailData.RequestId,
                            emailData.Recipients.Any(x => x.IsCurrentApprover) ? emailData.Recipients.Where(x => x.IsCurrentApprover).FirstOrDefault().EmployeeName : ""
                            );

                    emails.Add(new EmailDataModel($"{emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mail for next approvers
                    foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsNextApprover))
                    {
                        var UserName = "";
                        var Id = "";
                        var Designation = "";
                        var IsAction = "0";
                        var ActionQuery = "";
                        var Level = "";
                        SqlDataAdapter adapter = new SqlDataAdapter();
                        DataTable ds = new DataTable();
                        SqlCommand cmd = new SqlCommand();
                        con = new SqlConnection(sqlConString);
                        cmd = new SqlCommand("USP_GetEmployeeDataByEmailId", con);
                        cmd.Parameters.Add(new SqlParameter("@Email", receiver.Email));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmd.CommandType = CommandType.StoredProcedure;
                        adapter.SelectCommand = cmd;
                        con.Open();
                        adapter.Fill(ds);
                        con.Close();
                        if (ds.Rows.Count > 0)
                        {
                            for (int i = 0; i < ds.Rows.Count; i++)
                            {
                                UserName = ds.Rows[0]["UserName"] != DBNull.Value && ds.Rows[0]["UserName"] != "" ? Convert.ToString(ds.Rows[0]["UserName"]) : "";
                            }
                        }
                        SqlCommand cmd1 = new SqlCommand();
                        SqlDataAdapter adapter1 = new SqlDataAdapter();
                        DataTable ds1 = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("USP_GetApprovalMasterByFormId", con);
                        cmd1.Parameters.Add(new SqlParameter("@ApproverName", UserName));
                        cmd1.Parameters.Add(new SqlParameter("@FormId", emailData.FormId));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter.SelectCommand = cmd1;
                        con.Open();
                        adapter.Fill(ds1);
                        con.Close();
                        if (ds1.Rows.Count > 0)
                        {
                            for (int i = 0; i < ds1.Rows.Count; i++)
                            {
                                Id = ds1.Rows[0]["Id"] != DBNull.Value && ds1.Rows[0]["Id"] != "" ? Convert.ToString(ds1.Rows[0]["Id"]) : "";
                                Designation = ds1.Rows[0]["Designation"] != DBNull.Value && ds1.Rows[0]["Designation"] != "" ? Convert.ToString(ds1.Rows[0]["Designation"]) : "";
                                Level = ds1.Rows[0]["Level"] != DBNull.Value && ds1.Rows[0]["Level"] != "" ? Convert.ToString(ds1.Rows[0]["Level"]) : "";

                            }
                        }
                        cmd1 = new SqlCommand();
                        adapter1 = new SqlDataAdapter();
                        ds1 = new DataTable();
                        con = new SqlConnection(sqlConString);
                        cmd1 = new SqlCommand("GetFormParentDataByUniqueFormName", con);
                        cmd1.Parameters.Add(new SqlParameter("@UniqueFormName", emailData.UniqueFormName));
                        // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                        cmd1.CommandType = CommandType.StoredProcedure;
                        adapter.SelectCommand = cmd1;
                        con.Open();
                        adapter.Fill(ds1);
                        con.Close();
                        if (ds1.Rows.Count > 0)
                        {
                            for (int i = 0; i < ds1.Rows.Count; i++)
                            {
                                IsAction = ds1.Rows[0]["IsAction"] != DBNull.Value && ds1.Rows[0]["IsAction"] != "" ? Convert.ToString(ds1.Rows[0]["IsAction"]) : "0";
                                ActionQuery = ds1.Rows[0]["ActionQuery"] != DBNull.Value && ds1.Rows[0]["ActionQuery"] != "" ? Convert.ToString(ds1.Rows[0]["ActionQuery"]) : "";
                            }
                        }
                        if (IsAction == "1")
                        {
                            List<string> AQ = ActionQuery.Split(',').ToList<string>();

                            for (int i = 0; i < AQ.Count; i++)
                            {
                                if (AQ[i] == Designation)
                                {
                                    IsAction = "1";
                                }
                                else if (AQ[i] == Level)
                                {
                                    IsAction = "1";
                                }
                            }
                        }
                        string body = await GetEmailBodyForPartialApprovalToNextApprover(receiver.FullName,
                            emailData.RequestId, emailData.Sender.FullName, UserName, Id, IsAction, emailData);

                        emailData.Subject = $"Request {emailData.RequestId} received for Approval- {emailData.FormName} & Global Process No : {GPNo}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                        emails.Add(emailModel);
                    }

                    //mail for current approver
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsCurrentApprover))
                    //{
                    //    string body = GetEmailBodyForPartialApprovalToCurrentApprover(receiver.FullName,
                    //        emailData.RequestId);

                    //    emailData.Subject = $"Request {emailData.RequestId} successfully Approved- {emailData.FormName}";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail sent for Parallel approvers(OR condition)
                    foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                    {
                        string status = "Approved";

                        string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                            emailData.RequestId, receiverParallel.FullName, status);

                        emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                        emails.Add(emailModel);
                    }
                    //mail sent for Parallel approvers(OR condition)
                }
                else if (emailData.Action == FormStates.Reject)
                {
                    var submitterEmailBody = GetEmailBodyForRejectionToSubmitter(
                            emailData.Sender.FullName,
                            emailData.RequestId,
                            emailData.Recipients.Any(x => x.IsCurrentApprover) ? emailData.Recipients.Where(x => x.IsCurrentApprover).FirstOrDefault().FullName : "",
                            emailData.Comment
                            );

                    emails.Add(new EmailDataModel($"{emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    ////mail for next approvers
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsNextApprover))
                    //{
                    //    string body = GetEmailBodyForPartialApprovalToNextApprover(receiver.FullName,
                    //        emailData.RequestId, emailData.Sender.FullName);

                    //    emailData.Subject = $"Request {emailData.RequestId} received for Approval";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail for current approver
                    //foreach (var receiver in emailData.Recipients.Where(x => x.IsApprover && x.IsCurrentApprover))
                    //{
                    //    string body = GetEmailBodyForRejectionToRejector(receiver.FullName,
                    //        emailData.RequestId);

                    //    emailData.Subject = $"Request {emailData.RequestId} successfully rejected- {emailData.FormName}";
                    //    var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiver });
                    //    emails.Add(emailModel);
                    //}

                    //mail sent for Parallel approvers(OR condition)
                    foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                    {
                        string status = "Rejected";

                        string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                            emailData.RequestId, receiverParallel.FullName, status);

                        emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                        emails.Add(emailModel);
                    }
                    //mail sent for Parallel approvers(OR condition)

                }
                else if (emailData.Action == FormStates.Enquire)
                {
                    var submitterEmailBody = GetEmailBodyForEnquireToSubmitter(
                            emailData.Sender.FullName,
                            emailData.RequestId,
                            emailData.Recipients.Any(x => x.IsCurrentApprover) ? emailData.Recipients.Where(x => x.IsCurrentApprover).FirstOrDefault().FullName : "",
                            emailData.Comment
                            );

                    emails.Add(new EmailDataModel($"{emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}", submitterEmailBody, new List<UserData>() { emailData.Sender }));

                    //mail sent for Parallel approvers(OR condition)
                    foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                    {
                        string status = "Enquired";

                        string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                            emailData.RequestId, receiverParallel.FullName, status);

                        emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}";
                        var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                        emails.Add(emailModel);
                    }
                    //mail sent for Parallel approvers(OR condition)
                }
                else if (emailData.Action == FormStates.FinalApproval)
                {
                    var commonSubject = "";
                    if (commonSubjectCode.ToLower() == "uat")
                    {
                        commonSubject = $"*********eforms – Test Email - Please ignore - Request Id : {emailData.RequestId} - {emailData.FormName} & Global Process No : {GPNo}" + "*******************";
                    }
                    else if (commonSubjectCode.ToLower() == "prod")
                    {
                        commonSubject = $"*********eforms – Request Id : {emailData.RequestId} - {emailData.FormName} & Global Process No : {GPNo}" + "*******************";
                    }
                    else
                    {
                        commonSubject = $"*********eforms – Test Email - Please ignore - Request Id : {emailData.RequestId} - {emailData.FormName} & Global Process No : {GPNo}" + "*******************";
                    }

                    if (emailData.ParallelRecipients != null)
                    {
                        foreach (var receiverParallel in emailData.ParallelRecipients.Where(x => x.IsParallelApprover))
                        {
                            string status = "Approved";

                            string body = GetEmailBodyForParallelApproval(emailData.CurrentUser.FullName,
                                emailData.RequestId, receiverParallel.FullName, status);

                            emailData.Subject = $"Request {emailData.RequestId} Request Status Update- {emailData.FormName} & Global Process No : {GPNo}";
                            var emailModel = new EmailDataModel(emailData.Subject, body, new List<UserData>() { receiverParallel });
                            emails.Add(emailModel);
                        }
                    }

                    //mail sent for Parallel approvers(OR condition)
                    var finalBodyForSubmitter = "<span>Dear " + emailData.Sender.FullName + ",</span> <br/>";
                    finalBodyForSubmitter += "<span>Your Request " + emailData.UniqueFormName + emailData.FormId + " is approved successfully.</span> <br/>";

                    switch (emailData.UniqueFormName)
                    {
                        case "ISLS":
                            {
                                var finalTeamEmailIds = new List<string>() { "eform.notifications@mobinexttech.com" };

                                var commonBAL = new CommonBAL();
                                var tempEmailData = await commonBAL.GetISLSBody(Convert.ToInt32(emailData.FormId), emailData.CurrentUser);

                                emailData.ToIds = new List<string>();

                                emailData.Location = tempEmailData.Location;
                                emailData.Body = tempEmailData.Body;
                                //emailData.ToIds = tempEmailData.ToIds;

                                finalBodyForSubmitter += emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds));
                                emails.Add(new EmailDataModel(commonSubject, finalBodyForSubmitter, new List<UserData>() { emailData.Sender }));

                                emails.Add(new EmailDataModel()
                                {
                                    Subject = commonSubject,
                                    Body = emailData.Body.Replace("{assignedToSection}", string.Join(", ", emailData.ToIds)),
                                    ToIds = emailData.ToIds,
                                    CCIds = emailData.CCIds,
                                    Recipients = finalTeamEmailIds.Select(x => new UserData() { Email = x })
                                });
                                break;
                            }
                    }

                }
                return emails;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return emails;
            }
        }

        public async Task<string> TransactionBody(string UniqueFormName, string FormId, UserData CurrentUser)
        {
            string Body = "";
            switch (UniqueFormName)
            {
                case "ITCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetITClearanceFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "COIF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetConflictOfInterestFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "KSRMUICF":
                    {

                        var commonBAL = new CommonBAL();

                        var tempEmailData = await commonBAL.GetKSRMUserIdCreationFAM(Convert.ToInt32(FormId), CurrentUser, 1);

                        Body = tempEmailData.Body;


                        break;
                    }
                case "RADLF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetResourceAccountDLFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "SRCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetSRCFFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "GUICF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetGaneshIdFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "SPRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetSmartPhoneFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "DBRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetDataBackupRestoreFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "IA":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetInternetAccessFAM_SQL(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "SRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetSoftwareRequisitionFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "SFF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetSharedFolderFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "ITARF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetITAssetRequisitionFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "CBRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetCabBookingFormBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "SFO":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetSuggestionForOrderFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "DNF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetDeviationNoteFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "IDCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetIDCardFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "RIDCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetReissueIDCardFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "IJPF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetInternalJobPostingFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "BTF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetBusTranFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "OCRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetOCRFFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "ECF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetEmployeeClearanceFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "DAF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetDrivingAuthorizationFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "CRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetCourierRequestFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "DARF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetDoorAccessFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "PAF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetPAFFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "BEI":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetBEIFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "MRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetMaterialRequestFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "SUCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetSUCFFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "DLIC":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetDLICFormFAM(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "GAIF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetGiftsInvitationBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "NGCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetNewGlobalCodeBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "URCF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetUserRequestBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "ISLS":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetISLSBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "APFP":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetAPFPBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "QMCR":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetQMCRBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "IMAC":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetIMACBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "MMRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetMMRFBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "QFRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetQFRFBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;

                        break;
                    }
                case "EQSA":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetEQSABody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "IPAF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetIPAFFBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
                case "POCRF":
                    {
                        var commonBAL = new CommonBAL();
                        var tempEmailData = await commonBAL.GetPOCRFBody(Convert.ToInt32(FormId), CurrentUser, 1);
                        Body = tempEmailData.Body;
                        break;
                    }
            }
            return Body;
        }
    }
}
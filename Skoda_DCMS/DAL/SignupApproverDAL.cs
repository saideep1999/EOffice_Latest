using ExcelDataReader.Log;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Skoda_DCMS.DAL
{
    public class SignupApproverDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        dynamic approverEmailIds;
        public async Task<dynamic> ViewSAFormData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();

            return URCFData;
            //}
        }

        public async Task<dynamic> SignupApproverList()
        {
            List<SignupApproverList> SAFData = new List<SignupApproverList>();
            dynamic result = SAFData;
            try
            {

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("USP_GetSignupApproverList", con);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd1;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {
                    int j = 1;
                    for (int i = 0; i < ds1.Rows.Count; i++)
                    {
                        SignupApproverList model1 = new SignupApproverList();

                        model1.SrNo = j;
                        model1.Id = Convert.ToInt32(ds1.Rows[i]["Id"]);
                        model1.Name = Convert.ToString(ds1.Rows[i]["FirstName"]) + " " + Convert.ToString(ds1.Rows[i]["LastName"]);
                        model1.MiddleName = Convert.ToString(ds1.Rows[i]["MiddleName"]);
                        model1.LastName = Convert.ToString(ds1.Rows[i]["LastName"]);
                        model1.EmailID = Convert.ToString(ds1.Rows[i]["EmailID"]);
                        model1.PhoneNumber = Convert.ToString(ds1.Rows[i]["PhoneNumber"]);
                        model1.Department = Convert.ToString(ds1.Rows[i]["Department"]);
                        model1.SubDepartment = Convert.ToString(ds1.Rows[i]["SubDepartment"]);
                        SAFData.Add(model1);
                        j++;
                    }

                }
                result = SAFData;
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }
        public async Task<dynamic> UserdataList()
        {
            List<GetUserDetailsModel> userData = new List<GetUserDetailsModel>();
            dynamic result = userData;
            try
            {

                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd1 = new SqlCommand("sp_GetEmployeMusers", con);
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd1;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {
                    int j = 1;
                    for (int i = 0; i < ds1.Rows.Count; i++)
                    {
                        GetUserDetailsModel model1 = new GetUserDetailsModel();

                        model1.SrNo = j;
                        //model1.Id = Convert.ToInt32(ds1.Rows[i]["Id"]);
                        model1.EmployeeNumber = Convert.ToInt32(ds1.Rows[i]["EmployeeNumber"]);
                        model1.EmployeeName = Convert.ToString(ds1.Rows[i]["FirstName"]) + " " + Convert.ToString(ds1.Rows[i]["LastName"]);
                        model1.EmailID = Convert.ToString(ds1.Rows[i]["EmailID"]);
                        model1.CostCenter = Convert.ToString(ds1.Rows[i]["CostCenter"]);
                        model1.ManagerEmployeeNumber = Convert.ToString(ds1.Rows[i]["ManagerEmployeeNumber"]);

                        userData.Add(model1);
                        j++;
                    }

                }
                result = userData;
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        public List<SignupApproverModel> GetApproversData(long EmpNum)
        {
            List<SignupApproverModel> SAFData = new List<SignupApproverModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetApproversData", con);
                cmd.Parameters.Add(new SqlParameter("@EmpId", EmpNum));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        SignupApproverModel model1 = new SignupApproverModel();
                        model1.Id = Convert.ToInt32(ds.Rows[i]["Id"]);
                        model1.FirstName = Convert.ToString(ds.Rows[i]["FirstName"]);
                        model1.LastName = Convert.ToString(ds.Rows[i]["LastName"]);
                        model1.MiddleName = Convert.ToString(ds.Rows[i]["MiddleName"]);
                        model1.EmailID = Convert.ToString(ds.Rows[i]["EmailID"]);
                        model1.PhoneNumber = Convert.ToString(ds.Rows[i]["PhoneNumber"]);
                        model1.Department = Convert.ToString(ds.Rows[i]["Department"]);
                        model1.SubDepartment = Convert.ToString(ds.Rows[i]["SubDepartment"]);
                        model1.CostCenter = Convert.ToInt64(ds.Rows[i]["CostCenter"]);
                        model1.ManagerEmployeeNumber = Convert.ToString(ds.Rows[i]["ManagerEmployeeNumber"]);
                        SAFData.Add(model1);
                    }
                }
            }
            catch (Exception ex) { }
            return SAFData;
        }
        public ResponseData SaveApproveData(string Id, string FirstName, string MiddleName, string LastName, string EmailID, string PhoneNumber, string Department, string SubDepartment, string CostCenter, string ManagerEmployeeNumber)
        {
            ResponseData SAFData = new ResponseData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SaveApproversData", con);
                cmd.Parameters.Add(new SqlParameter("@Id", Convert.ToInt64(Id)));
                cmd.Parameters.Add(new SqlParameter("@FirstName", Convert.ToString(FirstName)));
                cmd.Parameters.Add(new SqlParameter("@MiddleName", Convert.ToString(MiddleName)));
                cmd.Parameters.Add(new SqlParameter("@LastName", Convert.ToString(LastName)));
                cmd.Parameters.Add(new SqlParameter("@EmailID", Convert.ToString(EmailID)));
                cmd.Parameters.Add(new SqlParameter("@PhoneNumber", Convert.ToString(PhoneNumber)));
                cmd.Parameters.Add(new SqlParameter("@Department", Convert.ToString(Department)));
                cmd.Parameters.Add(new SqlParameter("@SubDepartment", Convert.ToString(SubDepartment)));
                cmd.Parameters.Add(new SqlParameter("@CostCenter", Convert.ToInt64(CostCenter)));
                cmd.Parameters.Add(new SqlParameter("@ManagerEmployeeNumber", Convert.ToString(ManagerEmployeeNumber)));
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
                        if (SAFData.Status == 200)
                        {
                            string smtpHost = ConfigurationManager.AppSettings["smtpHost"];
                            string smtpEmail = ConfigurationManager.AppSettings["smtpEmailId"];
                            string password = ConfigurationManager.AppSettings["smtpPassword"];
                            string url = ConfigurationManager.AppSettings["AppHostUrl"];
                            string sender = "Mobinext Team";
                            MailMessage mailMessage = new MailMessage();
                            mailMessage.From = new MailAddress(smtpEmail);
                            mailMessage.To.Add(Convert.ToString(EmailID));
                            mailMessage.Subject = "User Account Status";
                            StringBuilder sb = new StringBuilder();
                            sb.Append("<h4>Hello User, </h4>");
                            sb.AppendLine("Your Account has been approved");
                            sb.AppendLine("<br/>");
                            sb.AppendLine("<br/>");
                            sb.Append("kindly login with this url ");
                            sb.Append("<br/>");
                            sb.Append(url);
                            sb.AppendLine("<br/>");
                            sb.AppendLine("<br/>");
                            sb.AppendLine("Regards,");
                            sb.AppendLine("<br/>");
                            sb.AppendLine(sender);

                            mailMessage.Body = sb.ToString(); ;
                            mailMessage.IsBodyHtml = true;
                            var smtpClient = new SmtpClient(smtpHost);
                            smtpClient.Port = 587;
                            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.EnableSsl = true;
                            smtpClient.Credentials = new NetworkCredential(smtpEmail, password);
                            smtpClient.Send(mailMessage);
                        }
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

        public ResponseData SaveUsersummaryData(string EmployeeNumber, string FirstName, string MiddleName, string LastName, string EmailID, string PhoneNumber, string Department, string SubDepartment, string CostCenter, string ManagerEmployeeNumber)
        {
            ResponseData SAFData = new ResponseData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SaveUserSummary", con);
                cmd.Parameters.Add(new SqlParameter("@EmployeeNumber", Convert.ToInt64(EmployeeNumber)));
                cmd.Parameters.Add(new SqlParameter("@FirstName", Convert.ToString(FirstName)));
                cmd.Parameters.Add(new SqlParameter("@MiddleName", Convert.ToString(MiddleName)));
                cmd.Parameters.Add(new SqlParameter("@LastName", Convert.ToString(LastName)));
                cmd.Parameters.Add(new SqlParameter("@EmailID", Convert.ToString(EmailID)));
                cmd.Parameters.Add(new SqlParameter("@PhoneNumber", Convert.ToString(PhoneNumber)));
                cmd.Parameters.Add(new SqlParameter("@Department", Convert.ToString(Department)));
                cmd.Parameters.Add(new SqlParameter("@SubDepartment", Convert.ToString(SubDepartment)));
                cmd.Parameters.Add(new SqlParameter("@CostCenter", Convert.ToInt64(CostCenter)));
                cmd.Parameters.Add(new SqlParameter("@ManagerEmployeeNumber", Convert.ToString(ManagerEmployeeNumber)));
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
        public ResponseData SaveRejectData(string Id,string EmailID, string RejectReason)
        {
            ResponseData SAFData = new ResponseData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_SaveRejectData", con);
                cmd.Parameters.Add(new SqlParameter("@Id", Convert.ToInt64(Id)));
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
                        if (SAFData.Status == 200)
                        {
                            string smtpHost = ConfigurationManager.AppSettings["smtpHost"];
                            string smtpEmail = ConfigurationManager.AppSettings["smtpEmailId"];
                            string password = ConfigurationManager.AppSettings["smtpPassword"];
                            string url = ConfigurationManager.AppSettings["AppHostUrl"];
                            string sender = "Mobinext Team";
                            MailMessage mailMessage = new MailMessage();
                            mailMessage.From = new MailAddress(smtpEmail);
                            mailMessage.To.Add(Convert.ToString(EmailID));
                            mailMessage.Subject = "User Account Status";
                            StringBuilder sb = new StringBuilder();
                            sb.Append("<h4>Hello User, </h4>");
                            sb.AppendLine("Your Account has been rejected");
                            sb.AppendLine("<br/>");
                            sb.AppendLine("<br/>");
                            sb.Append("Reason for rejection is : ");
                            sb.Append(RejectReason);
                            sb.Append("<br/>");
                            sb.AppendLine("<br/>");
                            sb.AppendLine("Regards,");
                            sb.AppendLine("<br/>");
                            sb.AppendLine(sender);

                            mailMessage.Body = sb.ToString(); ;
                            mailMessage.IsBodyHtml = true;
                            var smtpClient = new SmtpClient(smtpHost);
                            smtpClient.Port = 587;
                            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.EnableSsl = true;
                            smtpClient.Credentials = new NetworkCredential(smtpEmail, password);
                            smtpClient.Send(mailMessage);
                        }
                    }
                }
            }
            catch (Exception ex) {
                SAFData.Status = 600;
                SAFData.Message = ex.Message;
            }
            return SAFData;
        }

        public ResponseData Isactiveuser(long empnum)
        {
            ResponseData SAFData = new ResponseData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_ISActiveuser", con);
                cmd.Parameters.Add(new SqlParameter("@EmployeeNumber", Convert.ToInt64(empnum)));
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

        public List<SignupApproverModel> GetActiveApproversData(long EmpNum)
        {
            List<SignupApproverModel> SAFActiveData = new List<SignupApproverModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable ds = new DataTable();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetActiveApproversData", con);
                cmd.Parameters.Add(new SqlParameter("@EmpId", EmpNum));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Rows.Count; i++)
                    {
                        SignupApproverModel models = new SignupApproverModel();
                        models.EmployeeNumber = Convert.ToInt32(ds.Rows[i]["EmployeeNumber"]);
                        models.FirstName = Convert.ToString(ds.Rows[i]["FirstName"]);
                        models.LastName = Convert.ToString(ds.Rows[i]["LastName"]);
                        models.MiddleName = Convert.ToString(ds.Rows[i]["MiddleName"]);
                        models.EmailID = Convert.ToString(ds.Rows[i]["EmailID"]);
                        models.PhoneNumber = Convert.ToString(ds.Rows[i]["PhoneNumber"]);
                        models.Department = Convert.ToString(ds.Rows[i]["Department"]);
                        models.SubDepartment = Convert.ToString(ds.Rows[i]["SubDepartment"]);
                        models.CostCenter = Convert.ToInt64(ds.Rows[i]["CostCenter"]);
                        models.ManagerEmployeeNumber = Convert.ToString(ds.Rows[i]["ManagerEmployeeNumber"]);
                        SAFActiveData.Add(models);
                    }
                }
            }
            catch (Exception ex) { }
            return SAFActiveData;
        }

    }
}
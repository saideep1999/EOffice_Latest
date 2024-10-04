using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Skoda_DCMS.Models;
using Skoda_DCMS.Helpers;
using System.Data.SqlClient;
using System.Data;
using System.DirectoryServices;
using Skoda_DCMS.App_Start;
using static Microsoft.SharePoint.Workflow.SPWorkflowAssociationCollection;
using System.Net.Mail;
using System.Text;

namespace Skoda_DCMS.DAL
{
    public class LoginDAL
    {
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string adCode = ConfigurationManager.AppSettings["ADCode"];



        public async Task<(UserData UserDetails, string Message)> GetUser(LoginModel userCreds)
        {
            UserData userData = new UserData();
            string message = "This could be due to invalid username/password or your user might be disabled in Active Directory in which case you must contact your HR.";
            try
            {
                //if (adCode.ToLower() == "yes")
                //{
                //    //AD Code
                //    //ListDAL obj = new ListDAL();
                //    //userData = obj.GetUserDetailsFromUsernameAD(userCreds.UserName, userCreds.Password);
                //    //AD Code
                //    message = "I am in ad";
                //}
                //else
                //{
                //    ListDAL obj = new ListDAL();
                //    userData = obj.GetUserDetailsFromUsername(userCreds.UserName, userCreds.Password);
                //}
                ListDAL obj = new ListDAL();
                var HassPass = PasswordHasher(userCreds.Password);
                userData = obj.GetUserDetailsFromUsername(userCreds.UserName, userCreds.Password);
                //if (string.IsNullOrEmpty(userData.UserName))
                //    return (userData, message);

                if (userData.Email == "")
                    return (userData, message = "User's Email-Id is not found!");

                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetUser", con);
                cmd.Parameters.Add(new SqlParameter("@UserName", userData.UserName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                List<string> missingDataErrorList = new List<string>();
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["EmployeeNumber"].ToString()))
                        missingDataErrorList.Add("Employee Number");
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["CostCenter"].ToString()) || Convert.ToInt32(ds.Tables[0].Rows[0]["CostCenter"]) == 0)
                        missingDataErrorList.Add("Cost Center");
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["Department"].ToString()))
                        missingDataErrorList.Add("Department");
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["FirstName"].ToString()))
                        missingDataErrorList.Add("First Name");
                    //if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["PhoneNumber"].ToString()))
                    //    missingDataErrorList.Add("Phone Number");
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["LastName"].ToString()))
                        missingDataErrorList.Add("Last Name");
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["EmailID"].ToString()))
                        missingDataErrorList.Add("EmailID");
                    if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["SubDepartment"].ToString()))
                        missingDataErrorList.Add("Sub Department");
                    if (!string.IsNullOrEmpty(ds.Tables[0].Rows[0]["EmployeeNumber"].ToString())
                        && Convert.ToInt32(ds.Tables[0].Rows[0]["EmployeeNumber"]) != 610553)//610553
                    {
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["ManagerEmployeeNumber"].ToString()))
                            missingDataErrorList.Add("Manager Employee Number");
                    }
                    //if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["MiddleName"].ToString()))
                    //    missingDataErrorList.Add("Middle Name");

                    userData.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[0]["EmployeeNumber"]);
                    userData.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[0]["CostCenter"]);
                    userData.Department = Convert.ToString(ds.Tables[0].Rows[0]["Department"]);
                    userData.FirstName = Convert.ToString(ds.Tables[0].Rows[0]["FirstName"]);
                    userData.LastName = Convert.ToString(ds.Tables[0].Rows[0]["LastName"]);
                    userData.PhoneNumber = Convert.ToString(ds.Tables[0].Rows[0]["PhoneNumber"]);
                    userData.Email = Convert.ToString(ds.Tables[0].Rows[0]["EmailID"]);
                    userData.SubDepartment = Convert.ToString(ds.Tables[0].Rows[0]["SubDepartment"]);
                    userData.UserName = userCreds.UserName;
                    userData.Password = userCreds.Password;
                    //ConfigurationManager.AppSettings["SharepointUsername"] = userCreds.UserName;
                    //ConfigurationManager.AppSettings["SharepointPass"] = userCreds.Password;
                    GlobalClass.IsUserLoggedOut = false;

                    userData.UserAccessId = GetUserAcessData(userData.UserName);

                }
                userData.IsLoginSuccessful = true;
                if (ds.Tables[0].Rows.Count > 0 && missingDataErrorList.Count > 0)
                {
                    userData.IsMissingData = true;
                    return (userData, message = "Some user details not found in database like " + string.Join(", ", missingDataErrorList));
                }

                if (userData.EmpNumber == 0)
                {
                    userData.IsMissingData = true;
                    return (userData, message = "User not found in database");
                }
            }

            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return (userData, message = ex.Message);
            }
            Log.Error($"{userCreds.UserName} has logged in");
            userData.IsLoginSuccessful = true;
            return (userData, message = "Login Successful");
        }

        public string GetUserAcessData(string userName)
        {
            string result = string.Empty;
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                SqlConnection con;
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetUserAccessData", con);
                cmd.Parameters.Add(new SqlParameter("@UserName", userName));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        result = ds.Tables[0].Rows[i]["UserId"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }

        public ResponseModel<UserData> GetUserDetailsFromDB(long EmpNum)//, UserData userDetails
        {
            UserData userData = new UserData();
            try
            {
                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetUserByEmpNum", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", EmpNum));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                //if (!string.IsNullOrEmpty(Convert.ToString(ds.Tables[0].Rows[0]["EmailID"]))) {
                //    return new ResponseModel<UserData>
                //    {
                //        Model = userData,
                //        Status = 400,
                //        Message = "Incorrect user details"
                //    };
                //}
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    if (!string.IsNullOrEmpty(ds.Tables[0].Rows[0]["EmployeeNumber"].ToString()))
                        userData.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[0]["EmployeeNumber"]);
                    if (!string.IsNullOrEmpty(ds.Tables[0].Rows[0]["CostCenter"].ToString()) && Convert.ToInt32(ds.Tables[0].Rows[0]["CostCenter"]) != 0)
                        userData.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[0]["CostCenter"]);
                    userData.Department = Convert.ToString(ds.Tables[0].Rows[0]["Department"]);
                    userData.FirstName = Convert.ToString(ds.Tables[0].Rows[0]["FirstName"]);
                    userData.LastName = Convert.ToString(ds.Tables[0].Rows[0]["LastName"]);
                    userData.PhoneNumber = Convert.ToString(ds.Tables[0].Rows[0]["PhoneNumber"]);
                    userData.Email = Convert.ToString(ds.Tables[0].Rows[0]["EmailID"]);
                    if (!string.IsNullOrEmpty(ds.Tables[0].Rows[0]["ManagerEmployeeNumber"].ToString()))
                        userData.ManagerEmployeeNumber = Convert.ToInt32(ds.Tables[0].Rows[0]["ManagerEmployeeNumber"]);
                    userData.SubDepartment = Convert.ToString(ds.Tables[0].Rows[0]["SubDepartment"]);
                    userData.MiddleName = Convert.ToString(ds.Tables[0].Rows[0]["MiddleName"]);
                }
                else
                {
                    return new ResponseModel<UserData>
                    {
                        Model = userData,
                        Status = 404,
                        Message = "User not found in database. Kindly fill all required details to continue."
                    };
                }
                //if (!string.IsNullOrEmpty(userData.Email) && !string.IsNullOrEmpty(userDetails.Email) && userData.Email != userDetails.Email)
                //{
                //    return new ResponseModel<UserData>
                //    {
                //        Model = userData,
                //        Status = 406,
                //        Message = "Invalid Employee Number."
                //    };
                //}
                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Status = 200,
                    Message = "Data fetch successfully"
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Message = "Error occured while fetching employee data",
                    Status = 400
                };
            }
        }

        public ResponseModel<UserData> UpdateUserDetailsInDB(UserData Emp, string email)
        {
            try
            {


                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_AddUpdateUser", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", Emp.EmpNumber));
                cmd.Parameters.Add(new SqlParameter("@FirstName", Emp.FirstName));
                cmd.Parameters.Add(new SqlParameter("@MiddleName", string.IsNullOrEmpty(Emp.MiddleName) ? "" : Emp.MiddleName));
                cmd.Parameters.Add(new SqlParameter("@LastName", Emp.LastName));
                cmd.Parameters.Add(new SqlParameter("@CostCenter", Emp.CostCenter));
                cmd.Parameters.Add(new SqlParameter("@Email", Emp.Email));
                cmd.Parameters.Add(new SqlParameter("@PhoneNumber", string.IsNullOrEmpty(Emp.PhoneNumber) ? "" : Emp.PhoneNumber));
                cmd.Parameters.Add(new SqlParameter("@Department", Emp.Department));
                cmd.Parameters.Add(new SqlParameter("@SubDepartment", Emp.SubDepartment));
                cmd.Parameters.Add(new SqlParameter("@ManagerEmployeeNumber", Emp.ManagerEmployeeNumber));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                int Status = 0;
                string Message = "";
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    Status = Convert.ToInt32(ds.Tables[0].Rows[0]["Status"]);
                    Message = Convert.ToString(ds.Tables[0].Rows[0]["Message"]);
                }
                //Check if now user is in db
                UserData userData = new UserData();
                if (Status == 1)
                {
                    cmd = new SqlCommand("sp_GetUser", con);
                    ds = new DataSet();
                    cmd.Parameters.Add(new SqlParameter("@email", email));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand = cmd;
                    con.Open();
                    adapter.Fill(ds);
                    con.Close();
                    List<string> missingDataErrorList = new List<string>();
                    if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                    {
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["EmployeeNumber"].ToString()))
                            missingDataErrorList.Add("Employee Number");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["CostCenter"].ToString()) || Convert.ToInt32(ds.Tables[0].Rows[0]["CostCenter"]) == 0)
                            missingDataErrorList.Add("Cost Center");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["Department"].ToString()))
                            missingDataErrorList.Add("Department");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["FirstName"].ToString()))
                            missingDataErrorList.Add("First Name");
                        //if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["PhoneNumber"].ToString()))
                        //    missingDataErrorList.Add("Phone Number");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["LastName"].ToString()))
                            missingDataErrorList.Add("Last Name");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["EmailID"].ToString()))
                            missingDataErrorList.Add("EmailID");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["SubDepartment"].ToString()))
                            missingDataErrorList.Add("Sub Department");
                        if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["ManagerEmployeeNumber"].ToString())
                             && Convert.ToInt32(ds.Tables[0].Rows[0]["ManagerEmployeeNumber"]) != 0)
                            missingDataErrorList.Add("Manager Employee Number");
                        //if (string.IsNullOrEmpty(ds.Tables[0].Rows[0]["MiddleName"].ToString()))
                        //    missingDataErrorList.Add("Middle Name");

                        //set Data incase if email was null and 
                        userData.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[0]["EmployeeNumber"]);
                        userData.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[0]["CostCenter"]);
                        userData.Department = Convert.ToString(ds.Tables[0].Rows[0]["Department"]);
                        userData.FirstName = Convert.ToString(ds.Tables[0].Rows[0]["FirstName"]);
                        userData.LastName = Convert.ToString(ds.Tables[0].Rows[0]["LastName"]);
                        userData.PhoneNumber = Convert.ToString(ds.Tables[0].Rows[0]["PhoneNumber"]);
                        userData.Email = Convert.ToString(ds.Tables[0].Rows[0]["EmailID"]);
                        userData.SubDepartment = Convert.ToString(ds.Tables[0].Rows[0]["SubDepartment"]);
                        GlobalClass.IsUserLoggedOut = false;

                        userData.UserAccessId = GetUserAcessData(userData.UserName);
                        return new ResponseModel<UserData>
                        {
                            Model = userData,
                            Status = missingDataErrorList.Count > 0 ? 201 : 200,
                            Message = missingDataErrorList.Count > 0
                                ? "Some of the details are not present in database. Please search and update your details."
                                : Message
                        };
                    }
                    return new ResponseModel<UserData>
                    {
                        Model = userData,
                        Status = 201,
                        Message = "User not found in database"
                    };
                }
                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Status = Status == 1 ? 200 : 500,
                    Message = Message
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<UserData>
                {
                    Model = new UserData(),
                    Message = "Error occured while updating employee data",
                    Status = 400
                };
            }
        }
        public ResponseModel<UserData> SignUpUserDetailsInDB(UserData Emp, string email)
        {
            try
            {
                var HashPass = PasswordHasher(Emp.Password);
                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_AddSignUpUser", con);
                cmd.Parameters.Add(new SqlParameter("@EmpNum", Emp.EmpNumber));
                cmd.Parameters.Add(new SqlParameter("@FirstName", Emp.FirstName));
                cmd.Parameters.Add(new SqlParameter("@MiddleName", string.IsNullOrEmpty(Emp.MiddleName) ? "" : Emp.MiddleName));
                cmd.Parameters.Add(new SqlParameter("@LastName", Emp.LastName));
                cmd.Parameters.Add(new SqlParameter("@CostCenter", Emp.CostCenter));
                cmd.Parameters.Add(new SqlParameter("@Email", Emp.Email));
                cmd.Parameters.Add(new SqlParameter("@PhoneNumber", string.IsNullOrEmpty(Emp.PhoneNumber) ? "" : Emp.PhoneNumber));
                cmd.Parameters.Add(new SqlParameter("@Department", Emp.Department));
                cmd.Parameters.Add(new SqlParameter("@SubDepartment", Emp.SubDepartment));
                cmd.Parameters.Add(new SqlParameter("@ManagerEmployeeNumber", Emp.ManagerEmployeeNumber));
                cmd.Parameters.Add(new SqlParameter("@UserName", Emp.UserName));
                cmd.Parameters.Add(new SqlParameter("@Password", HashPass));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                int Status = 0;
                string Message = "";
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    Status = Convert.ToInt32(ds.Tables[0].Rows[0]["Status"]);
                    Message = Convert.ToString(ds.Tables[0].Rows[0]["Message"]);
                }
                //Check if now user is in db
                UserData userData = new UserData();

                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Status = Status == 1 ? 200 : 500,
                    Message = Message
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<UserData>
                {
                    Model = new UserData(),
                    Message = "Error occured while updating employee data",
                    Status = 400
                };
            }
        }
        public async Task<List<Department>> GetDepartmentList()
        {
            List<Department> users = new List<Department>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                SqlConnection con;
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetDepartment", con);
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        Department user = new Department();
                        user.DivId = ds.Tables[0].Rows[i]["DivId"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["DivId"]);
                        user.DivName = ds.Tables[0].Rows[i]["DivName"].ToString();
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }
        public async Task<List<SubDepartment>> GetSubDepartmentList(string DepId)
        {
            List<SubDepartment> users = new List<SubDepartment>();
            try
            {
                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetSubDepartment", con);
                cmd.Parameters.Add(new SqlParameter("@DepId", DepId));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        SubDepartment user = new SubDepartment();
                        user.DivId = ds.Tables[0].Rows[i]["DivId"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["DivId"]);
                        user.DeptId = ds.Tables[0].Rows[i]["DeptId"] == DBNull.Value ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["DeptId"]);
                        user.DeptName = ds.Tables[0].Rows[i]["DeptName"].ToString();
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }

        public string PasswordHasher(string password)
        {
            PasswordHash hasher = new PasswordHash(password);
            var hashedArray = hasher.ToArray();
            return Convert.ToBase64String(hashedArray);

        }

        public ResponseModel<UserData> ForgotPassword(string emailId)
        {
            UserData userData = new UserData();
            var pass = GeneratePassword();
            var passHash = PasswordHasher(pass);
            var result = UpdateUserPassword(emailId, passHash);
            if (result)
            {
                return SendForgotPasswordMail(emailId, pass);
            }
            else
            {
                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Message = "There was some issue while updating the password. Please try again in some time",
                    Status = 400
                };

            }
        }

        public string GeneratePassword()
        {
            string LowCase = "abcdefghijklmnopqrstuvxyz";
            string UpCase = "ABCDEFGHIJKLMNOPQRSTUVXYZ";
            string Numbers = "0123456789";
            string Special = "@$%&#";
            string password = "";
            Random random = new Random();
            int PasswordLength = random.Next(8, 12);
            for (int i = 0; i < PasswordLength; i++)
            {
                int r = random.Next(0, 3);
                int charIndex;
                switch (r)
                {
                    case 0:
                        charIndex = random.Next(0, LowCase.Length);
                        password += LowCase.ElementAt(charIndex);
                        break;
                    case 1:
                        charIndex = random.Next(0, UpCase.Length);
                        password += UpCase.ElementAt(charIndex);
                        break;
                    case 2:
                        charIndex = random.Next(0, Numbers.Length);
                        password += Numbers.ElementAt(charIndex);
                        break;
                    case 3:
                        charIndex = random.Next(0, Special.Length);
                        password += Special.ElementAt(charIndex);
                        break;
                }
            }
            return password;
        }

        public bool UpdateUserPassword(string emailId, string passHash)
        {

            object result = "";

            try
            {

                SqlConnection con;
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("UpdateForgetPassword", con);
                cmd.Parameters.Add(new SqlParameter("@emailId", emailId));
                cmd.Parameters.Add(new SqlParameter("@Password", passHash));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();
                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        result = ds.Tables[0].Rows[i]["Status"];
                    }
                }
                if (Convert.ToInt32(result) == 200)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return false;
            }

        }

        public ResponseModel<UserData> SendForgotPasswordMail(string emailId, string userPassword)
        {
            UserData userData = new UserData();
            try
            {
                string smtpHost = ConfigurationManager.AppSettings["smtpHost"];
                string smtpEmail = ConfigurationManager.AppSettings["smtpEmailId"];
                string password = ConfigurationManager.AppSettings["smtpPassword"];
                string sender = "Mobinext Team";
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpEmail);
                mailMessage.To.Add(emailId);
                mailMessage.Subject = sender + "- Password Reset";
                StringBuilder sb = new StringBuilder();
                sb.Append("<h4>Hello User, </h4>");
                sb.AppendLine("Please use the following password to login into the Application");
                sb.AppendLine("<br/>");
                sb.AppendLine("<br/>");
                sb.Append("<b>");
                sb.Append(userPassword);
                sb.Append("</b>");
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
                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Message = "The new password has been successfully mailed to your Email Id",
                    Status = 200
                };

            }
            catch (Exception ex)
            {
                return new ResponseModel<UserData>
                {
                    Model = userData,
                    Message = "There was some issue while mailing the password. Please try again in some time",
                    Status = 400
                };

            }
        }


    }
}
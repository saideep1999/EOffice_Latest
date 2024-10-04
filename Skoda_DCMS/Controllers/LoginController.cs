using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using Skoda_DCMS.Filters;
using Skoda_DCMS.App_Start;
using System.Web.Security;
using Skoda_DCMS.Helpers;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace Skoda_DCMS.Controllers
{
    public class LoginController : Controller
    {
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        SqlConnection con;
        // GET: Login
        //public ActionResult Index()
        //{
        //    //HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
        //    //if (authCookie == null || authCookie.Value == "")
        //    //    return View("Login", new LoginModel());

        //    //FormsAuthenticationTicket authTicket;
        //    //try
        //    //{
        //    //    authTicket = FormsAuthentication.Decrypt(authCookie.Value);
        //    //}
        //    //catch
        //    //{
        //    //    return View("Login", new LoginModel());
        //    //}
        //    //var userData = authTicket.UserData;
        //    HttpContext.Response.AddHeader("REQUIRES_AUTH", "1");
        //    return View("Login", new LoginModel());
        //}
        [OutputCache(NoStore = true, Duration = 0)]
        public ActionResult Index(string formName = "")
        {
            //HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            //if (authCookie == null || authCookie.Value == "")
            //    return View("Login", new LoginModel());
            //FormsAuthenticationTicket authTicket;
            //try
            //{
            //    authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            //}
            //catch
            //{
            //    return View("Login", new LoginModel());
            //}
            //var userData = authTicket.UserData;
            HttpContext.Response.AddHeader("REQUIRES_AUTH", "1");
            return View("Login", new LoginModel() { FormName = formName });
        }

        [OutputCache(NoStore = true, Duration = 0)]
        public ActionResult Login(string formName = "")
        {
            HttpContext.Response.AddHeader("REQUIRES_AUTH", "1");
            return View("Login", new LoginModel() { FormName = formName });
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginModel loginDetails)
        {
            var response = new ResponseModel<object>();
            try
            {

                LoginDAL user = new LoginDAL();
                var output = await user.GetUser(loginDetails);
                if (output.UserDetails != null)
                {
                    if (output.UserDetails.IsLoginSuccessful)
                    {
                        if (!output.UserDetails.IsMissingData)
                            response.Status = 200;
                        else
                            response.Status = 200;
                    }
                    else
                        response.Status = 400;
                }
                else
                    response.Status = 400;
                //response.Status = output.UserDetails != null ? (output.UserDetails.IsLoginSuccessful ? 200 : 400) : 400;
                response.Message = output.Message;

                //if (response.Status != 200)
                //    return Json(response, JsonRequestBehavior.AllowGet);
                if (response.Status != 200 && response.Status != 201)
                {
                    return Json(response, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Session["UserData"] = output.UserDetails;
                    response.RouteUrl = await GetCreateFormUrl(loginDetails.FormName);
                    return Json(response, JsonRequestBehavior.AllowGet);
                }

                //var authTicket = new FormsAuthenticationTicket(
                //        1,
                //        loginDetails.UserName,
                //        DateTime.Now,
                //        DateTime.Now.AddDays(20),
                //        true,
                //        "",
                //        "/"
                //    );
                //HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(authTicket));
                //Response.Cookies.Add(cookie);
                //Session.Timeout = 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult SignUpUserDetails(UserData model)
        {
            var res = new ResponseModel<UserData>();
            try
            {
                LoginDAL dal = new LoginDAL();
                var email = (Session["UserData"] as UserData)?.Email;
                if (!string.IsNullOrEmpty(model.Email))
                {

                    res = dal.SignUpUserDetailsInDB(model, email);
                    //Reset Session if the sessiondata has some missing data
                    //if (res != null && res.Status == 200)
                    //{
                    //    var sessionData = (Session["UserData"] as UserData);
                    //    sessionData.EmpNumber = res.Model.EmpNumber;
                    //    sessionData.CostCenter = res.Model.CostCenter;
                    //    sessionData.Department = res.Model.Department;
                    //    sessionData.FirstName = res.Model.FirstName;
                    //    sessionData.LastName = res.Model.LastName;
                    //    sessionData.PhoneNumber = res.Model.PhoneNumber;
                    //    sessionData.Email = res.Model.Email;
                    //    sessionData.SubDepartment = res.Model.SubDepartment;
                    //    sessionData.UserName = sessionData.UserName;
                    //    sessionData.Password = sessionData.Password;
                    //    GlobalClass.IsUserLoggedOut = false;
                    //    sessionData.UserAccessId = dal.GetUserAcessData(sessionData.UserName);
                    //    Session["UserData"] = sessionData;
                    //}
                }
                else //This will execute when user session is expired or Email Id is not set from AD
                {
                    res.Status = 401;
                    res.Message = "User session expired";
                    return Json(res, JsonRequestBehavior.AllowGet);
                }
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult LogoutUser()
        {
            Session.Abandon();
            Session.Clear();
            return RedirectToAction("Index");
        }

        public async Task<string> GetCreateFormUrl(string uniqueFormName)
        {
            string url = "/Dashboard";
            try
            {
                if (string.IsNullOrEmpty(uniqueFormName))
                    return url;

                var listDAL = new ListDAL();
                var result = await listDAL.GetFormParentDetailsByUniqueName(uniqueFormName);
                var selectedForm = result.Forms.FirstOrDefault();
                url = Url.Action("CreateForm", "List",
                    new
                    {
                        uniqueFormName = uniqueFormName,
                        formParentId = selectedForm.Id,
                        formName = selectedForm.FormName,
                        ControllerName = selectedForm.ControllerName
                    });
                return url;
            }
            catch (Exception ex)
            {
                return url;
            }
        }

        public ActionResult GetUserDataByEmpCode(long EmpNumber)
        {
            var res = new ResponseModel<UserData>();
            try
            {
                LoginDAL dal = new LoginDAL();
                //var user = (UserData)Session["UserData"];
                //if (user != null)
                res = dal.GetUserDetailsFromDB(EmpNumber);//, user
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateUserDetails(UserData model)
        {
            var res = new ResponseModel<UserData>();
            try
            {
                LoginDAL dal = new LoginDAL();
                var email = (Session["UserData"] as UserData)?.Email;
                if (!string.IsNullOrEmpty(email))
                {
                    if (!string.IsNullOrEmpty(model.Email) && email != model.Email)
                    {
                        res.Status = 500;
                        res.Message = "According to your userid the linked emailid is " + email + ". Kindly update the correct emailid";
                        return Json(res, JsonRequestBehavior.AllowGet);
                    }
                    res = dal.UpdateUserDetailsInDB(model, email);
                    //Reset Session if the sessiondata has some missing data
                    if (res != null && res.Status == 200)
                    {
                        var sessionData = (Session["UserData"] as UserData);
                        sessionData.EmpNumber = res.Model.EmpNumber;
                        sessionData.CostCenter = res.Model.CostCenter;
                        sessionData.Department = res.Model.Department;
                        sessionData.FirstName = res.Model.FirstName;
                        sessionData.LastName = res.Model.LastName;
                        sessionData.PhoneNumber = res.Model.PhoneNumber;
                        sessionData.Email = res.Model.Email;
                        sessionData.SubDepartment = res.Model.SubDepartment;
                        sessionData.UserName = sessionData.UserName;
                        sessionData.Password = sessionData.Password;
                        GlobalClass.IsUserLoggedOut = false;
                        sessionData.UserAccessId = dal.GetUserAcessData(sessionData.UserName);
                        Session["UserData"] = sessionData;
                    }
                }
                else //This will execute when user session is expired or Email Id is not set from AD
                {
                    res.Status = 401;
                    res.Message = "User session expired";
                    return Json(res, JsonRequestBehavior.AllowGet);
                }
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return Json(res, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<ActionResult> GetDepartmentList()
        {
            LoginDAL listDAL = new LoginDAL();
            var result = await listDAL.GetDepartmentList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetSubDepartmentList(string DepId)
        {
            LoginDAL listDAL = new LoginDAL();
            var result = await listDAL.GetSubDepartmentList(DepId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SaveResponse(string response, int appRowId, string UserName, string UserFullname, string comment = "", int IsEnquired = 0, int approvalType = 0)
        {
            ResponseModel<object> FinalResult = new ResponseModel<object>();
            string AppHostUrl = ConfigurationManager.AppSettings["AppHostUrl"];
            var model = new EmailApproverStatusModel() { Status = "0", Url = AppHostUrl };

            FinalResult.Status = 0;
            DataTable dt = new DataTable();
            con = new SqlConnection(sqlConString);
            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter adapter = new SqlDataAdapter();
            cmd = new SqlCommand("USP_SaveResponseAppRowId", con);
            cmd.Parameters.Add(new SqlParameter("@appRowId", appRowId));
            // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
            cmd.CommandType = CommandType.StoredProcedure;
            adapter.SelectCommand = cmd;
            con.Open();
            adapter.Fill(dt);
            con.Close();
            var EmailStatus = "0";
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    EmailStatus = Convert.ToString(dt.Rows[i]["EmailApproverStatus"]);
                }
            }

            if (EmailStatus != null && EmailStatus != "" && EmailStatus == "0")
            {
                if ((IsEnquired == 1 && response.ToLower() == "enquired") || response.ToLower() != "enquired")
                {
                    ListDAL listDAL = new ListDAL();
                    var result = listDAL.SaveResponseFromMail(response, appRowId, comment, approvalType, UserName, UserFullname);
                    DataTable dt_A = new DataTable();
                    con = new SqlConnection(sqlConString);
                    SqlCommand cmd_A = new SqlCommand();
                    SqlDataAdapter adapter_A = new SqlDataAdapter();
                    cmd_A = new SqlCommand("USP_UpdateEmailStatusById", con);
                    cmd_A.Parameters.Add(new SqlParameter("@appRowId", Convert.ToInt64(appRowId)));
                    cmd_A.Parameters.Add(new SqlParameter("@EmailStatus", Convert.ToString("1")));
                    cmd_A.CommandType = CommandType.StoredProcedure;
                    adapter_A.SelectCommand = cmd_A;
                    con.Open();
                    adapter_A.Fill(dt_A);
                    con.Close();

                    model = new EmailApproverStatusModel() { Status = "1", Url = AppHostUrl, Response = response };
                    //FinalResult.Status = 1;
                    if (response.ToLower() == "enquired")
                    {
                        FinalResult.Status = 200;
                        return Json(FinalResult, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    model = new EmailApproverStatusModel() { Status = "2", Url = AppHostUrl, Response = response, AppRowId = appRowId, UserName = UserName, UserFullname = UserFullname };
                }
            }
            //return View();
            return View("~/Views/Login/SaveResponse.cshtml", model);
            //return Json(FinalResult, JsonRequestBehavior.AllowGet);
        }

        public ActionResult validateEmail(string emailId)
        {
            try
            {
                LoginDAL listDAL = new LoginDAL();
                var result = listDAL.ForgotPassword(emailId);
                //ToastMessage response = adminDal.ForgotPassword(emailId);
                return Json(result);

            }
            catch (Exception ex)
            {
                return Json(ex.Message);
                //return new ResponseModel<UserData>
                //{
                //    Model = userData,
                //    Message = "There was some issue while logging in. Please contact your system administrator",
                //    Status = 400
                //};
                // return Json(new ResponseModel(0, "There was some issue while logging in. Please contact your system administrator"));
            }
        }


    }
}
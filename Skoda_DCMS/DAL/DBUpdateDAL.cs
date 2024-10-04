using Skoda_DCMS.App_Start;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.DAL
{
    public class DBUpdateDAL
    {
        public UserData user/* = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData()*/; public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;



        //public ActionResult CreateUserDetails(FormCollection form)
        //{
        //    SqlCommand cmd = new SqlCommand();
        //    SqlDataAdapter adapter = new SqlDataAdapter();
        //    //DataTable ds = new DataTable();

        //    con = new SqlConnection(sqlConString);
        //    cmd = new SqlCommand("sp_CreateUserDetails", con);

        //    cmd.Parameters.Add(new SqlParameter("@empNumber", form["txtEmployeeNumber"]));
        //    cmd.Parameters.Add(new SqlParameter("@firstname", form["txtFirstName"]));
        //    cmd.Parameters.Add(new SqlParameter("@middlename", form["txtMiddleName"]));
        //    cmd.Parameters.Add(new SqlParameter("@lastname", form["txtLastName"]));
        //    cmd.Parameters.Add(new SqlParameter("@email", form["txtEmailID"]));
        //    cmd.Parameters.Add(new SqlParameter("@phonenumber", form["txtPhoneNumber"]));
        //    cmd.Parameters.Add(new SqlParameter("@costccenter", form["txtCostCenter"]));
        //    cmd.Parameters.Add(new SqlParameter("@department", form["txtDepartment"]));
        //    cmd.Parameters.Add(new SqlParameter("@subdepartment", form["txtSubDepartment"]));
        //    cmd.Parameters.Add(new SqlParameter("@mgrempNumber", form["txtManagerEmployeeNumber"]));

        //    cmd.CommandType = CommandType.StoredProcedure;
        //    con.Open();
        //    cmd.ExecuteNonQuery();
        //    cmd.Dispose();
        //    con.Close();
        //    return null;

        //}
        //get user details-autocomplete for HR only
        public List<UserData> GetUserDetails(string search)
        {
            List<UserData> users = new List<UserData>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetUserDetails", con);
                cmd.Parameters.Add(new SqlParameter("@search", search));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        UserData user = new UserData();
                        user.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        user.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        user.EmployeeName = ds.Tables[0].Rows[i]["FullName"].ToString();
                        user.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        users.Add(user);
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return users;
        }

        public UserData GetExistingUserDetails(string empNumber)
        {
            UserData userData = new UserData();
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_GetExistingUserDetails", con);
                cmd.Parameters.Add(new SqlParameter("@empnumber", empNumber));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds);
                con.Close();

                if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        userData.EmpNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["EmployeeNumber"]);
                        userData.FirstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                        userData.MiddleName = ds.Tables[0].Rows[i]["MiddleName"].ToString();
                        userData.LastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                        userData.Email = ds.Tables[0].Rows[i]["EmailID"].ToString();
                        userData.PhoneNumber = Convert.ToString(ds.Tables[0].Rows[i]["PhoneNumber"]);
                        userData.CostCenter = Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"]);
                        userData.Department = ds.Tables[0].Rows[i]["Department"].ToString();
                        userData.SubDepartment = ds.Tables[0].Rows[i]["SubDepartment"].ToString();
                        userData.ManagerEmployeeNumber = Convert.ToInt32(ds.Tables[0].Rows[i]["ManagerEmployeeNumber"]);
                    }
                }
                //var data = GetCompanyNameFromAD(userData.Email);
                //userData.CompanyName = data.CompanyName;
                //userData.UserName = data.UserName;

            }
            catch (Exception ex) { Log.Error(ex.Message, ex); }
            return userData;
        }

        public int UpdateUserDetails(System.Web.Mvc.FormCollection form)
        {
            int result = 0;
            try
            {
                string[] costCenterArry = form["txtCostCenter"].Split('|');
                string ccNumber = costCenterArry[0];
                string[] mrgEmpNumberArry = form["txtManagerEmployeeNumber"].Split('|');
                string mgrEmpNumber = mrgEmpNumberArry[0];


                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("sp_UpdateUserDetails", con);
                cmd.Parameters.Add(new SqlParameter("@firstname", form["txtFirstName"]));
                cmd.Parameters.Add(new SqlParameter("@middlename", form["txtMiddleName"]));
                cmd.Parameters.Add(new SqlParameter("@lastname", form["txtLastName"]));
                cmd.Parameters.Add(new SqlParameter("@email", form["txtEmailID"]));
                cmd.Parameters.Add(new SqlParameter("@phonenumber", form["txtPhoneNumber"]));
                cmd.Parameters.Add(new SqlParameter("@costccenter", ccNumber));
                cmd.Parameters.Add(new SqlParameter("@department", form["txtDepartment"]));
                cmd.Parameters.Add(new SqlParameter("@subdepartment", form["txtSubDepartment"]));
                cmd.Parameters.Add(new SqlParameter("@mgrempNumber", mgrEmpNumber));
                cmd.Parameters.Add(new SqlParameter("@empNumber", form["txtEmployeeNumber"]));
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                result = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Dispose();
                con.Close();

                //result.one = 1;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return result;
        }
    }
}
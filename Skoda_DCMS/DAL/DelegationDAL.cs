
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows;

namespace Skoda_DCMS.DAL
{
    public class DelegationDAL
    {
        //public UserData user;
        public Delegation user;
        public readonly string conString = ConfigurationManager.AppSettings["SharepointServerURL"];
        public readonly string spUsername = ConfigurationManager.AppSettings["SharepointUsername"];
        public readonly string spPass = ConfigurationManager.AppSettings["SharepointPass"];
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        public DelegationDAL()
        {
            //GlobalClass obj = new GlobalClass();
            //user = obj.GetCurrentUser();
        }

        //Insert New Record
        public string CreateDelegation(System.Web.Mvc.FormCollection form)
        {

            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter adapter = new SqlDataAdapter();

            con = new SqlConnection(sqlConString);
            cmd = new SqlCommand("sp_CreateDelegateTask", con);
            var ToEmp = Convert.ToInt64(form["txtToEmployeeID"]);
            var FromEmp = Convert.ToInt64(form["txtFromEmployeeID"]);
            cmd.Parameters.Add(new SqlParameter("@FromEmployeeID", FromEmp));
            cmd.Parameters.Add(new SqlParameter("@ToEmployeeID", ToEmp));

            var startDate = form["txtStartDate"];
            var endDate = form["txtEndDate"];
            cmd.Parameters.Add(new SqlParameter("@StartDate", startDate));
            cmd.Parameters.Add(new SqlParameter("@EndDate", endDate + " 23:59:59"));
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();

            var insertResult = cmd.ExecuteScalar();
            cmd.Dispose();
            con.Close();
            return insertResult.ToString();

       

        }

        //List details
        public List<Delegation> GetUserDetails()
        {
            List<Delegation> users = new List<Delegation>();

            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds = new DataSet();
            GlobalClass gc = new GlobalClass();
            var currentUser = gc.GetCurrentUser();

            con = new SqlConnection(sqlConString);
            cmd = new SqlCommand("sp_GetListOfDelegation", con);
            cmd.Parameters.Add(new SqlParameter("@FromEmployeeID", currentUser.EmpNumber));
            cmd.CommandType = CommandType.StoredProcedure;
            adapter.SelectCommand = cmd;
            con.Open();
            adapter.Fill(ds);
            con.Close();

            if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    Delegation user = new Delegation();
                    user.ID = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]);
                    user.FromEmployeeID = Convert.ToString(ds.Tables[0].Rows[i]["FromEmployeeID"]);
                    user.ToEmployeeID = Convert.ToString(ds.Tables[0].Rows[i]["ToEmployeeID"]);
                    user.StartDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["StartDate"]).ToString("dd-MM-yyyy");
                    user.EndDate = Convert.ToDateTime(ds.Tables[0].Rows[i]["EndDate"]).ToString("dd-MM-yyyy");
                    users.Add(user);
                }
            }


          
            //catch (Exception ex);  
            return users;
        }

        public int DeleteDelegate(int ID)
        {
            int deleteResult = 0;
            SqlCommand cmd = new SqlCommand();
            con = new SqlConnection(sqlConString);
            cmd = new SqlCommand("sp_DeleteDelegateTask", con);
            cmd.Parameters.Add(new SqlParameter("@Id", ID));
            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
               // MessageBox.Show("Deleted...");
                con.Open();

                deleteResult = cmd.ExecuteNonQuery();
                cmd.Dispose();
                con.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            finally
            {
                con.Close();
            }
            return deleteResult;
        }
    }

}





    

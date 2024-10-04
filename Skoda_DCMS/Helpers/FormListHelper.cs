using Skoda_DCMS.App_Start;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Helpers
{
    public class FormListHelper
    {
        public static string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public static List<FormListModel> FilterFormList(UserData userDetails, List<FormListModel> list)
        {
            SqlConnection con = null;
            try
            {
                string uniqueFormNameList = String.Join(",", list.Select(x => x.UniqueName));
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                con = new SqlConnection(sqlConString);
                var command = new SqlCommand("sp_GetFormAuthorization", con);
                command.Parameters.Add(new SqlParameter("@department", userDetails.Department));
                command.Parameters.Add(new SqlParameter("@userName", userDetails.UserName));
                command.Parameters.Add(new SqlParameter("@uniqueFormName", uniqueFormNameList));
                command.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = command;
                con.Open();
                adapter.Fill(data);
                con.Close();
                if (data?.Tables[0] != null && data?.Tables[0]?.Rows?.Count > 0)
                {
                    List<FormAuthModel> authList = new List<FormAuthModel>();
                    for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                    {
                        var item = new FormAuthModel()
                        {
                            UniqueFormName = Convert.ToString(data.Tables[0].Rows[i]["UniqueFormName"]),
                            IsShow = Convert.ToBoolean(data.Tables[0].Rows[i]["IsShow"]),
                            IsActive = Convert.ToBoolean(data.Tables[0].Rows[i]["IsActive"]),
                            UserName = Convert.ToString(data.Tables[0].Rows[i]["UserName"]),
                            Department = Convert.ToString(data.Tables[0].Rows[i]["Department"])
                        };
                        authList.Add(item);
                    }

                    foreach (var item in authList)
                    {
                        //if (authList.Any(x => x.UserName.ToLower() == userDetails.UserName.ToLower() && x.UniqueFormName.ToLower() == item.UniqueFormName.ToLower()))
                        //{
                        //    if (!item.IsShow)
                        //    {
                        //        var obj = list.Find(x => x.UniqueName == item.UniqueFormName);
                        //        if (obj != null)
                        //            list.Remove(obj);
                        //        continue;
                        //    }
                        //    else if (authList.All(x => x.UserName.ToLower() != userDetails.UserName.ToLower()))
                        //    {

                        //    }
                        //}
                        if (item.IsActive && !item.IsShow && !string.IsNullOrEmpty(item.UserName) && item.UserName.ToLower() == userDetails.UserName.ToLower())
                        {
                            var obj = list.Find(x => x.UniqueName == item.UniqueFormName);
                            if (obj != null)
                                list.Remove(obj);
                            continue;
                        }
                        //if (
                        //    item.IsActive && !item.IsShow
                        //    && (!string.IsNullOrEmpty(item.Department) && item.Department.ToLower() == userDetails.Department.ToLower())
                        //    && (!string.IsNullOrEmpty(item.Department)
                        //        && !authList.Any(x => x.Department.ToLower() == userDetails.Department.ToLower()
                        //            && !string.IsNullOrEmpty(x.UserName) && x.UserName.ToLower() == userDetails.UserName.ToLower()
                        //            && x.IsActive && x.IsShow))
                        //)
                        //{
                        //    var obj = list.Find(x => x.UniqueName == item.UniqueFormName);
                        //    if (obj != null)
                        //        list.Remove(obj);
                        //}
                        //else if (item.IsActive && item.IsShow)// show but to only specifically one or more user
                        //{
                        //    if (!authList.Any(x =>
                        //        x.UniqueFormName == item.UniqueFormName && x.IsShow && x.IsActive
                        //        //&& !string.IsNullOrEmpty(x.UserName) && !string.IsNullOrEmpty(item.UserName)
                        //        && x.UserName.ToLower() == item.UserName.ToLower())
                        //    ) {
                        //        var obj = list.Find(x => x.UniqueName == item.UniqueFormName);
                        //        if (obj != null)
                        //            list.Remove(obj);
                        //        continue;
                        //    }
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return list;
            }
            finally
            {
                con.Close();
            }
            return list;
        }

        public static bool IsFormAccessibleToUser(UserData userDetails, string uniqueFormName)
        {
            SqlConnection con = null;
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                con = new SqlConnection(sqlConString);
                var command = new SqlCommand("sp_GetFormAuthorization", con);
                command.Parameters.Add(new SqlParameter("@department", userDetails.Department));
                command.Parameters.Add(new SqlParameter("@userName", userDetails.UserName));
                command.Parameters.Add(new SqlParameter("@uniqueFormName", uniqueFormName));
                command.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = command;
                con.Open();
                adapter.Fill(data);
                con.Close();
                if (data.Tables[0] != null && data.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                    {
                        var item = new FormAuthModel()
                        {
                            UniqueFormName = Convert.ToString(data.Tables[0].Rows[i]["UniqueFormName"]),
                            IsShow = Convert.ToBoolean(data.Tables[0].Rows[i]["IsShow"]),
                            IsActive = Convert.ToBoolean(data.Tables[0].Rows[i]["IsActive"]),
                            UserName = Convert.ToString(data.Tables[0].Rows[i]["UserName"]),
                            Department = Convert.ToString(data.Tables[0].Rows[i]["Department"])
                        };
                        if (
                            (!string.IsNullOrEmpty(item.UserName) && item.UserName.ToLower() == userDetails.UserName.ToLower()) // For UserName
                            || (string.IsNullOrEmpty(item.UserName) && !string.IsNullOrEmpty(item.Department) && item.Department.ToLower() == userDetails.Department.ToLower()) // Check for Department
                        )
                            return item.IsActive && item.IsShow;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return false;
            }
            finally
            {
                con.Close();
            }
            return true;
        }

        class FormAuthModel
        {
            public string UniqueFormName { get; set; }
            public string UserName { get; set; }
            public string Department { get; set; }
            public bool IsShow { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
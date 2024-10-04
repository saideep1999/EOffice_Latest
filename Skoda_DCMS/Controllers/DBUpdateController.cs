using ExcelDataReader;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


namespace Skoda_DCMS.Controllers
{
    public class DBUpdateController : BaseController
    {
        SqlConnection con;
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();

       

        public ActionResult ReadExcel()
        {
            return View();
        }



        //excel data insert into table
        public ActionResult CreateUser()
        {
            DataTable dt = new DataTable();

            try
            {
                dt = (DataTable)Session["UserData"];
            }
            catch (Exception ex)
            {

            }

            return View(dt);
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]// prevent cross-site request forgery attacks.
        public ActionResult CreateUserDetails()
        {
            var file = Request.Files[0];
            //return View();
            UserData userData = new UserData();
            SqlCommand cmd = new SqlCommand();
            var userDetailsList = new List<Tuple<string, string>>();
            if (ModelState.IsValid)
            {
                //ExcelDataReader works with the binary Excel file, so it needs a FileStream
                //to get started. This is how we avoid dependencies on ACE or Interop:
                if (file != null && file.ContentLength > 0)
                {
                    Stream stream = file.InputStream;

                    IExcelDataReader reader = null;

                    if (file.FileName.EndsWith(".xls"))
                    {
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                    }
                    else if (file.FileName.EndsWith(".xlsx"))
                    {
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    }
                    else
                    {
                        userDetailsList.Add(new Tuple<string, string>("This file format is not supported", ""));
                        return Json(userDetailsList, JsonRequestBehavior.AllowGet);
                        //ModelState.AddModelError("File", "This file format is not supported");
                        //return View();
                    }
                    int fieldcount = reader.FieldCount;
                    int rowcount = reader.RowCount;
                    DataTable dt = new DataTable();
                    DataTable dt_ = new DataTable();
                    DataRow row;
                    try
                    {
                        dt_ = reader.AsDataSet().Tables[0];
                        for (int i = 0; i < dt_.Columns.Count; i++)
                        {
                            dt.Columns.Add(dt_.Rows[0][i].ToString().Trim());
                        }
                        int rowcounter = 0;
                        for (int row_ = 1; row_ < dt_.Rows.Count; row_++)
                        {
                            row = dt.NewRow();

                            for (int col = 0; col < dt_.Columns.Count; col++)
                            {
                                row[col] = dt_.Rows[row_][col].ToString();
                                rowcounter++;
                            }
                            dt.Rows.Add(row);
                        }


                        //binding
                        DataSet ds = new DataSet();
                        ds.Tables.Add(dt);

                        SqlDataAdapter adapter = new SqlDataAdapter();


                        con = new SqlConnection(sqlConString);
                        cmd = new SqlCommand("sp_CreateUserDetails", con);
                        con.Open();

                        if (ds.Tables[0].Rows.Count > 0 && ds.Tables[0] != null)
                        {
                            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                            {
                                var isValidRecord = true;
                                var firstName = ds.Tables[0].Rows[i]["FirstName"].ToString();
                                if (string.IsNullOrEmpty(firstName))
                                {
                                    userDetailsList.Add(new Tuple<string, string>(
                                        "", $"First Name of row {i + 1} is required"));
                                    isValidRecord = false;
                                    continue;
                                }
                                var email = ds.Tables[0].Rows[i]["EmailId"].ToString();
                                if (string.IsNullOrEmpty(email))
                                {
                                    userDetailsList.Add(new Tuple<string, string>(
                                        "", $"Email Id of row {i + 1} is required"));
                                    isValidRecord = false;
                                    continue;
                                }
                                //var lastName = ds.Tables[0].Rows[i]["LastName"].ToString();
                                //if (string.IsNullOrEmpty(lastName))
                                //{
                                //    userDetailsList.Add(new Tuple<string, string>(
                                //        "", $"Last Name of row {i} is required"));
                                //    isValidRecord = false;
                                //    continue;
                                //}
                                var costCenter = ds.Tables[0].Rows[i]["CostCenter"].ToString();
                                if (string.IsNullOrEmpty(costCenter))
                                {
                                    userDetailsList.Add(new Tuple<string, string>(
                                        ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), $"Cost Center is required"));
                                    isValidRecord = false;
                                }
                                var mgrEmpNum = ds.Tables[0].Rows[i]["ManagerEmployeeNumber"].ToString();
                                if (string.IsNullOrEmpty(mgrEmpNum))
                                {
                                    userDetailsList.Add(new Tuple<string, string>(
                                        ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), $"Manager Employee Number is required"));
                                    isValidRecord = false;
                                }
                                var empNum = ds.Tables[0].Rows[i]["EmployeeNumber"].ToString();
                                if (string.IsNullOrEmpty(empNum))
                                {
                                    userDetailsList.Add(new Tuple<string, string>(
                                        ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), $"Employee Number is required"));
                                    isValidRecord = false;
                                }
                                if (isValidRecord)
                                {
                                    cmd.Parameters.Add(new SqlParameter("@empnumber", ds.Tables[0].Rows[i]["EmployeeNumber"]));
                                    cmd.Parameters.Add(new SqlParameter("@firstname", ds.Tables[0].Rows[i]["FirstName"].ToString()));
                                    cmd.Parameters.Add(new SqlParameter("@middlename", ds.Tables[0].Rows[i]["MiddleName"].ToString()));
                                    cmd.Parameters.Add(new SqlParameter("@lastname", ds.Tables[0].Rows[i]["LastName"].ToString()));
                                    cmd.Parameters.Add(new SqlParameter("@email", ds.Tables[0].Rows[i]["EmailId"].ToString()));
                                    cmd.Parameters.Add(new SqlParameter("@phonenumber", Convert.ToString(ds.Tables[0].Rows[i]["PhoneNumber"])));
                                    cmd.Parameters.Add(new SqlParameter("@costccenter", Convert.ToInt32(ds.Tables[0].Rows[i]["CostCenter"])));
                                    cmd.Parameters.Add(new SqlParameter("@department", ds.Tables[0].Rows[i]["Department"].ToString()));
                                    cmd.Parameters.Add(new SqlParameter("@subdepartment", ds.Tables[0].Rows[i]["SubDepartment"].ToString()));
                                    cmd.Parameters.Add(new SqlParameter("@mgrempNumber", Convert.ToInt32(mgrEmpNum)));


                                    cmd.CommandType = CommandType.StoredProcedure;
                                    var output = Convert.ToInt32(cmd.ExecuteScalar());
                                    if (output == -1)
                                    {
                                        userDetailsList.Add(new Tuple<string, string>(
                                            ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), $"Employee Number {ds.Tables[0].Rows[i]["EmployeeNumber"] } already exists"));
                                    }
                                    else if (output == -2)
                                    {
                                        userDetailsList.Add(new Tuple<string, string>(
                                            ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), $"Email Id {ds.Tables[0].Rows[i]["EmailId"] } already exists"));
                                    }
                                    else if (output == 1)
                                    {
                                        userDetailsList.Add(new Tuple<string, string>(
                                            ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), "Successfully Added"));//Do not change this msg as it is checked in the front end
                                    }
                                    else
                                    {
                                        userDetailsList.Add(new Tuple<string, string>(
                                            ds.Tables[0].Rows[i]["FirstName"].ToString() + " " + ds.Tables[0].Rows[i]["LastName"].ToString(), "Unable to Add to Database"));
                                    }
                                    cmd.Parameters.Clear();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(userDetailsList, JsonRequestBehavior.AllowGet);
                    }
                    finally
                    {
                        cmd.Dispose();
                        con.Close();
                    }
                    //DataSet result = new DataSet();
                    //result.Tables.Add(dt);
                    //reader.Close();
                    //reader.Dispose();
                    //DataTable tmp = result.Tables[0];
                    //Session["UserData"] = tmp;  //store datatable into session
                    //return RedirectToAction("UserDetails");
                }
                else
                {
                    ModelState.AddModelError("File", "Please Upload Your file");
                }


            }
            return Json(userDetailsList, JsonRequestBehavior.AllowGet);
        }
    
       // GET: DBUpdate
        public ActionResult UserDetails()
        {
            return View();
        }

        public ActionResult GetUserDetails(string searchText)
        {
            DBUpdateDAL dBUpdateDAL = new DBUpdateDAL();
            var result = dBUpdateDAL.GetUserDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetExistingUserDetails(string empNumber)
        {
            DBUpdateDAL dBUpdateDAL = new DBUpdateDAL();
            var result = dBUpdateDAL.GetExistingUserDetails(empNumber);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult UpdateUserDetails(FormCollection form)
        {
            DBUpdateDAL dBUpdateDAL = new DBUpdateDAL();
            var result = dBUpdateDAL.UpdateUserDetails(form);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
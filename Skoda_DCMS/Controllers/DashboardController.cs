using System.Collections.Generic;
using System.Web.Mvc;
using System.Threading.Tasks;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System.Diagnostics;
using Skoda_DCMS.Filters;
using System.Linq;
using System;
using static Skoda_DCMS.Helpers.Flags;
using OfficeOpenXml;
using Newtonsoft.Json;
using System.IO;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Helpers;

namespace Skoda_DCMS.Controllers
{
    public class DashboardController : BaseController
    {
        ListDAL listDAL;
        DelegationDAL DlistDAL;
        // GET: Dashboard
        public async Task<ActionResult> Index(int tab = 1)  
        {
            //string Checked = string.Empty;
            //string Filter = string.Empty;
            //ListDAL listDAL = new ListDAL();
            //var result = await listDAL.GetPendingForms(Checked, Filter);
            //var request = await listDAL.GetAllFormsList("", "", "");
            //var data = await listDAL.GetForms();
            //var model = new DashboardModel();
            //model.Data = data;
            //model.Data.Forms = result ?? new List<FormData>();
            //model.Data.StatusCount = listDAL.GetStatusCount();
            //model.Data.FormsRequest = request;
            ViewBag.Tab = tab;
            return View();
            //return View(model);
        }


        /// <summary>
        /// Dashboard-It is used to create MyTask section.
        /// </summary>
        /// <returns></returns>
        public async Task<PartialViewResult> GetMyTasks(string Checked, string Filter)
        {
            //string Checked = "1";
            ListDAL listDAL = new ListDAL();
           var result = await listDAL.GetPendingForms(Checked, Filter);
            var model = new DashboardModel();
            model.Data = new DataModel();
            model.Data.Forms = result ?? new List<FormData>();
            return PartialView("_MyTaskView", model);
        }

        //public async Task<ActionResult> FormDashboard()
        //{
        //    ListDAL listDAL = new ListDAL();
        //    var result = await listDAL.GetAllFormsList();
        //    var model = new DashboardModel();
        //    model.Data = new DataModel();
        //    model.Data.Forms = result ?? new List<FormData>();
        //    return View("~/Views/Pages/FormDashboard.cshtml",model);
        //}

        //public ActionResult ViewFormDash(string formName)
        //{
        //    ListDAL listDAL = new ListDAL();
        //    var result = listDAL.GetUserFormsList(formName);
        //    var model = new DashboardModel();
        //    model.Data = new DataModel();
        //    model.Data.Forms = result ?? new List<FormData>();
        //    return View("~/Views/Pages/FormDashboard.cshtml", model);
        //}

        /// <summary>
        /// Dashboard-It is used to get forms on dashboard tiles with status.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetMyForms(string formName, string department, string status, string fullName)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "_CommonFormDashboard" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle(formName, department, status, fullName);
            var result = await listDAL.GetAllFormsList(formName, department, status);
            model.Data.Forms = result;
            model.Data.StatusCount = listDAL.GetStatusCount();
            return View("~/Views/Pages/FormDashboard.cshtml", model);
        }

        [HttpGet]
        public async Task<ActionResult> LoadFormDashboard(FormDashboard dashboardType)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() };
            var result = await listDAL.GetFormsBySearch();
            model.Data = result;
            model.Data.PartialViewName = "_CommonFormDashboard";
            model.Data.BreadCrumbs = new List<KeyValuePair<string, string>>();
            model.Data.BreadCrumbs.Add(new KeyValuePair<string, string>("Dashboard", "../Dashboard/Index"));

            switch (dashboardType)
            {
                case FormDashboard.FreqUsedForms:
                    {
                        model.Data.Forms = model.Data.FreqUsedForms;
                        break;
                    }
                case FormDashboard.NewlyAddedForms:
                    {
                        model.Data.Forms = model.Data.NewlyAddedForms;
                        break;
                    }
            }
            model.Data.StatusCount = listDAL.GetStatusCount();
            return View("~/Views/Pages/FormDashboard.cshtml", model);
        }

        #region Dashboard Related
        /// <summary>
        /// Dashboard-It is used to get the View Form Dashboard by Form Name in View Click of Any Form.
        /// </summary>
        /// <returns></returns>
        /// 

        [HttpGet]
        public async Task<ActionResult> GetMyFormsByName(string formName, int formParentId, string Department, string status, string fullName, string FormOwner, string ControllerName)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "ViewCommonFormDashbaord" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle(formName, Department, status, fullName);
            var result = await listDAL.GetAllFormsListByName(formName, "", status);
            model.Data.Forms = result;
            model.Data.UniqueFormName = formName;
            model.Data.FormParentId = formParentId;
            model.Data.FullFormName = fullName;
            model.Data.Department = Department;
            model.Data.FormOwner = FormOwner;
            model.Data.ControllerName = ControllerName;
            model.Data.StatusCount = listDAL.GetStatusCountByForm(formName);

            return View("~/Views/Pages/ViewFormDashboard.cshtml", model);
        }

        /// <summary>
        /// Dashboard-It is used to get My Task Section via Side Menu Click. 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetMyTaskByLink(string formName, string department, string status, string fullName)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "_MyTaskView" } };
            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle(formName, department, status, fullName);
            string Checked = "", Filter = "";
            var result = await listDAL.GetPendingForms(Checked, Filter);

            model.Data.Forms = result ?? new List<FormData>();
            return View("~/Views/Pages/MyTask.cshtml", model);
        }

        /// <summary>
        /// Dashboard-It is used to Get Form Name List Dropdown.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetDashboardFormsList(string department)
        {
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetDashboardFormsList(department);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        //[HttpPost]
        //public async Task<JsonResult> PostUser(string userName)
        //{
        //    ListDAL listDAL = new ListDAL();
        //    var result = await listDAL.AddUserToSharepointList(userName);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}


        /// <summary>
        /// Dashboard-It is used to get department wise forms.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetDepartmentWiseForms(string department)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "_DeptFormDashboard" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle("", department, "", "");
            var result = await listDAL.GetDepartmentWiseForms(department);
            model.Data.Forms = result;
            return View("~/Views/Pages/FormDashboard.cshtml", model);
        }


        /// <summary>
        /// Dashboard-It is used to Get Department Wise Form from Side Menu link.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetDeptWiseForm(string formName, string department, string status, string fullName)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "DepartmentWiseFormPartialView" } };
            var result = await listDAL.GetForms();
            model.Data = result;
            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle("", department, "", "");

            return View("~/Views/Pages/DepartmentWiseForm.cshtml", model);
        }

        //public ActionResult CoordinatesFromPdf()
        //{
        //    ListDAL listDAL = new ListDAL();
        //    var result = listDAL.CoordinatesFromPdf("3213");
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        #region Search Form in Dashboard - Auto
        /// <summary>
        /// Dashboard-It is used to Search Form Name - Auto-Complete Code
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> SearchForms(string Prefix)
        {
            try
            {
                var listDAL = new ListDAL();
                var result = await listDAL.SearchForms(Prefix);
                var forms = result.NewlyAddedForms.Where(x => x.FormName.ToLower().Contains(Prefix.ToLower()) || x.UniqueFormName.ToLower().Contains(Prefix.ToLower()))
                    .Select(y => new FormListModel { IsDisable = y.IsDisable, Message = y.Message, FormName = y.FormName, FormId = y.UniqueFormId, UniqueName = y.UniqueFormName });

                //var user = (UserData)Session["UserData"];
                //if (user != null)
                //{
                //    forms = FormListHelper.FilterFormList(user, forms.ToList());
                //}

                return Json(forms, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Dashboard-It is used to Search Form List Data For  Auto-Complete Code
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> FormLink(string formName)
        {
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetFormDet(formName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Newly Added & Freq. Used Search Code
        /// <summary>
        /// Dashboard-It is used to Search Form Name on Footer section of dashboard Newly added form.
        /// </summary>
        /// <returns></returns>

        public async Task<ActionResult> GetFormsBySearch(string Search)
        {
            ListDAL listDAL = new ListDAL();
            var data = await listDAL.GetFormsBySearch(Search);
            var model = new DashboardModel();
            model.Data = data;
            return Json(data.NewlyAddedForms, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Dashboard-It is used to Search Form Name on Footer section of dashboard Freq. used form.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetFreqFormsBySearch(string search)
        {
            ListDAL listDAL = new ListDAL();
            var data = await listDAL.GetFormsBySearch(search);
            var model = new DashboardModel();
            model.Data = data;
            return Json(data.FreqUsedForms, JsonRequestBehavior.AllowGet);
        }

        #endregion


        //Delegation Section
        [HttpPost]
        public async Task<ActionResult> DelegationList(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                DlistDAL = new DelegationDAL();
                var result = DlistDAL.CreateDelegation(form);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            return Json(0, JsonRequestBehavior.AllowGet);
        }
        //GET:  List Delegation
        //public ActionResult DelegationDetails()
        //{
        //    return View();
        //}

        public ActionResult GetDelegationDetails()
        {
            DelegationDAL DlistDAL = new DelegationDAL();
            var result = DlistDAL.GetUserDetails();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //Delete delegation

        //public ActionResult DeleteDelegation()
        //{
        //    return View();
        //}

        public ActionResult DeleteDelegation(int ID)
        {
            DelegationDAL DlistDAL = new DelegationDAL();
            var result = DlistDAL.DeleteDelegate(ID);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        #region Report Section
        /// <summary>
        /// Dashboard-It is used to Get Report Section Page.
        /// </summary>
        /// <returns></returns>
        /// 
        [HttpGet]
        public async Task<PartialViewResult> GetMyReport(string formName, string Created, string status)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "ReportPartialView" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle(formName, Created, status);
            var result = await listDAL.GetAllFormsListForReport(formName, Created, status);
            model.Data.Forms = result;
            model.Data.StatusCount = listDAL.GetStatusCount();
            //return View("~/Views/Pages/Report.cshtml", model);
            return PartialView("Report", model);
        }
        
        /// <summary>
        /// Dashboard-It is used to Get Report Section Forms List Dropdown.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetFormsList()
        {
            ListDAL listDAL = new ListDAL();
            //var result = await listDAL.GetFormsList();
            var result = await listDAL.GetFormsList_SQL();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> PieChart(string formName = "", string fromDate = "", string toDate = "", string status = "", string location = "")
        {
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetPieChart(formName, fromDate, toDate, status, location);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        


        /// <summary>
        /// Dashboard-It is used to Filter Report Section.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetMyReportFilter(string formName, string fromDate, string toDate, string status, string location)
        {
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetAllFormsListForReport(formName, fromDate, toDate, status, location);
            var model = new DashboardModel();
            model.Data = new DataModel();
            model.Data.Forms = result ?? new List<FormData>();
            model.Data.UniqueFormName = formName;
            return PartialView("ReportPartialView", model);
        }

        #endregion

        public async Task<ActionResult> ExportToExcel(DashboardModel model)
        {
            var data = new byte[0];
            try
            {
                if (Convert.ToString(Session["ActiveFlag"]) == "RPT")
                {
                    DashboardModel ModelSession = new DashboardModel();
                    model = (DashboardModel)Session["Common"];
                }

                if (model == null)
                {
                    return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, "Report.xlsx");
                }

                //var firstFormName = model.Data.Forms.FirstOrDefault().UniqueFormName;
                var firstFormName = model.Data.UniqueFormName;
                //if (model.Data.Forms.All(x => x.UniqueFormName == firstFormName))

                if (!string.IsNullOrEmpty(firstFormName))
                {
                    switch (firstFormName)
                    {
                        case "CBRF":
                            {
                                data = await GetCabRequestReport(model.Data.Forms);
                                break;
                            }
                        case "BTF":
                            {
                                data = await GetBusTransportReport(model.Data.Forms);
                                break;
                            }
                        case "MRF":
                            {
                                data = await GetMaterialRequestReport(model.Data.Forms);
                                break;
                            }
                        case "IDCF":
                            {
                                data = await GetIDCardExcelReport(model.Data.Forms);
                                break;
                            }
                        case "GAIF":
                            {
                                data = await GetGiftInvitationReport(model.Data.Forms);
                                break;
                            }
                        case "DLIC":
                            data = await GetDLICExcelReport(model.Data.Forms);
                            break;
                        case "NGCF":
                            {
                                data = await GetNGCFReport(model.Data.Forms);
                                break;
                            }
                        case "URCF":
                            {
                                data = await GetURCFReport(model.Data.Forms);
                                break;
                            }
                        case "QMCR":
                            {
                                data = await GetQMCRReport(model.Data.Forms);
                                break;
                            }
                        case "MMRF":
                            {
                                data = await GetMMRFReport(model.Data.Forms);
                                break;
                            }
                        case "APFP":
                            {
                                data = await GetAPFPReport(model.Data.Forms);
                                break;
                            }
                        case "IMAC":
                            {
                                data = await GetIMACReport(model.Data.Forms);
                                break;
                            }
                        case "EQSA":
                            {
                                data = await GetEQSAReport(model.Data.Forms);
                                break;
                            }
                        case "QFRF":
                            {
                                data = await GetQFRFReport(model.Data.Forms);
                                break;
                            }
                        case "IPAF":
                            {
                                data = await GetIPAFReport(model.Data.Forms);
                                break;
                            }
                        case "POCRF":
                            {
                                List<POCRFormModel> routeList = new List<POCRFormModel>();
                                data = await GetPOCRFReport(routeList);
                                break;
                            }
                        //case "NEIF":
                        //    {
                        //        data = await GetNEIFReport(model.Data.Forms);
                        //        break;
                        //    }
                        default:
                            {
                                data = await GetAllFormRequestReport(model.Data.Forms);
                                break;
                            }
                    }
                }
                else
                {
                    data = await GetAllFormRequestReport(model.Data.Forms);
                }

                return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, "Report.xlsx");

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return null;
        }
        public async Task<byte[]> GetAllFormRequestReport(List<FormData> formsList)
        {
            var reportData = new byte[0];

            var newList = formsList.OrderByDescending(x => x.RecievedDate).ToList();
            List<FormData> arrayData = newList;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

            Sheet.Cells["A1"].Value = "Form Id";
            Sheet.Cells["B1"].Value = "Form Name";
            Sheet.Cells["C1"].Value = "Request From";
            Sheet.Cells["D1"].Value = "Details/Business Needs";
            Sheet.Cells["E1"].Value = "Status";
            Sheet.Cells["F1"].Value = "Recieved Date";
            Sheet.Cells["G1"].Value = "Comment";
            int row = 2;
            foreach (var item in arrayData)
            {

                Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                Sheet.Cells[string.Format("G{0}", row)].Value = item.Comment;
                row++;
            }

            Sheet.Cells["A:AZ"].AutoFitColumns();
            reportData = Ep.GetAsByteArray();
            Ep.Dispose();

            return reportData;
        }

        public async Task<byte[]> GetCabRequestReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var cablist = await obj.ViewCBRFFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = cablist.Where(x => x.FormID.Id == formRow.UniqueFormId);

                    foreach (var cabRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (cabRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.CostCenterNumber = cabRecord.OnBehalfCostCenterNumber;
                        }
                        else
                        {
                            formData.CostCenterNumber = cabRecord.CostCenterNumber;
                        }

                        formData.FlightNo = cabRecord.FlightNo;

                        formData.FlightTime = cabRecord.FlightTime;

                        formData.CarRequiredFromDate = cabRecord.CarRequiredFromDate;
                        formData.CarRequiredToDate = cabRecord.CarRequiredToDate;



                        //Cab Booking User Details
                        formData.UserName = cabRecord.UserName;
                        formData.UserContactNumber = cabRecord.UserContactNumber;
                        formData.Destination = cabRecord.Destination;
                        formData.ReportingTime = cabRecord.ReportingTime;
                        formData.ReportingPlaceWithAddress = cabRecord.ReportingPlaceWithAddress;
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Cost Center Number";
                Sheet.Cells["H1"].Value = "Flight No";
                Sheet.Cells["I1"].Value = "Flight Time";
                Sheet.Cells["J1"].Value = "Comment";

                //Cab Booking User Details
                Sheet.Cells["K1"].Value = "Username";
                Sheet.Cells["L1"].Value = "User Contact Number";
                Sheet.Cells["M1"].Value = "Reporting Time";
                Sheet.Cells["N1"].Value = "Reporting Place With Address";
                Sheet.Cells["O1"].Value = "Destination";
                Sheet.Cells["P1"].Value = "Car Required From Date";
                Sheet.Cells["Q1"].Value = "Car Required To Date";
                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.FlightNo;
                    if (string.IsNullOrEmpty(item.FlightNo))
                    {
                        Sheet.Cells[string.Format("I{0}", row)].Value = "";
                    }
                    else
                    {
                        Sheet.Cells[string.Format("I{0}", row)].Value = item.FlightTime.ToString("hh:mm tt");
                    }

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("J{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("J{0}", row)].Value = "";
                    }

                    //Cab Booking User Details
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.UserName;
                    if (item.UserContactNumber != 0)
                    {
                        Sheet.Cells[string.Format("L{0}", row)].Value = item.UserContactNumber;
                    }

                    if (Convert.ToString(item.ReportingTime.Date) == "01-01-0001 00:00:00")
                    {
                        Sheet.Cells[string.Format("M{0}", row)].Value = "";
                    }
                    else
                    {
                        Sheet.Cells[string.Format("M{0}", row)].Value = item.ReportingTime.ToString("hh:mm tt");
                    }
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.ReportingPlaceWithAddress;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.Destination;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.CarRequiredFromDate.ToString("dd-MM-yyyy:hh:mm tt");
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.CarRequiredToDate.ToString("dd-MM-yyyy:hh:mm tt");
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetPOCRFReport(List<POCRFormModel> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var cablist = await obj.ViewPOCRExcelData();
                var finalDataList = new List<POCRFormModel>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = cablist.Where(x => x.FormId == formRow.FormId);

                    foreach (var cabRecord in matchingRecords)
                    {
                        var formData = new POCRFormModel();
                        formData = formRow.Clone();
                        //if (cabRecord.RequestSubmissionFor == "OnBehalf")
                        //{
                        //    formData.CostCenterNumber = cabRecord.OnBehalfCostCenterNumber;
                        //}
                        //else
                        //{
                        //    formData.CostCenterNumber = cabRecord.CostCenterNumber;
                        //}


                        formData.FormId = cabRecord.FormId;
                        formData.POCRNo = cabRecord.POCRNo;
                        formData.EmployeeType = cabRecord.EmployeeType;
                        formData.EmployeeName = cabRecord.EmployeeName;
                        formData.TSEName = cabRecord.TSEName;
                        formData.ZSMName = cabRecord.ZSMName;
                        formData.ASMName = cabRecord.ASMName;
                        formData.TSELocation = cabRecord.TSELocation;
                        formData.DealerCode = cabRecord.DealerCode;
                        formData.DealerName = cabRecord.DealerName;
                        formData.DealerLocation = cabRecord.DealerLocation;
                        formData.FILDealership = cabRecord.FILDealership;
                        formData.DealerSalesLastYr = cabRecord.DealerSalesLastYr;
                        formData.DealerSalesTill = cabRecord.DealerSalesTill;
                        formData.Status = cabRecord.Status;
                        formData.BuilderName = cabRecord.BuilderName;
                        formData.ProjectName = cabRecord.ProjectName;
                        formData.SiteName = cabRecord.SiteName;
                        formData.RERANumber = cabRecord.RERANumber;
                        formData.CustomerReference = cabRecord.CustomerReference;
                        formData.OrderValue = cabRecord.OrderValue;
                        formData.PreferredPlant = cabRecord.PreferredPlant;
                        formData.FirstLifting = cabRecord.FirstLifting;
                        formData.POCRRequest = cabRecord.POCRRequest;
                        formData.AddDisMaterial = cabRecord.AddDisMaterial;
                        formData.CreditReq = cabRecord.CreditReq;
                        formData.CreditPer = cabRecord.CreditPer;
                        formData.InterstCost = cabRecord.InterstCost;
                        formData.CreditLimit = cabRecord.CreditLimit;
                        formData.CustOverview = cabRecord.CustOverview;
                        formData.WhyProject = cabRecord.WhyProject;
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.POCRNo).ToList();
                List<POCRFormModel> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Cost Center Number";
                Sheet.Cells["H1"].Value = "Flight No";
                Sheet.Cells["I1"].Value = "Flight Time";
                Sheet.Cells["J1"].Value = "Comment";

                //Cab Booking User Details
                Sheet.Cells["K1"].Value = "Username";
                Sheet.Cells["L1"].Value = "User Contact Number";
                Sheet.Cells["M1"].Value = "Reporting Time";
                Sheet.Cells["N1"].Value = "Reporting Place With Address";
                Sheet.Cells["O1"].Value = "Destination";
                Sheet.Cells["P1"].Value = "Car Required From Date";
                Sheet.Cells["Q1"].Value = "Car Required To Date";
                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.FormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.POCRNo;

                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }


        public async Task<byte[]> GetBusTransportReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var busList = await obj.ViewBTFExcelData();

                var finalDataList = new List<FormData>();
                foreach (var formRow in formsList)
                {
                    var matchingRecords = busList.Where(x => x.FormID.Id == formRow.UniqueFormId);

                    foreach (var busRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();

                        formData.BusinessNeed = busRecord.BusinessNeed;
                        formData.Created_Date = busRecord.Created_Date;
                        formData.TransportationRequired = busRecord.TransportationRequired;
                        formData.Gender = busRecord.Gender;
                        formData.BusShift = busRecord.BusShift;
                        formData.BusRouteName = busRecord.BusRouteName;
                        formData.BusRouteNumber = busRecord.BusRouteNumber;
                        formData.PickupPoint = busRecord.PickupPoint;
                        formData.Distance = busRecord.Distance;
                        formData.Address = busRecord.Address;
                        formData.Region = busRecord.BusRouteName;
                        if (!string.IsNullOrEmpty(busRecord.Slab))
                        {
                            if (busRecord.Slab.Contains('-'))
                            {
                                string[] slabArry = busRecord.Slab.Split('-');
                                string slab = slabArry[0];
                                string slabAmount = slabArry[1];
                                formData.Slab = slab;
                                formData.SlabAmount = slabAmount;
                            }
                            else
                            {
                                formData.Slab = busRecord.Slab;
                            }
                        }

                        var requestSubmissionFor = busRecord.RequestSubmissionFor;
                        formData.RequestSubmissionFor = busRecord.RequestSubmissionFor;
                        var onBehalfOption = busRecord.OnBehalfOption;
                        formData.EmployeeType = busRecord.EmployeeType;

                        if (requestSubmissionFor == "Self")
                        {
                            formData.EmployeeContactNo = busRecord.EmployeeContactNo;
                            formData.EmployeeCode = busRecord.EmployeeCode;
                            formData.EmployeeName = busRecord.EmployeeName;
                        }
                        else if (requestSubmissionFor == "OnBehalf")
                        {
                            formData.OtherEmployeeCode = busRecord.OtherEmployeeCode;
                            //formData.OtherEmployeeContactNo = busRecord.OtherEmployeeContactNo;
                            formData.OtherEmployeeName = busRecord.OtherEmployeeName;
                            if (onBehalfOption == "SAVWIPLEmployee")
                            {
                                formData.ExternalOrganizationName = busRecord.ExternalOrganizationName;
                            }
                            else if (onBehalfOption == "Others")
                            {
                                formData.OtherExternalOrganizationName = busRecord.OtherExternalOrganizationName;
                            }
                        }

                        //var regionList = obj.GetRegionData();
                        //var regionMainList = regionList.Result;
                        //foreach (var item in regionMainList)
                        //{
                        //    if (!string.IsNullOrEmpty(formData.Address))
                        //    {
                        //        if (formData.Address.Contains(item.Region))
                        //        {
                        //            formData.Region = item.Region;
                        //            break;
                        //        }
                        //        else
                        //        {
                        //            formData.Region = "";
                        //        }
                        //    }
                        //    else
                        //    {
                        //        formData.Region = "";
                        //    }
                        //}


                        finalDataList.Add(formData);
                    }
                }
                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();

                //New Record added into new array list
                List<FormData> arrayData = newList;

                BusTransportationDAL objBus = new BusTransportationDAL();

                //Old Record
                var oldBusReport = new List<BTFData>();

                var resultAdmin = objBus.GetBusAdminApprover();

                if (resultAdmin != null)
                {
                    if (resultAdmin.Rows.Count == 1)
                    {
                        oldBusReport = await obj.ViewBTFOldExcelData();
                    }
                }

                //Employee Removes from Report
                var empNumberList = await obj.GetBusFromsOldEmployeeData();

                foreach (var empNum in empNumberList)
                {
                    var itemsToRemove = arrayData.Where(r => r.EmployeeCode == empNum.EmployeeCode || r.OtherEmployeeCode == empNum.EmployeeCode).ToList();
                    foreach (var item in itemsToRemove)
                    {
                        arrayData.Remove(item);
                    }

                    var itemToRemoveoldBusReport = oldBusReport.Where(r => r.OldEmployeeNumber == empNum.EmployeeCode.ToString()).ToList();
                    foreach (var item in itemToRemoveoldBusReport)
                    {
                        oldBusReport.Remove(item);
                    }
                }
                //Employee Removes from Report

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");
                Sheet.Cells["A1"].Value = "Company Name";
                Sheet.Cells["B1"].Value = "Internal/External";
                Sheet.Cells["C1"].Value = "Contact No";
                Sheet.Cells["D1"].Value = "Employee Number";
                Sheet.Cells["E1"].Value = "Form Id";
                Sheet.Cells["F1"].Value = "Form Name";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Details/Business Needs";
                Sheet.Cells["I1"].Value = "Status";
                Sheet.Cells["J1"].Value = "Recieved Date";
                Sheet.Cells["K1"].Value = "Admin Comment";
                Sheet.Cells["L1"].Value = "Transport Required";
                Sheet.Cells["M1"].Value = "Gender";
                Sheet.Cells["N1"].Value = "Shift";
                Sheet.Cells["O1"].Value = "Route Name";
                Sheet.Cells["P1"].Value = "Route Number";
                Sheet.Cells["Q1"].Value = "Pick Up Point";
                Sheet.Cells["R1"].Value = "Distance from residence to pickup point";
                Sheet.Cells["S1"].Value = "Address";
                Sheet.Cells["T1"].Value = "Slab";
                Sheet.Cells["U1"].Value = "Slab Amount";
                Sheet.Cells["V1"].Value = "Region";
                Sheet.Cells["W1"].Value = "Location";
                int row = 2;

                foreach (var item in arrayData)
                {
                    if (item.RequestSubmissionFor == "Self")
                    {
                        Sheet.Cells[string.Format("A{0}", row)].Value = "";
                    }
                    if (item.RequestSubmissionFor == "OnBehalf")
                    {
                        if (item.OnBehalfOption == "SAVWIPLEmployee")
                        {
                            Sheet.Cells[string.Format("A{0}", row)].Value = item.ExternalOrganizationName;
                        }
                        else if (item.OnBehalfOption == "Others")
                        {
                            Sheet.Cells[string.Format("A{0}", row)].Value = item.OtherExternalOrganizationName;
                        }
                    }

                    Sheet.Cells[string.Format("B{0}", row)].Value = item.EmployeeType;
                    if (item.RequestSubmissionFor == "Self")
                    {
                        Sheet.Cells[string.Format("C{0}", row)].Value = item.EmployeeContactNo;
                        Sheet.Cells[string.Format("D{0}", row)].Value = item.EmployeeCode;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("C{0}", row)].Value = item.OtherEmployeeContactNo;
                        Sheet.Cells[string.Format("D{0}", row)].Value = item.OtherEmployeeCode;
                    }


                    Sheet.Cells[string.Format("E{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormName;
                    if (item.RequestSubmissionFor == "Self")
                    {
                        Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("G{0}", row)].Value = item.OtherEmployeeName;
                    }
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("K{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("K{0}", row)].Value = "";
                    }


                    Sheet.Cells[string.Format("L{0}", row)].Value = item.TransportationRequired;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Gender;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.BusShift;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.BusRouteName;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.BusRouteNumber;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.PickupPoint;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.Distance;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.Address;
                    Sheet.Cells[string.Format("T{0}", row)].Value = item.Slab;
                    Sheet.Cells[string.Format("U{0}", row)].Value = item.SlabAmount;
                    Sheet.Cells[string.Format("V{0}", row)].Value = item.Region;
                    row++;
                }


                //Old Record
                foreach (var item in oldBusReport)
                {
                    Sheet.Cells[string.Format("A{0}", row)].Value = item.CompanyName;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.EmployeeType;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.OldEmployeeContactNo;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.OldEmployeeNumber;
                    Sheet.Cells[string.Format("E{0}", row)].Value = "";
                    Sheet.Cells[string.Format("F{0}", row)].Value = "Bus Transportation Form";
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("I{0}", row)].Value = "";
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.Created_Date.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("K{0}", row)].Value = "Old Data";
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.TransportationRequired;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Gender;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.BusShift;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.BusRouteName;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.BusRouteNumber;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.PickupPoint;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.Distance;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.Address;
                    Sheet.Cells[string.Format("T{0}", row)].Value = item.Slab;
                    Sheet.Cells[string.Format("U{0}", row)].Value = item.Amount;
                    Sheet.Cells[string.Format("V{0}", row)].Value = "";
                    Sheet.Cells[string.Format("W{0}", row)].Value = item.BusLocationName;
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetMaterialRequestReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var materialList = await obj.MaterialRequestExcelData();

                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = materialList.Where(x => x.FormID.Id == formRow.UniqueFormId);

                    foreach (var materialRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (materialRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.CostCenterNumber = materialRecord.OtherEmployeeCCCode.ToString();
                        }
                        else
                        {
                            formData.CostCenterNumber = materialRecord.EmployeeCCCode.ToString();
                        }

                        formData.RequestNumber = materialRecord.RequestNumber;
                        formData.RequestTo = materialRecord.RequestTo;
                        formData.RequestFrom = materialRecord.RequestFrom;

                        //Material Details
                        formData.PartNumber = materialRecord.PartNumber;
                        formData.PartDescription = materialRecord.PartDescription;
                        formData.Quantity = materialRecord.Quantity;
                        formData.Remarks = materialRecord.Remarks;
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Request Number";
                Sheet.Cells["G1"].Value = "Request To";
                Sheet.Cells["H1"].Value = "Request From";
                //Material Details
                Sheet.Cells["I1"].Value = "Issue Date";
                Sheet.Cells["J1"].Value = "Part No.";
                Sheet.Cells["K1"].Value = "Part Description";
                Sheet.Cells["L1"].Value = "VIN No.";
                Sheet.Cells["M1"].Value = "Qty";

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.RequestNumber;
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.RequestTo;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.RequestFrom;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.RecievedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.PartNumber;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.PartNumber;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.PartDescription;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.Remarks;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Quantity;

                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetGiftInvitationReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var giftlist = await obj.ViewGAIFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = giftlist.Where(x => x.FormIDGift.Id == formRow.UniqueFormId);

                    foreach (var giftRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (giftRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.CostCenterNumber = Convert.ToString(giftRecord.OtherEmployeeCCCode);
                        }
                        else
                        {
                            formData.CostCenterNumber = Convert.ToString(giftRecord.EmployeeCCCode);
                        }

                        formData.RequestType = giftRecord.RequestType;
                        formData.Transaction = giftRecord.Transaction;
                        formData.IsGiftOrInviteToPublicOfficial = giftRecord.IsGiftOrInviteToPublicOfficial;
                        formData.NameRelationOtherDet = giftRecord.NameRelationOtherDet;
                        formData.FrequencyOfGiftsOrInvitationfrm = giftRecord.FrequencyOfGiftsOrInvitationfrm;
                        formData.ApproxValueOfGiftsInvt = giftRecord.ApproxValueOfGiftsInvt;
                        formData.ReasonForGiftingInvitation = giftRecord.ReasonForGiftingInvitation;
                        formData.GiftIsAcceptedRefused = giftRecord.GiftIsAcceptedRefused;
                        formData.ReasonGiftIsAcceptedRefused = giftRecord.ReasonGiftIsAcceptedRefused;
                        formData.GiftTobeDepoWithGRC = giftRecord.GiftTobeDepoWithGRC;
                        formData.Answers = giftRecord.Answers;

                        //Question Details
                        formData.Question = giftRecord.Question;

                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Cost Center Number";
                Sheet.Cells["H1"].Value = "Request Type";
                Sheet.Cells["I1"].Value = "Transaction";
                Sheet.Cells["J1"].Value = "Is Gift given to public official";
                Sheet.Cells["K1"].Value = "Name of Business partner/ Other individual providing the gift";
                Sheet.Cells["L1"].Value = "Frequency of receipt of gifts from the same business partner in a year?";
                Sheet.Cells["M1"].Value = "Approximate value of gifts (How is the value estimated) ?";
                Sheet.Cells["N1"].Value = "Reason for gifting ?";
                Sheet.Cells["O1"].Value = "Invitation is accepted/ Refused ?";
                Sheet.Cells["P1"].Value = "Reason of acceptance/ refusal of invitation";
                Sheet.Cells["Q1"].Value = "Gift To be Deposite With GRC";
                Sheet.Cells["R1"].Value = "Question";
                Sheet.Cells["S1"].Value = "Answers";
                Sheet.Cells["T1"].Value = "Comment";

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.RequestType;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.Transaction;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.IsGiftOrInviteToPublicOfficial;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.NameRelationOtherDet;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.FrequencyOfGiftsOrInvitationfrm;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.ApproxValueOfGiftsInvt;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.ReasonForGiftingInvitation;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.GiftIsAcceptedRefused;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.ReasonGiftIsAcceptedRefused;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.GiftTobeDepoWithGRC;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.Question;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.Answers;

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("T{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("T{0}", row)].Value = "";
                    }
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetDLICExcelReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var DLICList = await obj.GetDLICListData();
                var finalDataList = new List<DLICReportModel>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = DLICList.Where(x => x.Form_ID.Id == formRow.UniqueFormId);

                    foreach (var item in matchingRecords)
                    {
                        var formData = new DLICReportModel();
                        var json = JsonConvert.SerializeObject(formRow);
                        var Parallelsettings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        formData = JsonConvert.DeserializeObject<DLICReportModel>(json, Parallelsettings);
                        formData.TicketNum = item.TicketNum;
                        formData.T_EmployeeName = item.T_EmployeeName;
                        formData.T_EmployeeCode = item.T_EmployeeCode;
                        formData.T_UserId = item.T_UserId;
                        formData.T_CostCenter = item.T_CostCenter;
                        formData.Make = item.Make;
                        formData.Modal = item.Modal;
                        formData.SerialNumber = item.SerialNumber;
                        formData.HostName = item.HostName;
                        formData.IsIDoCompleted = item.IsIDoCompleted;
                        formData.IsBitLockerCompleted = item.IsBitLockerCompleted;
                        formData.IsAntivirusUpdated = item.IsAntivirusUpdated;
                        formData.IsProxyConfig = item.IsProxyConfig;
                        formData.IsUSBBluetoothDisabled = item.IsUSBBluetoothDisabled;
                        formData.IsUserIdConfigured = item.IsUserIdConfigured;
                        formData.IsOutLookConfiguration = item.IsOutLookConfiguration;
                        formData.IsFirEyeAgent = item.IsFirEyeAgent;
                        formData.IsEncryptedEmailConfiguration = item.IsEncryptedEmailConfiguration;
                        formData.IsPKIDigitSignCert = item.IsPKIDigitSignCert;
                        formData.IsPrinterConfiguration = item.IsPrinterConfiguration;
                        formData.IsVPNConfigurationDone = item.IsVPNConfigurationDone;
                        formData.IsSharedFolderAccessDone = item.IsSharedFolderAccessDone;
                        formData.IsDataRestored = item.IsDataRestored;
                        formData.IsNessusAgent = item.IsNessusAgent;
                        formData.IsClassificationAddInForOffice = item.IsClassificationAddInForOffice;
                        formData.IsUsedMachineToBeClean = item.IsUsedMachineToBeClean;
                        formData.IsOneDriveConfiguration = item.IsOneDriveConfiguration;
                        formData.IsLocalApps = item.IsLocalApps;
                        formData.IsOthers = item.IsOthers;
                        formData.OthersText = item.OthersText;
                        formData.EmployeeName = item.RequestSubmissionFor == "OnBehalf" ? item.OtherEmployeeName : item.EmployeeName;
                        //formData.EmployeeContactNo = item.RequestSubmissionFor == "OnBehalf" ? item.OtherEmployeeContactNo : item.EmployeeContactNo;
                        formData.UserName = item.RequestSubmissionFor == "OnBehalf" && !string.IsNullOrEmpty(item.OtherEmployeeUserId) ? item.OtherEmployeeUserId : item.EmployeeUserId;
                        finalDataList.Add(formData);
                    }
                }

                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                var arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Comment";

                Sheet.Cells["H1"].Value = "Ticket Number";
                Sheet.Cells["I1"].Value = "Employee Name";
                Sheet.Cells["J1"].Value = "Employee Number";
                Sheet.Cells["K1"].Value = "User ID";
                Sheet.Cells["L1"].Value = "Cost Center";
                Sheet.Cells["M1"].Value = "Make";
                Sheet.Cells["N1"].Value = "Model";
                Sheet.Cells["O1"].Value = "Serial Number";
                Sheet.Cells["P1"].Value = "Host Name";
                Sheet.Cells["Q1"].Value = "i.Do completed";
                Sheet.Cells["R1"].Value = "Bitlocker";
                Sheet.Cells["S1"].Value = "Antivirus Updates Checked";
                Sheet.Cells["T1"].Value = "Zscaler/Proxy Configuration";
                Sheet.Cells["U1"].Value = "USB/Bluetooth Disabled";
                Sheet.Cells["V1"].Value = "User ID Configured";
                Sheet.Cells["W1"].Value = "Outlook Configuration";
                Sheet.Cells["X1"].Value = "FireEye Agent";
                Sheet.Cells["Y1"].Value = "Encrypted email Configuraion";
                Sheet.Cells["Z1"].Value = "PKI Card &  Digital Signature Setting";
                Sheet.Cells["AA1"].Value = "Printer Configuration";
                Sheet.Cells["AB1"].Value = "VPN Configuration Done (Laptop Only)";
                Sheet.Cells["AC1"].Value = "Shared Folder Access Done";
                Sheet.Cells["AD1"].Value = "Data Restored (for Replacement / re-i.do)";
                Sheet.Cells["AE1"].Value = "Nessus Agent";
                Sheet.Cells["AF1"].Value = "Classification add-in for Office/ Azure Info Protect";
                Sheet.Cells["AG1"].Value = "Used machine to be cleaned";
                Sheet.Cells["AH1"].Value = "One Drive Configuration";
                Sheet.Cells["AI1"].Value = "Local Apps (SmartSign)";
                Sheet.Cells["AJ1"].Value = "Is Others";
                Sheet.Cells["AK1"].Value = "Others";
                Sheet.Cells["AL1"].Value = "Form Submitter Employee Name";
                Sheet.Cells["AM1"].Value = "Form Submitter Employee Contact Number";
                Sheet.Cells["AN1"].Value = "Form Submitter User ID";
                int row = 2;

                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");

                    if (item.Status.ToLower() == "rejected" || item.Status.ToLower() == "enquired")
                    {
                        Sheet.Cells[string.Format("G{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("G{0}", row)].Value = "";
                    }
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.TicketNum;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.T_EmployeeName;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.T_EmployeeCode;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.T_UserId;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.T_CostCenter;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Make;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.Modal;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.SerialNumber;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.HostName;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.IsIDoCompleted ? "Yes" : "No";
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.IsBitLockerCompleted ? "Yes" : "No";
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.IsAntivirusUpdated ? "Yes" : "No";
                    Sheet.Cells[string.Format("T{0}", row)].Value = item.IsProxyConfig ? "Yes" : "No";
                    Sheet.Cells[string.Format("U{0}", row)].Value = item.IsUSBBluetoothDisabled ? "Yes" : "No";
                    Sheet.Cells[string.Format("V{0}", row)].Value = item.IsUserIdConfigured ? "Yes" : "No";
                    Sheet.Cells[string.Format("W{0}", row)].Value = item.IsOutLookConfiguration ? "Yes" : "No";
                    Sheet.Cells[string.Format("X{0}", row)].Value = item.IsFirEyeAgent ? "Yes" : "No";
                    Sheet.Cells[string.Format("Y{0}", row)].Value = item.IsEncryptedEmailConfiguration ? "Yes" : "No";
                    Sheet.Cells[string.Format("Z{0}", row)].Value = item.IsPKIDigitSignCert ? "Yes" : "No";
                    Sheet.Cells[string.Format("AA{0}", row)].Value = item.IsPrinterConfiguration ? "Yes" : "No";
                    Sheet.Cells[string.Format("AB{0}", row)].Value = item.IsVPNConfigurationDone ? "Yes" : "No";
                    Sheet.Cells[string.Format("AC{0}", row)].Value = item.IsSharedFolderAccessDone ? "Yes" : "No";
                    Sheet.Cells[string.Format("AD{0}", row)].Value = item.IsDataRestored ? "Yes" : "No";
                    Sheet.Cells[string.Format("AE{0}", row)].Value = item.IsNessusAgent ? "Yes" : "No";
                    Sheet.Cells[string.Format("AF{0}", row)].Value = item.IsClassificationAddInForOffice ? "Yes" : "No";
                    Sheet.Cells[string.Format("AG{0}", row)].Value = item.IsUsedMachineToBeClean ? "Yes" : "No";
                    Sheet.Cells[string.Format("AH{0}", row)].Value = item.IsOneDriveConfiguration ? "Yes" : "No";
                    Sheet.Cells[string.Format("AI{0}", row)].Value = item.IsLocalApps ? "Yes" : "No";
                    Sheet.Cells[string.Format("AJ{0}", row)].Value = item.IsOthers ? "Yes" : "No";
                    Sheet.Cells[string.Format("AK{0}", row)].Value = item.IsOthers ? item.OthersText : "";
                    Sheet.Cells[string.Format("AL{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("AM{0}", row)].Value = item.EmployeeContactNo;
                    Sheet.Cells[string.Format("AN{0}", row)].Value = item.UserName;
                    row++;
                }
                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return reportData;
        }

        public async Task<byte[]> GetNGCFReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var giftlist = await obj.ViewNGCFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = giftlist.Where(x => x.FormIDNGCF.Id == formRow.UniqueFormId);

                    foreach (var ngcfRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (ngcfRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.OtherEmployeeCCCode);
                        }
                        else
                        {
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.EmployeeCCCode);
                        }

                        formData.RequestType = ngcfRecord.RequestType;
                        formData.NameOfGLToOpen = ngcfRecord.NameOfGLToOpen;
                        formData.NatureOfTranInGL = ngcfRecord.NatureOfTranInGL;
                        formData.Purpose = ngcfRecord.Purpose;
                        formData.DateToOpenNewGL = ngcfRecord.DateToOpenNewGL;
                        formData.GLCode = ngcfRecord.GLCode;
                        formData.GLName = ngcfRecord.GLName;
                        formData.GLSeries = ngcfRecord.GLSeries;
                        formData.NewGLNo = ngcfRecord.NewGLNo;
                        formData.CommitmentItem = ngcfRecord.CommitmentItem;
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Cost Center Number";
                Sheet.Cells["H1"].Value = "Request Type";
                Sheet.Cells["I1"].Value = "Name Of GL To Open";
                Sheet.Cells["J1"].Value = "Nature of transaction to be captured in GL";
                Sheet.Cells["K1"].Value = "Purpose";
                Sheet.Cells["L1"].Value = "Date To Open New GL";
                Sheet.Cells["M1"].Value = "GL Code";
                Sheet.Cells["N1"].Value = "GL Name";
                Sheet.Cells["O1"].Value = "GL Series";
                Sheet.Cells["P1"].Value = "Comment";
                Sheet.Cells["Q1"].Value = "New GL No";
                Sheet.Cells["R1"].Value = "Commitment Item";

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.RequestType;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.NameOfGLToOpen;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.NatureOfTranInGL;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.Purpose;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.DateToOpenNewGL.ToString("dd-MM-yyyy"); ;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.GLCode;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.GLName;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.GLSeries;

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("P{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("P{0}", row)].Value = "";
                    }
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.NewGLNo;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.CommitmentItem;
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetURCFReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var giftlist = await obj.ViewURCFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = giftlist.Where(x => x.FormIDURCF.Id == formRow.UniqueFormId);

                    foreach (var ngcfRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (ngcfRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.OtherEmployeeCCCode);
                        }
                        else
                        {
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.EmployeeCCCode);
                        }

                        formData.Brand = ngcfRecord.Brand;
                        formData.ServiceType = ngcfRecord.ServiceType;
                        formData.TypeofRequest = ngcfRecord.TypeofRequest;

                        formData.ServiceCategory = ngcfRecord.ServiceCategory;
                        formData.ServiceSubCategory = ngcfRecord.ServiceSubCategory;
                        formData.Role = ngcfRecord.Role;
                        formData.AccessType = ngcfRecord.AccessType;
                        formData.BrandApp = ngcfRecord.BrandApp;
                        formData.ApplicationUserID = ngcfRecord.ApplicationUserID;
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Cost Center Number";
                Sheet.Cells["H1"].Value = "Brand";
                Sheet.Cells["I1"].Value = "Service Type";
                Sheet.Cells["J1"].Value = "Type of Request";
                Sheet.Cells["K1"].Value = "Service Category";
                Sheet.Cells["L1"].Value = "Service  Sub Category";
                Sheet.Cells["M1"].Value = "Role";
                Sheet.Cells["N1"].Value = "Access Type";
                Sheet.Cells["O1"].Value = "Brand";
                Sheet.Cells["P1"].Value = "Application User ID";
                Sheet.Cells["Q1"].Value = "Comment";

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.Brand;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.ServiceType;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.TypeofRequest;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.ServiceCategory;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.ServiceSubCategory;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Role;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.AccessType;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.BrandApp;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.ApplicationUserID;
                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("Q{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("Q{0}", row)].Value = "";
                    }
                    
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        #region Admin Section
        /// <summary>
        /// Dashboard-It is used to get the Admin Section Page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetAdmin(string formName, string Created, string status)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "AdminMaster1View" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle(formName, Created, status);
            var result = await listDAL.GetAllFormsListForAdmin(formName, Created, status);
            model.Data.Forms = result;
            model.Data.StatusCount = listDAL.GetStatusCount();
            return View("~/Views/Pages/Admin.cshtml", model);
        }

        /// <summary>
        /// Dashboard-It is used to filter Admin Section Page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetMyAdminFilter(string formName, string Created, string status, string location)
        {
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetAllFormsListForReport(formName, Created, status, location);
            var model = new DashboardModel();
            model.Data = new DataModel();
            model.Data.Forms = result ?? new List<FormData>();
            return PartialView("AdminMaster1View", model);
        }

        #endregion

        #region Newly Added & Freq. Used Forms Section

        /// <summary>
        /// Dashboard-It is used to get the Newly Added Form Dashboard via Side Menu clicke.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetNewlyAddedForms(string dashboardType)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "NewlyAddedAndFreqForms" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle("", dashboardType, "", "");
            var result = await listDAL.GetNewlyAddedForms("");
            model.Data.Forms = result;
            return View("~/Views/Pages/FormDashboard.cshtml", model);
        }

        /// <summary>
        /// Dashboard-It is used to get the Freq. Used Form Dashboard via Side Menu clicke.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetFreqAddedForms(string dashboardType)
        {
            ListDAL listDAL = new ListDAL();
            var model = new DashboardModel() { Data = new DataModel() { PartialViewName = "NewlyAddedAndFreqForms" } };

            model.Data.BreadCrumbs = listDAL.GetBreadCrumbTitle("", dashboardType, "", "");
            var result = await listDAL.GetFreqAddedForms("");
            model.Data.Forms = result;
            return View("~/Views/Pages/FormDashboard.cshtml", model);
        }
        #endregion

        public async Task<PartialViewResult> GetMyReqtab()
        {
            ListDAL listDAL = new ListDAL();
            var request = await listDAL.GetAllFormsList("", "", "");
            var data = await listDAL.GetForms();
            var model = new DashboardModel();
            model.Data = data;
            //model.Data.StatusCount = listDAL.GetStatusCount();
            model.Data.FormsRequest = request;
            return PartialView("_MyRequest", model);
        }

        public async Task<ActionResult> GetDashBoardDept()
        {
            ListDAL objDAL = new ListDAL();
            var result = await objDAL.GetDashBoardDept();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<byte[]> GetIDCardExcelReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var idCardList = await obj.GetIDFormListData();
                var finalDataList = new List<IDCardReportModel>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = idCardList.Where(x => x.FormID.Id == formRow.UniqueFormId);

                    foreach (var item in matchingRecords)
                    {
                        var formData = new IDCardReportModel();
                        var json = JsonConvert.SerializeObject(formRow);
                        var Parallelsettings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        formData = JsonConvert.DeserializeObject<IDCardReportModel>(json, Parallelsettings);
                        formData.TypeOfCard = item.TypeOfCard;
                        formData.DateofJoining = item.DateofJoining;
                        formData.DateofIssue = item.DateofIssue;
                        formData.EmployeeName = item.RequestSubmissionFor == "OnBehalf" ? item.OtherEmployeeName : item.EmployeeName;
                        formData.EmployeeCode = item.RequestSubmissionFor == "OnBehalf" ? item.OtherEmployeeCode : item.EmployeeCode;
                        formData.UserName = item.RequestSubmissionFor == "OnBehalf" && !string.IsNullOrEmpty(item.OtherEmployeeUserId) ? item.OtherEmployeeUserId : item.EmployeeUserId;
                        formData.Company = item.RequestSubmissionFor == "OnBehalf" && item.OnBehalfOption?.ToLower() == "savwiplemployee"
                                ? item.ExternalOrganizationName
                                : (item.RequestSubmissionFor == "OnBehalf" ? item.OtherExternalOrganizationName : item.Company);
                        formData.Company = !string.IsNullOrEmpty(formData.Company) ? formData.Company : "Skoda";//Default is skoda in 
                        formData.Chargable = item.Chargable;
                        finalDataList.Add(formData);
                    }
                }

                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<IDCardReportModel> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Comment";

                Sheet.Cells["H1"].Value = "Type Of Card";
                Sheet.Cells["I1"].Value = "Date Of Joining";
                Sheet.Cells["J1"].Value = "Issue Date";
                Sheet.Cells["K1"].Value = "Employee Name";
                Sheet.Cells["L1"].Value = "Employee Number";
                Sheet.Cells["M1"].Value = "User ID";
                Sheet.Cells["N1"].Value = "Company Name";
                Sheet.Cells["O1"].Value = "Chargable";
                int row = 2;

                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("G{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("G{0}", row)].Value = "";
                    }

                    Sheet.Cells[string.Format("H{0}", row)].Value = item.TypeOfCard;
                    if (Convert.ToString(item.DateofJoining.Date) == "01-01-0001 00:00:00")
                    {
                        Sheet.Cells[string.Format("I{0}", row)].Value = "";
                    }
                    else
                    {
                        Sheet.Cells[string.Format("I{0}", row)].Value = item.DateofJoining.ToString("dd-MM-yyyy");
                    }
                    if (Convert.ToString(item.DateofIssue.Date) == "01-01-0001 00:00:00")
                    {
                        Sheet.Cells[string.Format("J{0}", row)].Value = "";
                    }
                    else
                    {
                        Sheet.Cells[string.Format("J{0}", row)].Value = item.DateofIssue.ToString("dd-MM-yyyy");
                    }
                    //Sheet.Cells[string.Format("I{0}", row)].Value = item.DateofJoining.ToString("dd-MM-yyyy");
                    //Sheet.Cells[string.Format("J{0}", row)].Value = item.DateofIssue.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.EmployeeCode;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.UserName;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.Company;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.Chargable;
                    row++;
                }
                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return reportData;
        }

        public async Task<byte[]> GetQMCRReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var giftlist = await obj.ViewQMCRExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = giftlist.Where(x => x.FormIDQMCR.Id == formRow.UniqueFormId);

                    foreach (var ngcfRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (ngcfRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            //if(ngcfRecord.EmployeeType == "SAVWIPLEmployee")
                            //{

                            //}
                            formData.EmployeeName = Convert.ToString(ngcfRecord.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(ngcfRecord.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(ngcfRecord.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(ngcfRecord.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(ngcfRecord.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(ngcfRecord.EmployeeDepartment);
                        }

                        formData.FormType = ngcfRecord.FormType;
                        formData.ModelQCM = ngcfRecord.ModelQCM;
                        formData.Series = ngcfRecord.Series;
                        formData.PartName = ngcfRecord.PartName;
                        formData.PartQuantity = ngcfRecord.PartQuantity;
                        formData.ProblemReported = ngcfRecord.ProblemReported;
                        formData.OtherDetails = ngcfRecord.OtherDetails;
                        
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Cost Center Number";
                Sheet.Cells["I1"].Value = "Employee Designation";
                Sheet.Cells["J1"].Value = "Employee Department";
                Sheet.Cells["K1"].Value = "Form Type";
                Sheet.Cells["L1"].Value = "Model";
                Sheet.Cells["M1"].Value = "Series";
                Sheet.Cells["N1"].Value = "Part Name";
                Sheet.Cells["O1"].Value = "Part Quantity";
                Sheet.Cells["P1"].Value = "Problem Reported";
                Sheet.Cells["Q1"].Value = "Other Details";
                Sheet.Cells["R1"].Value = "Comment";
                

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.FormType;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.ModelQCM;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Series;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.PartName;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.PartQuantity;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.ProblemReported;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.OtherDetails;

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("R{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("R{0}", row)].Value = "";
                    }
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetMMRFReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var giftlist = await obj.ViewMMRFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = giftlist.Where(x => x.FormIDMMRF.Id == formRow.UniqueFormId);

                    foreach (var ngcfRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (ngcfRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            //if(ngcfRecord.EmployeeType == "SAVWIPLEmployee")
                            //{

                            //}
                            formData.EmployeeName = Convert.ToString(ngcfRecord.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(ngcfRecord.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(ngcfRecord.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(ngcfRecord.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(ngcfRecord.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(ngcfRecord.EmployeeDepartment);
                        }

                        formData.ExistingDepartment = ngcfRecord.ExistingDepartment;
                        formData.NewDepartment = ngcfRecord.NewDepartment;
                        formData.FutureOwner = ngcfRecord.FutureOwner;
                        formData.MMRIdentification = ngcfRecord.MMRIdentification;
                        formData.MMRDescription = ngcfRecord.MMRDescription;
                        formData.HandoverDate = ngcfRecord.HandoverDate;
                        formData.MMREpus = ngcfRecord.MMREpus;
                        formData.TransferType = ngcfRecord.TransferType;
                        formData.Details = ngcfRecord.Details;

                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Cost Center Number";
                Sheet.Cells["I1"].Value = "Employee Designation";
                Sheet.Cells["J1"].Value = "Employee Department";
                Sheet.Cells["K1"].Value = "Existing Department";
                Sheet.Cells["L1"].Value = "New Department";
                Sheet.Cells["M1"].Value = "Future Owner";
                Sheet.Cells["N1"].Value = "MMR Identification";
                Sheet.Cells["O1"].Value = "MMR Description";
                Sheet.Cells["P1"].Value = "Handover Date";
                Sheet.Cells["Q1"].Value = "MMR Removed by existing user from ERUS";
                Sheet.Cells["R1"].Value = "Transfer Type";
                Sheet.Cells["S1"].Value = "Other Details";
                Sheet.Cells["T1"].Value = "Comment";


                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.ExistingDepartment;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.NewDepartment;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.FutureOwner;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.MMRIdentification;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.MMRDescription;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.HandoverDate?.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.MMREpus.ToString("yyyy-MM-ddThh:mm:ss");
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.TransferType;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.OtherDetails;

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("T{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("T{0}", row)].Value = "";
                    }
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }


        public async Task<byte[]> GetAPFPReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var apfplist = await obj.ViewAPFPExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);


                    foreach (var item in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (item.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.EmployeeName = Convert.ToString(item.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(item.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.EmployeeDepartment);
                        }
                     
                        formData.WeekNo = item.WeekNo;
                        formData.Topic = item.Topic;
                        formData.Department = item.Department;

                        formData.SrNo = item.SrNo;
                        formData.Project = item.Project;
                        formData.PartName = item.Parts;
                        formData.Quantity = item.Quantity;
                        formData.Reason = item.Reason;
                        formData.PartDescription = item.DetailDescription;
                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Cost Center Number";
                Sheet.Cells["I1"].Value = "Employee Designation";
                Sheet.Cells["J1"].Value = "Employee Department";
                Sheet.Cells["K1"].Value = "Week No";
                Sheet.Cells["L1"].Value = "Topic";
                Sheet.Cells["M1"].Value = "Department";
                Sheet.Cells["N1"].Value = "Sr No";
                Sheet.Cells["O1"].Value = "Project";
                Sheet.Cells["P1"].Value = "Parts";
                Sheet.Cells["Q1"].Value = "Quantity";
                Sheet.Cells["R1"].Value = "Reason";
                Sheet.Cells["S1"].Value = "Detail Description";

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = "APFP" + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.WeekNo;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.Topic;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.Department;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.SrNo;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.Project;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.PartName;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.Quantity;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.Reason;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.PartDescription;
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetIMACReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var apfplist = await obj.ViewIMACExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    //var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);
                    var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormId) == formRow.UniqueFormId);


                    foreach (var item in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (item.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.EmployeeName = Convert.ToString(item.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(item.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.EmployeeDepartment);
                        }

                        formData.IMACtype = item.IMACtype;
                        formData.AssetName = item.AssetName;
                        formData.SubCategory = item.SubAssetName;

                        formData.Make = item.Make;
                        formData.Modal = item.Modal;
                        formData.AssetType = item.AssetType;
                        formData.SerialNumber = item.SerialNo;
                        formData.HostName = item.HostName;
                        formData.Location = item.Location;
                        formData.Acknowledgement = item.Acknowledgement;
                        formData.AssignType = item.AssignType;
                        formData.FromDate = item.FromDate;
                        formData.ToDate = item.ToDate;
                        formData.BusinessJustification = item.BusinessJustification;
                        formData.SrNo = item.SrNo;

                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Sr No";
                Sheet.Cells["B1"].Value = "Form Id";
                Sheet.Cells["C1"].Value = "Form Name";
                Sheet.Cells["D1"].Value = "Request From";
                Sheet.Cells["E1"].Value = "Details/Business Needs";
                Sheet.Cells["F1"].Value = "Status";
                Sheet.Cells["G1"].Value = "Recieved Date";
                Sheet.Cells["H1"].Value = "Employee Name";
                Sheet.Cells["I1"].Value = "Cost Center Number";
                Sheet.Cells["J1"].Value = "Employee Designation";
                Sheet.Cells["K1"].Value = "Employee Department";
                Sheet.Cells["L1"].Value = "IMAC Type";
                Sheet.Cells["M1"].Value = "Asset Name";
                Sheet.Cells["N1"].Value = "Sub Category";
                Sheet.Cells["O1"].Value = "Make";
                Sheet.Cells["P1"].Value = "Model";
                Sheet.Cells["Q1"].Value = "Asset Type";
                Sheet.Cells["R1"].Value = "Serial Number";
                Sheet.Cells["S1"].Value = "Acknowledgement";
                Sheet.Cells["T1"].Value = "Type";
                Sheet.Cells["U1"].Value = "Host Name";
                Sheet.Cells["V1"].Value = "Location";
                Sheet.Cells["W1"].Value = "Business Justification";
                Sheet.Cells["X1"].Value = "From Date";
                Sheet.Cells["Y1"].Value = "To Date";

                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.SrNo;
                    Sheet.Cells[string.Format("B{0}", row)].Value = "IMAC" + item.UniqueFormId;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.IMACtype;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.AssetName;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.SubCategory;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.Make;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.Modal;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.AssetType;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.SerialNumber;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.Acknowledgement;
                    Sheet.Cells[string.Format("T{0}", row)].Value = item.AssignType;
                    Sheet.Cells[string.Format("U{0}", row)].Value = item.HostName;
                    Sheet.Cells[string.Format("V{0}", row)].Value = item.Location;
                    Sheet.Cells[string.Format("W{0}", row)].Value = item.BusinessJustification;
                    Sheet.Cells[string.Format("X{0}", row)].Value = item.FromDate?.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("Y{0}", row)].Value = item.ToDate?.ToString("dd-MM-yyyy");
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetQFRFReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var giftlist = await obj.ViewQFRFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    var matchingRecords = giftlist.Where(x => x.FormIDQFRF.Id == formRow.UniqueFormId);

                    foreach (var ngcfRecord in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (ngcfRecord.RequestSubmissionFor == "OnBehalf")
                        {
                            //if(ngcfRecord.EmployeeType == "SAVWIPLEmployee")
                            //{

                            //}
                            formData.EmployeeName = Convert.ToString(ngcfRecord.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(ngcfRecord.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(ngcfRecord.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(ngcfRecord.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(ngcfRecord.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(ngcfRecord.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(ngcfRecord.EmployeeDepartment);
                        }

                        formData.FixtureName = ngcfRecord.FixtureName;
                        formData.FixtureNo = ngcfRecord.FixtureNo;
                        formData.ProjectName = ngcfRecord.ProjectName;
                        formData.FromDate = ngcfRecord.FromDate;
                        formData.ToDate = ngcfRecord.ToDate;
                        formData.Reason = ngcfRecord.Reason;
                        formData.RpsPin = ngcfRecord.RpsPin;
                        formData.RpsPinRemark = ngcfRecord.RpsPinRemark;
                        formData.Clamps = ngcfRecord.Clamps;
                        formData.ClampsRemark = ngcfRecord.ClampsRemark;
                        formData.Wheels = ngcfRecord.Wheels;
                        formData.WheelsRemark = ngcfRecord.WheelsRemark;
                        formData.RpsStick = ngcfRecord.RpsStick;
                        formData.RpsStickRemark = ngcfRecord.RpsStickRemark;
                        formData.LoseElement = ngcfRecord.LoseElement;
                        formData.LoseRemark = ngcfRecord.LoseRemark;
                        formData.Mylers = ngcfRecord.Mylers;
                        formData.MylerRemark = ngcfRecord.MylerRemark;
                        formData.PinThreads = ngcfRecord.PinThreads;
                        formData.PinRemark = ngcfRecord.PinRemark;
                        formData.RestingPads = ngcfRecord.RestingPads;
                        formData.PadsRemark = ngcfRecord.PadsRemark;
                        formData.Sliders = ngcfRecord.Sliders;
                        formData.SlidersRemark = ngcfRecord.SlidersRemark;
                        formData.Kugel = ngcfRecord.Kugel;
                        formData.KugelRemark = ngcfRecord.KugelRemark;

                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Cost Center Number";
                Sheet.Cells["I1"].Value = "Employee Designation";
                Sheet.Cells["J1"].Value = "Employee Department";
                Sheet.Cells["K1"].Value = "FixtureName";
                Sheet.Cells["L1"].Value = "FixtureNo";
                Sheet.Cells["M1"].Value = "ProjectName";
                Sheet.Cells["N1"].Value = "FromDate";
                Sheet.Cells["O1"].Value = "ToDate";
                Sheet.Cells["P1"].Value = "Reason";
                Sheet.Cells["Q1"].Value = "RpsPin Status";
                Sheet.Cells["R1"].Value = "RpsPin Remark";
                Sheet.Cells["S1"].Value = "Clamps Status";
                Sheet.Cells["T1"].Value = "Clamps Remark";
                Sheet.Cells["U1"].Value = "Wheels Status";
                Sheet.Cells["V1"].Value = "Wheels Remark";
                Sheet.Cells["W1"].Value = "RpsStick Status";
                Sheet.Cells["X1"].Value = "RpsStick Remark";
                Sheet.Cells["Y1"].Value = "LoseElement Status";
                Sheet.Cells["Z1"].Value = "LoseElement Remark";
                Sheet.Cells["AA1"].Value = "Mylers Status";
                Sheet.Cells["AB1"].Value = "Mylers Remark";
                Sheet.Cells["AC1"].Value = "PinThreads Status";
                Sheet.Cells["AD1"].Value = "PinThreads Remark";
                Sheet.Cells["AE1"].Value = "RestingPads Status";
                Sheet.Cells["AF1"].Value = "RestingPads Remark";
                Sheet.Cells["AG1"].Value = "Sliders Status";
                Sheet.Cells["AH1"].Value = "Sliders Remark";
                Sheet.Cells["AI1"].Value = "Kugel Status";
                Sheet.Cells["AJ1"].Value = "Kugel Remark";
                Sheet.Cells["AK1"].Value = "Comment";


                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = item.UniqueFormName + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.FixtureName;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.FixtureNo;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.ProjectName;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.FromDate?.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.ToDate?.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.Reason;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.RpsPin;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.RpsPinRemark;
                    Sheet.Cells[string.Format("S{0}", row)].Value = item.Clamps;
                    Sheet.Cells[string.Format("T{0}", row)].Value = item.ClampsRemark;
                    Sheet.Cells[string.Format("U{0}", row)].Value = item.Wheels;
                    Sheet.Cells[string.Format("V{0}", row)].Value = item.WheelsRemark;
                    Sheet.Cells[string.Format("W{0}", row)].Value = item.RpsStick;
                    Sheet.Cells[string.Format("X{0}", row)].Value = item.RpsStickRemark;
                    Sheet.Cells[string.Format("Y{0}", row)].Value = item.LoseElement;
                    Sheet.Cells[string.Format("Z{0}", row)].Value = item.LoseRemark;
                    Sheet.Cells[string.Format("AA{0}", row)].Value = item.Mylers;
                    Sheet.Cells[string.Format("AB{0}", row)].Value = item.MylerRemark;
                    Sheet.Cells[string.Format("AC{0}", row)].Value = item.PinThreads;
                    Sheet.Cells[string.Format("AD{0}", row)].Value = item.PinRemark;
                    Sheet.Cells[string.Format("AE{0}", row)].Value = item.RestingPads;
                    Sheet.Cells[string.Format("AF{0}", row)].Value = item.PadsRemark;
                    Sheet.Cells[string.Format("AG{0}", row)].Value = item.Sliders;
                    Sheet.Cells[string.Format("AH{0}", row)].Value = item.SlidersRemark;
                    Sheet.Cells[string.Format("AI{0}", row)].Value = item.Kugel;
                    Sheet.Cells[string.Format("AJ{0}", row)].Value = item.KugelRemark;

                    if (item.ApproverStatus == "Rejected" || item.ApproverStatus == "Enquired")
                    {
                        Sheet.Cells[string.Format("AK{0}", row)].Value = item.Comment;
                    }
                    else
                    {
                        Sheet.Cells[string.Format("AK{0}", row)].Value = "";
                    }
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetEQSAReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var apfplist = await obj.ViewEQSAExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    //var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);
                    var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);


                    foreach (var item in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (item.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.EmployeeName = Convert.ToString(item.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(item.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.EmployeeDepartment);
                        }

                        formData.RequestType = item.RequestType;
                        formData.BusinessJustification = item.BusinessJustification;
                        formData.EQSAEmployeeName = item.EmployeeName;
                        formData.EmployeeID = item.EmployeeID;
                        formData.LogicCardID = item.LogicCardID;
                        formData.StationName = item.StationName;
                        formData.Shop = item.Shop;
                        formData.AccessGroup = item.AccessGroup;


                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Cost Center Number";
                Sheet.Cells["I1"].Value = "Employee Designation";
                Sheet.Cells["J1"].Value = "Employee Department";
                Sheet.Cells["K1"].Value = "Request Type";
                Sheet.Cells["L1"].Value = "Business Justification";
                Sheet.Cells["M1"].Value = "Employee Name";
                Sheet.Cells["N1"].Value = "Employee ID";
                Sheet.Cells["O1"].Value = "Logic Card ID";
                Sheet.Cells["P1"].Value = "Station Name";
                Sheet.Cells["Q1"].Value = "Shop";
                Sheet.Cells["R1"].Value = "Access Group";


                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = "IMAC" + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.RequestType;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.BusinessJustification;
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.EQSAEmployeeName;
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.EmployeeID;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.LogicCardID;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.StationName;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.Shop;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.AccessGroup;
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }

        public async Task<byte[]> GetIPAFReport(List<FormData> formsList)
        {
            var reportData = new byte[0];
            try
            {
                ListDAL obj = new ListDAL();
                var apfplist = await obj.ViewIPAFExcelData();
                var finalDataList = new List<FormData>();

                foreach (var formRow in formsList)
                {
                    //var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);
                    var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);


                    foreach (var item in matchingRecords)
                    {
                        var formData = new FormData();
                        formData = formRow.Clone();
                        if (item.RequestSubmissionFor == "OnBehalf")
                        {
                            formData.EmployeeName = Convert.ToString(item.OtherEmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.OtherEmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.OtherEmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.OtherEmployeeDepartment);
                        }
                        else
                        {
                            formData.EmployeeName = Convert.ToString(item.EmployeeName);
                            formData.CostCenterNumber = Convert.ToString(item.EmployeeCCCode);
                            formData.EmployeeDesignation = Convert.ToString(item.EmployeeDesignation);
                            formData.EmployeeDepartment = Convert.ToString(item.EmployeeDepartment);
                        }

                        formData.RequestType = item.RequestType;
                        formData.Applicationname = item.Applicationname;
                        formData.Applicationurl = item.Applicationurl;

                        formData.Applicationaccess = item.Applicationaccess;
                        formData.Accessgroup = item.Accessgroup;
                        formData.RequestFromDate = item.RequestFromDate;
                        formData.RequestToDate = item.RequestToDate;
                        formData.BusinessJustification = item.BusinessJustification;
                        //formData.AssignType = item.AssignType;
                        //formData.FromDate = item.FromDate;
                        //formData.ToDate = item.ToDate;

                        finalDataList.Add(formData);
                    }
                }


                var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
                List<FormData> arrayData = newList;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage Ep = new ExcelPackage();
                ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

                Sheet.Cells["A1"].Value = "Form Id";
                Sheet.Cells["B1"].Value = "Form Name";
                Sheet.Cells["C1"].Value = "Request From";
                Sheet.Cells["D1"].Value = "Details/Business Needs";
                Sheet.Cells["E1"].Value = "Status";
                Sheet.Cells["F1"].Value = "Recieved Date";
                Sheet.Cells["G1"].Value = "Employee Name";
                Sheet.Cells["H1"].Value = "Cost Center Number";
                Sheet.Cells["I1"].Value = "Employee Designation";
                Sheet.Cells["J1"].Value = "Employee Department";
                Sheet.Cells["K1"].Value = "Request Type";
                Sheet.Cells["L1"].Value = "from";
                Sheet.Cells["M1"].Value = "to";
                Sheet.Cells["N1"].Value = "Application Name";
                Sheet.Cells["O1"].Value = "Application URL";
                Sheet.Cells["P1"].Value = "Type Of Access";
                Sheet.Cells["Q1"].Value = "Access Group";
                Sheet.Cells["R1"].Value = "Business Justification";
               
                int row = 2;
                foreach (var item in arrayData)
                {

                    Sheet.Cells[string.Format("A{0}", row)].Value = "IPAF" + item.UniqueFormId;
                    Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
                    Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
                    Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
                    Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
                    Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
                    Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
                    Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
                    Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
                    Sheet.Cells[string.Format("K{0}", row)].Value = item.RequestType;
                    Sheet.Cells[string.Format("L{0}", row)].Value = item.RequestFromDate?.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("M{0}", row)].Value = item.RequestToDate?.ToString("dd-MM-yyyy");
                    Sheet.Cells[string.Format("N{0}", row)].Value = item.Applicationname;
                    Sheet.Cells[string.Format("O{0}", row)].Value = item.Applicationurl ;
                    Sheet.Cells[string.Format("P{0}", row)].Value = item.Applicationaccess;
                    Sheet.Cells[string.Format("Q{0}", row)].Value = item.Accessgroup;
                    Sheet.Cells[string.Format("R{0}", row)].Value = item.BusinessJustification;
                  
                    row++;
                }

                Sheet.Cells["A:AZ"].AutoFitColumns();
                reportData = Ep.GetAsByteArray();
                Ep.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return reportData;
        }
        
        //public async Task<byte[]> GetTSFReport(List<FormData> formsList)
        //{
        //    var reportData = new byte[0];
        //    try
        //    {
        //        ListDAL obj = new ListDAL();
        //        var apfplist = await obj.ViewTSFExcelData();
        //        var finalDataList = new List<FormData>();

        //        foreach (var formRow in formsList)
        //        {
        //            //var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);
        //            var matchingRecords = apfplist.Where(x => Convert.ToInt32(x.FormIDId) == formRow.UniqueFormId);


        //            foreach (var item in matchingRecords)
        //            {
        //                var formData = new FormData();
        //                formData = formRow.Clone();
        //                if (item.RequestSubmissionFor == "OnBehalf")
        //                {
        //                    formData.EmployeeName = Convert.ToString(item.OtherEmployeeName);
        //                    formData.CostCenterNumber = Convert.ToString(item.OtherEmployeeCCCode);
        //                    formData.EmployeeDesignation = Convert.ToString(item.OtherEmployeeDesignation);
        //                    formData.EmployeeDepartment = Convert.ToString(item.OtherEmployeeDepartment);
        //                }
        //                else
        //                {
        //                    formData.EmployeeName = Convert.ToString(item.EmployeeName);
        //                    formData.CostCenterNumber = Convert.ToString(item.EmployeeCCCode);
        //                    formData.EmployeeDesignation = Convert.ToString(item.EmployeeDesignation);
        //                    formData.EmployeeDepartment = Convert.ToString(item.EmployeeDepartment);
        //                }

        //                formData.RequestType = item.RequestType;
        //                formData.Applicationname = item.Applicationname;
        //                formData.Applicationurl = item.Applicationurl;

        //                formData.Applicationaccess = item.Applicationaccess;
        //                formData.Accessgroup = item.Accessgroup;
        //                formData.RequestFromDate = item.RequestFromDate;
        //                formData.RequestToDate = item.RequestToDate;
        //                formData.BusinessJustification = item.BusinessJustification;
        //                //formData.AssignType = item.AssignType;
        //                //formData.FromDate = item.FromDate;
        //                //formData.ToDate = item.ToDate;

        //                finalDataList.Add(formData);
        //            }
        //        }


        //        var newList = finalDataList.OrderByDescending(x => x.RecievedDate).ToList();
        //        List<FormData> arrayData = newList;
        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //        ExcelPackage Ep = new ExcelPackage();
        //        ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Report");

        //        Sheet.Cells["A1"].Value = "Form Id";
        //        Sheet.Cells["B1"].Value = "Form Name";
        //        Sheet.Cells["C1"].Value = "Request From";
        //        Sheet.Cells["D1"].Value = "Details/Business Needs";
        //        Sheet.Cells["E1"].Value = "Status";
        //        Sheet.Cells["F1"].Value = "Recieved Date";
        //        Sheet.Cells["G1"].Value = "Employee Name";
        //        Sheet.Cells["H1"].Value = "Cost Center Number";
        //        Sheet.Cells["I1"].Value = "Employee Designation";
        //        Sheet.Cells["J1"].Value = "Employee Department";

        //        Sheet.Cells["K1"].Value = "Request Type";
        //        Sheet.Cells["L1"].Value = "from";
        //        Sheet.Cells["M1"].Value = "to";
        //        Sheet.Cells["N1"].Value = "Application Name";
        //        Sheet.Cells["O1"].Value = "Application URL";
        //        Sheet.Cells["P1"].Value = "Type Of Access";
        //        Sheet.Cells["Q1"].Value = "Access Group";
        //        Sheet.Cells["R1"].Value = "Business Justification";
               
        //        int row = 2;
        //        foreach (var item in arrayData)
        //        {

        //            Sheet.Cells[string.Format("A{0}", row)].Value = "TSF" + item.UniqueFormId;
        //            Sheet.Cells[string.Format("B{0}", row)].Value = item.FormName;
        //            Sheet.Cells[string.Format("C{0}", row)].Value = item.Author.Submitter;
        //            Sheet.Cells[string.Format("D{0}", row)].Value = item.BusinessNeed;
        //            Sheet.Cells[string.Format("E{0}", row)].Value = item.Status;
        //            Sheet.Cells[string.Format("F{0}", row)].Value = item.FormCreatedDate.ToString("dd-MM-yyyy");
        //            Sheet.Cells[string.Format("G{0}", row)].Value = item.EmployeeName;
        //            Sheet.Cells[string.Format("H{0}", row)].Value = item.CostCenterNumber;
        //            Sheet.Cells[string.Format("I{0}", row)].Value = item.EmployeeDesignation;
        //            Sheet.Cells[string.Format("J{0}", row)].Value = item.EmployeeDepartment;
        //            Sheet.Cells[string.Format("K{0}", row)].Value = item.RequestType;

        //            Sheet.Cells[string.Format("L{0}", row)].Value = item.RequestFromDate?.ToString("dd-MM-yyyy");
        //            Sheet.Cells[string.Format("M{0}", row)].Value = item.RequestToDate?.ToString("dd-MM-yyyy");
        //            Sheet.Cells[string.Format("N{0}", row)].Value = item.Applicationname;
        //            Sheet.Cells[string.Format("O{0}", row)].Value = item.Applicationurl ;
        //            Sheet.Cells[string.Format("P{0}", row)].Value = item.Applicationaccess;
        //            Sheet.Cells[string.Format("Q{0}", row)].Value = item.Accessgroup;
        //            Sheet.Cells[string.Format("R{0}", row)].Value = item.BusinessJustification;
                  
        //            row++;
        //        }

        //        Sheet.Cells["A:AZ"].AutoFitColumns();
        //        reportData = Ep.GetAsByteArray();
        //        Ep.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message, ex);
        //        Console.WriteLine(ex.Message);
        //    }
        //    return reportData;
        //}


        //public async Task<ActionResult> GetCCNameByFilter(string Name)
        //{
        //    ListDAL objDAL = new ListDAL();
        //    var result = await objDAL.GetCCNameByFilter(Name);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        public async Task<ActionResult> ChangePassword(int ID, string CurrentPassword, string NewPassword)
        {
            ListDAL objDAL = new ListDAL();
            var result = await objDAL.ChangePassword(ID, CurrentPassword, NewPassword);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetAllMobileTask()
        {
            var email = (Session["UserData"] as UserData)?.Email;
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetAllMobileTask(email);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetMobilePTask(string Checked, string Filter)
        {
            //string Checked = "1";
            List<FormData> list = new List<FormData>();
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetApprovedForms(Checked, Filter);
            var model = new DashboardModel();
            model.Data = new DataModel();
            list = result ?? new List<FormData>();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public async Task<PartialViewResult> GetMyTasks_MobileView(string Checked, string Filter)
        {
            //string Checked = "1";
            ListDAL listDAL = new ListDAL();
            var result = await listDAL.GetPendingForms(Checked, Filter);
            var model = new DashboardModel();
            model.Data = new DataModel();
            model.Data.Forms = result ?? new List<FormData>();
            return PartialView("_MyTaskMobileView", model);
        }


        [HttpGet]
        public JsonResult GetTotalTask(string values)
        {
            var email = (Session["UserData"] as UserData)?.Email;
            ListDAL listDAL = new ListDAL();
            List<Tasklist> list = new List<Tasklist>();
            list = listDAL.GetTotalTask(values, email);
            return Json(list, JsonRequestBehavior.AllowGet);

        }
        public async Task<JsonResult> GetMyRequestMobile()
        {
            List<FormData> list = new List<FormData>();
            ListDAL listDAL = new ListDAL();
            var request = await listDAL.GetAllFormsList("", "", "");
            var data = await listDAL.GetForms();
            var model = new DashboardModel();
            model.Data = data;
            //model.Data.StatusCount = listDAL.GetStatusCount();
            list = request;

            return Json(list, JsonRequestBehavior.AllowGet);
        }

    }
}

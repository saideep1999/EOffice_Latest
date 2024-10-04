using OfficeOpenXml;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class BusTransportationController : BaseController
    {
        BusTransportationDAL BusTransportationDAL;
        public async Task<ActionResult> SaveBusTransportationForm(FormCollection form)
        {
            BusTransportationDAL = new BusTransportationDAL();
            var result = await BusTransportationDAL.SaveBusTransportationForm(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> BusActionUpdate(FormCollection form)
        {
            BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.BusActionUpdate(form, (UserData)Session["UserData"]);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetBusRouteName(string routeName, string routeNumber, string shift, string locationName)
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.GetBusRouteName(routeName, routeNumber, shift, locationName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetGenderName()
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.GetGenderName();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetShiftName()
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.GetShiftName();
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public async Task<ActionResult> GetBusRouteNumber(string routeName, string routeNumber, string shift, string locationName)
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.GetBusRouteNumber(routeName, routeNumber, shift, locationName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> GetBusPickUpPoint(string routeName, string routeNumber, string shift, string locationName)
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.GetBusPickUpPoint(routeName, routeNumber, shift, locationName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public ActionResult BusTransportEmployee()
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var empData = BusTransportationDAL.GetBusTransportOldEmp();
            return View(empData);
        }

        [HttpPost]
        public ActionResult BusTransportEmployee(FormCollection form)
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.AddEmployeeData(form);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteEmployeeData(string rowId)
        {
            BusTransportationDAL BusTransportationDAL = new BusTransportationDAL();
            var result = BusTransportationDAL.DeleteEmployeeData(rowId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> DeleteReportDownload()
        {
            BusTransportationDAL obj = new BusTransportationDAL();
            List<BTFData> userDet = new List<BTFData>();
            var data = new byte[0];
            try
            {

                userDet = obj.GetDeletedEmpNumber();

                data = await GetDeletedReport(userDet);

                return File(data, System.Net.Mime.MediaTypeNames.Application.Octet, "DeletedEmployeesBusReport.xlsx");

            }
            catch (Exception ex)
            {
                //Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<byte[]> GetDeletedReport(List<BTFData> userDet)
        {
            BusTransportationDAL obj = new BusTransportationDAL();
            ListDAL objList = new ListDAL();
            var reportData = new byte[0];


            //Forms and Approval Master Record
            var formsList = await obj.GetBusFormsReport(userDet);

            //Old Record
            var oldBusReport = await obj.ViewBTFOldExcelData(userDet);

            //Transaction Record
            var busList = await objList.ViewBTFExcelData();

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

                    //var regionList = objList.GetRegionData();
                    //var regionMainList = regionList.Result;
                    //foreach (var item in regionMainList)
                    //{
                    //    if (formData.Address.Contains(item.Region))
                    //    {
                    //        formData.Region = item.Region;
                    //        break;
                    //    }
                    //    else
                    //    {
                    //        formData.Region = "";
                    //    }
                    //}
                    finalDataList.Add(formData);
                }
            }
                    

            var mainNewList = new List<FormData>();
            foreach (var userDel in userDet)
            {
                var itemsToAdd = finalDataList.Where(r => r.EmployeeCode == userDel.EmployeeCode || r.OtherEmployeeCode == userDel.EmployeeCode).ToList();
                foreach (var item in itemsToAdd)
                {
                    mainNewList.Add(item);
                }
               
            }
            var newList = mainNewList.OrderByDescending(x => x.RecievedDate).ToList();

            //New Record
            List<FormData> arrayData = newList;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("DeletedBusReport");
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

            return reportData;
        }
    }
}
using System;
using System.Web.Mvc;
using Skoda_DCMS.Models;
using Skoda_DCMS.DAL;
using System.Threading.Tasks;
using Skoda_DCMS.Helpers;
using System.IO;
using System.Diagnostics;
using Rotativa;
using Skoda_DCMS.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Skoda_DCMS.App_Start;
using System.Web;
using Syncfusion.Presentation;

namespace Skoda_DCMS.Controllers
{
    public class ListController : BaseController
    {
        ListDAL listDAL;
        // GET: List
        public ActionResult Index()
        {
            return View("~/Views/List/CreateList.cshtml");
        }

        [HttpPost]
        public ActionResult CreateList(FormCollection form)
        {
            listDAL = new ListDAL();
            var result = listDAL.CreateList(form);
            return RedirectToAction("loadFields", "Default", form["listName"]);
        }
        public ActionResult Order()
        {
            ViewBag.IsNew = true;
            var user = (UserData)Session["UserData"];
            if (user == null)
            {
                if (GlobalClass.IsUserLoggedOut)
                    return RedirectToAction("Index", "Login");
            }
            return View("~/Views/List/Order.cshtml", user);
        }
        /// <summary>
        /// Dashboard/FormDashboard-It is used to create all the forms.
        /// </summary>
        /// <returns></returns>
        //[CustomActionFilter]
        public async Task<ActionResult> CreateForm(string uniqueFormName, int formParentId, string formName, string ControllerName)
        {
            //var watch = new Stopwatch();
            //watch.Start();
            listDAL = new ListDAL();
            //var newForms = await listDAL.GetForms();
            var result = await listDAL.GetFormByFormName(uniqueFormName);
            string viewName = result.Item1;
            dynamic model = result.Item2;
            ViewBag.IsNewMode = true;
            ViewBag.IsEditMode = false;
            ViewBag.IsViewMode = false;

            ViewBag.FormData = new FormData() { UniqueFormName = uniqueFormName, Id = formParentId, FormName = formName };
            ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            //watch.Stop();
            //var timeTaken = watch.ElapsedMilliseconds;
            //return View("~/Views/List/" + viewName, model);
            return View("~/Views/" + ControllerName + "/" + viewName, model);

        }
        /// <summary>
        /// SFO-It is used for Saving data.
        /// </summary>
        /// <returns></returns>    


        /// <summary>
        /// ALL-It is used to save approver response in ApprovalMaster.
        /// </summary>
        /// <returns></returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SaveResponse(string response, int appRowId, string comment, int approvalType)
        {
            var st = new System.Diagnostics.Stopwatch();
            st.Start();

            listDAL = new ListDAL();
            var result = listDAL.SaveResponse(response, appRowId, comment, approvalType);

            st.Stop();
            var time = st.ElapsedMilliseconds;

            string returnMsg = $"{time} : Approval Total Time Consumed";
            Log.Error(returnMsg);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// ALL-It is used to cancel the form.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CancelForm(int formId)
        {
            listDAL = new ListDAL();
            var result = await listDAL.CancelForm(formId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// PAF-It is used for populating the Location Dropdown from the LocationDetails master in SharePoint list.
        /// </summary>
        /// <returns></returns>

        //public async Task<JsonResult> ViewForm(int rowId, int formId, int appRowId)
        //{
        //    listDAL = new ListDAL();
        //    var result = await listDAL.ViewForm(rowId, formId);
        //    ViewBag.Order = result;
        //    ViewBag.IsNew = false;
        //    ViewBag.RowId = rowId;
        //    ViewBag.FormId = formId;
        //    ViewBag.AppRowId = appRowId;
        //    var user = (UserData)Session["UserData"];
        //    return Json(new
        //    {
        //        view = RenderRazorViewToString(ControllerContext, "~/Views/List/Order.cshtml"),
        //        isValid = true,
        //    }, JsonRequestBehavior.AllowGet);
        //}

        /// <summary>
        /// ALL-It is used to print the PDF copy of the form.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> PrintIndex(string formName, int formId, int rowId, int appRowId, string ControllerName)
        {
            try
            {
                listDAL = new ListDAL();
                var result = await listDAL.GetFilledFormByFormName(formName, rowId, formId);
                ViewBag.Model = result.Item2;
                ViewBag.IsNew = false;
                ViewBag.IsEdit = true;
                ViewBag.IsNewMode = false;
                ViewBag.IsEditMode = false;
                ViewBag.IsViewMode = true;
                ViewBag.RowId = rowId;
                ViewBag.FormId = formId;
                ViewBag.AppRowId = appRowId;
                ViewBag.ApprovalType = 0;
                if (formName == "PAF" || formName == "CBRF" || formName == "IDCF" || formName == "ECF" || formName == "DAF" || formName == "CRF")
                {
                    ViewBag.IsValidityCheck = true;
                }

                ViewBag.Layout = "~/Views/Shared/_LayoutWithCss.cshtml";
                ViewBag.formName = formName;
                //var result = new ViewAsPdf("ViewFilledForm", new { formName = formName, rowId = rowId, appRowId = appRowId, formId = formId, user = user })
                //return new ViewAsPdf("~/Views/List/" + result.Item1)

                //Test code for KSRM form pdf
                string form = result.Item1;
                var pdfName = "";
                if (form == "AnalysisPartsFormPresentation.cshtml")
                {
                    pdfName = "AnalysisPartsFormPresentationPDF.cshtml";
                }
                else
                {
                    pdfName = form.Contains("Form") ? form.Replace("Form.cshtml", "PDF.cshtml") : form.Replace(".cshtml", "PDF.cshtml");
                }
               
                var pdfFileHtml = $"~/Views/{ControllerName}/{pdfName}";
                if (!System.IO.File.Exists(Server.MapPath(pdfFileHtml)))
                {
                    return Json("PDF is not created, please contact administrator.", JsonRequestBehavior.AllowGet);
                }
                return new ViewAsPdf(pdfFileHtml)
                {
                    FileName = formName + formId + ".pdf",
                    IsLowQuality = false,
                    //CustomSwitches = "--disable-smart-shrinking",
                    //PageSize = Rotativa.Options.Size.A3,
                    PageOrientation = Rotativa.Options.Orientation.Portrait,
                    //PageMargins = new Rotativa.Options.Margins(0, 0, 0, 0),
                    IsJavaScriptDisabled = false,
                    // PageMargins = new Rotativa.Options.Margins(0,0,0,0),
                    PageSize = Rotativa.Options.Size.A4,
                    //PageOrientation = Rotativa.Options.Orientation.Portrait,
                    CustomSwitches = "--page-offset 0 --footer-center [page] --footer-font-size 14 "
                };
                //return View(pdfFileHtml);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return View("~/Views/Shared/Errpr.cshtml");
            }
        }



        public async Task<ActionResult> ViewFilledForm(string formName, int formId, int rowId, int appRowId, UserData user)
        {
            try
            {
                listDAL = new ListDAL();
                var result = await listDAL.GetFilledFormByFormName(formName, rowId, formId);
                ViewBag.Order = result;
                ViewBag.IsNew = false;
                ViewBag.RowId = rowId;
                ViewBag.FormId = formId;
                ViewBag.AppRowId = appRowId;
                ViewBag.Layout = "~/Views/Shared/_LayoutWithCss.cshtml";
                ViewBag.formName = formName;
                return View("~/Views/List/" + result.Item1, result.Item2);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return View("~/Views/Shared/Errpr.cshtml");
            }

        }
        /// <summary>
        /// Dashboard/FormDashboard-It is used to perform the ViewForm functionality for all the forms.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> ViewFilledFormNew(string formName, int formId, int rowId = 0, int appRowId = 0, int MyTask = 0, int ApprovalType = 0, string Department = "", string ControllerName = "")//all filled forms will be showed via this function only.
        {
            listDAL = new ListDAL();
            var result = await listDAL.GetFilledFormByFormName(formName, rowId, formId);
            ViewBag.Model = result.Item2;
            ViewBag.IsNew = false;
            ViewBag.IsEdit = true;
            ViewBag.IsNewMode = false;
            ViewBag.IsEditMode = false;
            ViewBag.IsViewMode = true;
            ViewBag.RowId = rowId;
            ViewBag.FormId = formId;
            ViewBag.AppRowId = appRowId;
            ViewBag.LayoutValue = null;
            ViewBag.ApprovalType = ApprovalType;
            ViewBag.Department = Department;
            ViewBag.ControllerName = ControllerName;
            if (MyTask == 1)
            {
                ViewBag.IsValidityCheck = false;
            }
            else
            {
                ViewBag.IsValidityCheck = true;
            }
            //string view = RenderRazorViewToString(ControllerContext, "~/Views/List/" + result.Item1 + "").ToString();
            string view = RenderRazorViewToString(ControllerContext, "~/Views/" + ControllerName + "/" + result.Item1 + "").ToString();
            var jsonValue = Json(new
            {
                isValid = true,
                view = view
            }, JsonRequestBehavior.AllowGet);
            jsonValue.MaxJsonLength = int.MaxValue;
            return jsonValue;
        }
        /// <summary>
        /// ALL-It is used to render razor view for all the forms.
        /// </summary>
        /// <returns></returns>
        public static string RenderRazorViewToString(ControllerContext controllerContext, string viewName)
        {
            //controllerContext.Controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var ViewResult = ViewEngines.Engines.FindPartialView(controllerContext, viewName);
                var ViewContext = new ViewContext(controllerContext, ViewResult.View, controllerContext.Controller.ViewData, controllerContext.Controller.TempData, sw);
                ViewResult.View.Render(ViewContext, sw);
                ViewResult.ViewEngine.ReleaseView(controllerContext, ViewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
        public async Task<ActionResult> EditForm(string formName, int formId, int rowId = 0, int appRowId = 0, int MyTask = 0, string ControllerName = "")
        {
            ////var watch = new Stopwatch();
            ////watch.Start();
            //listDAL = new ListDAL();
            ////var newForms = await listDAL.GetForms();
            //var result = await listDAL.GetFormByFormName(uniqueFormName);
            //string viewName = result.Item1;
            //dynamic model = result.Item2;

            listDAL = new ListDAL();
            var result = await listDAL.GetFilledFormByFormName(formName, rowId, formId);
            ViewBag.Model = result.Item2;
            ViewBag.IsNew = true;
            ViewBag.IsEdit = false;
            ViewBag.IsNewMode = false;
            ViewBag.IsEditMode = true;
            ViewBag.IsViewMode = false;
            ViewBag.RowId = rowId;
            ViewBag.FormId = formId;
            ViewBag.AppRowId = appRowId;
            ViewBag.LayoutValue = null;
            ViewBag.IsValidityCheck = true;
            //if (MyTask == 1)
            //{
            //    ViewBag.IsValidityCheck = false;
            //}
            //else
            //{
            //    ViewBag.IsValidityCheck = true;
            //}
            //string view = RenderRazorViewToString(ControllerContext, "~/Views/List/" + result.Item1 + "").ToString();
            string view = RenderRazorViewToString(ControllerContext, "~/Views/" + ControllerName + "/" + result.Item1 + "").ToString();
            var jsonValue = Json(new
            {
                isValid = true,
                view = view
            }, JsonRequestBehavior.AllowGet);
            jsonValue.MaxJsonLength = int.MaxValue;
            return jsonValue;
        }

        public ActionResult PdfSharpConvert()
        {
            string html = "<html><body><p>Priyanshi</p></body></html>";
            Byte[] res = null;
            using (MemoryStream ms = new MemoryStream())
            {
                var pdf = TheArtOfDev.HtmlRenderer.PdfSharp.PdfGenerator.GeneratePdf(html, PdfSharp.PageSize.A4);
                pdf.Save(ms);
                res = ms.ToArray();
            }
            return File(res, "application/pdf", "sample.pdf");
        }
        /// <summary>
        /// InternetAccess-It is used to save the data in sharepoint list.
        /// </summary>
        /// <returns></returns>


        ///// <summary>
        ///// IDCF-It is used to get the department deatils from SharePoint list.
        ///// </summary>
        ///// <returns></returns>

        /// <summary>
        /// InternetAccess/SmartPhone/SoftwareRequisition-It is used to get the designations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetDesignations()
        {
            listDAL = new ListDAL();
            var result = listDAL.GetDesignations();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        //public ActionResult GetDesignationsPost()
        //{
        //    listDAL = new ListDAL();
        //    var result = listDAL.GetDesignations();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        /// <summary>
        /// InternetAccess-It is used to get the organizations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetExternalOrganization()
        {
            listDAL = new ListDAL();
            var result = await listDAL.GetExternalOrganization();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// InternetAccess-It is used to get the locations from SharePoint list.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetServiceDeskLocations()
        {
            listDAL = new ListDAL();
            var result = await listDAL.GetLocations();
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// InternetAccess-It is used to get the employee details using firstname as parameter from sql db.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetEmployeeDetails(string searchText)
        {
            listDAL = new ListDAL();
            var result = listDAL.GetEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetEmployeeDetailsByEmpNumber(long EmpNum)
        {
            listDAL = new ListDAL();
            var result = listDAL.GetEmployeeDetailsByEmpNum(EmpNum);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SearchEmployeeDetails(string first, string last)
        {
            listDAL = new ListDAL();
            var result = listDAL.SearchEmployeeDetails(first, last);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// InternetAccess-It is used to get the employee details using userid as parameter from sql db.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetExistingEmployeeDetails(string otherEmpUserId, long EmpNumber = 0)
        {
            listDAL = new ListDAL();
            var result = listDAL.GetExistingEmployeeDetails(otherEmpUserId, EmpNumber);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// InternetAccess-It is used for viewing the Internet Access form.
        /// </summary>
        /// <returns></returns>
        //public async Task<ActionResult> GetInternetAccessRequestDetails(int formId)
        //{
        //    listDAL = new ListDAL();
        //    var result = await listDAL.GetInternetAccessRequestDetails(formId);
        //    return Json(result[0], JsonRequestBehavior.AllowGet);
        //}
        /// <summary>
        /// InternetAccess-It is used to get the cost center from sql db.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCostCenterDetails(string searchText)
        {
            listDAL = new ListDAL();
            var result = listDAL.GetCostCenterDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        //public async Task<ActionResult> SaveRIDCF(FormCollection form)
        //{
        //    listDAL = new ListDAL();
        //    var result = await listDAL.SaveRIDCF(form, (UserData)Session["UserData"]);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        #region ID Card Form     

        /// <summary>
        /// ID Card-It is used to get the Department Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Department()
        {
            listDAL = new ListDAL();
            var result = await listDAL.GetDepartment();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// ID Card-It is used to get the Cost Center Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CostCenter()
        {
            listDAL = new ListDAL();
            var result = await listDAL.GetCostCenter();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// ID Card-It is used to get the Location Master Dropdown data.
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> LocationMaster()
        {
            listDAL = new ListDAL();
            var result = await listDAL.GetLocationName();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// PAF Form-It is used to get the Employee Data in PAF Form - Auto Complete code.
        /// </summary>
        /// <returns></returns>

        #endregion
        /// <summary>
        /// PAF Form-It is used to get the Employee Data in PAF Form - Auto Complete code.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetPAFEmployeeDetails(string searchText)
        {
            listDAL = new ListDAL();
            var result = listDAL.GetPAFEmployeeDetails(searchText);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// PAF Form-It is used to get the exisitng Employee Data in PAF Form.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetPAFExistingEmployeeDetails(string otherEmpUserId)
        {
            listDAL = new ListDAL();
            var result = listDAL.GetPAFExistingEmployeeDetails(otherEmpUserId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewUserManual()
        {
            return View("~/Views/Pages/ViewUserManual.cshtml");
        }

        /// <summary>
        /// All Forms-It is used to get the DomainIDs from sql server.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetDomainIDs()
        {
            listDAL = new ListDAL();
            var result = listDAL.GetDomainIDs();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> CreateFormUrlAndRedirect(string uniqueFormName)
        {
            string url = "";
            try
            {
                var listDAL = new ListDAL();
                var result = await listDAL.GetFormParentDetailsByUniqueName(uniqueFormName);
                var selectedForm = result.Forms.FirstOrDefault();
                return RedirectToAction("CreateForm", "List",
                    new
                    {
                        uniqueFormName = uniqueFormName,
                        formParentId = selectedForm.Id,
                        formName = selectedForm.FormName,
                        ControllerName = selectedForm.ControllerName
                    });
            }
            catch (Exception ex)
            {
                return View("~/Views/Error/NotFound.cshtml");
            }
        }


        //public ActionResult GetDepartmentList()
        //{
        //    listDAL = new ListDAL();
        //    var result = listDAL.GetDepartmentList();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult GetSubDepartmentList()
        //{
        //    listDAL = new ListDAL();
        //    var result = listDAL.GetSubDepartmentList();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        [HttpPost]
        public ActionResult SaveDesignationData(string Designation)
        {
            listDAL = new ListDAL();
            var result = listDAL.SaveDesignationData(Designation);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}

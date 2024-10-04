using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.WebControls;
using Newtonsoft.Json;
using Skoda_DCMS.App_Start;
using Skoda_DCMS.Extension;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Xml;
using static Skoda_DCMS.Helpers.Flags;
using DataTable = System.Data.DataTable;

namespace Skoda_DCMS.DAL
{
    public class POCRFormDAL : CommonDAL
    {
        public UserData user = HttpContext.Current.Session != null ? (UserData)(HttpContext.Current.Session["UserData"]) : new UserData();
        public string sqlConString = ConfigurationManager.ConnectionStrings["connectionString"].ToString();
        public string adCode = ConfigurationManager.AppSettings["ADCode"];
        SqlConnection con;

        dynamic approverEmailIds;
        public async Task<dynamic> ViewPOCRFormData(int rowId, int formId)
        {
            dynamic URCFData = new ExpandoObject();
            List<POCRModel> MainList = new List<POCRModel>();
            try
            {
                GlobalClass gc = new GlobalClass();
                var user = gc.GetCurrentUser();
               
                List<POCRFormModel> item = new List<POCRFormModel>();
                POCRFormModel model = new POCRFormModel();
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataTable dt = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("ViewPOCRMFDetails", con);
                cmd.Parameters.Add(new SqlParameter("@rowId", rowId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(dt);
                con.Close();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        model.AppRowId = dt.Rows[0]["AutoID"] != DBNull.Value || dt.Rows[0]["AutoID"] != "0" ? Convert.ToInt32(dt.Rows[i]["AutoID"]) : 0;
                        model.EmployeeType = dt.Rows[0]["EmployeeType"] != DBNull.Value || dt.Rows[0]["EmployeeType"] != "0" ?Convert.ToString(dt.Rows[0]["EmployeeType"]) : "";
                        model.EmployeeCode = dt.Rows[0]["EmployeeCode"] != DBNull.Value || dt.Rows[0]["EmployeeCode"] != "0" ?Convert.ToInt64(dt.Rows[0]["EmployeeCode"]) : 0;
                        model.EmployeeCCCode = dt.Rows[0]["EmployeeCCCode"] != DBNull.Value || dt.Rows[0]["EmployeeCCCode"] != "0" ?Convert.ToInt64(dt.Rows[0]["EmployeeCCCode"]) : 0;
                        model.EmployeeUserId = dt.Rows[0]["EmployeeUserId"] != DBNull.Value || dt.Rows[0]["EmployeeUserId"] != "0" ?Convert.ToString(dt.Rows[0]["EmployeeUserId"]) : "";
                        model.EmployeeName = dt.Rows[0]["EmployeeName"] != DBNull.Value || dt.Rows[0]["EmployeeName"] != "0" ?Convert.ToString(dt.Rows[0]["EmployeeName"]) : "";
                        model.EmployeeDepartment = dt.Rows[0]["EmployeeDepartment"] != DBNull.Value || dt.Rows[0]["EmployeeDepartment"] != "0" ?Convert.ToString(dt.Rows[0]["EmployeeDepartment"]) : "";
                        model.EmployeeContactNo = dt.Rows[0]["EmployeeContactNo"] != DBNull.Value || dt.Rows[0]["EmployeeContactNo"] != "0" ? Convert.ToInt64(dt.Rows[0]["EmployeeContactNo"]) : 0;
                        model.ExternalOrganizationName = dt.Rows[0]["ExternalOrganizationName"] != DBNull.Value || dt.Rows[0]["ExternalOrganizationName"] != "0" ?Convert.ToString(dt.Rows[0]["ExternalOrganizationName"]) : "";
                        model.EmployeeLocation = dt.Rows[0]["EmployeeLocation"] != DBNull.Value || dt.Rows[0]["EmployeeLocation"] != "0" ?Convert.ToString(dt.Rows[0]["EmployeeLocation"]) : "";
                        model.EmployeeDesignation = dt.Rows[0]["EmployeeDesignation"] != DBNull.Value || dt.Rows[0]["EmployeeDesignation"] != "0" ?Convert.ToString(dt.Rows[0]["EmployeeDesignation"]) : "";
                        model.RequestSubmissionFor = dt.Rows[0]["RequestSubmissionFor"] != DBNull.Value || dt.Rows[0]["RequestSubmissionFor"] != "0" ?Convert.ToString(dt.Rows[0]["RequestSubmissionFor"]) : "";
                        model.OnBehalfOption = dt.Rows[0]["OnBehalfOption"] != DBNull.Value || dt.Rows[0]["OnBehalfOption"] != "0" ?Convert.ToString(dt.Rows[0]["OnBehalfOption"]) : "";
                        model.OtherEmployeeType = dt.Rows[0]["OtherEmployeeType"] != DBNull.Value || dt.Rows[0]["OtherEmployeeType"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeType"]) : "";
                        model.OtherEmployeeCode = dt.Rows[0]["OtherEmployeeCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCode"] != "0" && dt.Rows[0]["OtherEmployeeCode"] != "" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCode"]) : 0;
                        model.OtherEmployeeCCCode = dt.Rows[0]["OtherEmployeeCCCode"] != null && dt.Rows[0]["OtherEmployeeCCCode"] != DBNull.Value && dt.Rows[0]["OtherEmployeeCCCode"] != "0" ? Convert.ToInt64(dt.Rows[0]["OtherEmployeeCCCode"]) : 0;
                        model.OtherEmployeeContactNo = dt.Rows[0]["OtherEmployeeContactNo"] != DBNull.Value || dt.Rows[0]["OtherEmployeeContactNo"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeContactNo"]) : "";
                        model.OtherEmployeeUserId = dt.Rows[0]["OtherEmployeeUserId"] != DBNull.Value || dt.Rows[0]["OtherEmployeeUserId"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeUserId"]) : "";
                        model.OtherEmployeeName = dt.Rows[0]["OtherEmployeeName"] != DBNull.Value || dt.Rows[0]["OtherEmployeeName"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeName"]) : "";
                        model.OtherEmployeeDepartment = dt.Rows[0]["OtherEmployeeDepartment"] != DBNull.Value || dt.Rows[0]["OtherEmployeeDepartment"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeDepartment"]) : "";
                        model.OtherEmployeeLocation = dt.Rows[0]["OtherEmployeeLocation"] != DBNull.Value || dt.Rows[0]["OtherEmployeeLocation"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeLocation"]) : "";
                        model.OtherEmployeeDesignation = dt.Rows[0]["OtherEmployeeDesignation"] != DBNull.Value || dt.Rows[0]["OtherEmployeeDesignation"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeDesignation"]) : "";
                        model.OtherExternalOrganizationName = dt.Rows[0]["OtherExternalOrganizationName"] != DBNull.Value || dt.Rows[0]["OtherExternalOrganizationName"] != "0" ?Convert.ToString(dt.Rows[0]["OtherExternalOrganizationName"]) : "";
                        model.OtherEmployeeEmailId = dt.Rows[0]["OtherEmployeeEmailId"] != DBNull.Value || dt.Rows[0]["OtherEmployeeEmailId"] != "0" ?Convert.ToString(dt.Rows[0]["OtherEmployeeEmailId"]) : "";
                        model.BusinessNeed = dt.Rows[0]["BusinessNeed"] != DBNull.Value || dt.Rows[0]["BusinessNeed"] != "0" ?Convert.ToString(dt.Rows[0]["BusinessNeed"]) : "";
                        model.ID = dt.Rows[0]["AutoID"] != DBNull.Value || dt.Rows[0]["AutoID"] != "0" ?Convert.ToInt32(dt.Rows[0]["AutoID"]) : 0;
                        model.POCRID = dt.Rows[0]["POCRID"] != DBNull.Value || dt.Rows[0]["POCRID"] != "0" ?Convert.ToInt32(dt.Rows[0]["POCRID"]) : 0;
                        model.POCRNo = dt.Rows[0]["POCRNo"] != DBNull.Value || dt.Rows[0]["POCRNo"] != "0" ?Convert.ToString(dt.Rows[0]["POCRNo"]) : "";
                        model.TSEName = dt.Rows[0]["TSEName"] != DBNull.Value || dt.Rows[0]["TSEName"] != "0" ?Convert.ToString(dt.Rows[0]["TSEName"]) : "";
                        model.ZSMName = dt.Rows[0]["ZSMName"] != DBNull.Value || dt.Rows[0]["ZSMName"] != "0" ?Convert.ToString(dt.Rows[0]["ZSMName"]) : "";
                        model.ASMName = dt.Rows[0]["ASMName"] != DBNull.Value || dt.Rows[0]["ASMName"] != "0" ?Convert.ToString(dt.Rows[0]["ASMName"]) : "";
                        model.State = dt.Rows[0]["StateID"] != DBNull.Value || dt.Rows[0]["StateID"] != "0" ?Convert.ToString(dt.Rows[0]["StateID"]) : "";
                        model.DealerCode = dt.Rows[0]["DealerCode"] != DBNull.Value || dt.Rows[0]["DealerCode"] != "0" ?Convert.ToString(dt.Rows[0]["DealerCode"]) : "";
                        model.DealerName = dt.Rows[0]["DealerName"] != DBNull.Value || dt.Rows[0]["DealerName"] != "0" ?Convert.ToString(dt.Rows[0]["DealerName"]) : "";
                        model.DealerLocation = dt.Rows[0]["DealerLocation"] != DBNull.Value || dt.Rows[0]["DealerLocation"] != "0" ?Convert.ToString(dt.Rows[0]["DealerLocation"]) : "";
                        model.FILDealership = dt.Rows[0]["FILDealership"] != DBNull.Value || dt.Rows[0]["FILDealership"] != "0" ?Convert.ToString(dt.Rows[0]["FILDealership"]) : "";
                        model.DealerSalesLastYr = dt.Rows[0]["DealerSalesLY"] != DBNull.Value || dt.Rows[0]["DealerSalesLY"] != "0" ?Convert.ToString(dt.Rows[0]["DealerSalesLY"]) : "";
                        model.DealerSalesTill = dt.Rows[0]["DealerSalesTD"] != DBNull.Value || dt.Rows[0]["DealerSalesTD"] != "0" ?Convert.ToString(dt.Rows[0]["DealerSalesTD"]) : "";
                        model.Status = dt.Rows[0]["ExclusiveStatus"] != DBNull.Value || dt.Rows[0]["ExclusiveStatus"] != "0" ?Convert.ToString(dt.Rows[0]["ExclusiveStatus"]) : "";
                        model.BuilderName = dt.Rows[0]["BuilderName"] != DBNull.Value || dt.Rows[0]["BuilderName"] != "0" ?Convert.ToString(dt.Rows[0]["BuilderName"]) : "";
                      //  model.CustomerName = dt.Rows[0]["CustomerName"] != DBNull.Value || dt.Rows[0]["CustomerName"] != "0" ?Convert.ToString(dt.Rows[0]["CustomerName"]) : "";
                       // model.LocationName = dt.Rows[0]["LocationName"] != DBNull.Value || dt.Rows[0]["LocationName"] != "0" ?Convert.ToString(dt.Rows[0]["LocationName"]) : "";
                        model.ProjectName = dt.Rows[0]["ProjectName"] != DBNull.Value || dt.Rows[0]["ProjectName"] != "0" ?Convert.ToString(dt.Rows[0]["ProjectName"]) : "";
                        model.SiteName = dt.Rows[0]["SiteNameLoc"] != DBNull.Value || dt.Rows[0]["SiteNameLoc"] != "0" ?Convert.ToString(dt.Rows[0]["SiteNameLoc"]) : "";
                        model.RERANumber = dt.Rows[0]["ReraNo"] != DBNull.Value || dt.Rows[0]["ReraNo"] != "0" ?Convert.ToString(dt.Rows[0]["ReraNo"]) : "";
                        model.CustomerReference = dt.Rows[0]["ReferenceCustomer"] != DBNull.Value || dt.Rows[0]["ReferenceCustomer"] != "0" ?Convert.ToString(dt.Rows[0]["ReferenceCustomer"]) : "";
                        //   model.CustFrequency = dt.Rows[0]["CustFrequency"] != DBNull.Value || dt.Rows[0]["CustFrequency"] != "0" ?Convert.ToString(dt.Rows[0]["CustFrequency"]) : "";
                        DateTime? DateofEnquiry = null;
                        if (dt.Rows[0]["EnquiryDate"] != DBNull.Value)
                            DateofEnquiry = Convert.ToDateTime(dt.Rows[0]["EnquiryDate"]);
                        DateTime? DeliveryFrom = null;
                        if (dt.Rows[0]["DeliveryFrom"] != DBNull.Value)
                            DeliveryFrom = Convert.ToDateTime(dt.Rows[0]["DeliveryFrom"]);
                        DateTime? DeliveryTo = null;
                        if (dt.Rows[0]["DeliveryTo"] != DBNull.Value)
                            DeliveryTo = Convert.ToDateTime(dt.Rows[0]["DeliveryTo"]);
                        DateTime? LiftingDate = null;
                        if (dt.Rows[0]["FirstLiftingD"] != DBNull.Value)
                            LiftingDate = Convert.ToDateTime(dt.Rows[0]["FirstLiftingD"]);
                        DateTime? ProDisValid = null;
                        if (dt.Rows[0]["ProjectDiscountValid"] != DBNull.Value)
                            ProDisValid = Convert.ToDateTime(dt.Rows[0]["ProjectDiscountValid"]);

                        model.DateofEnquiry = DateofEnquiry;

                        model.OrderValue = dt.Rows[0]["ApproxOrderValue"] != DBNull.Value || dt.Rows[0]["ApproxOrderValue"] != "0" ?Convert.ToString(dt.Rows[0]["ApproxOrderValue"]) : "";
                        model.DateFrom = DeliveryFrom;
                        model.DateTo = DeliveryTo;
                        model.PreferredPlant = dt.Rows[0]["PreferredPlant"] != DBNull.Value || dt.Rows[0]["PreferredPlant"] != "0" ?Convert.ToString(dt.Rows[0]["PreferredPlant"]) : "";
                        model.FirstLifting = dt.Rows[0]["FirstLiftingV"] != DBNull.Value || dt.Rows[0]["FirstLiftingV"] != "0" ?Convert.ToString(dt.Rows[0]["FirstLiftingV"]) : "";
                        model.LiftingDate = LiftingDate;
                        model.POCRRequest = dt.Rows[0]["POCRRequest"] != DBNull.Value || dt.Rows[0]["POCRRequest"] != "0" ?Convert.ToString(dt.Rows[0]["POCRRequest"]) : "";
                        model.AddDisMaterial = dt.Rows[0]["AddDiscountPerMaterial"] != DBNull.Value || dt.Rows[0]["AddDiscountPerMaterial"] != "0" ?Convert.ToString(dt.Rows[0]["AddDiscountPerMaterial"]) : "";
                        model.ProDisValid = ProDisValid;
                        model.CreditReq = dt.Rows[0]["CreditRequired"] != DBNull.Value || dt.Rows[0]["CreditRequired"] != "0" ?Convert.ToString(dt.Rows[0]["CreditRequired"]) : "";
                        model.CreditPer = dt.Rows[0]["CreditPeriod"] != DBNull.Value || dt.Rows[0]["CreditPeriod"] != "0" ?Convert.ToString(dt.Rows[0]["CreditPeriod"]) : "";
                        model.InterstCost = dt.Rows[0]["InterestCost"] != DBNull.Value || dt.Rows[0]["InterestCost"] != "0" ?Convert.ToString(dt.Rows[0]["InterestCost"]) : "";
                        model.CreditLimit = dt.Rows[0]["CreditLimit"] != DBNull.Value || dt.Rows[0]["CreditLimit"] != "0" ?Convert.ToString(dt.Rows[0]["CreditLimit"]) : "";
                        model.CustOverview = dt.Rows[0]["BriefCustomer"] != DBNull.Value || dt.Rows[0]["BriefCustomer"] != "0" ?Convert.ToString(dt.Rows[0]["BriefCustomer"]) : "";
                        model.WhyProject = dt.Rows[0]["WhyProject"] != DBNull.Value || dt.Rows[0]["WhyProject"] != "0" ?Convert.ToString(dt.Rows[0]["WhyProject"]) : "";
                        model.CompName = dt.Rows[0]["CompetitorName"] != DBNull.Value || dt.Rows[0]["CompetitorName"] != "0" ?Convert.ToString(dt.Rows[0]["CompetitorName"]) : "";
                        model.Invoice = dt.Rows[0]["Quotation"] != DBNull.Value || dt.Rows[0]["Quotation"] != "0" ?Convert.ToString(dt.Rows[0]["Quotation"]) : "";
                        model.Freight = dt.Rows[0]["FrieghtValue"] != DBNull.Value || dt.Rows[0]["FrieghtValue"] != "0" ?Convert.ToString(dt.Rows[0]["FrieghtValue"]) : "";
                        model.TruckVol = dt.Rows[0]["TruckVolume"] != DBNull.Value || dt.Rows[0]["TruckVolume"] != "0" ?Convert.ToString(dt.Rows[0]["TruckVolume"]) : "";
                        model.Place = dt.Rows[0]["Place"] != DBNull.Value || dt.Rows[0]["Place"] != "0" ?Convert.ToString(dt.Rows[0]["Place"]) : "";
                        model.VehValue = dt.Rows[0]["Vehiclevalue"] != DBNull.Value || dt.Rows[0]["Vehiclevalue"] != "0" ?Convert.ToString(dt.Rows[0]["Vehiclevalue"]) : "";
                        model.ID = dt.Rows[0]["ID"] != DBNull.Value || dt.Rows[0]["ID"] != "0" ?Convert.ToInt32(dt.Rows[0]["ID"]) : 0;
                        model.FormSrId = dt.Rows[0]["FormID"] != DBNull.Value || dt.Rows[0]["FormID"] != "0" ?Convert.ToString(dt.Rows[0]["FormID"]) : "";
                        model.TSELocation = dt.Rows[0]["TSELoction"] != DBNull.Value || dt.Rows[0]["TSELoction"] != "0" ?Convert.ToString(dt.Rows[0]["TSELoction"]) : "";
                        model.ProjectBrochure = dt.Rows[0]["TSELoction"] != DBNull.Value || dt.Rows[0]["TSELoction"] != "0" ?Convert.ToString(dt.Rows[0]["TSELoction"]) : "";

                        item.Add(model);
                    }
                }
                URCFData.one = item;
                Log.Error(item.Count.ToString());
                List<POCRList> OtherList = new List<POCRList>();
                SqlCommand cmd1 = new SqlCommand();
                SqlDataAdapter adapter1 = new SqlDataAdapter();
                DataTable ds1 = new DataTable();
                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("GetPOCRDDetails", con);
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                // cmd.Parameters.Add(new SqlParameter("@FutureOwnerEmail", model.FutureOwnerEmail));
                cmd.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = cmd;
                con.Open();
                adapter.Fill(ds1);
                con.Close();
                if (ds1.Rows.Count > 0)
                {

                    for (int j = 0; j < ds1.Rows.Count; j++)
                    {
                        POCRList model1 = new POCRList();
                        model1.ProductID = ds1.Rows[j]["ProductID"] != DBNull.Value || ds1.Rows[j]["ProductID"] != "0" ? Convert.ToInt32(ds1.Rows[j]["ProductID"]) : 0;
                        model1.Value = ds1.Rows[j]["Value"] != DBNull.Value || ds1.Rows[j]["Value"] != "0" ? Convert.ToInt32(ds1.Rows[j]["Value"]) : 0;
                        model1.MaterialDes = ds1.Rows[j]["MaterialDes"] != DBNull.Value || ds1.Rows[j]["MaterialDes"] != "0" ? Convert.ToString(ds1.Rows[j]["MaterialDes"]) : "";
                        model1.ListPrice = ds1.Rows[j]["ListPrice"] != DBNull.Value || ds1.Rows[j]["ListPrice"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ListPrice"]) : 0;
                        model1.LessStdDis = ds1.Rows[j]["LessStdDis"] != DBNull.Value || ds1.Rows[j]["LessStdDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["LessStdDis"]) : 0;
                        model1.LessPriceAftStdDis = ds1.Rows[j]["LessPriceAftStdDis"] != DBNull.Value || ds1.Rows[j]["LessPriceAftStdDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["LessPriceAftStdDis"]) : 0;
                        model1.LessAddDis = ds1.Rows[j]["LessAddDis"] != DBNull.Value || ds1.Rows[j]["LessAddDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["LessAddDis"]) : 0;
                        model1.PriceAftAddDis = ds1.Rows[j]["PriceAftAddDis"] != DBNull.Value || ds1.Rows[j]["PriceAftAddDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftAddDis"]) : 0;
                        model1.OTLSchemeDis = ds1.Rows[j]["OTLSchemeDis"] != DBNull.Value || ds1.Rows[j]["OTLSchemeDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["OTLSchemeDis"]) : 0;
                        model1.PriceAftOTLSDis = ds1.Rows[j]["PriceAftOTLSDis"] != DBNull.Value || ds1.Rows[j]["PriceAftOTLSDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftOTLSDis"]) : 0;
                        model1.TruckScheme = ds1.Rows[j]["TruckScheme"] != DBNull.Value || ds1.Rows[j]["TruckScheme"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["TruckScheme"]) : 0;
                        model1.PriceAftTruckSch = ds1.Rows[j]["PriceAftTruckSch"] != DBNull.Value || ds1.Rows[j]["PriceAftTruckSch"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftTruckSch"]) : 0;
                        model1.DealerSpecific = ds1.Rows[j]["DealerSpecific"] != DBNull.Value || ds1.Rows[j]["DealerSpecific"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DealerSpecific"]) : 0;
                        model1.PriceDealerSpecific = ds1.Rows[j]["PriceDealerSpecific"] != DBNull.Value || ds1.Rows[j]["PriceDealerSpecific"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceDealerSpecific"]) : 0;
                        model1.GST = ds1.Rows[j]["GST"] != DBNull.Value || ds1.Rows[j]["GST"] != "0" ? Convert.ToInt32(ds1.Rows[j]["GST"]) : 0;
                        model1.GSTPrice = ds1.Rows[j]["GSTPrice"] != DBNull.Value || ds1.Rows[j]["GSTPrice"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["GSTPrice"]) : 0;
                        model1.PrimaryFreight = ds1.Rows[j]["PrimaryFreight"] != DBNull.Value || ds1.Rows[j]["PrimaryFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PrimaryFreight"]) : 0;
                        model1.PricePriFreight = ds1.Rows[j]["PricePriFreight"] != DBNull.Value || ds1.Rows[j]["PricePriFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PricePriFreight"]) : 0;
                        model1.SecondaryFreight = ds1.Rows[j]["SecondaryFreight"] != DBNull.Value || ds1.Rows[j]["SecondaryFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["SecondaryFreight"]) : 0;
                        model1.PriceSecFreight = ds1.Rows[j]["PriceSecFreight"] != DBNull.Value || ds1.Rows[j]["PriceSecFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceSecFreight"]) : 0;
                        model1.COIForCredit = ds1.Rows[j]["COIForCredit"] != DBNull.Value || ds1.Rows[j]["COIForCredit"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["COIForCredit"]) : 0;
                        model1.PriceCOIForCredit = ds1.Rows[j]["PriceCOIForCredit"] != DBNull.Value || ds1.Rows[j]["PriceCOIForCredit"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceCOIForCredit"]) : 0;
                        model1.DealerLanding = ds1.Rows[j]["DealerLanding"] != DBNull.Value || ds1.Rows[j]["DealerLanding"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DealerLanding"]) : 0;
                        model1.DealerMargin = ds1.Rows[j]["DealerMargin"] != DBNull.Value || ds1.Rows[j]["DealerMargin"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DealerMargin"]) : 0;
                        model1.BillRateBuild = ds1.Rows[j]["BillRateBuild"] != DBNull.Value || ds1.Rows[j]["BillRateBuild"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["BillRateBuild"]) : 0;
                        model1.DiffBtnBRBAndDL = ds1.Rows[j]["DiffBtnBRBAndDL"] != DBNull.Value || ds1.Rows[j]["DiffBtnBRBAndDL"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DiffBtnBRBAndDL"]) : 0;
                        model1.ProjectDis = ds1.Rows[j]["ProjectDis"] != DBNull.Value || ds1.Rows[j]["ProjectDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ProjectDis"]) : 0;
                        model1.ActDiscount = ds1.Rows[j]["ActDiscount"] != DBNull.Value || ds1.Rows[j]["ActDiscount"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ActDiscount"]) : 0;
                        model1.ActMargin = ds1.Rows[j]["ActMargin"] != DBNull.Value || ds1.Rows[j]["ActMargin"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ActMargin"]) : 0;
                        model1.PriceAftQuotDis = ds1.Rows[j]["PriceAftQuotDis"] != DBNull.Value || ds1.Rows[j]["PriceAftQuotDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftQuotDis"]) : 0;
                        model1.PriceAftSchDis = ds1.Rows[j]["PriceAftSchDis"] != DBNull.Value || ds1.Rows[j]["PriceAftSchDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftSchDis"]) : 0;
                        model1.PriceAftDealerBillAnnDis = ds1.Rows[j]["PriceAftDealerBillAnnDis"] != DBNull.Value || ds1.Rows[j]["PriceAftDealerBillAnnDis"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["PriceAftDealerBillAnnDis"]) : 0;
                        model1.ExeGSTPrice = ds1.Rows[j]["ExeGSTPrice"] != DBNull.Value || ds1.Rows[j]["ExeGSTPrice"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeGSTPrice"]) : 0;
                        model1.ExePricePriFreight = ds1.Rows[j]["ExePricePriFreight"] != DBNull.Value || ds1.Rows[j]["ExePricePriFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExePricePriFreight"]) : 0;
                        model1.ExePriceSecFreight = ds1.Rows[j]["ExePriceSecFreight"] != DBNull.Value || ds1.Rows[j]["ExePriceSecFreight"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExePriceSecFreight"]) : 0;
                        model1.ExePriceCOIForCredit = ds1.Rows[j]["ExePriceCOIForCredit"] != DBNull.Value || ds1.Rows[j]["ExePriceCOIForCredit"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExePriceCOIForCredit"]) : 0;
                        model1.ExeDealerLanding = ds1.Rows[j]["ExeDealerLanding"] != DBNull.Value || ds1.Rows[j]["ExeDealerLanding"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeDealerLanding"]) : 0;
                        model1.ExeBillRateBuild = ds1.Rows[j]["ExeBillRateBuild"] != DBNull.Value || ds1.Rows[j]["ExeBillRateBuild"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeBillRateBuild"]) : 0;
                        model1.BillRateBuildInclDealMargin = ds1.Rows[j]["BillRateBuildInclDealMargin"] != DBNull.Value || ds1.Rows[j]["BillRateBuildInclDealMargin"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["BillRateBuildInclDealMargin"]) : 0;
                        model1.CompetitorLP = ds1.Rows[j]["CompetitorLP"] != DBNull.Value || ds1.Rows[j]["CompetitorLP"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["CompetitorLP"]) : 0;
                        model1.CompetitorQuote = ds1.Rows[j]["CompetitorQuote"] != DBNull.Value || ds1.Rows[j]["CompetitorQuote"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["CompetitorQuote"]) : 0;
                        model1.ExeDiffBtnBRBAndDL = ds1.Rows[j]["ExeDiffBtnBRBAndDL"] != DBNull.Value || ds1.Rows[j]["ExeDiffBtnBRBAndDL"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["ExeDiffBtnBRBAndDL"]) : 0;
                        model1.DiffBtnBRBFVC = ds1.Rows[j]["DiffBtnBRBFVC"] != DBNull.Value || ds1.Rows[j]["DiffBtnBRBFVC"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DiffBtnBRBFVC"]) : 0;
                        model1.DiffWOPOCR = ds1.Rows[j]["DiffWOPOCR"] != DBNull.Value || ds1.Rows[j]["DiffWOPOCR"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["DiffWOPOCR"]) : 0;
                        model1.WeightAvgWOPOCR = ds1.Rows[j]["WeightAvgWOPOCR"] != DBNull.Value || ds1.Rows[j]["WeightAvgWOPOCR"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["WeightAvgWOPOCR"]) : 0;
                        model1.WeightAvgWPOCR = ds1.Rows[j]["WeightAvgWPOCR"] != DBNull.Value || ds1.Rows[j]["WeightAvgWPOCR"] != "0" ? Convert.ToDecimal(ds1.Rows[j]["WeightAvgWPOCR"]) : 0;
                        model1.SrNo = ds1.Rows[j]["SrNo"] != DBNull.Value || ds1.Rows[j]["SrNo"] != "0" ? Convert.ToString(ds1.Rows[j]["SrNo"]) : "";
                        model1.ProductName = ds1.Rows[j]["ProductName"] != DBNull.Value || ds1.Rows[j]["ProductName"] != "0" ? Convert.ToString(ds1.Rows[j]["ProductName"]) : "";

                        OtherList.Add(model1);

                    }

                }
                URCFData.two = OtherList;
                //approval start
                var (r1, r2) = await GetApproversData(user, rowId, formId);
                if (r1.Status == 500)
                    return r1;
                else if (r2.Status == 500)
                    return r2;
                URCFData.three = r1.Model;
                URCFData.four = r2.Model;
                //approval end
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return 0;
            }
            return URCFData;
        }

        public async Task<ResponseModel<object>> SavePOCR(POCRFormModel BasicDetails, DataTable ItemdataTable, UserData user, HttpPostedFileBase files)
        {
            ResponseModel<object> result = new ResponseModel<object>();

            int RowId = 0;
            string formShortName = "POCRF";
            //string formName = "AnalysisPartsFormPresentation";
            string formName = "POCRForm";
            var listName = GlobalClass.ListNames.ContainsKey(formShortName) ? GlobalClass.ListNames[formShortName] : "";
            if (listName == "")
            {
                result.Status = 500;
                result.Message = "List not found.";
                return result;
            }
            int prevItemId = Convert.ToInt32(BasicDetails.FormSrId);
            DateTime tempDate = new DateTime(1500, 1, 1);
            int formId = 0;
            formId = Convert.ToInt32(BasicDetails.FormId);
            bool IsResubmit = formId == 0 ? false : true;
            int AppRowId = Convert.ToInt32(BasicDetails.AppRowId);


            var requestSubmissionFor = BasicDetails.RequestSubmissionFor;
            var otherEmpType = BasicDetails.OnBehalfOption ?? "";
            bool isSelf = requestSubmissionFor == "Self", isSAVWIPL = otherEmpType == "SAVWIPLEmployee";
            long ccNum = user.CostCenter;
            long empNum = user.EmpNumber;
            string empDes = BasicDetails.EmployeeDesignation;
            var empLocName = isSelf ? Convert.ToString(BasicDetails.EmployeeLocation) : (isSAVWIPL ? Convert.ToString(BasicDetails.OtherEmployeeLocation) : Convert.ToString(BasicDetails.OtherNewEmpLocation));
            var locations = await new ListDAL().GetLocations();
            if (locations == null && locations.Count <= 0)
            {
                result.Status = 500;
                result.Message = "There were some issue fetching Location data.";
                return result;
            }
            var locObj = locations.Find(x => x.LocationName == empLocName);
            if (locObj == null)
            {
                result.Status = 500;
                result.Message = "Could not found location data.";
                return result;
            }

            long empLoc = locObj.LocationId == 1 || locObj.LocationId == 3 ? locObj.LocationId : 2;
            decimal totalvalue = BasicDetails.TotalValue;
            decimal finalperc = BasicDetails.FinalPercent;
            var response = await GetPOCRApprovers(totalvalue, ccNum, finalperc);
            if (response.Status != 200 && (response.Model == null || response.Model.Count == 0))
            {
                result.Status = 500;
                result.Message = response.Message;
                return result;
            }

            var approvers = response.Model;

            SqlCommand cmd_form = new SqlCommand();
            SqlDataAdapter adapter_form = new SqlDataAdapter();
            DataSet ds_form = new DataSet();
            try
            {
                if (formId == 0)
                {
                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveDataInForm", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                    cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                    cmd_form.Parameters.Add(new SqlParameter("@Created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@CreatedBy", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@ListName", listName));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterId", DBNull.Value));
                    if (formId == 0)
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Submitted"));
                    }
                    else
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Status", "Resubmitted"));
                    }
                    cmd_form.Parameters.Add(new SqlParameter("@UniqueFormName", formShortName));
                    if (requestSubmissionFor == "Self")
                    {
                        cmd_form.Parameters.Add(new SqlParameter("@Location", BasicDetails.EmployeeLocation));
                    }
                    else
                    {
                        if (otherEmpType == "SAVWIPLEmployee")
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", BasicDetails.OtherEmployeeLocation));
                        }
                        else
                        {
                            cmd_form.Parameters.Add(new SqlParameter("@Location", BasicDetails.OtherNewEmpLocation));
                        }

                    }

                    cmd_form.Parameters.Add(new SqlParameter("@Modified", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@TimeStamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd_form.Parameters.Add(new SqlParameter("@Department", user.Department));
                    cmd_form.Parameters.Add(new SqlParameter("@DataRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@ControllerName", "POCRForm"));
                    cmd_form.Parameters.Add(new SqlParameter("@Title", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", DBNull.Value));
                    cmd_form.Parameters.Add(new SqlParameter("@BusinessNeed", ""));
                    cmd_form.Parameters.Add(new SqlParameter("@SubmitterUserName", user.UserName));
                    cmd_form.Parameters.Add(new SqlParameter("@FormParentId", 51));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con_form.Open();
                    adapter_form.Fill(ds_form);
                    con_form.Close();

                    if (ds_form.Tables[0].Rows.Count > 0 && ds_form.Tables[0] != null)
                    {
                        for (int i = 0; i < ds_form.Tables[0].Rows.Count; i++)
                        {
                            formId = Convert.ToInt32(ds_form.Tables[0].Rows[i]["FormID"]);
                        }
                    }
                }
                else
                {
                    ListDAL dal = new ListDAL();
                    //var resubmitResult = await dal.ResubmitUpdate(formId);

                    DataTable dt1 = new DataTable();
                    var con_form = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_updateFlagInApprovalMaster", con_form);
                    cmd_form.Parameters.Add(new SqlParameter("@formId", formId));
                    cmd_form.Parameters.Add(new SqlParameter("@AppRowId", AppRowId));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con_form.Open();
                    adapter_form.Fill(dt1);
                    con_form.Close();

                    if (dt1.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt1.Rows.Count; i++)
                        {
                            new ResponseModel<object> { Message = Convert.ToString(dt1.Rows[i]["message"]), Status = Convert.ToInt32(dt1.Rows[i]["Status"]) };
                        }
                    }


                }

                var userDetailsResponse = SaveSubmitterAndApplicantDetailsModelData(BasicDetails, "POCRM", formId);
                if (userDetailsResponse.Status != 200)
                {
                    return new ResponseModel<object> { Message = userDetailsResponse.Message, Status = userDetailsResponse.Status };
                }
                RowId = Convert.ToInt32(userDetailsResponse.RowId);
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds = new DataSet();

                con = new SqlConnection(sqlConString);
                cmd = new SqlCommand("USP_UpdatePOCRM", con);
                cmd.Parameters.Add(new SqlParameter("@POCRID", BasicDetails.POCRID));
                cmd.Parameters.Add(new SqlParameter("@POCRNo", BasicDetails.POCRNo));
                cmd.Parameters.Add(new SqlParameter("@TSEName", BasicDetails.TSEName));
                cmd.Parameters.Add(new SqlParameter("@ZSMName", BasicDetails.ZSMName));
                cmd.Parameters.Add(new SqlParameter("@ASMName", BasicDetails.ASMName));
                cmd.Parameters.Add(new SqlParameter("@TSELocation", BasicDetails.TSELocation));
                cmd.Parameters.Add(new SqlParameter("@State", BasicDetails.State));
                cmd.Parameters.Add(new SqlParameter("@DealerCode", BasicDetails.DealerCode));
                cmd.Parameters.Add(new SqlParameter("@DealerName", BasicDetails.DealerName));
                cmd.Parameters.Add(new SqlParameter("@DealerLocation", BasicDetails.DealerLocation));
                cmd.Parameters.Add(new SqlParameter("@FILDealership", BasicDetails.FILDealership));
                cmd.Parameters.Add(new SqlParameter("@DealerSalesLastYr", BasicDetails.DealerSalesLastYr));
                cmd.Parameters.Add(new SqlParameter("@DealerSalesTill", BasicDetails.DealerSalesTill));
                cmd.Parameters.Add(new SqlParameter("@Status", BasicDetails.Status));
                cmd.Parameters.Add(new SqlParameter("@BuilderName", BasicDetails.BuilderName));
                cmd.Parameters.Add(new SqlParameter("@ProjectName", BasicDetails.ProjectName));
                cmd.Parameters.Add(new SqlParameter("@SiteName", BasicDetails.SiteName));
                cmd.Parameters.Add(new SqlParameter("@RERANumber", BasicDetails.RERANumber));
                cmd.Parameters.Add(new SqlParameter("@ProjectBrochure", BasicDetails.ProjectBrochure));
                cmd.Parameters.Add(new SqlParameter("@ProjectWebsite", BasicDetails.ProjectWebsite));
                cmd.Parameters.Add(new SqlParameter("@CustomerReference", BasicDetails.CustomerReference));
                cmd.Parameters.Add(new SqlParameter("@DateofEnquiry", BasicDetails.DateofEnquiry));
                cmd.Parameters.Add(new SqlParameter("@OrderValue", BasicDetails.OrderValue));
                cmd.Parameters.Add(new SqlParameter("@PreferredPlant", BasicDetails.PreferredPlant));
                cmd.Parameters.Add(new SqlParameter("@DateFrom", BasicDetails.DateFrom));
                cmd.Parameters.Add(new SqlParameter("@DateTo", BasicDetails.DateTo));
                cmd.Parameters.Add(new SqlParameter("@FirstLifting", BasicDetails.FirstLifting));
                cmd.Parameters.Add(new SqlParameter("@LiftingDate", BasicDetails.LiftingDate));
                cmd.Parameters.Add(new SqlParameter("@POCRRequest", BasicDetails.POCRRequest));
                cmd.Parameters.Add(new SqlParameter("@AddDisMaterial", BasicDetails.AddDisMaterial));
                cmd.Parameters.Add(new SqlParameter("@ProDisValid", BasicDetails.ProDisValid));
                cmd.Parameters.Add(new SqlParameter("@CreditReq", BasicDetails.CreditReq));
                cmd.Parameters.Add(new SqlParameter("@CreditPer", BasicDetails.CreditPer));
                cmd.Parameters.Add(new SqlParameter("@InterstCost", BasicDetails.InterstCost));
                cmd.Parameters.Add(new SqlParameter("@CreditLimit", BasicDetails.CreditLimit));
                cmd.Parameters.Add(new SqlParameter("@CustOverview", BasicDetails.CustOverview));
                cmd.Parameters.Add(new SqlParameter("@WhyProject", BasicDetails.WhyProject));
                cmd.Parameters.Add(new SqlParameter("@CompName", BasicDetails.CompName));
                cmd.Parameters.Add(new SqlParameter("@Invoice", BasicDetails.Invoice));
                cmd.Parameters.Add(new SqlParameter("@Freight", BasicDetails.Freight));
                cmd.Parameters.Add(new SqlParameter("@TruckVol", BasicDetails.TruckVol));
                cmd.Parameters.Add(new SqlParameter("@Place", BasicDetails.Place));
                cmd.Parameters.Add(new SqlParameter("@VehValue", BasicDetails.VehValue));
                cmd.Parameters.Add(new SqlParameter("@FormID", formId));
                cmd.Parameters.Add(new SqlParameter("@RowId", userDetailsResponse.RowId));

                cmd.CommandType = CommandType.StoredProcedure;
                if (ItemdataTable != null)
                {
                    SqlParameter param1 = new SqlParameter();
                    param1.ParameterName = "@PT_SaveListItem";
                    param1.Value = ItemdataTable;
                    param1.TypeName = "PT_SaveListItem";
                    param1.SqlDbType = SqlDbType.Structured;
                    cmd.Parameters.Add(param1);
                }
                string path = "";
                if (files != null)
                {
                    path = System.Web.HttpContext.Current.Server.MapPath("~/Attachment/ISCLSCF/" + formId + "/");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    else
                    {
                        Directory.Delete(path, recursive: true);
                        Directory.CreateDirectory(path);
                    }
                    files.SaveAs(path + Path.GetFileName(files.FileName));
                    path = "~/Attachment/POCRF/" + formId + "/" + files.FileName;
                    cmd_form = new SqlCommand();
                    DataSet ds2 = new DataSet();
                    con = new SqlConnection(sqlConString);
                    cmd_form = new SqlCommand("USP_SaveFormAttachment", con);
                    cmd_form.Parameters.Add(new SqlParameter("@RowId", RowId == null || RowId == 0 ? 0 : Convert.ToInt64(RowId)));
                    cmd_form.Parameters.Add(new SqlParameter("@Path", path == null || path == "" ? "" : path));
                    cmd_form.Parameters.Add(new SqlParameter("@TableName", "POCRM"));
                    cmd_form.CommandType = CommandType.StoredProcedure;
                    adapter_form.SelectCommand = cmd_form;
                    con.Open();
                    adapter_form.Fill(ds2);
                    con.Close();
                }

                adapter.SelectCommand = cmd;
                con.Close();
                con.Open();
                adapter.Fill(ds);
                con.Close();
                result.Status = 200;
                result.Message = formId.ToString();


                var approverIdList = response.Model;


                var approvalResponse = await SaveDataApprovalMasterData(approverIdList, BasicDetails.BusinessNeed ?? "", RowId, formId);
                if (approvalResponse.Status != 200 && approvalResponse.Model == null)
                {
                    return approvalResponse;
                }

                var updateRowResponse = UpdateDataRowIdInFormsList(RowId, formId);
                if (updateRowResponse.Status != 200 && updateRowResponse.Model == null)
                {
                    return updateRowResponse;
                }

                //email
                ListDAL listDal = new ListDAL();
                var userList = await listDal.GetSubmitterDetails(formId, formShortName, RowId);
                foreach (var approver in approverIdList)
                {
                    var data = new UserData()
                    {
                        EmployeeName = approver.FName + " " + approver.LName,
                        Email = approver.EmailId,
                        ApprovalLevel = approver.ApprovalLevel,
                        IsApprover = true
                    };
                    userList.Add(data);
                }

                //status Approved


                var emailData = new EmailDataModel()
                {
                    FormId = formId.ToString(),
                    Action = IsResubmit ? FormStates.ReSubmit : FormStates.Submit,
                    //Action = FormStates.FinalApproval,
                    Recipients = userList.Where(p => p.ApprovalLevel == 1).ToList(),
                    UniqueFormName = formShortName,
                    Sender = userList.Where(p => !p.IsOnBehalf && !p.IsApprover).FirstOrDefault(),
                    OnBehalfSender = userList.Where(p => p.IsOnBehalf).FirstOrDefault(),
                    FormName = formName,
                    CurrentUser = user
                };

                var emailService = new EmailService();
                emailService.SendMail(emailData);

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                result.Status = 500;
                result.Message = "There were some issue while saving form data.";
                return result;
            }

            return result;
        }


        public List<ProductM> GetItemList(int Type, string Text, int ID)
        {
            try
            {
                DataTable ItemList = new DataTable();
                SqlCommand cmd_form = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                var con_form = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_ProductSummaryList", con_form);
                //cmd_form.Parameters.Add(new SqlParameter("@formID", formId));
                //cmd_form.Parameters.Add(new SqlParameter("@FormName", formName));
                adapter.SelectCommand = cmd_form;
                con_form.Open();
                adapter.Fill(ItemList);
                con_form.Close();

                List<ProductM> enqBL = new List<ProductM>();
                int i = 0;
                if (ItemList != null)
                {
                    foreach (DataRow row in ItemList.Rows)
                    {
                        ProductM enq = new ProductM();
                        i++;

                        enq.SrNo = i;
                        enq.ProductId = Convert.ToInt32(row["ID"]);
                        enq.ProductName = Convert.ToString(row["ProductName"]);
                        enq.ProductPrice = Convert.ToDecimal(row["Price"]);

                        enqBL.Add(enq);
                    }
                }
                return enqBL;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<ResponseModel<List<ApprovalMatrix>>> GetPOCRApprovers(decimal empNum, long filledForEmpNum, decimal empLoc)
        {
            List<ApprovalMatrix> list = new List<ApprovalMatrix>();
            try
            {
                SqlCommand sqlCommand = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet data = new DataSet();
                List<ApprovalMatrix> appList = new List<ApprovalMatrix>();
                SqlConnection con = new SqlConnection(sqlConString);
                sqlCommand = new SqlCommand("sp_GetPOCRFormApproval", con);
                sqlCommand.Parameters.Add(new SqlParameter("@AgriOrder", empNum));
                sqlCommand.Parameters.Add(new SqlParameter("@NonAgriOrder", filledForEmpNum));
                sqlCommand.Parameters.Add(new SqlParameter("@Percentage", empLoc));
                sqlCommand.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand = sqlCommand;
                con.Open();
                adapter.Fill(data);
                con.Close();

                if (data.Tables[0].Rows.Count > 0 && data.Tables[0] != null)
                {
                    for (int i = 0; i < data.Tables[0].Rows.Count; i++)
                    {
                        ApprovalMatrix app = new ApprovalMatrix();
                        app.EmpNumber = Convert.ToInt32(data.Tables[0].Rows[i]["EmployeeNumber"]);
                        app.FName = Convert.ToString(data.Tables[0].Rows[i]["FirstName"]);
                        app.LName = Convert.ToString(data.Tables[0].Rows[i]["LastName"]);
                        app.EmailId = Convert.ToString(data.Tables[0].Rows[i]["EmailId"]);
                        app.Designation = Convert.ToString(data.Tables[0].Rows[i]["desg"]);
                        app.ApprovalLevel = (int)data.Tables[0].Rows[i]["approvalLevel"];
                        app.Logic = Convert.ToString(data.Tables[0].Rows[i]["logic"]);
                        if (data.Tables[0].Columns.Contains("Contents"))
                            app.ExtraDetails = Convert.ToString(data.Tables[0].Rows[i]["Contents"]);
                        if (data.Tables[0].Columns.Contains("LogicId"))
                            app.LogicId = Convert.ToInt64(data.Tables[0].Rows[i]["LogicId"]);
                        if (data.Tables[0].Columns.Contains("LogicWith"))
                            app.LogicWith = Convert.ToInt64(data.Tables[0].Rows[i]["LogicWith"]);
                        if (data.Tables[0].Columns.Contains("RelationId"))
                            app.RelationId = Convert.ToInt64(data.Tables[0].Rows[i]["RelationId"]);
                        if (data.Tables[0].Columns.Contains("RelationWith"))
                            app.RelationWith = Convert.ToInt64(data.Tables[0].Rows[i]["RelationWith"]);
                        if (data.Tables[0].Columns.Contains("UserName"))
                            app.ApproverUserName = Convert.ToString(data.Tables[0].Rows[i]["UserName"]);
                        if (data.Tables[0].Columns.Contains("UName"))
                            app.ApproverName = Convert.ToString(data.Tables[0].Rows[i]["UName"]);
                        appList.Add(app);
                    }
                }
                //if (data.Tables[0].Rows.Count < 2)
                //{
                //    return new ResponseModel<List<ApprovalMatrix>>
                //    {
                //        Model = appList,
                //        Status = 500,
                //        Message = $"Employee Number {(appList.Any(x => x.EmpNumber == empNum) ? filledForEmpNum : empNum)} approver data not found."
                //    };
                //}
                return new ResponseModel<List<ApprovalMatrix>> { Model = appList, Status = 200, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new ResponseModel<List<ApprovalMatrix>> { Model = new List<ApprovalMatrix>(), Status = 500, Message = "Error while fetching approver data." };
            }
        }


        public async Task<ResponseModel<object>> SaveAttachmentPOCR(int formId, int rowID, UserData user, HttpPostedFileBase files)
        {
            ResponseModel<object> result = new ResponseModel<object>();
            SqlCommand cmd_form = new SqlCommand();
            SqlDataAdapter adapter_form = new SqlDataAdapter();
            DataSet ds_form = new DataSet();
            string path = "";
            if (files != null)
            {
                path = System.Web.HttpContext.Current.Server.MapPath("~/Attachment/POCR/" + formId + "/");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                else
                {
                    Directory.Delete(path, recursive: true);
                    Directory.CreateDirectory(path);
                }
                files.SaveAs(path + Path.GetFileName(files.FileName));
                path = "~/Attachment/POCRF/" + formId + "/" + files.FileName;
                cmd_form = new SqlCommand();
                DataSet ds2 = new DataSet();
                con = new SqlConnection(sqlConString);
                cmd_form = new SqlCommand("USP_SaveFormAttachment", con);
                cmd_form.Parameters.Add(new SqlParameter("@TableName", "POCRM"));
                cmd_form.Parameters.Add(new SqlParameter("@RowId", rowID));
                cmd_form.Parameters.Add(new SqlParameter("@Path", path == null || path == "" ? "" : path));

                cmd_form.CommandType = CommandType.StoredProcedure;
                adapter_form.SelectCommand = cmd_form;
                con.Open();
                adapter_form.Fill(ds2);
                con.Close();
            }
            result.Status = 200;
            result.Message = "Attachment saved successfully.";
            return result;
        }
    }
}
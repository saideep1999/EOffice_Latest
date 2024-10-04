using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Skoda_DCMS.DAL;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class POCRFormController : BaseController
    {
        // GET: POCRForm
        POCRFormDAL POCr;
        public ActionResult Index()
        {
            return View();
        }


        public async Task<ActionResult> SavePOCR(POCRFormModel BasicDetails, List<POCRList> ListDetails)
        {
   
            #region ListDetails
            POCr = new POCRFormDAL();
            DataTable ItemdataTable = new DataTable();

            ItemdataTable.Columns.Add("ProductID");
            ItemdataTable.Columns.Add("Value");
            ItemdataTable.Columns.Add("MaterialDes");
            ItemdataTable.Columns.Add("ListPrice");
            ItemdataTable.Columns.Add("LessStdDis");
            ItemdataTable.Columns.Add("LessPriceAftStdDis");
            ItemdataTable.Columns.Add("LessAddDis");
            ItemdataTable.Columns.Add("PriceAftAddDis");
            ItemdataTable.Columns.Add("OTLSchemeDis");
            ItemdataTable.Columns.Add("PriceAftOTLSDis");
            ItemdataTable.Columns.Add("TruckScheme");
            ItemdataTable.Columns.Add("PriceAftTruckSch");
            ItemdataTable.Columns.Add("DealerSpecific");
            ItemdataTable.Columns.Add("PriceDealerSpecific");
            ItemdataTable.Columns.Add("GST");
            ItemdataTable.Columns.Add("GSTPrice");
            ItemdataTable.Columns.Add("PrimaryFreight");
            ItemdataTable.Columns.Add("PricePriFreight");
            ItemdataTable.Columns.Add("SecondaryFreight");
            ItemdataTable.Columns.Add("PriceSecFreight");
            ItemdataTable.Columns.Add("COIForCredit");
            ItemdataTable.Columns.Add("PriceCOIForCredit");
            ItemdataTable.Columns.Add("DealerLanding");
            ItemdataTable.Columns.Add("DealerMargin");
            ItemdataTable.Columns.Add("BillRateBuild");
            ItemdataTable.Columns.Add("DiffBtnBRBAndDL");
            ItemdataTable.Columns.Add("ProjectDis");
            ItemdataTable.Columns.Add("ActDiscount");
            ItemdataTable.Columns.Add("ActMargin");
            ItemdataTable.Columns.Add("PriceAftQuotDis");
            ItemdataTable.Columns.Add("PriceAftSchDis");
            ItemdataTable.Columns.Add("PriceAftDealerBillAnnDis");
            ItemdataTable.Columns.Add("PriceAftProjDis");
            ItemdataTable.Columns.Add("ExeGSTPrice");
            ItemdataTable.Columns.Add("ExePricePriFreight");
            ItemdataTable.Columns.Add("ExePriceSecFreight");
            ItemdataTable.Columns.Add("ExePriceCOIForCredit");
            ItemdataTable.Columns.Add("ExeDealerLanding");
            ItemdataTable.Columns.Add("ExeBillRateBuild");
            ItemdataTable.Columns.Add("BillRateBuildInclDealMargin");
            ItemdataTable.Columns.Add("CompetitorLP");
            ItemdataTable.Columns.Add("CompetitorQuote");
            ItemdataTable.Columns.Add("ExeDiffBtnBRBAndDL");
            ItemdataTable.Columns.Add("DiffBtnBRBFVC");
            ItemdataTable.Columns.Add("DiffWOPOCR");
            ItemdataTable.Columns.Add("WeightAvgWOPOCR");
            ItemdataTable.Columns.Add("WeightAvgWPOCR");
            ItemdataTable.Columns.Add("SrNo");

            ItemdataTable.TableName = "PT_SaveListItem";

            foreach (POCRList element in ListDetails)
            {
                DataRow row = ItemdataTable.NewRow();
                row["ProductID"] = element.ProductID;
                row["Value"] = element.Value;
                row["MaterialDes"] = element.MaterialDes;
                row["ListPrice"] = element.ListPrice;
                row["LessStdDis"] = element.LessStdDis;
                row["LessPriceAftStdDis"] = element.LessPriceAftStdDis;
                row["LessAddDis"] = element.LessAddDis;
                row["PriceAftAddDis"] = element.PriceAftAddDis;
                row["OTLSchemeDis"] = element.OTLSchemeDis;
                row["PriceAftOTLSDis"] = element.PriceAftOTLSDis;
                row["TruckScheme"] = element.TruckScheme;
                row["PriceAftTruckSch"] = element.PriceAftTruckSch;
                row["DealerSpecific"] = element.DealerSpecific;
                row["PriceDealerSpecific"] = element.PriceDealerSpecific;
                row["GST"] = element.GST;
                row["GSTPrice"] = element.GSTPrice;
                row["PrimaryFreight"] = element.PrimaryFreight;
                row["PricePriFreight"] = element.PricePriFreight;
                row["SecondaryFreight"] = element.SecondaryFreight;
                row["PriceSecFreight"] = element.PriceSecFreight;
                row["COIForCredit"] = element.COIForCredit;
                row["PriceCOIForCredit"] = element.PriceCOIForCredit;
                row["DealerLanding"] = element.DealerLanding;
                row["DealerMargin"] = element.DealerMargin;
                row["BillRateBuild"] = element.BillRateBuild;
                row["DiffBtnBRBAndDL"] = element.DiffBtnBRBAndDL;
                row["ProjectDis"] = element.ProjectDis;
                row["ActDiscount"] = element.ActDiscount;
                row["ActMargin"] = element.ActMargin;
                row["PriceAftQuotDis"] = element.PriceAftQuotDis;
                row["PriceAftSchDis"] = element.PriceAftSchDis;
                row["PriceAftDealerBillAnnDis"] = element.PriceAftDealerBillAnnDis;
                row["PriceAftProjDis"] = element.PriceAftProjDis;
                row["ExeGSTPrice"] = element.ExeGSTPrice;
                row["ExePricePriFreight"] = element.ExePricePriFreight;
                row["ExePriceSecFreight"] = element.ExePriceSecFreight;
                row["ExePriceCOIForCredit"] = element.ExePriceCOIForCredit;
                row["ExeDealerLanding"] = element.ExeDealerLanding;
                row["ExeBillRateBuild"] = element.ExeBillRateBuild;
                row["BillRateBuildInclDealMargin"] = element.BillRateBuildInclDealMargin;
                row["CompetitorLP"] = element.CompetitorLP;
                row["CompetitorQuote"] = element.CompetitorQuote;
                row["ExeDiffBtnBRBAndDL"] = element.ExeDiffBtnBRBAndDL;
                row["DiffBtnBRBFVC"] = element.DiffBtnBRBFVC;
                row["DiffWOPOCR"] = element.DiffWOPOCR;
                row["WeightAvgWOPOCR"] = element.WeightAvgWOPOCR;
                row["WeightAvgWPOCR"] = element.WeightAvgWPOCR;
                row["SrNo"] = element.SrNo;

                ItemdataTable.Rows.Add(row);
            }
            #endregion
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            var result = await POCr.SavePOCR(BasicDetails, ItemdataTable, (UserData)Session["UserData"], file);

            return Json(result, JsonRequestBehavior.AllowGet);
            //if (Attachment != null)
            //{
            //    for (int j = 0; j < Attachment.Count; j++)
            //    {
            //        string root = Server.MapPath("~/Uploads/SurveyAttachment/AttachmentFor" + responce.SurveyNo + "/");
            //        if (!Directory.Exists(root))
            //        {
            //            Directory.CreateDirectory(root);
            //        }
            //        string oldPath = Attachment[j].FilePath;
            //        string newPath = Path.Combine(Server.MapPath("~/Uploads/SurveyAttachment/AttachmentFor" + responce.SurveyNo + "/"), Attachment[j].DocName);
            //        if (!System.IO.File.Exists(newPath))
            //        {
            //            System.IO.File.Move(oldPath, newPath);

            //        }

            //        Attachment[j].FilePath = newPath;
            //    }
            //}
            //if (Attachment != null)
            //{
            //    DataTable dataTable2 = new DataTable();
            //    dataTable2.Columns.Add("SurveyNo");
            //    dataTable2.Columns.Add("UploadFor");
            //    dataTable2.Columns.Add("DocName");
            //    dataTable2.Columns.Add("FilePath");
            //    dataTable2.Columns.Add("ContentType");
            //    dataTable2.TableName = "PT_SurveyAttachment";
            //    foreach (BE.SupplierInfoAttach item in Attachment)
            //    {
            //        DataRow row = dataTable2.NewRow();
            //        row["SurveyNo"] = responce.SurveyNo;
            //        row["UploadFor"] = item.UploadFor;
            //        row["DocName"] = item.DocName;
            //        row["FilePath"] = item.FilePath;
            //        row["ContentType"] = item.ContentType;
            //        dataTable2.Rows.Add(row);
            //    }
            //    BE.ResponseMessage message = PIBusiness.SaveAttachment(dataTable2, TaxInvoiceInfo.AddedBy);
            //}

        }

        [HttpPost]
        public ActionResult GetSummaryList(int Type, string Text, int ID)
        {
            POCRFormDAL POCr = new POCRFormDAL();
            List<ProductM> ItemList = new List<ProductM>();
            ItemList = POCr.GetItemList(Type, Text, ID);

            var jsonResult = Json(ItemList, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }


        public async Task<ActionResult> SaveAttachmentPOCR(int formID, int rowID)
        {
            POCr = new POCRFormDAL();
            HttpPostedFileBase file = null;
            if (Request.Files.Count > 0 && Request.Files[0].ContentLength > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                file = files[0];
            }
            var result = await POCr.SaveAttachmentPOCR(formID, rowID, (UserData)Session["UserData"], file);
            return Json(1);
        }

    }
}
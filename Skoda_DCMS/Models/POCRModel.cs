using Newtonsoft.Json;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Skoda_DCMS.Models
{
    public class POCRModel
    {
        [JsonProperty("d")]
        public POCRResults POCRResults { get; set; }
    }
    //public class ISCLSCModel
    //{
    //    [JsonProperty("d")]
    //    public ISCLSResults ISCLSResults { get; set; }
    //}
    public class POCRResults
    {
        [JsonProperty("results")]
        public List<POCRFormModel> POCRFormModel { get; set; }

    }
    public class POCRFormModel : ApplicantDataModel
    {
        public POCRFormModel Clone()
        {
            return (POCRFormModel)base.MemberwiseClone();
        }
        public int FormId { get; set; }
        public int SrNo { get; set; }
        public string FormIDId { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("ID")]
        public int ID { get; set; }

        public string FormSrId { get; set; }
        public int POCRID { get; set; }
        public int RevisionNo { get; set; }
        public string POCRNo { get; set; }
        public string TSEName { get; set; }
        public string ZSMName { get; set; }
        public string ASMName { get; set; }
        public string TSELocation { get; set; }
        public string State { get; set; }
        public string DealerCode { get; set; }
        public string DealerName { get; set; }
        public string DealerLocation { get; set; }
        public string FILDealership { get; set; }
        public string DealerSalesLastYr { get; set; }
        public string DealerSalesTill { get; set; }
        public string Status { get; set; }
        public string BuilderName { get; set; }
        public string ProjectName { get; set; }
        public string SiteName { get; set; }
        public string RERANumber { get; set; }
        public string ProjectBrochure { get; set; }
        public string ProjectWebsite { get; set; }
        public string CustomerReference { get; set; }
        public DateTime? DateofEnquiry { get; set; }
        public string OrderValue { get; set; }
        public string PreferredPlant { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string FirstLifting { get; set; }
        public DateTime? LiftingDate { get; set; }
        public string POCRRequest { get; set; }
        public string AddDisMaterial { get; set; }
        public DateTime? ProDisValid { get; set; }
        public string CreditReq { get; set; }
        public string CreditPer { get; set; }
        public string InterstCost { get; set; }
        public string CreditLimit { get; set; }
        public string CustOverview { get; set; }
        public string WhyProject { get; set; }
        public string CompName { get; set; }
        public string CompName2 { get; set; }
        public string Invoice { get; set; }
        public string Freight { get; set; }
        public string TruckVol { get; set; }
        public string Place { get; set; }
        public string VehValue { get; set; }
        public string MaterialDes { get; set; }

        public decimal TotalValue { get; set; }
        public decimal FinalPercent { get; set; }
        public int AddedBy { get; set; }
        public List<POCRList> POCRFDataList { get; set; }

    }

    public class POCRFormDataList
    {
        [JsonProperty("results")]
        public List<POCRList> POCRList { get; set; }

    }
    public class POCRList
    {
        public string SrNo { get; set; }
        public string MaterialDes { get; set; }

        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Value { get; set; }
        public decimal ListPrice { get; set; }
        public decimal LessStdDis { get; set; }
        public decimal LessPriceAftStdDis { get; set; }
        public decimal LessAddDis { get; set; }
        public decimal PriceAftAddDis { get; set; }
        public decimal OTLSchemeDis { get; set; }
        public decimal PriceAftOTLSDis { get; set; }
        public decimal TruckScheme { get; set; }
        public decimal PriceAftTruckSch { get; set; }
        public decimal DealerSpecific { get; set; }
        public decimal PriceDealerSpecific { get; set; }
        public int GST { get; set; }
        public decimal GSTPrice { get; set; }
        public decimal PrimaryFreight { get; set; }
        public decimal PricePriFreight { get; set; }
        public decimal SecondaryFreight { get; set; }
        public decimal PriceSecFreight { get; set; }
        public decimal COIForCredit { get; set; }
        public decimal PriceCOIForCredit { get; set; }
        public decimal DealerLanding { get; set; }
        public decimal DealerMargin { get; set; }
        public decimal BillRateBuild { get; set; }
        public decimal DiffBtnBRBAndDL { get; set; }
        public decimal ProjectDis { get; set; }
        public decimal ActDiscount { get; set; }
        public decimal ActMargin { get; set; }
        public decimal PriceAftQuotDis { get; set; }
        public decimal PriceAftSchDis { get; set; }
        public decimal PriceAftDealerBillAnnDis { get; set; }
        public decimal PriceAftProjDis { get; set; }
        public decimal ExeGSTPrice { get; set; }
        public decimal ExePricePriFreight { get; set; }
        public decimal ExePriceSecFreight { get; set; }
        public decimal ExePriceCOIForCredit { get; set; }
        public decimal ExeDealerLanding { get; set; }
        public decimal ExeBillRateBuild { get; set; }
        public decimal BillRateBuildInclDealMargin { get; set; }
        public decimal BillRateWOProjDis { get; set; }
        public decimal CompetitorLP { get; set; }
        public decimal CompetitorQuote { get; set; }
        public decimal ExeDiffBtnBRBAndDL { get; set; }
        public decimal DiffBtnBRBFVC { get; set; }
        public decimal DiffWOPOCR { get; set; }
        public decimal WeightAvgWOPOCR { get; set; }
        public decimal WeightAvgWPOCR { get; set; }
    }

    public class POCRTableData
    {
        public int SrNo { get; set; }
        //public int SrNo { get; set; }
    }

    public class TestPOC
    {
        public int POCRID { get; set; }
        public int RevisionNo { get; set; }
        public string POCRNo { get; set; }
        public string TSEName { get; set; }
        public string ZSMName { get; set; }
        public string ASMName { get; set; }
        public string TSELocation { get; set; }
        public string State { get; set; }
        public string DealerCode { get; set; }
        public string DealerName { get; set; }
        public string DealerLocation { get; set; }
    }
}
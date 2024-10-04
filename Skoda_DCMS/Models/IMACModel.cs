using Newtonsoft.Json;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Skoda_DCMS.Models
{
    public class IMACModel
    {
        [JsonProperty("d")]
        public IMACResults IMACResults { get; set; }
    }
    //public class ISCLSCModel
    //{
    //    [JsonProperty("d")]
    //    public ISCLSResults ISCLSResults { get; set; }
    //}
    public class IMACResults
    {
        [JsonProperty("results")]
        public List<IMACFormModel> IMACFormModel { get; set; }

    }
    public  class IMACFormModel : ApplicantDataModel
    {
        public IMACFormModel Clone()
        {
            return (IMACFormModel)base.MemberwiseClone();
        }
        public int FormId { get; set; }
        public int SrNo { get; set; }
        public string FormIDId { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
       
        [JsonProperty("BidderApprovalDate")]
        public DateTime? BidderApprovalDate { get; set; }
        [JsonProperty("ID")]
        public int ID { get; set; }
       
        [JsonProperty("IsActive")]
        public string IsActive { get; set; }

        [JsonProperty("IMACtype")]
        public string IMACtype { get; set; }


        [JsonProperty("AssetName")]
        public string AssetName { get; set; }

        [JsonProperty("SubAssetName")]
        public string SubAssetName { get; set; }

        [JsonProperty("Make")]
        public string Make { get; set; }

        [JsonProperty("Model")]
        public string Modal { get; set; }

        [JsonProperty("AssetType")]
        public string AssetType { get; set; }

        [JsonProperty("Remarks")]
        public string Remarks { get; set; }

        [JsonProperty("Acknowledgement")]
        public string Acknowledgement { get; set; }

        [JsonProperty("AssignType")]
        public string AssignType { get; set; }

        [JsonProperty("FromDate")]
        public DateTime FromDate { get; set; }

        [JsonProperty("ToDate")]
        public DateTime ToDate { get; set; }

        [JsonProperty("AssetID")]
        public string AssetID { get; set; }

        public string FormSrId { get; set; }
      
        [JsonProperty("TypeofForm")]
        public long TypeofForm { get; set; }

        [JsonProperty("AssetCategory")]
        public long AssetCategory { get; set; }

        [JsonProperty("SubCategory")]
        public long SubCategory { get; set; }

        //[JsonProperty("AssetCategory1")]
        //public long AssetCategory1 { get; set; }
        //[JsonProperty("SubCategory1")]
        //public long SubCategory1 { get; set; }
        //[JsonProperty("Make1")]
        //public string Make1 { get; set; }
        //[JsonProperty("Model1")]
        //public string Modal1 { get; set; }


        [JsonProperty("SerialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("HostName")]
        public string HostName { get; set; }
        [JsonProperty("Quantity")]
        public string Quantity { get; set; }
        public string Location { get; set; }
        public string SerialNo { get; set; }

        //[JsonProperty("AssetType1")]
        //public string AssetType1 { get; set; }
        //[JsonProperty("Acknowledgement1")]
        //public string Acknowledgement1 { get; set; }
        //[JsonProperty("AssignType1")]
        //public string AssignType1 { get; set; }
        //[JsonProperty("FromDate1")]
        //public DateTime FromDate1 { get; set; }
        //[JsonProperty("ToDate1")]
        //public DateTime ToDate1 { get; set; }
        //[JsonProperty("Remarks1")]
        //public string Remarks1 { get; set; }



        public bool IsOthers { get; set; }
       
        public string OthersText { get; set; }
        public string BusinessJustification { get; set; }
        public List<IMACList> IMACFormDataList { get; set; }

    }

    public class IMACFormDataList
    {
        [JsonProperty("results")]
        public List<IMACList> IMACList { get; set; }

    }
    public class IMACList
    {
         public string SrNo { get; set; }
      
        [JsonProperty("AssetName")]
        public string AssetName { get; set; }

        [JsonProperty("SubAssetName")]
        public string SubAssetName { get; set; }

        [JsonProperty("Make")]
        public string Make { get; set; }

        [JsonProperty("Model")]
        public string Modal { get; set; }

        [JsonProperty("AssetType")]
        public string AssetType { get; set; }

        [JsonProperty("Remarks")]
        public string Remarks { get; set; }

        [JsonProperty("Acknowledgement")]
        public string Acknowledgement { get; set; }

        [JsonProperty("AssignType")]
        public string AssignType { get; set; }

        [JsonProperty("FromDate")]
        public DateTime FromDate { get; set; }

        [JsonProperty("ToDate")]
        public DateTime ToDate { get; set; }
        public int FormId { get; set; }
        public string SerialNo { get; set; }
        public string HostName { get; set; }
        public string Location { get; set; }
    }

    public class IMACTableData
    {
        public int SrNo { get; set; }
        //public int SrNo { get; set; }
    }
}
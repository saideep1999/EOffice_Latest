using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ISCLSCModel
    {
        [JsonProperty("d")]
        public ISCLSResults ISCLSResults { get; set; }
    }
    public class ISCLSResults
    {
        [JsonProperty("results")]
        public List<ISCLSData> ISCLSData { get; set; }

    }
    public class ISCLSData : ApplicantDataModel
    {
        public ISCLSData Clone()
        {
            return (ISCLSData)base.MemberwiseClone();
        }

        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("ActionType")]
        public string ActionType { get; set; }

        [JsonProperty("BuyerName")]
        public string BuyerName { get; set; }

        [JsonProperty("Team")]
        public string Team { get; set; }

        [JsonProperty("GlobalProcessNumber")]
        public string GlobalProcessNumber { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("InitialBudget")]
        public string InitialBudget { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("BidderApprovalDate")]
        public DateTime? BidderApprovalDate { get; set; }

        [JsonProperty("RFQReceiptDate")]
        public DateTime? RFQReceiptDate { get; set; }

        [JsonProperty("RFQSentDate")]
        public DateTime? RFQSentDate { get; set; }

        [JsonProperty("OfferReceiptDate")]
        public DateTime? OfferReceiptDate { get; set; }

        [JsonProperty("SFODate")]
        public DateTime? SFODate { get; set; }

        [JsonProperty("BestBidOffer")]
        public string BestBidOffer { get; set; }

        [JsonProperty("OrderVolume")]
        public string OrderVolume { get; set; }

        [JsonProperty("TransactionVolume")]
        public string TransactionVolume { get; set; }

        [JsonProperty("DiffBudgetAmount")]
        public string DiffBudgetAmount { get; set; }

        [JsonProperty("TargetClouseDate")]
        public DateTime? TargetClouseDate { get; set; }

        public string FormSrId { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDISLS { get; set; }

        [JsonProperty("Status1")]
        public string Status1 { get; set; }

        [JsonProperty("Status2")]
        public string Status2 { get; set; }
        public string AttachmentPath { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("attachedfile")]
        public string attachedfile { get; set; }

        [JsonProperty("attachedfileName")]
        public string attachedfileName { get; set; }

    }
}
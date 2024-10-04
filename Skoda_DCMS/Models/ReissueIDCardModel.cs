using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ReissueIDCardModel
    {
            [JsonProperty("d")]
            public ReissueIDCardResults List { get; set; }
    }

        public partial class ReissueIDCardResults
        {
            [JsonProperty("results")]
            public List<ReissueIDCardData> ReissueIDCardList { get; set; }

        }

    public partial class ReissueIDCardData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
      

        [JsonProperty("TypeOfCard")]
        public string TypeOfCard { get; set; }
        [JsonProperty("DateofJoining")] 
        public DateTime DateofJoining { get; set; }
        [JsonProperty("ActiveFrom")]
        public DateTime ActiveFrom { get; set; }
        [JsonProperty("EndDate")]
        public DateTime EndDate { get; set; }
        [JsonProperty("ReasonforReissue")]
        public string ReasonforReissue { get; set; }
        [JsonProperty("IDCardNumber")]
        public string IDCardNumber { get; set; }
        [JsonProperty("DateOfIssue")]
        public DateTime DateOfIssue { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        public DateTime Created { get; set; }
        [JsonProperty("Chargeable")]
        public string Chargeable { get; set; }
        [JsonProperty("OtherReason")]
        public string OtherReason { get; set; }
        public string UploadPhoto { get; set; }
        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }
    }
}
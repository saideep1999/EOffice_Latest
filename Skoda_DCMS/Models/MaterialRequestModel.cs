using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class MaterialRequestModel
    {
        [JsonProperty("d")]
        public MaterialRequestResults List { get; set; }
    }
    public partial class MaterialRequestResults
    {
        [JsonProperty("results")]
        public List<MaterialRequestData> MaterialRequestList { get; set; }

    }
    public partial class MaterialRequestData : ApplicantDataModel
    {
        public MaterialRequestData Clone()
        {
            return (MaterialRequestData)base.MemberwiseClone();
        }
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        [JsonProperty("RequestNumber")]
        public string RequestNumber { get; set; }
        [JsonProperty("RequestTo")]
        public string RequestTo { get; set; }
        [JsonProperty("RequestFrom")]
        public string RequestFrom { get; set; }
        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("PartNumber")]
        public string PartNumber { get; set; }
        [JsonProperty("PartDescription")]
        public string PartDescription { get; set; }
        [JsonProperty("Quantity")]
        public int Quantity { get; set; }
        [JsonProperty("Remarks")]
        public string Remarks { get; set; }
        [JsonProperty("Modified")]
        public DateTime Modified { get; set; }

        //public List<MaterialDetailsData> MaterialDetailsList { get; set; }
    }

    public class MaterialDetailsModel
    {
        [JsonProperty("d")]
        public MaterialDetailsResults List { get; set; }
    }
    public partial class MaterialDetailsResults
    {
        [JsonProperty("results")]
        public List<MaterialDetailsData> MaterialDetailsList { get; set; }

    }
    public partial class MaterialDetailsData
    {
        [JsonProperty("MaterialRequestID")]
        public MaterialRequestData MaterialRequestID { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        [JsonProperty("PartNumber")]
        public string PartNumber { get; set; }
        [JsonProperty("PartDescription")]
        public string PartDescription { get; set; }
        [JsonProperty("Quantity")]
        public int Quantity { get; set; }
        [JsonProperty("Remarks")]
        public string Remarks { get; set; }
        [JsonProperty("Modified")]
        public DateTime Modified { get; set; }
    }
}
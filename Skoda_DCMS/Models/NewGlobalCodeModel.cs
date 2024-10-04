using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class NewGlobalCodeModel
    {
        [JsonProperty("d")]
        public NGCFResults NGCFResults { get; set; }
    }

    public partial class NGCFResults
    {
        [JsonProperty("results")]
        public List<NewGlobalCodeData> NewGlobalCodeData { get; set; }

    }

    public class NewGlobalCodeData : ApplicantDataModel
    {
        public NewGlobalCodeData Clone()
        {
            return (NewGlobalCodeData)base.MemberwiseClone();
        }

        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("RequestType")]
        public string RequestType { get; set; }

        [JsonProperty("NameOfGLToOpen")]
        public string NameOfGLToOpen { get; set; }

        [JsonProperty("NatureOfTranInGL")]
        public string NatureOfTranInGL { get; set; }

        [JsonProperty("Purpose")]
        public string Purpose { get; set; }

        [JsonProperty("DateToOpenNewGL")]
        public DateTime DateToOpenNewGL { get; set; }

        [JsonProperty("GLCode")]
        public string GLCode { get; set; }

        [JsonProperty("GLName")]
        public string GLName { get; set; }

        [JsonProperty("GLSeries")]
        public string GLSeries { get; set; }
        public string FormSrId { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDNGCF { get; set; }

        [JsonProperty("NewGLNo")]
        public string NewGLNo { get; set; }

        [JsonProperty("CommitmentItem")]
        public string CommitmentItem { get; set; }

    }
}
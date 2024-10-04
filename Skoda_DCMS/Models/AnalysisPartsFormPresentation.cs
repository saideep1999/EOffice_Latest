using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class AnalysisPartsFormPresentationModel
    {
        [JsonProperty("d")]
        public AnalysisPartsFormPresentationResults AnalysisPartsFormPresentationResults { get; set; }
    }
    public class AnalysisPartsFormPresentationResults
    {
        [JsonProperty("results")]
        public List<AnalysisPartsFormPresentationData> AnalysisPartsFormPresentationData { get; set; }

    }
    public class AnalysisPartsFormPresentationData : ApplicantDataModel
    {
        public AnalysisPartsFormPresentationData Clone()
        {
            return (AnalysisPartsFormPresentationData)base.MemberwiseClone();
        }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("WeekNo")]
        public string WeekNo { get; set; }
        public string FormSrId { get; set; }

        [JsonProperty("Topic")]
        public string Topic { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("SrNo")]
        public int SrNo { get; set; }

        [JsonProperty("Project")]
        public string Project { get; set; }

        [JsonProperty("Parts")]
        public string Parts { get; set; }

        [JsonProperty("Quantity")]
        public int Quantity { get; set; }

        [JsonProperty("Reason")]
        public string Reason { get; set; }

        [JsonProperty("DetailDescription")]
        public string DetailDescription { get; set; }

        public List<formList> UserDataList { get; set; }
        [JsonProperty("FormIDId")]
        public string FormIDId { get; set; }
        [JsonProperty("ListItemIdId")]
        public string ListItemIdId { get; set; }

    }

    public class UserDataList
    {
        [JsonProperty("results")]
        public List<formList> formList { get; set; }

    }

    public class formList
    {
        public string SrNo { get; set; }
        public string Project { get; set; }
        public string Parts { get; set; }
        public string Reason { get; set; }
        public string Quantity { get; set; }
    }
}
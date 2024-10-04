using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class InterviewRatingSheetModel
    {
        [JsonProperty("d")]
        public IRSFResults irsflist { get; set; }
    }
    public partial class IRSFResults
    {
        [JsonProperty("results")]
        public List<IRSFData> irsfData { get; set; }

    }
    public partial class IRSFData
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("FormID")]
        public string FormID { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("JobTitle")]
        public string JobTitle { get; set; }

        [JsonProperty("InterviewDate")]
        public DateTime InterviewDate { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("Section")]
        public string Section { get; set; }

        [JsonProperty("InterviewPlace")]
        public string InterviewPlace { get; set; }

        [JsonProperty("CompetenceGeneral")]
        public string CompetenceGeneral { get; set; }

        [JsonProperty("CompetenceTechnical")]
        public string CompetenceTechnical { get; set; }

        [JsonProperty("LivingIntegrityandResponsibility")]
        public string LivingIntegrityandResponsibility { get; set; }

        [JsonProperty("OtherComments")]
        public string OtherComments { get; set; }

    }
}
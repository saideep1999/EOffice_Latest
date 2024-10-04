using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
  
    public class QualityMeisterbockCubingModel
    {
        [JsonProperty("d")]
        public QualityMeisterbockCubingResults QualityMeisterbockCubingResults { get; set; }
    }
    public class QualityMeisterbockCubingResults
    {
        [JsonProperty("results")]
        public List<QualityMeisterbockCubingData> QualityMeisterbockCubingData { get; set; }

    }

    public class QualityMeisterbockCubingData : ApplicantDataModel
    {
        public QualityMeisterbockCubingData Clone()
        {
            return (QualityMeisterbockCubingData)base.MemberwiseClone();
        }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("FormType")]
        public string FormType { get; set; }

        [JsonProperty("ModelQCM")]
        public string ModelQCM { get; set; }

        [JsonProperty("Series")]
        public string Series { get; set; }

        [JsonProperty("PartQuantity")]
        public string PartQuantity { get; set; }

        [JsonProperty("OtherDetails")]
        public string OtherDetails { get; set; }

        [JsonProperty("ProblemSheet")]
        public string ProblemSheet { get; set; }

        [JsonProperty("TrialReported")]
        public string TrialReported { get; set; }

        [JsonProperty("ProblemReported")]
        public string ProblemReported { get; set; }

        [JsonProperty("Details")]
        public string Details { get; set; }

        [JsonProperty("PartName")]
        public string PartName { get; set; }
        public string FormSrId { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("AttachmentFiles1")]
        public AttachmentFilesResults AttachmentFiles1 { get; set; }

        [JsonProperty("attachedfile")]
        public string attachedfile { get; set; }

        [JsonProperty("attachedfileName")]
        public string attachedfileName { get; set; }
        public string chkQMCR { get; set; }
        public string chkproblemsheet { get; set; }
        public string chkProblemReported { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ConditionPostTrial { get; set; }

        public int  PSAttach1 { get; set; }
        public int  TRAttach2 { get; set; }
        [JsonProperty("attachedfile1")]
        public string attachedfile1 { get; set; }

        [JsonProperty("attachedfileName1")]
        public string attachedfileName1 { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDQMCR { get; set; }
    }

}
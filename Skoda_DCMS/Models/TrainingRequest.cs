using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{

    public class TrainingRequestModel
    {
        [JsonProperty("d")]
        public TrainingRequestResults TrainingRequestList { get; set; }
    }
    public partial class TrainingRequestResults
    {
        [JsonProperty("results")]
        public List<TrainingRequestData> TrainingRequestData { get; set; }
    }

    public partial class TrainingRequestData : ApplicantDataModel
    {
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        public int Id { get; set; }
        public string EmployeeID { get; set; }
        public int TrainingID { get; set; }
        public string TrainingProgramTitle { get; set; }
        public string TrainingProvider { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReasonForTraining { get; set; }
        public string ExpectedOutcomes { get; set; }
        public string CompletionStatus { get; set; }
        public string Assessment { get; set; }
        public DateTime Created { get; set; }
    }

}
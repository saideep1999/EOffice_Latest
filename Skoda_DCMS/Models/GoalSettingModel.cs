using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class GoalSettingModel
    {
        [JsonProperty("d")]
        public GoalSettingResults GoalSettingList { get; set; }
    }
    public partial class GoalSettingResults
    {
        [JsonProperty("results")]
        public List<GoalSettingData> GoalSettingData { get; set; }
    }

    public partial class GoalSettingData : ApplicantDataModel
    {
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        public int Id { get; set; }
        public string EmployeeID { get; set; }
        public string GoalTitle { get; set; }
        public string GoalDescription { get; set; }
        public DateTime StartDate { get; set; } 
        public DateTime EndDate { get; set; }
        public string MeasurementCriteria { get; set; } 
        public string PriorityLevel { get; set; }
        public DateTime Created { get; set; }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{

    public class SalaryReviewAdjustmentModel
    {
        [JsonProperty("d")]
        public SalaryReviewAdjustmentResults SalaryReviewAdjustmentList { get; set; }
    }
    public partial class SalaryReviewAdjustmentResults
    {
        [JsonProperty("results")]
        public List<SalaryReviewAdjustmentData> SalaryReviewAdjustmentData { get; set; }
    }

    public partial class SalaryReviewAdjustmentData : ApplicantDataModel
    {
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        public int Id { get; set; }
        public int EmployeeID { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public decimal CurrentSalary { get; set; }
        public decimal RequestedSalary { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string SalaryJustification { get; set; }
        public DateTime Created { get; set; }
    }
}
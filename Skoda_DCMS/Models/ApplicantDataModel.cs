using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ApplicantDataModel : SubmitterDataModel
    {
        [JsonProperty("OtherEmployeeType")]
        public string OtherEmployeeType { get; set; }

        [JsonProperty("OtherExternalOrganizationName")]
        public string OtherExternalOrganizationName { get; set; }

        public string OtherNewExternalOrganizationName { get; set; }

        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }
        public string OtherNewEmployeeName { get; set; }

        [JsonProperty("OtherEmployeeCode")]
        public long OtherEmployeeCode { get; set; }
        public long OtherCostcenterCode { get; set; }
        public long OtherNewEmployeeCode { get; set; }

        [JsonProperty("OtherEmployeeCCCode")]
        public long OtherEmployeeCCCode { get; set; }
        public long OtherNewCostcenterCode { get; set; }

        [JsonProperty("OtherEmployeeUserId")]
        public string OtherEmployeeUserId { get; set; }
        public string OtherNewUserId { get; set; }

        [JsonProperty("OtherEmployeeDesignation")]
        public string OtherEmployeeDesignation { get; set; }

        [JsonProperty("OtherEmployeeLocation")]
        public string OtherEmployeeLocation { get; set; }

        [JsonProperty("OtherEmployeeContactNo")]
        public string OtherEmployeeContactNo { get; set; }
        public long OtherNewContactNo { get; set; }

        [JsonProperty("OtherEmployeeEmailId")]
        public string OtherEmployeeEmailId { get; set; }
        public string OtherNewEmailId { get; set; }

        [JsonProperty("OtherEmployeeDepartment")]
        public string OtherEmployeeDepartment { get; set; }
        public string OtherNewDepartment { get; set; }
        public string OtherNewEmpDesignation { get; set; }
        public string OtherNewEmpLocation { get; set; }
        public string OtherNewEmployeeType { get; set; }

        //Other Employee
        public ApplicantDataModel Clone()
        {
            return (ApplicantDataModel)base.MemberwiseClone();
        }
    }
}
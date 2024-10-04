using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace Skoda_DCMS.Models
{
    public class SubmitterDataModel
    {
        [JsonProperty("EmployeeType")]
        public string EmployeeType { get; set; }

        [JsonProperty("ExternalOrganizationName")]
        public string ExternalOrganizationName { get; set; }

        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; }

        [JsonProperty("EmployeeCCCode")]
        public long EmployeeCCCode { get; set; }

        [JsonProperty("EmployeeCode")]
        public long EmployeeCode { get; set; }

        [JsonProperty("EmployeeUserId")]
        public string EmployeeUserId { get; set; }

        [JsonProperty("EmployeeDepartment")]
        public string EmployeeDepartment { get; set; }

        [JsonProperty("EmployeeDesignation")]
        public string EmployeeDesignation { get; set; }

        [JsonProperty("EmployeeLocation")]
        public string EmployeeLocation { get; set; }

        [JsonProperty("EmployeeContactNo")]
        public long EmployeeContactNo { get; set; }

        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }

        [JsonProperty("OnBehalfOption")]
        public string OnBehalfOption { get; set; }
        public SubmitterDataModel Clone()
        {
            return (SubmitterDataModel)base.MemberwiseClone();
        }

        public string OldEmployeeNumber { get; set; }
        public string OldEmployeeContactNo { get; set; }
        public int FormId { get; set; }
        public int AppRowId { get; set; }

        public string CompanyName { get; set; }
    }
}
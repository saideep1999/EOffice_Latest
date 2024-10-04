using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
   
        public partial class ITClearanceModel
        {
            [JsonProperty("d")]
            public ITClearanceResults List { get; set; }
        }

        public partial class ITClearanceResults
        {
            [JsonProperty("results")]
            public List<ITClearanceData> ITClearanceList { get; set; }
        }

        public partial class ITClearanceData
        {
            [JsonProperty("ID")]
            public int Id { get; set; }
            [JsonProperty("FormID")]
            public FormLookup FormID { get; set; }
            [JsonProperty("EmployeeType")]
            public string EmployeeType { get; set; }
            [JsonProperty("ExternalOrganizationName")]
            public string ExternalOrganizationName { get; set; }
            [JsonProperty("ExternalOtherOrganizationName")]
            public string ExternalOtherOrganizationName { get; set; }
            [JsonProperty("EmployeeCode")]
            public long EmployeeCode { get; set; }
            [JsonProperty("EmployeeCCCode")]
            public long EmployeeCCCode { get; set; }
            [JsonProperty("EmployeeUserId")]
            public string EmployeeUserId { get; set; }
            [JsonProperty("EmployeeName")]
            public string EmployeeName { get; set; }
            [JsonProperty("EmployeeDepartment")]
            public string EmployeeDepartment { get; set; }
         
            [JsonProperty("EmployeeContactNo")]
            public string EmployeeContactNo { get; set; }
            [JsonProperty("EmployeeDesignation")]
            public string EmployeeDesignation { get; set; }//string
            [JsonProperty("EmployeeLocation")]
            public string EmployeeLocation { get; set; }//string
        [JsonProperty("EmployeeEmailId")]
        public string EmployeeEmailId { get; set; }

        //Other Employee Fields
        [JsonProperty("OnBehalfOption")]
        public string OnBehalfOption { get; set; }
        [JsonProperty("OtherEmployeeType")]
        public string OtherEmployeeType { get; set; }
        [JsonProperty("OtherExternalOrganizationName")]
        public string OtherExternalOrganizationName { get; set; }
        [JsonProperty("OtherExternalOtherOrgName")]
        public string OtherExternalOtherOrganizationName { get; set; }
        [JsonProperty("OtherEmployeeCode")]
        public long OtherEmployeeCode { get; set; }

        [JsonProperty("OtherEmployeeCCCode")]
        public long OtherEmployeeCCCode { get; set; }

        [JsonProperty("OtherEmployeeUserId")]
        public string OtherEmployeeUserId { get; set; }
        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }
        [JsonProperty("OtherEmployeeDepartment")]
        public string OtherEmployeeDepartment { get; set; }
        [JsonProperty("OtherEmployeeDesignation")]
        public string OtherEmployeeDesignation { get; set; }//string
        [JsonProperty("OtherEmployeeLocation")]
        public string OtherEmployeeLocation { get; set; }//string
        [JsonProperty("OtherEmployeeContactNo")]
        public long OtherEmployeeContactNo { get; set; }
        [JsonProperty("OtherEmployeeEmailId")]
        public string OtherEmployeeEmailId { get; set; }
        [JsonProperty("LastWorkingDay")]
            public DateTime LastWorkingDay { get; set; }
            [JsonProperty("HandedDataToName")]
            public string HandedDataToName { get; set; }
            [JsonProperty("HandedDataToEmpNumber")]
            public long HandedDataToEmpNumber { get; set; }
            [JsonProperty("RequestSubmissionFor")]
            public string RequestSubmissionFor { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
    }
}
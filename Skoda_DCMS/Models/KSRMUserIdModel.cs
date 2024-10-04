using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
  
        public partial class KSRMUserIdModel
        {
            [JsonProperty("d")]
            public KSRMUserIdResults List { get; set; }
        }

        public partial class KSRMUserIdResults
        {
            [JsonProperty("results")]
            public List<KSRMUserIdData> KSRMUserIdList { get; set; }
        }

        public partial class KSRMUserIdData
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
            [JsonProperty("EmployeeDesignation")]
            public string EmployeeDesignation { get; set; }
            [JsonProperty("EmployeeLocation")]
            public string EmployeeLocation { get; set; }
            [JsonProperty("EmployeeContactNo")]
            public string EmployeeContactNo { get; set; }

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
            public string OtherEmployeeContactNo { get; set; }
            [JsonProperty("OtherEmployeeEmailId")]
            public string OtherEmployeeEmailId { get; set; }
            [JsonProperty("EmployeeRequestType")]
            public string EmployeeRequestType { get; set; }
            [JsonProperty("BusinessNeed")]
            public string BusinessNeed { get; set; }
            [JsonProperty("RequestFor")]
            public string RequestFor { get; set; }
            [JsonProperty("Role")]
            public string Role { get; set; }
            [JsonProperty("Amount")]
            public decimal Amount { get; set; }
            [JsonProperty("RequestSubmissionFor")]
            public string RequestSubmissionFor { get; set; }
            [JsonProperty("IsKSRMIdAccess")]
            public bool IsKSRMIdAccess { get; set; }
            [JsonProperty("IsKSRMOpReportingAccess")]
            public bool IsKSRMOpReportingAccess { get; set; }
        }    
}
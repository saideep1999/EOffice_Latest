using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class ITAssetRequisitionModel
    {

        [JsonProperty("d")]
        public ITAssetRequisitionResults List { get; set; }
    }
    public partial class ITAssetRequisitionResults
    {
        [JsonProperty("results")]
        public List<ITAssetRequisitionRequestDto> ITAssetList { get; set; }
    }
    public partial class ITAssetRequisitionRequestDto
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
        [JsonProperty("PartnerOrganizationName")]
        public string PartnerOrganizationName { get; set; }

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
        [JsonProperty("OtherPartnerOrganizationName")]
        public string OtherPartnerOrganizationName { get; set; }
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
        [JsonProperty("RequestType")]
        public string RequestType { get; set; }
        [JsonProperty("TempFrom")]
        public Nullable<System.DateTime> TempFrom { get; set; }
        [JsonProperty("TempTo")]
        public Nullable<System.DateTime> TempTo { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        [JsonProperty("WorkstationDesktop")]
        public string WorkstationDesktop { get; set; }
        [JsonProperty("WorkstationLaptop")]
        public string WorkstationLaptop { get; set; }
        [JsonProperty("Desktop")]
        public string Desktop { get; set; }
        [JsonProperty("Laptop")]
        public string Laptop { get; set; }
        [JsonProperty("RSAToken")]
        public string RSAToken { get; set; }
        [JsonProperty("SIMAndData")]
        public string SIMAndData { get; set; }
        [JsonProperty("Landline")]
        public string Landline { get; set; }
        [JsonProperty("LANCableAndPort")]
        public string LANCableAndPort { get; set; }
        [JsonProperty("JabraSpeaker")]
        public string JabraSpeaker { get; set; }
        [JsonProperty("AdditionalOfficeMonitor")]
        public string AdditionalOfficeMonitor { get; set; }
        [JsonProperty("iPad")]
        public string iPad { get; set; }
        [JsonProperty("CabinsScreen")]
        public string CabinsScreen { get; set; }
        [JsonProperty("NewMeetingRoomSetup")]
        public string NewMeetingRoomSetup { get; set; }

        [JsonProperty("WorkflowType")]
        public string WorkflowType { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("SubDepartment")]
        public string SubDepartment { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("ApproverEmailId")]
        public string ApproverEmailId { get; set; }

        [JsonProperty("ApproverEmployeeCode")]
        public string ApproverEmployeeCode { get; set; }
        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }
        [JsonProperty("UsageType")]
        public string UsageType { get; set; }
    }
}
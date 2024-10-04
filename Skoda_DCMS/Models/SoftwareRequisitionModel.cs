using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class SoftwareRequisitionModel
    {
        [JsonProperty("d")]
        public SoftwareRequisitionResults List { get; set; }
    }
    public partial class SoftwareRequisitionResults
    {
        [JsonProperty("results")]
        public List<SoftwareRequisitionRequestDto> SoftwareList { get; set; }
    }
    public class SoftwareRequisitionRequestDto
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
        [JsonProperty("TempFrom")]
        public DateTime TempFrom { get; set; }

        [JsonProperty("TempTo")]
        public DateTime TempTo { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
             
        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }
        [JsonProperty("IsNonStandard")]
        public bool IsNonStandard { get; set; }
        [JsonProperty("IsStandard")]
        public bool IsStandard { get; set; }
    }


    public partial class SelectedSoftwareModel
    {
        [JsonProperty("d")]
        public SelectedSoftwareResults List { get; set; }
    }
    public partial class SelectedSoftwareResults
    {
        [JsonProperty("results")]
        public List<SelectedSoftwareDto> AVLSoftwareList { get; set; }
    }
    
    public class SelectedSoftwareDto
    {
        [JsonProperty("ID")]
        public long SoftwareReqID { get; set; }
        [JsonProperty("SoftwareType")]
        public string SoftwareType { get; set; }
        [JsonProperty("SoftwareName")]
        public string SoftwareName { get; set; }
        [JsonProperty("SoftwareVersion")]
        public string SoftwareVersion { get; set; }
        [JsonProperty("IsOtherSoftware")]
        public string IsOtherSoftware { get; set; }
    }
}

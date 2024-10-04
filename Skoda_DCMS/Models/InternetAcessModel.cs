using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class InternetAcessModel
    {
        [JsonProperty("d")]
        public InternetAcessResults List { get; set; }
    }

    public partial class InternetAcessResults
    {
        [JsonProperty("results")]
        public List<InternetAcessData> InternetList { get; set; }
    }

    public partial class InternetAcessData
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        public FormLookup FormId { get; set; }
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
        public Nullable<System.DateTime> TempFrom { get; set; }
        [JsonProperty("TempTo")]
        public Nullable<System.DateTime> TempTo { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        [JsonProperty("IsSpecialRequest")]
        public string IsSpecialRequest { get; set; }
        [JsonProperty("MoreInformation")]
        public string MoreInformation { get; set; }
        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }
        [JsonProperty("IsOnBehalf")]
        public string IsOnBehalf { get; set; }
        public Author Author { get; set; }
    }

    public partial class SubmitterModel
    {
        [JsonProperty("d")]
        public SubmitterDataList List { get; set; }
    }
     public partial class Author
    {
        [JsonProperty("Title")]
        public string Title { get; set; }
    }

    public partial class SubmitterDataList
    {
        [JsonProperty("results")]
        public List<SubmitterData> JobList { get; set; }
        public List<SubmitterData> LocationList { get; set; }
        public List<SubmitterData> OrgList { get; set; }
    }

    public partial class SubmitterData
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("Jobtitle")]
        public string JobTitle { get; set; }
        [JsonProperty("Organization")]
        public string Organization { get; set; }
        [JsonProperty("LocationName")]
        public string LocationName { get; set; }
    }
}
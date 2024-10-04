using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class ECFModel
    {
        [JsonProperty("d")]
        public ECFResults ecflist { get; set; }
    }

    public partial class ECFResults
    {
        [JsonProperty("results")]
        public List<ECFData> ecfData { get; set; }
    }

    public partial class ECFData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        //[JsonProperty("Name")]
        //public string Name { get; set; }

        //[JsonProperty("EmployeeCode")]
        //public string EmployeeCode { get; set; }

        //[JsonProperty("Designation")]
        //public string Designation { get; set; }

        //[JsonProperty("Section")]
        //public string Section { get; set; }

        //[JsonProperty("Department")]
        //public string Department { get; set; }

        //[JsonProperty("EmployeeSection")]
        //public string EmployeeSection { get; set; }

        //[JsonProperty("EmployeeDepartment")]
        //public string EmployeeDepartment { get; set; }

        //[JsonProperty("SubDepartment")]
        //public string SubDepartment { get; set; }

        //[JsonProperty("ApproverEmailId")]
        //public string ApproverEmailId { get; set; }

        //[JsonProperty("ApproverEmployeeId")]
        //public int ApproverEmployeeId { get; set; }

        //[JsonProperty("Location")]
        //public string Location { get; set; }

        //[JsonProperty("EmployeeLocation")]
        //public string EmployeeLocation { get; set; }

        //[JsonProperty("DateOfJoining")]
        //public DateTime DateOfJoining { get; set; }

        //[JsonProperty("DateOfRelieving")]
        //public DateTime DateOfRelieving { get; set; }

        //[JsonProperty("Created")]
        //public DateTime Created { get; set; }

        //[JsonProperty("HandOverTo")]
        //public string HandOverTo { get; set; }

        //[JsonProperty("PhoneNumber")]
        //public long PhoneNumber { get; set; }

        //[JsonProperty("Email")]
        //public string Email { get; set; }

        //[JsonProperty("ResignationReceived")]
        //public DateTime ResignationReceived { get; set; }

        //[JsonProperty("ResignationGiven")]
        //public DateTime ResignationGiven { get; set; }

        //[JsonProperty("NoticePeriod")]
        //public string NoticePeriod { get; set; }

        //[JsonProperty("ApplicableDays")]
        //public string ApplicableDays { get; set; }

        //[JsonProperty("EligibleForGratuity")]
        //public string EligibleForGratuity { get; set; }

        //[JsonProperty("ApproverEmployeeCode")]
        //public int ApproverEmployeeCode { get; set; }

        ////User data

        //[JsonProperty("Id")]
        //public int UserId { get; set; }
        //[JsonProperty("LoginName")]
        //public string LoginName { get; set; }
        //[JsonProperty("Title")]
        //public string UserName { get; set; }
        //[JsonProperty("IsSiteAdmin")]
        //public bool IsSiteAdmin { get; set; }
        //public int EmpNumber { get; set; }
        //public int CostCenter { get; set; }
        ////public string Department { get; set; }
        //public string FirstName { get; set; }
        //public string LastName { get; set; }
        //public string EmployeeName { get; set; }
        ////public ExceptionType ExceptionType { get; set; }
        //public bool IsLoginSuccessful { get; set; }
        //public string Password { get; set; }
        [JsonProperty("DateOfJoining")]
        public DateTime DateOfJoining { get; set; }

        [JsonProperty("DateOfRelieving")]
        public DateTime DateOfRelieving { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("ChargeHandOverToEmpName")]
        public string ChargeHandOverToEmpName { get; set; }

        [JsonProperty("ChargeHandOverToEmpNum")]
        public long ChargeHandOverToEmpNum { get; set; }

        [JsonProperty("ResignationReceivedDate")]
        public DateTime? ResignationReceivedDate { get; set; }

        [JsonProperty("ResignationGivenDate")]
        public DateTime? ResignationGivenDate { get; set; }

        [JsonProperty("NoticePeriod")]
        public string NoticePeriod { get; set; }

        [JsonProperty("ApplicableDays")]
        public long ApplicableDays { get; set; }

        [JsonProperty("Gratuity")]
        public string Gratuity { get; set; }

        [JsonProperty("DisciplinaryAction")]
        public string DisciplinaryAction { get; set; }

        [JsonProperty("CreditCard")]
        public string CreditCard { get; set; }
    }
}
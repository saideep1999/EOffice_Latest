using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class NEIFResults
    {
        public List<NEIFModel> NEIFfData { get; set; }
    }
    public partial class NEIFModel :ApplicantDataModel
    {

        public int Id { get; set; }
        // Personal Information
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; }

        public string PreferredName { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        public string MaritalStatus { get; set; }

        [Required(ErrorMessage = "Nationality is required.")]
        public string Nationality { get; set; }

        [Required(ErrorMessage = "Personal Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string PersonalEmail { get; set; }

        [Required(ErrorMessage = "Mobile Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string MobilePhoneNumber { get; set; }

        // Emergency Contact
        [Required(ErrorMessage = "Emergency Contact Name is required.")]
        public string EmergencyContactName { get; set; }

        [Required(ErrorMessage = "Emergency Contact Relationship is required.")]
        public string EmergencyContactRelationship { get; set; }

        [Required(ErrorMessage = "Emergency Contact Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string EmergencyContactPhoneNumber { get; set; }

        // Address
        [Required(ErrorMessage = "Current Address is required.")]
        public string CurrentAddress { get; set; }

        public string PermanentAddress { get; set; }

        [Required(ErrorMessage = "Postal/Zip Code is required.")]
        public string PostalCode { get; set; }

        // Employment Information
        [Required(ErrorMessage = "Job Title is required.")]
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Date of Joining is required.")]
        [DataType(DataType.Date)]
        public DateTime? DateOfJoining { get; set; }

        [Required(ErrorMessage = "Employment Type is required.")]
        public string EmploymentType { get; set; }

        [Required(ErrorMessage = "Manager/Supervisor Name is required.")]
        public string ManagerName { get; set; }

        // Bank Information
        [Required(ErrorMessage = "Bank Account Number is required.")]
        public string BankAccountNumber { get; set; }

        [Required(ErrorMessage = "Bank Name is required.")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "IFSC Code is required.")]
        public string IFSCCode { get; set; }

        [Required(ErrorMessage = "Tax ID Number is required.")]
        public string TaxIDNumber { get; set; }

        // Optional Identifications
        public string PAN { get; set; }
        public string AADHAR { get; set; }
        public string DrivingLicense { get; set; }
        public string Passport { get; set; }
        public string ValidVisa { get; set; }
        public DateTime Created_Date { get; set; }
    }
}
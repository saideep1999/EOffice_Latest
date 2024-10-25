using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class CAFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime Created_Date { get; set; }
        public int ApplicationID { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string Nationality { get; set; }

        [Required]
        [Phone]
        public string ContactNumber { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public string CurrentAddress { get; set; }

        public string PermanentAddress { get; set; }
        public string LinkedInProfile { get; set; }
        public string WebsitePortfolio { get; set; }

        [Required]
        public string PositionAppliedFor { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Please enter valid salary")]
        public decimal ExpectedSalary { get; set; }

        [Required]
        public string NoticePeriod { get; set; }

        [Required]
        public string HighestQualification { get; set; }

        [Required]
        public string InstitutionName { get; set; }

        [Required]
        [Range(1900, int.MaxValue, ErrorMessage = "Please enter a valid year")]
        public int YearOfGraduation { get; set; }

        public string Specialization { get; set; }
        public string AdditionalCertifications { get; set; }

        [Required]
        [Range(0, 50, ErrorMessage = "Please enter valid experience")]
        public decimal TotalYearsOfExperience { get; set; }

        [Required]
        public string LastCompany { get; set; }

        [Required]
        public string LastJobTitle { get; set; }

        [Required]
        public string TechnicalSkills { get; set; }

        public string OtherSkills { get; set; }

        [Required]
        public string ResumeFilePath { get; set; }
        public string attachedfile3 { get; set; }
    }
}
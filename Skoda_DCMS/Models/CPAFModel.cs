using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class CPAFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime Created_Date { get; set; }
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Employee ID")]
        public string CPAFEmployeeID { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Required]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Required]
        [Display(Name = "Acknowledgement Date")]
        [DataType(DataType.Date)]
        public DateTime? AcknowledgementDate { get; set; }

        [Display(Name = "Acknowledgement Statement")]
        public string AcknowledgementStatement { get; set; } // Static Text
    }
}
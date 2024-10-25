using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class DCAFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime Created_Date { get; set; }
        [Required]
        [Display(Name = "Employee ID")]
        public string DCAFEmployeeID { get; set; }

        [Required]
        [Display(Name = "Date of Incident")]
        [DataType(DataType.Date)]
        public DateTime? DateOfIncident { get; set; }

        [Required]
        [Display(Name = "Description of Incident")]
        public string DescriptionOfIncident { get; set; }

        [Display(Name = "Witnesses (if any)")]
        public string Witnesses { get; set; }

        [Required]
        [Display(Name = "Type of Disciplinary Action")]
        public string DisciplinaryActionType { get; set; }

        [Required]
        [Display(Name = "Action Taken By")]
        public string ActionTakenBy { get; set; }

        [Required]
        [Display(Name = "Date of Action")]
        [DataType(DataType.Date)]
        public DateTime? DateOfAction { get; set; }

        [Display(Name = "Employee Comments")]
        public string EmployeeComments { get; set; }
    }
}
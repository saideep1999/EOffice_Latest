using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class LAFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime Created_Date { get; set; }

        [Required]
        [Display(Name = "Employee ID")]
        public string LAFEmployeeID { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; }

        [Required]
        [Display(Name = "Leave Start Date")]
        [DataType(DataType.Date)]
        public DateTime? LeaveStartDate { get; set; }

        [Required]
        [Display(Name = "Leave End Date")]
        [DataType(DataType.Date)]
        public DateTime? LeaveEndDate { get; set; }

        [Display(Name = "Total Leave Days")]
        public int TotalLeaveDays { get; set; }
        
        [Required]
        [Display(Name = "Reason for Leave")]
        [DataType(DataType.MultilineText)]
        public string LeaveReason { get; set; }
    }
}
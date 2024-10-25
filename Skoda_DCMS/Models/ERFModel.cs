using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ERFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime Created_Date { get; set; }
        [Required]
        public string ERFEmployeeID { get; set; }

        [Required]
        public string ExpenseType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? ExpenseDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal ExpenseAmount { get; set; }

        public bool ReceiptAttached { get; set; }

        [Required]
        public string CostCode { get; set; }
    }
}
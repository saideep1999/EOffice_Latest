using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class TSFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime? Created_Date { get; set; }
        public int TimeEntryId { get; set; }

        [Required]
        public string EmployeeID { get; set; }
        public string hdnTSFDataList { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? WeekEndingDate { get; set; }

        [Required]
        [Range(0, 168)] // Max hours in a week
        public decimal TotalHoursWorked { get; set; }

        public decimal? LeaveTaken { get; set; }

        public virtual List<DailyHour> DailyHours { get; set; }
    }
    public class DailyHour
    {
        public int DailyHourId { get; set; }
        public int TSFId { get; set; }

        [Required]
        public string CostCode { get; set; }

        [Range(0, 24)]
        public decimal MondayHours { get; set; }

        [Range(0, 24)]
        public decimal TuesdayHours { get; set; }

        [Range(0, 24)]
        public decimal WednesdayHours { get; set; }

        [Range(0, 24)]
        public decimal ThursdayHours { get; set; }

        [Range(0, 24)]
        public decimal FridayHours { get; set; }

        public decimal? SaturdayHours { get; set; }
        public decimal? SundayHours { get; set; }

        public int TimeEntryId { get; set; }
        public virtual TSFModel TimeEntry { get; set; }
    }
}
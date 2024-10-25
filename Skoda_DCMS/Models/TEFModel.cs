using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class TEFModel : ApplicantDataModel
    {
        public int Id { get; set; }
        public DateTime Created_Date { get; set; }
        public string TEFEmployeeID { get; set; }
        public string TrainingProgramTitle { get; set; }
        public DateTime? TrainingDate { get; set; }
        public string TrainerName { get; set; }
        public int OverallRating { get; set; }
        public string Likes { get; set; }
        public int TrainerRating { get; set; }
        public int ContentRating { get; set; }
        public int FacilitiesRating { get; set; }
        public string ImprovementAreas { get; set; }
    }
}
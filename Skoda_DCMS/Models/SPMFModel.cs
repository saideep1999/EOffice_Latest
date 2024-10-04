using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class SPMFModel
    {
        [JsonProperty("d")]
        public SPMFResults SPMFResults { get; set; }
    }
    public class SPMFResults
    {
        [JsonProperty("results")]
        public List<SPMFData> SPMFData { get; set; }

    }
    public class SPMFData : ApplicantDataModel
    {
        public SPMFData Clone()
        {
            return (SPMFData)base.MemberwiseClone();
        }

        public int FormId { get; set; }
        public int FormIDId { get; set; }
        [JsonProperty("Id")]
        public int Id { get; set; }
        [JsonProperty("mastertablelist")]
        public string mastertablelist { get; set; }
        public string TableName { get; set; }
        public string TableNicName { get; set; }
        public string GenderName { get; set; }
        public string AddedBy { get; set; }
        public string AddedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }


    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class DesignationModel
    {
        [JsonProperty("DesignationId")]
        public int Id { get; set; }
        [JsonProperty("JobTitle")]
        public string JobTitle { get; set; }
        [JsonProperty("SmartPhoneApplicable")]
        public int IsSmartPhoneApplicable { get; set; }
     
    }
}
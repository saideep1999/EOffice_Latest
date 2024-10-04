using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class LocationModel
    {
        [JsonProperty("d")]
        public LocationResults List { get; set; }
    }
    public class LocationResults
    {
        [JsonProperty("results")]
        public List<LocationData> Locations { get; set; }
    }
    public class LocationData
    {
        [JsonProperty("LocationId")]
        public int LocationId { get; set; }
        [JsonProperty("LocationName")]
        public string LocationName { get; set; }
        [JsonProperty("AuthEmail")]
        public string AuthEmail { get; set; }


    }
}
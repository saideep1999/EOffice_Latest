using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class SoftwareModel
    {
        [JsonProperty("SoftwareID")]
        public int Id { get; set; }
        [JsonProperty("Classification")]
        public string Classification { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Version")]
        public string Version { get; set; }
        [JsonProperty("Publisher")]
        public string Publisher { get; set; }
        [JsonProperty("Product")]
        public string Product { get; set; }
        [JsonProperty("Category")]
        public string Category { get; set; }
    }
}
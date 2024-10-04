using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ResourceAccountLocationModel
    {
        [JsonProperty("d")]
        public ResourceAccountLocationResults List { get; set; }
    }
    public partial class ResourceAccountLocationResults
    {
        [JsonProperty("results")]
        public List<ResourceAccountLocation> ResourceAccountLocations { get; set; }
    }

    public class ResourceAccountLocation
    {
        [JsonProperty("ResAcctLocId")]
        public long ResAcctLocId { get; set; }
        [JsonProperty("Location")]
        public string Location { get; set; }
    }
}
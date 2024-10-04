using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ITServiceDeskContactModel
    {

        [JsonProperty("ContactId")]
        public int ContactId { get; set; }

        [JsonProperty("Email")]
        public string Email { get; set; }
        [JsonProperty("IsManager")]
        public int IsManager { get; set; }

        [JsonProperty("LocationId")]
        public int LocationId { get; set; }

    }
    public class ITAssetLocationModel
    {
        [JsonProperty("AssetEmailId")]
        public string AssetEmailId { get; set; }

        [JsonProperty("IsActive")]
        public long IsActive { get; set; }

        [JsonProperty("LocationName")]
        public string LocationName { get; set; }
    }

}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class ADUserModel
    {
        [JsonProperty("d")]
        public ADUserModelResults List { get; set; }
    }
    public partial class ADUserModelResults
    {
        [JsonProperty("results")]
        public List<ADData> ADUserList { get; set; }
    }

    public partial class ADData
    {
        [JsonProperty("Id")]
        public int UserId { get; set; }
        [JsonProperty("Title")]
        public string Title { get; set; }
        [JsonProperty("JobTitle")]
        public string JobTitle { get; set; }
        [JsonProperty("EMail")]
        public string EMail { get; set; }
    }
}
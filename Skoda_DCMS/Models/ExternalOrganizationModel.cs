using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ExternalOrganizationModel
    {

        [JsonProperty("EmployeeID")]
        public long Id { get; set; }
        [JsonProperty("Organization")]
        public string Organization { get; set; }
    }
}
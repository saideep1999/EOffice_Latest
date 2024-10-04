using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class AccessDoorListModel
    {
        [JsonProperty("ID")]
        public int ID { get; set; }
        [JsonProperty("LocationName")]
        public string LocationName { get; set; }
        [JsonProperty("Department")]
        public string Department { get; set; }
        [JsonProperty("DoorName")]
        public string DoorName { get; set; }
        [JsonProperty("EmailID")]
        public string EmailID { get; set; }
        [JsonProperty("DoorID")]
        public int DoorID { get; set; }
    }
}
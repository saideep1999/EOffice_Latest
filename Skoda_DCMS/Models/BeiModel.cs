using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class BeiModel
    {
        [JsonProperty("d")]
        public BeiResults list { get; set; }
    }

    public partial class BeiResults
    {
        [JsonProperty("results")]
        public List<BeiData> beiData { get; set; }
        //public List<BeiPartData> beiPartData { get; set; }
    }

    public partial class BeiData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        [JsonProperty("Vin")]
        public string Vin { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        public List<BeiPartData> Data { get; set; } 
    }

    public partial class BeiDataModel
    {
        [JsonProperty("d")]
        public BeiDataResults beiDatalist { get; set; }
    }

    public partial class BeiDataResults
    {
        [JsonProperty("results")]
        public List<BeiPartData> beiPartData { get; set; }
    }

    public partial class BeiPartData 
    {
        [JsonProperty("PartDesc")]
        public string PartDesc { get; set; }

        [JsonProperty("Quantity")]
        public long Quantity { get; set; }

        [JsonProperty("ID")]
        public long Id { get; set; }

        [JsonProperty("Availability")]
        public string Availability { get; set; }      

        [JsonProperty("Remark")]
        public string Remark { get; set; }

        [JsonProperty("Created")]
        public DateTime Created { get; set; }
        
    }

}
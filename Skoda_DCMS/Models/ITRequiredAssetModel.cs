using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class ITAssetRequiredModel
    {
        [JsonProperty("d")]
        public ITAssetRequiredResults List { get; set; }
    }
    public partial class ITAssetRequiredResults
    {
        [JsonProperty("results")]
        public List<ITRequiredAssetModel> ITAssetList { get; set; }
    }
    public class ITRequiredAssetModel
    {
        [JsonProperty("ITAssetReqID")]
        public long ITAssetReqID { get; set; }

        [JsonProperty("FormID")]
        public string FormID { get; set; }
        [JsonProperty("AssetName")]
        public string AssetName { get; set; }
        [JsonProperty("Others")]
        public bool Others { get; set; }

    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class SUCFModel
    {
        [JsonProperty("d")]
        public SUCFResults list { get; set; }
    }

    public partial class SUCFResults
    {
        [JsonProperty("results")]
        public List<SAPUserIdCreationModel> data { get; set; }
    }
    public class SAPUserIdCreationModel : ApplicantDataModel
    {
        //[JsonProperty("")]
        public List<SAPUserIDCreationDataModel> UserData { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
    }

    public partial class SUCFUserDataModel
    {
        [JsonProperty("d")]
        public SUCFUserDataResults list { get; set; }
    }

    public partial class SUCFUserDataResults
    {
        [JsonProperty("results")]
        public List<SAPUserIDCreationDataModel> data { get; set; }
    }

    public class SAPUserIDCreationDataModel
    {
        [JsonProperty("SrNo")]
        public int SrNo { get; set; }

        [JsonProperty("System")]
        public string System { get; set; }

        [JsonProperty("Client")]
        public string Client { get; set; }
        
        [JsonProperty("TypeOfUser")]
        public string Type { get; set; }
        
        [JsonProperty("Reason")]
        public string Reason { get; set; }
        
        [JsonProperty("Module")]
        public string Module { get; set; }
        
        [JsonProperty("ModuleDescription")]
        public string ModuleDescription { get; set; }
        
        [JsonProperty("SubModule")]
        public string SubModule { get; set; }
        
        [JsonProperty("RequestType")]
        public string RequestType { get; set; }
        
        [JsonProperty("TempFrom")]
        public DateTime? TempFrom { get; set; }
        
        [JsonProperty("TempTo")]
        public DateTime? TempTo { get; set; }
    }
}
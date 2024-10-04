using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class CourierRequestModel
    {
        [JsonProperty("d")]
        public CRFResults crflist { get; set; }
    }

    public partial class CRFResults
    {
        [JsonProperty("results")]
        public List<CRFData> crfData { get; set; }

    }

    public partial class CRFData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
    
        [JsonProperty("CourierType")]
        public string CourierType { get; set; }

        [JsonProperty("ConsignmentType")]
        public string ConsignmentType { get; set; }

        [JsonProperty("AddressofConsignee")]
        public string AddressofConsignee { get; set; }

        [JsonProperty("AddressofReceiver")]
        public string AddressofReceiver { get; set; }

        [JsonProperty("WeightDimension")]
        public string WeightDimension { get; set; }

        [JsonProperty("WeightDimensionIn")]
        public string WeightDimensionIn { get; set; }

        [JsonProperty("CourierInwardRegisterNo")]
        public string CourierInwardRegisterNo { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        
    }
}
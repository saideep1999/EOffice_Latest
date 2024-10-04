using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class BusTransportationFormModel
    {
        [JsonProperty("d")]
        public BTFResults btflist { get; set; }
    }

    public partial class BTFResults
    {
        [JsonProperty("results")]
        public List<BTFData> btfData { get; set; }
    }

    public partial class BTFData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("Address")]
        public string Address { get; set; }

        [JsonProperty("TransportationRequired")]
        public string TransportationRequired { get; set; }

        [JsonProperty("Gender")]
        public string Gender { get; set; }

        [JsonProperty("BusShift")]
        public string BusShift { get; set; }

        [JsonProperty("Distance")]
        public string Distance { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("ExternalOtherOrganizationName")]
        public string ExternalOtherOrganizationName { get; set; }


        [JsonProperty("OtherExternalOtherOrganizationName")]
        public string OtherExternalOtherOrganizationName { get; set; }



        [JsonProperty("PickupPoint")]
        public string PickupPoint { get; set; }

        [JsonProperty("BusRouteName")]
        public string BusRouteName { get; set; }

        [JsonProperty("BusRouteNumber")]
        public string BusRouteNumber { get; set; }

        [JsonProperty("Slab")]
        public string Slab { get; set; }

        [JsonProperty("Amount")]
        public string Amount { get; set; }

        [JsonProperty("Created_Date")]
        public DateTime Created_Date { get; set; }

        public string BusLocationName { get; set; }

        public string EmailId { get; set; }

    }

}
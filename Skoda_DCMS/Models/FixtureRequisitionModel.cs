using Newtonsoft.Json;
using Skoda_DCMS.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class FixtureRequisitionModel
    {
        [JsonProperty("d")]
        public FixtureRequisitionResults FixtureRequisitionResults { get; set; }
    }
    public class FixtureRequisitionResults
    {
        [JsonProperty("results")]
        public List<FixtureRequisitionData> FixtureRequisitionData { get; set; }

    }

    public class FixtureRequisitionData : ApplicantDataModel
    {
        public FixtureRequisitionData Clone()
        {
            return (FixtureRequisitionData)base.MemberwiseClone();
        }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("FixtureName")]
        public string FixtureName { get; set; }

        [JsonProperty("FixtureNo")]
        public string FixtureNo { get; set; }

        [JsonProperty("ProjectName")]
        public string ProjectName { get; set; }

        [JsonProperty("FromDate")]
        public DateTime? FromDate { get; set; }

        [JsonProperty("ToDate")]
        public DateTime? ToDate { get; set; }

        [JsonProperty("Reason")]
        public string Reason { get; set; }

        [JsonProperty("RpsPin")]
        public string RpsPin { get; set; }

        [JsonProperty("RpsPinRemark")]
        public string RpsPinRemark { get; set; }

        [JsonProperty("Clamps")]
        public string Clamps { get; set; }

        [JsonProperty("ClampsRemark")]
        public string ClampsRemark { get; set; }

        [JsonProperty("Wheels")]
        public string Wheels { get; set; }

        [JsonProperty("WheelsRemark")]
        public string WheelsRemark { get; set; }

        [JsonProperty("RpsStick")]
        public string RpsStick { get; set; }

        [JsonProperty("RpsStickRemark")]
        public string RpsStickRemark { get; set; }

        [JsonProperty("LoseElement")]
        public string LoseElement { get; set; }

        [JsonProperty("LoseRemark")]
        public string LoseRemark { get; set; }

        [JsonProperty("Mylers")]
        public string Mylers { get; set; }

        [JsonProperty("MylerRemark")]
        public string MylerRemark { get; set; }

        [JsonProperty("PinThreads")]
        public string PinThreads { get; set; }

        [JsonProperty("PinRemark")]
        public string PinRemark { get; set; }

        [JsonProperty("RestingPads")]
        public string RestingPads { get; set; }

        [JsonProperty("PadsRemark")]
        public string PadsRemark { get; set; }

        [JsonProperty("SlidersRemark")]
        public string SlidersRemark { get; set; }

        [JsonProperty("Sliders")]
        public string Sliders { get; set; }

        [JsonProperty("Kugel")]
        public string Kugel { get; set; }

        [JsonProperty("KugelRemark")]
        public string KugelRemark { get; set; }
        [JsonProperty("ARpsPin")]
        public string ARpsPin { get; set; }

        [JsonProperty("ARpsPinRemark")]
        public string ARpsPinRemark { get; set; }

        [JsonProperty("AClamps")]
        public string AClamps { get; set; }

        [JsonProperty("AClampsRemark")]
        public string AClampsRemark { get; set; }

        [JsonProperty("AWheels")]
        public string AWheels { get; set; }

        [JsonProperty("AWheelsRemark")]
        public string AWheelsRemark { get; set; }

        [JsonProperty("ARpsStick")]
        public string ARpsStick { get; set; }

        [JsonProperty("ARpsStickRemark")]
        public string ARpsStickRemark { get; set; }

        [JsonProperty("ALoseElement")]
        public string ALoseElement { get; set; }

        [JsonProperty("ALoseRemark")]
        public string ALoseRemark { get; set; }

        [JsonProperty("AMylers")]
        public string AMylers { get; set; }

        [JsonProperty("AMylerRemark")]
        public string AMylerRemark { get; set; }

        [JsonProperty("APinThreads")]
        public string APinThreads { get; set; }

        [JsonProperty("APinRemark")]
        public string APinRemark { get; set; }

        [JsonProperty("ARestingPads")]
        public string ARestingPads { get; set; }

        [JsonProperty("APadsRemark")]
        public string APadsRemark { get; set; }

        [JsonProperty("ASlidersRemark")]
        public string ASlidersRemark { get; set; }

        [JsonProperty("ASliders")]
        public string ASliders { get; set; }

        [JsonProperty("AKugel")]
        public string AKugel { get; set; }

        [JsonProperty("AKugelRemark")]
        public string AKugelRemark { get; set; }
        public string FormSrId { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormIDQFRF { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

    }
}
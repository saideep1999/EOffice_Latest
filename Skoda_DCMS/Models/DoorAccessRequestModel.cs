using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class DoorAccessRequestModel
    {
        [JsonProperty("d")]
        public DoorAccessRequestResults List { get; set; }
    }

    public partial class DoorAccessRequestResults
    {
        [JsonProperty("results")]
        public List<DoorAccessRequestData> DoorAccessRequestList { get; set; }

    }

    public partial class DoorAccessRequestData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        [JsonProperty("IDCardNumber")]
        public string IDCardNumber { get; set; }
    }

    public partial class SelectedAcsessDoorModel
    {
        [JsonProperty("d")]
        public SelectedAcsessDoorResults List { get; set; }
    }
    public partial class SelectedAcsessDoorResults
    {
        [JsonProperty("results")]
        public List<SelectedAcsessDoorDto> AccessDoorList { get; set; }
    }

    public class SelectedAcsessDoorDto
    {
        [JsonProperty("ID")]
        public long ID { get; set; }
        public FormLookup FormID { get; set; }
        [JsonProperty("Location")]
        public string Location { get; set; }
        [JsonProperty("DoorDepartment")]
        public string DoorDepartment { get; set; }
        [JsonProperty("DoorName")]
        public string DoorName { get; set; }
        [JsonProperty("AuthPersonEmail")]
        public string AuthPersonEmail { get; set; }
        [JsonProperty("DoorID")]
        public int DoorID { get; set; }
        public string DoorAccessReqId { get; set; }
    }
}
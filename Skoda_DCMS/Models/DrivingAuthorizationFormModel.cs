using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class DrivingAuthorizationFormModel
    {
        [JsonProperty("d")]
        public DAFResults daflist { get; set; }
    }
    public partial class DAFResults
    {
        [JsonProperty("results")]
        public List<DAFData> dafData { get; set; }

    }

    public class FormID
    {
        public int ID { get; set; }
        public string Created { get; set; }
    }

    public partial class DAFData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        public FormID FormID { get; set; }
        [JsonProperty("FormIDId")]
        public string FormIDId { get; set; }
        //[JsonProperty("FormID")]
        //public FormLookup FormID { get; set; }
        [JsonProperty("SubDepartment")]
        public string SubDepartment { get; set; }

        [JsonProperty("DateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("LicenseNumber")]
        public string LicenseNumber { get; set; }

        [JsonProperty("ValidFrom")]
        public DateTime? ValidFrom { get; set; }

        [JsonProperty("ValidTill")]
        public DateTime? ValidTill { get; set; }

        [JsonProperty("DrivingExperience")]
        public long DrivingExperience { get; set; }

        [JsonProperty("VehiclesDriven")]
        public string VehiclesDriven { get; set; }

        [JsonProperty("Address")]
        public string Address { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("AuthorizationForInternal")]
        public string AuthorizationForInternal { get; set; }

        [JsonProperty("AuthorizationForTestTrack")]
        public string AuthorizationForTestTrack { get; set; }

        [JsonProperty("AuthorizationForExternal")]
        public string AuthorizationForExternal { get; set; }

        [JsonProperty("AuthorizationForMaterialHandling")]
        public string AuthorizationForMaterialHandling { get; set; }

        //[JsonProperty("Mobile")]
        //public long Mobile { get; set; }

        //[JsonProperty("LandlineNumber")]
        //public long LandlineNumber { get; set; }

        [JsonProperty("BloodGroup")]
        public string BloodGroup { get; set; }

        [JsonProperty("EyeSight")]
        public string EyeSight { get; set; }
        public string Imagepath { get; set; }
        public string Imagepathforlicence { get; set; }

        [JsonProperty("LT")]
        public float LT { get; set; }

        [JsonProperty("RT")]
        public float RT { get; set; }

        [JsonProperty("HistoryofEpilepsy")]
        public string HistoryofEpilepsy { get; set; }

        [JsonProperty("Remarks")]
        public string Remarks { get; set; }

        [JsonProperty("Certification")]
        public string Certification { get; set; }
        public DAFData Clone()
        {
            return (DAFData)base.MemberwiseClone();
        }
    }
}
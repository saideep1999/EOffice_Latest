using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class MMRModel
    {
        [JsonProperty("d")]
        public MMRResults MMRResults { get; set; }
    }
    public class MMRResults
    {
        [JsonProperty("results")]
        public List<MMRData> MMRData { get; set; }

    }

    public class MMRData : ApplicantDataModel
    {
        public MMRData Clone()
        {
            return (MMRData)base.MemberwiseClone();
        }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("ExistingDepartment")]
        public string ExistingDepartment { get; set; }

        [JsonProperty("NewDepartment")]
        public string NewDepartment { get; set; }

        [JsonProperty("FutureOwner")]
        public string FutureOwner { get; set; }

        [JsonProperty("FutureOwnerEmail")]
        public string FutureOwnerEmail { get; set; }

        [JsonProperty("MMRIdentification")]
        public string MMRIdentification { get; set; }

        [JsonProperty("HandoverDate")]
        public DateTime? HandoverDate { get; set; }

        [JsonProperty("MMRDescription")]
        public string MMRDescription { get; set; }

        [JsonProperty("TransferType")]
        public string TransferType { get; set; }

        [JsonProperty("MMREpus")]
        public DateTime MMREpus { get; set; }

        [JsonProperty("NewOwnEPUS")]
        public DateTime NewOwnEPUS { get; set; }

        [JsonProperty("Details")]
        public string Details { get; set; }

        public string FormSrId { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("attachedfile")]
        public string attachedfile { get; set; }

        [JsonProperty("attachedfileName")]
        public string attachedfileName { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDMMRF { get; set; }

        [JsonProperty("TransferFromDate")]
        public DateTime? TransferFromDate { get; set; }
        [JsonProperty("TransferToDate")]
        public DateTime? TransferToDate { get; set; }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class UserRequestModel
    {
        [JsonProperty("d")]
        public URCFResults URCFResults { get; set; }
    }

    public partial class URCFResults
    {
        [JsonProperty("results")]
        public List<UserRequestData> UserRequestData { get; set; }

    }

    public class UserRequestData : ApplicantDataModel
    {
        public UserRequestData Clone()
        {
            return (UserRequestData)base.MemberwiseClone();
        }

        public List<ApplicationCategoryData> ApplicationCategoryData { get; set; }

        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("Brand")]
        public string Brand { get; set; }

        [JsonProperty("ServiceType")]
        public string ServiceType { get; set; }

        [JsonProperty("TypeofRequest")]
        public string TypeofRequest { get; set; }
        public string FormSrId { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDURCF { get; set; }
        public string attachedfile { get; set; }

        public string attachedfileName { get; set; }

        public string attachedfileName1 { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }


        public string ServiceCategory { get; set; }
        public string ServiceSubCategory { get; set; }
        public string Role { get; set; }
        public string AccessType { get; set; }
        public string BrandApp { get; set; }

        public string ApplicationUserID { get; set; }
        public string totalrows { get; set; }

        public string fileToUpload { get; set; }



    }

    public partial class AttachmentURCFResults
    {
        [JsonProperty("results")]
        public List<attachmentURCFData> attachmentURCFData { get; set; }
    }

    public partial class attachmentURCFData
    {
        [JsonProperty("FileName")]
        public string UserFileName { get; set; }

        [JsonProperty("ServerRelativeUrl")]
        public string UserServerRelativeUrl { get; set; }
    }

    public partial class ApplicationCategoryModel
    {
        [JsonProperty("d")]
        public ApplicationCategoryResults ApplicationCategoryResults { get; set; }
    }
    public partial class ApplicationCategoryResults
    {
        [JsonProperty("results")]
        public List<ApplicationCategoryData> data { get; set; }
    }

    public class ApplicationCategoryData
    {
        [JsonProperty("ID")]
        public int AppId { get; set; }

        [JsonProperty("AppId")]
        public FormLookup AppCatId { get; set; }
        public int ListAppId { get; set; }

        [JsonProperty("ServiceType")]
        public string ServiceType { get; set; }

        [JsonProperty("ServiceCategory")]
        public string ServiceCategory { get; set; }

        [JsonProperty("ServiceSubCategory")]
        public string ServiceSubCategory { get; set; }

        [JsonProperty("Role")]
        public string Role { get; set; }

        [JsonProperty("AccessType")]
        public string AccessType { get; set; }

        [JsonProperty("Brand")]
        public string BrandApp { get; set; }

        [JsonProperty("ApplicationUserID")]
        public string ApplicationUserID { get; set; }

        [JsonProperty("OwnerName")]
        public string OwnerName { get; set; }

        [JsonProperty("OwnerEmail")]
        public string OwnerEmail { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDAppCat { get; set; }
    }
}
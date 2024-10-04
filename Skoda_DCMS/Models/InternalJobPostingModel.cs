using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class InternalJobPostingModel
    {
        [JsonProperty("d")]
        public InternalJobPostingResults List { get; set; }
    }
    public partial class InternalJobPostingResults
    {
        [JsonProperty("results")]
        public List<InternalJobPostingData> IJPList { get; set; }
    }
    public partial class InternalJobPostingData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("IJPReference")]
        public string IJPReference { get; set; }

        [JsonProperty("MPRReference")]
        public string MPRReference { get; set; }      

        [JsonProperty("DateOfJoining")]
        public DateTime DateOfJoining { get; set; }
        [JsonProperty("Qualification")]
        public string Qualification { get; set; }

        [JsonProperty("Level")]
        public int Level { get; set; }
        [JsonProperty("CurrentRole")]
        public string CurrentRole { get; set; }
        [JsonProperty("CurrentDepartmentDuration")]
        public string CurrentDepartmentDuration { get; set; }
        [JsonProperty("CurrentRoleDuration")]
        public string CurrentRoleDuration { get; set; }
        [JsonProperty("CurrentReportingManagerName")]
        public string CurrentReportingManagerName { get; set; }
        [JsonProperty("PositionAndDepartmentAppliedFor")]
        public string PositionAndDepartmentAppliedFor { get; set; }
        [JsonProperty("ReasonForChangingJobProfile")]
        public string ReasonForChangingJobProfile { get; set; }
        [JsonProperty("Achievements")]
        public string Achievements { get; set; }
        [JsonProperty("AboutRoleAppliedFor")]
        public string AboutRoleAppliedFor { get; set; }


        //Approval List     

        [JsonProperty("ApproverEmailId")]
        public string ApproverEmailId { get; set; }

        [JsonProperty("ApproverEmployeeCode")]
        public int ApproverEmployeeCode { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentIJPFResults attachmentIJPFlist { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("OnBehalfLocation")]
        public string OnBehalfLocation { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
   
    }
    public partial class AttachmentIJPFResults
    {
        [JsonProperty("results")]
        public List<AttachmentIJPFData> attachmentIJPFData { get; set; }
    }

    public partial class AttachmentIJPFData
    {
        [JsonProperty("FileName")]
        public string FileName { get; set; }

        [JsonProperty("ServerRelativeUrl")]
        public string ServerRelativeUrl { get; set; }
    }


    public class IJPFEmploymentDetailsModel
    {
        [JsonProperty("d")]
        public IJPFEmploymentDetailsResults List { get; set; }
    }
    public partial class IJPFEmploymentDetailsResults
    {
        [JsonProperty("results")]
        public List<IJPFEmploymentDetailsData> IJPEDList { get; set; }
    }
    public partial class IJPFEmploymentDetailsData
    {
        [JsonProperty("ID")]
        public long ID { get; set; }

        [JsonProperty("IJPFID")]
        public long IJPFID { get; set; }

        [JsonProperty("Organisation")]
        public string Organisation { get; set; }

        [JsonProperty("Designation")]
        public string Designation { get; set; }

        [JsonProperty("FromDate")]
        public DateTime FromDate { get; set; }

        [JsonProperty("ToDate")]
        public DateTime ToDate { get; set; }

        [JsonProperty("MainResponsibilities")]
        public string MainResponsibilities { get; set; }

     
    }
}
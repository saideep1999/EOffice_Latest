using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class GAIFModel
    {
        [JsonProperty("d")]
        public GAIFResults list { get; set; }

        //[JsonProperty("FormID")]
        //public FormLookup FormID { get; set; }
    }

    public partial class GAIFResults
    {
        [JsonProperty("results")]
        public List<GiftsInvitationData> data { get; set; }

    }
    public class GiftsInvitationData : ApplicantDataModel
    {
        public GiftsInvitationData Clone()
        {
            return (GiftsInvitationData)base.MemberwiseClone();
        }
        public List<QuestionDto> QuestionData { get; set; }

        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        public string totalrows { get; set; }

        public string fileToUpload { get; set; }

        public string txtQuestion_ { get; set; }

        [JsonProperty("RequestType")]
        public string RequestType { get; set; }

        [JsonProperty("Answers")]
        public string Answers { get; set; }        

        [JsonProperty("Transaction")]
        public string Transaction { get; set; }

        [JsonProperty("IsGiftOrInviteToPublicOfficial")]
        public string IsGiftOrInviteToPublicOfficial { get; set; }

        [JsonProperty("NameRelationOtherDet")]
        public string NameRelationOtherDet { get; set; }

        [JsonProperty("FrequencyOfGiftsOrInvitationfrm")]
        public string FrequencyOfGiftsOrInvitationfrm { get; set; }

        [JsonProperty("ApproxValueOfGiftsInvt")]
        public string ApproxValueOfGiftsInvt { get; set; }

        [JsonProperty("ReasonForGiftingInvitation")]
        public string ReasonForGiftingInvitation { get; set; }

        [JsonProperty("GiftIsAcceptedRefused")]
        public string GiftIsAcceptedRefused { get; set; }

        [JsonProperty("ReasonGiftIsAcceptedRefused")]
        public string ReasonGiftIsAcceptedRefused { get; set; }

        [JsonProperty("GiftTobeDepoWithGRC")]
        public string GiftTobeDepoWithGRC { get; set; }
        
        public string attachedfile { get; set; }

        public string FormSrId { get; set; }

        public string attachedfileName { get; set; }

        public string attachedfileName1 { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDGift { get; set; }

        [JsonProperty("Question")]
        public string Question { get; set; }


    }
    public partial class AttachmentGAIFResults
    {
        [JsonProperty("results")]
        public List<AttachmentGAIFData> attachmentGAIFData { get; set; }
    }

    public partial class AttachmentGAIFData
    {
        [JsonProperty("FileName")]
        public string GiftFileName { get; set; }

        [JsonProperty("ServerRelativeUrl")]
        public string GiftServerRelativeUrl { get; set; }
    }


    public partial class QuestionModel
    {
        [JsonProperty("d")]
        public QuestionResults QuestionList { get; set; }
    }
    public partial class QuestionResults
    {
        [JsonProperty("results")]
        public List<QuestionDto> data { get; set; }
    }
    public class QuestionDto
    {
        [JsonProperty("QuestionId")]
        public FormLookup QuestionId { get; set; }

        [JsonProperty("Question")]
        public string Question { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormIDQuestion { get; set; }
    }
}
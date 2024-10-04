using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class DLADModel
    {
        [JsonProperty("d")]
        public DLADResults DLADResults { get; set; }
    }

    public class DLADResults
    {
        [JsonProperty("results")]
        public List<DLADFormModel> DLADFormModel { get; set; }
    }
    public class DLADFormModel : ApplicantDataModel
    {
        public DLADFormModel Clone()
        {
            return (DLADFormModel)base.MemberwiseClone();
        }
        [JsonProperty("Id")]
        public int Id { get; set; }
        public List<IndividualAdminServerList> IndividualAdminServerList { get; set; }
        [JsonProperty("FormIDId")]
        public string FormIDId { get; set; }
        public string EmployeeEmailId { get; set; }
       
        public string FormSrId { get; set; }
        public string ListItemIdId { get; set; }
        public string chklocation { get; set; }
        public string Location { get; set; }

        public string DurationType { get; set; }
        public int ServiceType { get; set; }
        public string chkdldaPermanent { get; set; }
        public string chkName { get; set; }
        public string txt_make1 { get; set; }
        public string txt_make2 { get; set; }
        public string txt_make3 { get; set; }
        public string isNewServerAdminAccess { get; set; }
        public string isNewLaptopAdminAccess { get; set; }
        public string DLADIndividual { get; set; }
        public string DLADServerAccess { get; set; }
        public string DLADLaptopAccess { get; set; }
        public string AccessTypeIndividual { get; set; }
        public string AccessTypeServerAccess { get; set; }
        public string AccessTypeLaptopAccess { get; set; }
        public string DurationIndividual { get; set; }
        public string DurationServerAccess { get; set; }
        public string DurationLaptopAccess { get; set; }
        public string BusinessReason { get; set; }
        public string DLAD_Admin { get; set; }
        public string AccessType_Admin { get; set; }
        public string Duration_Admin { get; set; }
        public DateTime FromDate_Admin { get; set; }
        public DateTime ToDate_Admin { get; set; }
        public string DLAD_LD { get; set; }
        public string AccessType_LD { get; set; }
        public string Duration_LD { get; set; }
        public DateTime FromDate_LD { get; set; }
        public DateTime ToDate_LD { get; set; }
        public DateTime CreatedDate { get; set; }
        public string DLAD_Individual { get; set; }
        public string AccessType_Individual { get; set; }
        public string Duration_Individual { get; set; }
        public DateTime FromDate_Individual { get; set; }
        public DateTime ToDate_Individual { get; set; }
        public DateTime TemporaryDateFrom { get; set; }
        public DateTime TemporaryDateTo { get; set; }
        public string HostName { get; set; }
        public string FormType { get; set; }
        public string AssetType { get; set; }
        public string ticketNumber { get; set; }
        public string ActionDLAD { get; set; }

        public string ApplicationNameRole { get; set; }
        public string ServerHostName { get; set; }
        public string ServerIPAddress { get; set; }
        public string chk1 { get; set; }
        public string chk2 { get; set; }
        public string chk3 { get; set; }
        public string ServerTypeDDL { get; set; }
        public string attachedfile { get; set; }
        public string attachedfileName { get; set; }
        public string IndividualAdminServerListDataList { get; set; }
        public string fileToUpload { get; set; }
        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }
        public partial class AttachmentFilesResults
        {
            [JsonProperty("results")]
            public List<Attachments> Attachments { get; set; }


        }
    }

    public class IndividualAdminServerList
    {
        public int SrNo { get; set; }
        public string ApplicationNameRole { get; set; }
        public string ServerHostName { get; set; }
        public string ServerIPAddress { get; set; }
    }


}
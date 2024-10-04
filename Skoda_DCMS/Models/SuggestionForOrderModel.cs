using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public partial class SuggestionForOrderModel
    {
        [JsonProperty("d")]
        public SuggestionForOrderResults List { get; set; }
    }

    public partial class SuggestionForOrderResults
    {
        [JsonProperty("results")]
        public List<SuggestionForOrderData> SuggestionForOrderList { get; set; }
    }

    public partial class SuggestionForOrderData
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
        [JsonProperty("EmployeeType")]
        public string EmployeeType { get; set; }
        [JsonProperty("ExternalOrganizationName")]
        public string ExternalOrganizationName { get; set; }
        [JsonProperty("ExternalOtherOrganizationName")]
        public string ExternalOtherOrganizationName { get; set; }
        [JsonProperty("EmployeeCode")]
        public long EmployeeCode { get; set; }
        [JsonProperty("EmployeeCCCode")]
        public long EmployeeCCCode { get; set; }
        [JsonProperty("EmployeeUserId")]
        public string EmployeeUserId { get; set; }
        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; }
        [JsonProperty("EmployeeDepartment")]
        public string EmployeeDepartment { get; set; }
        [JsonProperty("EmployeeDesignation")]
        public string EmployeeDesignation { get; set; }
        [JsonProperty("EmployeeLocation")]
        public string EmployeeLocation { get; set; }
        [JsonProperty("EmployeeContactNo")]
        public string EmployeeContactNo { get; set; }
        [JsonProperty("EmployeeEmailId")]
        public string EmployeeEmailId { get; set; }

        //Other Employee Fields
        [JsonProperty("OnBehalfOption")]
        public string OnBehalfOption { get; set; }
        [JsonProperty("OtherEmployeeType")]
        public string OtherEmployeeType { get; set; }
        [JsonProperty("OtherExternalOrganizationName")]
        public string OtherExternalOrganizationName { get; set; }
        [JsonProperty("OtherExternalOtherOrgName")]
        public string OtherExternalOtherOrganizationName { get; set; }
        [JsonProperty("OtherEmployeeCode")]
        public long OtherEmployeeCode { get; set; }

        [JsonProperty("OtherEmployeeCCCode")]
        public long OtherEmployeeCCCode { get; set; }

        [JsonProperty("OtherEmployeeUserId")]
        public string OtherEmployeeUserId { get; set; }
        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }
        [JsonProperty("OtherEmployeeDepartment")]
        public string OtherEmployeeDepartment { get; set; }
        [JsonProperty("OtherEmployeeDesignation")]
        public string OtherEmployeeDesignation { get; set; }//string
        [JsonProperty("OtherEmployeeLocation")]
        public string OtherEmployeeLocation { get; set; }//string
        [JsonProperty("OtherEmployeeContactNo")]
        public string OtherEmployeeContactNo { get; set; }
        [JsonProperty("OtherEmployeeEmailId")]
        public string OtherEmployeeEmailId { get; set; }
        [JsonProperty("EmployeeRequestType")]
        public string EmployeeRequestType { get; set; }
        [JsonProperty("TempFrom")]
        public Nullable<System.DateTime> TempFrom { get; set; }
        [JsonProperty("TempTo")]
        public Nullable<System.DateTime> TempTo { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }

        [JsonProperty("ShopCartNumber")]
        public string ShopCartNumber { get; set; }

        [JsonProperty("ConcernSection")]
        public string ConcernSection { get; set; }

        [JsonProperty("Budget")]
        public decimal Budget { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("TechDisqualify")]
        public string TechDisqualify { get; set; }
        [JsonProperty("SuggestOrder")]
        public string SuggestOrder { get; set; }
        [JsonProperty("DeviationNoteForm")]
        public string DeviationNoteForm { get; set; }
        [JsonProperty("ConversionValue")]
        public decimal ConversionValue { get; set; }
        [JsonProperty("Department")]
        public string Department { get; set; }
        [JsonProperty("Date")]
        public DateTime Date { get; set; }
        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }
        
    }

    public partial class SFOOrderItemsModel
    {
        [JsonProperty("d")]
        public SFOOrderItemsResults List { get; set; }
    }
    public partial class SFOOrderItemsResults
    {
        [JsonProperty("results")]
        public List<SFOOrderItemsData> SFOOrderItemsList { get; set; }

    }
    public class SFOOrderItemsData
    {
        [JsonProperty("ID")]
        public long OrderItemsID { get; set; }
        [JsonProperty("OrderDetailsID")]
        public long OrderDetailsID { get; set; }
        [JsonProperty("SupplierName")]
        public string SupplierName { get; set; }
        [JsonProperty("TechAcceptance")]
        public string TechAcceptance { get; set; }
        [JsonProperty("OfferPrice")]
        public decimal? OfferPrice { get; set; }
      
        [JsonProperty("Comments")]
        public string Comments { get; set; }
        [JsonProperty("Currency")]
        public string Currency { get; set; }
        [JsonProperty("ConversionRate")]
        public decimal ConversionRate { get; set; }
    }

    public partial class AttachmentFilesResults
    {
        [JsonProperty("results")]
        public List<Attachments> Attachments { get; set; }

    }
    public partial class Attachments
    {
        public string title { get; set; }
        [JsonProperty("FileName")]
        public string FileName { get; set; }
        [JsonProperty("ServerRelativeUrl")]
        public string ServerRelativeUrl { get; set; }
        public string file_format { get; set; }
        public byte[] file_data { get; set; }
    }
}
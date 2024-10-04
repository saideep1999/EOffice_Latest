using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Profile;
using Newtonsoft.Json;
using Skoda_DCMS.Converters;

namespace Skoda_DCMS.Models
{
    public class DashboardModel
    {
        [JsonProperty("d")]
        public DataModel Data { get; set; }
    }
    public partial class DataModel
    {
        [JsonProperty("results")]
        public List<FormData> Forms { get; set; }


        public List<FormData> FormsRequest { get; set; }
        public List<FormData> NewlyAddedForms { get; set; }
        public List<FormData> FreqUsedForms { get; set; }
        public FormStatus StatusCount { get; set; }
        public List<(string, int)> DepartmentList { get; set; }
        public string BreadCrumbTitleFirst { get; set; }
        public string BreadCrumbTitleSecond { get; set; }
        public List<KeyValuePair<string, string>> BreadCrumbs { get; set; }
        public string PartialViewName { get; set; }
        public string UniqueFormName { get; set; }
        public int? FormParentId { get; set; }
        public string FullFormName { get; set; }
        public string Department { get; set; }
        public string FormOwner { get; set; }
        public string ControllerName { get; set; }
        public string RunWorkflow { get; set; }
        public int? Level { get; set; }
        public string Logic { get; set; }
        public int? IsActive { get; set; }
        public int? ApproverId { get; set; }
    }
    public partial class FormData
    {
        public FormData Clone()
        {
            return (FormData)base.MemberwiseClone();
        }
        //Approver data
        [JsonProperty("FormId")]
        public FormLookup FormRelation { get; set; }

        [JsonProperty("Author")]
        public Author Author { get; set; }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("RowId")]
        public int RowId { get; set; }

        [JsonProperty("NextApproverId")]
        public int NextApproverId { get; set; }

        [JsonProperty("Modified")]
        public DateTime RecievedDate { get; set; }
        public string strRecievedDate { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }

        [JsonProperty("ApproverStatus")]
        public string ApproverStatus { get; set; }

        [JsonProperty("AuthorityToEdit")]
        public int? AuthorityToEdit { get; set; }

        [JsonProperty("AssistantForEmployeeUserId")]
        public int? AssistantForEmployeeUserId { get; set; }

        //Form data
        [JsonProperty("FormName")]
        public string FormName { get; set; }

        [JsonProperty("UniqueFormName")]
        public string UniqueFormName { get; set; }

        [JsonProperty("ListName")]
        public string ListName { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("ID")]
        public int UniqueFormId { get; set; }

        [JsonProperty("ApplicantName")]
        public string ApplicantName { get; set; }

        [JsonProperty("DataRowId")]
        public int? DataRowId { get; set; }

        [JsonProperty("Created")]
        public DateTime FormCreatedDate { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        //[JsonProperty("TimeStamp")]
        //public DateTime TimeStamp { get; set; }

        //User Forms Data
        [JsonProperty("FormParentId")]
        public FormParentModel FormParent { get; set; }
       
        public int FormCount { get; set; }

        [JsonProperty("IsDisable")]
        public string IsDisable { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("ReleaseDate")]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("Businessneed")]
        public string Businessneed { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("FormOwner")]
        public string FormOwner { get; set; }

        [JsonProperty("ApprovalType")]
        public string ApprovalType { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("CostCenterNumber")]
        public string CostCenterNumber { get; set; }

        [JsonProperty("ControllerName")]
        public string ControllerName { get; set; }

        [JsonProperty("RunWorkflow")]
        public string RunWorkflow { get; set; }

        [JsonProperty("Level")]
        public int? Level { get; set; }

        [JsonProperty("Logic")]
        public string Logic { get; set; }

        [JsonProperty("IsActive")]
        public int? IsActive { get; set; }

        [JsonProperty("ApproverId")]
        public int? ApproverId { get; set; }

        [JsonProperty("SubmitterId")]
        public int? SubmitterId { get; set; }
        [JsonConverter(typeof(LowerCaseConverter))]

        [JsonProperty("SubmitterUserName")]
        public string SubmitterUserName { get; set; }

        [JsonConverter(typeof(LowerCaseConverter))]

        [JsonProperty("ApproverUserName")]
        public string ApproverUserName { get; set; }

        [JsonProperty("ApproverName")]
        public string ApproverName { get; set; }


        [JsonProperty("Applicationname")]
        public string Applicationname { get; set; }

        [JsonProperty("Applicationurl")]
        public string Applicationurl { get; set; }

        [JsonProperty("Applicationaccess")]
        public string Applicationaccess { get; set; }
        [JsonProperty("Accessgroup")]
        public string Accessgroup { get; set; }


        //Cab Form

        [JsonProperty("Destination")]
        public string Destination { get; set; }

        [JsonProperty("ReportingPlaceWithAddress")]
        public string ReportingPlaceWithAddress { get; set; }

        [JsonProperty("AirportPickUpDrop")]
        public string AirportPickUpDrop { get; set; }

        [JsonProperty("FlightNo")]
        public string FlightNo { get; set; }

        [JsonProperty("FlightTime")]
        public DateTime FlightTime { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("UserContactNumber")]
        public long UserContactNumber { get; set; }

        [JsonProperty("ReportingTime")]
        public DateTime ReportingTime { get; set; }

        [JsonProperty("CarRequiredFromDate")]
        public DateTime CarRequiredFromDate { get; set; }

        [JsonProperty("CarRequiredToDate")]
        public DateTime CarRequiredToDate { get; set; }



        //Bus Form
        [JsonProperty("TransportationRequired")]
        public string TransportationRequired { get; set; }

        [JsonProperty("Gender")]
        public string Gender { get; set; }

        [JsonProperty("BusShift")]
        public string BusShift { get; set; }

        [JsonProperty("PickupPoint")]
        public string PickupPoint { get; set; }

        [JsonProperty("BusRouteName")]
        public string BusRouteName { get; set; }

        [JsonProperty("BusRouteNumber")]
        public string BusRouteNumber { get; set; }

        [JsonProperty("Slab")]
        public string Slab { get; set; }

        [JsonProperty("Distance")]
        public string Distance { get; set; }

        [JsonProperty("Address")]
        public string Address { get; set; }

        [JsonProperty("SlabAmount")]
        public string SlabAmount { get; set; }

        [JsonProperty("Region")]
        public string Region { get; set; }

        public string CompanyName { get; set; }
        public string EmployeeName { get; set; }
        public DateTime Created_Date { get; set; }

        [JsonProperty("ExternalOrganizationName")]
        public string ExternalOrganizationName { get; set; }

        [JsonProperty("OtherExternalOrganizationName")]
        public string OtherExternalOrganizationName { get; set; }

        [JsonProperty("EmployeeType")]
        public string EmployeeType { get; set; }

        [JsonProperty("EmployeeContactNo")]
        public long EmployeeContactNo { get; set; }

        [JsonProperty("EmployeeCode")]
        public long EmployeeCode { get; set; }

        [JsonProperty("OtherEmployeeContactNo")]
        public long OtherEmployeeContactNo { get; set; }

        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }

        [JsonProperty("OtherEmployeeCode")]
        public long OtherEmployeeCode { get; set; }

        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }

        [JsonProperty("OnBehalfOption")]
        public string OnBehalfOption { get; set; }

        [JsonProperty("RelationWith")]
        public int? RelationWith { get; set; } = 0;

        [JsonProperty("RelationId")]
        public int? RelationId { get; set; } = 0;


        //MRF
        public string PartNumber { get; set; }
        [JsonProperty("PartDescription")]
        public string PartDescription { get; set; }
        [JsonProperty("Quantity")]
        public int Quantity { get; set; }
        [JsonProperty("Remarks")]
        public string Remarks { get; set; }
        // public List<MaterialDetailsData> MaterialDetailsList { get; set; } 
        [JsonProperty("RequestNumber")]
        public string RequestNumber { get; set; }
        [JsonProperty("RequestTo")]
        public string RequestTo { get; set; }
        [JsonProperty("RequestFrom")]
        public string RequestFrom { get; set; }
        [JsonProperty("RequestFromDate")]
        public DateTime? RequestFromDate { get; set; }
        [JsonProperty("RequestToDate")]
        public DateTime? RequestToDate { get; set; }


        //GAIF Form
        public string RequestType { get; set; }
        public string Transaction { get; set; }
        public string IsGiftOrInviteToPublicOfficial { get; set; }
        public string NameRelationOtherDet { get; set; }
        public string FrequencyOfGiftsOrInvitationfrm { get; set; }
        public string ApproxValueOfGiftsInvt { get; set; }
        public string ReasonForGiftingInvitation { get; set; }
        public string GiftIsAcceptedRefused { get; set; }
        public string ReasonGiftIsAcceptedRefused { get; set; }
        public string GiftTobeDepoWithGRC { get; set; }
        public string Answers { get; set; }
        public string Question { get; set; }

        //NGCF Form
        public string NameOfGLToOpen { get; set; }
        public string NatureOfTranInGL { get; set; }
        public string Purpose { get; set; }
        public DateTime DateToOpenNewGL { get; set; }
        public string GLCode { get; set; }
        public string GLName { get; set; }
        public string GLSeries { get; set; }
        public string NewGLNo { get; set; }
        public string CommitmentItem { get; set; }


        //URCF
        public string Brand { get; set; }
        public string ServiceType { get; set; }
        public string TypeofRequest { get; set; }
        public string ServiceCategory { get; set; }
        public string ServiceSubCategory { get; set; }
        public string Role { get; set; }
        public string AccessType { get; set; }
        public string BrandApp { get; set; }
        public string ApplicationUserID { get; set; }

        //QMCR
        public string FormType { get; set; }

        public string ModelQCM { get; set; }

        public string Series { get; set; }

        public string PartQuantity { get; set; }

        public string OtherDetails { get; set; }

        public string ProblemSheet { get; set; }

        public string TrialReported { get; set; }

        public string ProblemReported { get; set; }

        public string Details { get; set; }
        public string OtherEmployeeDesignation { get; set; }
        public string OtherEmployeeDepartment { get; set; }
        public string EmployeeDesignation { get; set; }
        public string EmployeeDepartment { get; set; }

        public string PartName { get; set; }

        // MMRF

        public string ExistingDepartment { get; set; }

        public string NewDepartment { get; set; }

        public string FutureOwner { get; set; }

        public string FutureOwnerEmail { get; set; }

        public string MMRIdentification { get; set; }

        public DateTime? HandoverDate { get; set; }

        public string MMRDescription { get; set; }

        public string TransferType { get; set; }

        public DateTime MMREpus { get; set; }

        //APFP Form

        public int SrNo { get; set; }
        public string WeekNo { get; set; }
        public string Topic { get; set; }
        public string Project { get; set; }
        public string Reason { get; set; }

        // IMAC Form

        public string IMACtype { get; set; }
        public string SubCategory { get; set; }
        public string Make { get; set; }
        public string Modal { get; set; }
        public string AssetType { get; set; }
        public string Acknowledgement { get; set; }
        public string AssignType { get; set; }
        public string AssetName { get; set; }
        public string SerialNumber { get; set; }
        public string HostName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        //QFRF Form
        public string FixtureName { get; set; }

        public string FixtureNo { get; set; }

        public string ProjectName { get; set; }

        public string RpsPin { get; set; }

        public string RpsPinRemark { get; set; }

        public string Clamps { get; set; }

        public string ClampsRemark { get; set; }

        public string Wheels { get; set; }

        public string WheelsRemark { get; set; }

        public string RpsStick { get; set; }

        public string RpsStickRemark { get; set; }

        public string LoseElement { get; set; }

        public string LoseRemark { get; set; }

        public string Mylers { get; set; }

        public string MylerRemark { get; set; }

        public string PinThreads { get; set; }

        public string PinRemark { get; set; }

        public string RestingPads { get; set; }

        public string PadsRemark { get; set; }

        public string SlidersRemark { get; set; }

        public string Sliders { get; set; }

        public string Kugel { get; set; }

        //EQSA FORM

        public string EQSAEmployeeName { get; set; }
        public string EmployeeID { get; set; }
        public string LogicCardID { get; set; }
        public string StationName { get; set; }
        public string Shop { get; set; }
        public string AccessGroup { get; set; }
        public string BusinessJustification { get; set; }
        public string KugelRemark { get; set; }
    }

    public partial class Author
    {
        [JsonProperty("Title")]
        public string Submitter { get; set; }
       
    }

    public partial class FormLookup
    {
        [JsonProperty("FormName")]
        public string FormName { get; set; }

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Created")]
        public DateTime CreatedDate { get; set; }
        public DateTime Created { get; set; }

        [JsonProperty("ListName")]
        public string ListName { get; set; }

        [JsonProperty("Status")]
        public string FormStatus { get; set; }

        [JsonProperty("UniqueFormName")]
        public string UniqueFormName { get; set; }
        public string StringRecievedDate { get; set; }

        [JsonProperty("DataRowId")]
        public int? DataRowId { get; set; }

        [JsonProperty("ControllerName")]
        public string ControllerName { get; set; }

        [JsonProperty("RunWorkflow")]
        public string RunWorkflow { get; set; }

        [JsonProperty("Level")]
        public int? Level { get; set; }

        [JsonProperty("Logic")]
        public string Logic { get; set; }

        [JsonProperty("IsActive")]
        public int? IsActive { get; set; }

        [JsonProperty("ApproverId")]
        public int? ApproverId { get; set; }

        [JsonProperty("SubmitterId")]
        public int? SubmitterId { get; set; }

        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("CreatedBy")]
        public string CreatedBy { get; set; }

        

    }

    public class FormStatus
    {
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Processed { get; set; }
        public int Cancelled { get; set; }
        public int Submitted { get; set; }

        public int Resubmitted { get; set; }
    }

    public partial class FormParentModel
    {
        [JsonProperty("Id")]
        public int Id { get; set; }
    }

    public class Graph
    {
        public string FormCount { get; set; }

        public string FormStatus { get; set; }
    }

    public class IDCardReportModel : FormData
    {

        [JsonProperty("Chargable")]
        public string Chargable { get; set; }


        [JsonProperty("DateofIssue")]
        public DateTime DateofIssue { get; set; }

        [JsonProperty("DateofJoining")]
        public DateTime DateofJoining { get; set; }

        [JsonProperty("TypeOfCard")]
        public string TypeOfCard { get; set; }
        public string EmployeeName { get; set; }
        public string Company { get; set; }
    }

    public class DLICReportModel : FormData
    {
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        [JsonProperty("TicketNum")]
        public string TicketNum { get; set; }
        [JsonProperty("T_EmployeeName")]
        public string T_EmployeeName { get; set; }
        [JsonProperty("T_EmployeeCode")]
        public long T_EmployeeCode { get; set; }
        [JsonProperty("T_UserId")]
        public string T_UserId { get; set; }
        [JsonProperty("T_CostCenter")]
        public long T_CostCenter { get; set; }
        [JsonProperty("Make")]
        public string Make { get; set; }
        [JsonProperty("Model")]
        public string Modal { get; set; }
        [JsonProperty("SerialNumber")]
        public string SerialNumber { get; set; }
        [JsonProperty("HostName")]
        public string HostName { get; set; }
        [JsonProperty("IsIDoCompleted")]
        public bool IsIDoCompleted { get; set; }
        public bool IsBitLockerCompleted { get; set; }
        public bool IsAntivirusUpdated { get; set; }
        public bool IsProxyConfig { get; set; }
        public bool IsUSBBluetoothDisabled { get; set; }
        public bool IsUserIdConfigured { get; set; }
        public bool IsOutLookConfiguration { get; set; }
        public bool IsFirEyeAgent { get; set; }
        public bool IsEncryptedEmailConfiguration { get; set; }
        public bool IsPKIDigitSignCert { get; set; }
        public bool IsPrinterConfiguration { get; set; }
        public bool IsVPNConfigurationDone { get; set; }
        public bool IsSharedFolderAccessDone { get; set; }
        public bool IsDataRestored { get; set; }
        public bool IsNessusAgent { get; set; }
        public bool IsClassificationAddInForOffice { get; set; }
        public bool IsUsedMachineToBeClean { get; set; }
        public bool IsOneDriveConfiguration { get; set; }
        public bool IsLocalApps { get; set; }
        public bool IsOthers { get; set; }
        public string OthersText { get; set; }
    }

    public class FormListModel
    {
        public string IsDisable { get; set; }
        public string Message { get; set; }
        public string FormName { get; set; }
        public int FormId { get; set; }
        public string UniqueName { get; set; }
    }

}
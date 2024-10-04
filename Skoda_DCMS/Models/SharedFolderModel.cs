using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class SharedFolderModel
    {
        [JsonProperty("d")]
        public SharedFolderResults List { get; set; }
    }

    public partial class SharedFolderResults
    {
        [JsonProperty("results")]
        public List<SharedFolderData> SharedFolderList { get; set; }
     
    }

    public partial class SharedFolderData
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

        [JsonProperty("RequestType")]
        public string RequestType { get; set; }
        [JsonProperty("TempFrom")]
        public Nullable<System.DateTime> TempFrom { get; set; }
        [JsonProperty("TempTo")]
        public Nullable<System.DateTime> TempTo { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        [JsonProperty("RequestFor")]
        public string RequestFor { get; set; }
        [JsonProperty("ChangeType")]
        public string ChangeType { get; set; }
        [JsonProperty("ChangeFileServerName")]
        public string ChangeFileServerName { get; set; }
        [JsonProperty("ChangeFolderPath")]
        public string ChangeFolderPath { get; set; }
        [JsonProperty("ChangeSize")]
        public decimal ChangeSize { get; set; }
        [JsonProperty("FolderOwnerName")]
        public string FolderOwnerName { get; set; }
        [JsonProperty("FolderOwnerEmployeeNumber")]
        public long FolderOwnerEmployeeNumber { get; set; }
        [JsonProperty("ProposedFolderOwnerName")]
        public string ProposedFolderOwnerName { get; set; }
        [JsonProperty("ProposedFolderOwnerEmpNum")]
        public long ProposedFolderOwnerEmployeeNumber { get; set; }
        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }
    }
    public class SharedFolderAddRemoveUserModel
    {
        [JsonProperty("d")]
        public SharedFolderAddRemoveUserResults AddRemoveList { get; set; }
    }
    public class SharedFolderAddRemoveUserResults
    {
        [JsonProperty("results")]
        public List<SharedFolderAddRemoveUserRequestDto> AddRemoveUsersList { get; set; }
    }
    public class SharedFolderAddRemoveUserRequestDto
    {
        public long SharedFolderAddRemoveUserID { get; set; }
        public long SharedFolderCreationChangeReqID { get; set; }
        [JsonProperty("FileServerName")]
        public string FileServerName { get; set; }
        [JsonProperty("FolderPath")]
        public string FolderPath { get; set; }
        [JsonProperty("FolderOwnerName")]
        public string FolderOwnerName { get; set; }
        [JsonProperty("OwnerEmployeeNumber")]
        public long OwnerEmployeeNumber { get; set; }
       
        [JsonProperty("UserId")]
        public string UserId { get; set; }
        [JsonProperty("Read")]
        public Nullable<bool> Read { get; set; }
        [JsonProperty("ReadWrite")]
        public Nullable<bool> ReadWrite { get; set; }
        [JsonProperty("Remove")]
        public Nullable<bool> Remove { get; set; }
        [JsonProperty("Email")]
        public string Email { get; set; }
    }

    public class SharedFolderCreationModel
    {
        [JsonProperty("d")]
        public SharedFolderCreationResults List { get; set; }
    }
    public class SharedFolderCreationResults
    {
        [JsonProperty("results")]
        public List<SharedFolderCreationRequestDto> CreationList { get; set; }
    }
    public class SharedFolderCreationRequestDto
    {
        public long SharedFolderCreationID { get; set; }
        public long SharedFolderCreationChangeReqID { get; set; }
        [JsonProperty("FileServerName")]
        public string FileServerName { get; set; }
        [JsonProperty("FolderPath")]
        public string CreationFolderPath { get; set; }
        [JsonProperty("Size")]
        public decimal Size { get; set; }
        [JsonProperty("OwnerName")]
        public string CreationOwnerName { get; set; }
        [JsonProperty("OwnerEmployeeNumber")]
        public long CreationOwnerEmployeeNumber { get; set; }

        public string Permission { get; set; }
    }
}
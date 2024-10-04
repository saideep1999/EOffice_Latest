using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.Models
{
    public class OCRFModel
    {
        [JsonProperty("d")]
        public OCRFResults ocrfflist { get; set; }
    }

    public partial class OCRFResults
    {
        [JsonProperty("results")]
        public List<OCRFData> ocrfData { get; set; }
    }

    public partial class OCRFData : ApplicantDataModel
    {

        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("RequestType")]
        public string RequestType { get; set; }

        [JsonProperty("ChkTypeOfChange")]
        public string TypeOfChange { get; set; }

        [JsonProperty("ChkPosition")]
        public string ChkPosition { get; set; }        

        [JsonProperty("ChkEmployeeTransfer")]
        public string ChkEmployeeTransfer { get; set; }

        [JsonProperty("ChkCostCenter")]
        public string ChkCostCenter { get; set; }

        [JsonProperty("ChkReportingAuthority")]
        public string ChkReportingAuthority { get; set; }

        [JsonProperty("ReasonforChange")]
        public string ReasonforChange { get; set; }

        [JsonProperty("TransferEffectiveDate")]
        public DateTime TransferEffectiveDate { get; set; }


        //Employee Transfer Details

        [JsonProperty("CurrentRoleFrom")]
        public string CurrentRoleFrom { get; set; }

        [JsonProperty("CurrentRoleTo")]
        public string CurrentRoleTo { get; set; }

        [JsonProperty("WorkContractFrom")]
        public string WorkContractFrom { get; set; }

        [JsonProperty("WorkContractTo")]
        public string WorkContractTo { get; set; }

        [JsonProperty("DivisionFrom")]
        public string DivisionFrom { get; set; }

        [JsonProperty("DivisionTo")]
        public string DivisionTo { get; set; }

        [JsonProperty("DepartmentFrom")]
        public string DepartmentFrom { get; set; }

        [JsonProperty("DepartmentTo")]
        public string DepartmentTo { get; set; }

        [JsonProperty("SubDepartmentFrom")]
        public string SubDepartmentFrom { get; set; }

        [JsonProperty("SubDepartmentTo")]
        public string SubDepartmentTo { get; set; }

        [JsonProperty("ReportingManagerFrom")]
        public string ReportingManagerFrom { get; set; }

        [JsonProperty("ReportingManagerTo")]
        public string ReportingManagerTo { get; set; }

        [JsonProperty("CostCentreFrom")]
        public string CostCentreFrom { get; set; }

        [JsonProperty("CostCentreTo")]
        public string CostCentreTo { get; set; }

        [JsonProperty("BusinessLocationFrom")]
        public string BusinessLocationFrom { get; set; }

        [JsonProperty("BusinessLocationTo")]
        public string BusinessLocationTo { get; set; }

        [JsonProperty("WorkLocationFrom")]
        public string WorkLocationFrom { get; set; }
        public string WorkLocationFrom1 { get; set; }

        [JsonProperty("WorkLocationTo")]
        public string WorkLocationTo { get; set; }


        //Transfer Request Form(Blue Collar)
        [JsonProperty("EmployeeCategoryFromTRF")]
        public string EmployeeCategoryFromTRF { get; set; }

        [JsonProperty("EmployeeCategoryToTRF")]
        public string EmployeeCategoryToTRF { get; set; }

        [JsonProperty("SubDepartment1FromTRF")]
        public string SubDepartment1FromTRF { get; set; }

        [JsonProperty("SubDepartment1ToTRF")]
        public string SubDepartment1ToTRF { get; set; }

        [JsonProperty("SubDepartment2FromTRF")]
        public string SubDepartment2FromTRF { get; set; }

        [JsonProperty("SubDepartment2ToTRF")]
        public string SubDepartment2ToTRF { get; set; }

        [JsonProperty("ChkEmplMovesWithPosition")]
        public string ChkEmplMovesWithPosition { get; set; }

        [JsonProperty("ChkTransferToAvailablePositionNewDept")]
        public string ChkTransferToAvailablePositionNewDept { get; set; }


        //Division 
        [JsonProperty("DivId")]
        public string DivId { get; set; }

        [JsonProperty("DivName")]
        public string DivName { get; set; }

        //Department 
        [JsonProperty("DeptId")]
        public string DeptId { get; set; }

        [JsonProperty("DeptName")]
        public string DeptName { get; set; }

        //Sub-Department 
        [JsonProperty("SubDeptId")]
        public string SubDeptId { get; set; }

        [JsonProperty("SubDeptName")]
        public string SubDeptName { get; set; }




        //User data

        [JsonProperty("Id")]
        public int UserId { get; set; }
        [JsonProperty("LoginName")]
        public string LoginName { get; set; }
        [JsonProperty("Title")]
        public string UserName { get; set; }
        [JsonProperty("Email")]
        public string Email { get; set; }
        [JsonProperty("IsSiteAdmin")]
        public bool IsSiteAdmin { get; set; }
        public int EmpNumber { get; set; }
  
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ExceptionType ExceptionType { get; set; }
        public bool IsLoginSuccessful { get; set; }
        public string Password { get; set; }


        //Approval List
   
        [JsonProperty("ApproverEmailId")]
        public string ApproverEmailId { get; set; }

        [JsonProperty("ApproverEmployeeCode")]
        public int ApproverEmployeeCode { get; set; }

        [JsonProperty("AttachmentFiles")]
        public AttachmentOCRFResults attachmentOCRFlist { get; set; }

        //Action
        
        [JsonProperty("CRNumber")]
        public string CRNumber { get; set; }
    }

    public partial class AttachmentOCRFResults
    {
        [JsonProperty("results")]
        public List<AttachmentOCRFData> attachmentOCRFData { get; set; }
    }

    public partial class AttachmentOCRFData
    {
        [JsonProperty("FileName")]
        public string FileName { get; set; }

        [JsonProperty("ServerRelativeUrl")]
        public string ServerRelativeUrl { get; set; }
    }

}
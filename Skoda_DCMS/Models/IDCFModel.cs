using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class IDCFModel
    {
        [JsonProperty("d")]
        public IDCFResults idcfflist { get; set; }

        [JsonProperty("a")]
        public IDCFAreaSubAreaResults idcffAreaSubArealist { get; set; }
    }


    public partial class IDCFResults
    {
        [JsonProperty("results")]
        public List<IDCFData> idcfData { get; set; }

        [JsonProperty("__next")]
        public string nextDataURL { get; set; }
    }

    public partial class IDCFAreaSubAreaResults
    {

        [JsonProperty("results")]
        public List<IDCFDataAreaSubArea> idcfFDataAreaSubArea { get; set; }
    }

    public partial class IDCFData : ApplicantDataModel
    {

        [JsonProperty("ID")]
        public long Id { get; set; }

        [JsonProperty("Date")]
        public DateTime Date { get; set; }

        [JsonProperty("Priority")]
        public string Priority { get; set; }

        //[JsonProperty("EmployeeType")]
        //public string EmployeeType { get; set; }

        [JsonProperty("LocationId")]
        public string LocationId { get; set; }

        [JsonProperty("LocationName")]
        public string LocationName { get; set; }

        [JsonProperty("AreaId")]
        public string AreaId { get; set; }

        [JsonProperty("AreaName")]
        public string AreaName { get; set; }

        [JsonProperty("SubAreaName")]
        public string SubAreaName { get; set; }

        [JsonProperty("Area")]
        public string Area { get; set; }

        [JsonProperty("SubAreas")]
        public string SubAreas { get; set; }

        [JsonProperty("Surname")]
        public string Surname { get; set; }

        [JsonProperty("Company")]
        public string Company { get; set; }

        [JsonProperty("Barcode")]
        public string Barcode { get; set; }

        //[JsonProperty("EmployeeName")]
        //public string EmployeeName { get; set; }

        [JsonProperty("DateofJoining")]
        public DateTime DateofJoining { get; set; }

        [JsonProperty("EmployeeNumber")]
        public string EmployeeNumber { get; set; }

        [JsonProperty("CostCentre")]
        public string CostCentre { get; set; }

        [JsonProperty("CostCenter")]
        public string CostCenter { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("DateofIssue")]
        public DateTime DateofIssue { get; set; }

        [JsonProperty("ActiveFrom")]
        public DateTime ActiveFrom { get; set; }

        [JsonProperty("EndDate")]
        public DateTime EndDate { get; set; }

        [JsonProperty("DropdownEndDate")]
        public int DropdownEndDate { get; set; }
        
        [JsonProperty("IDCardNumber")]
        public string IDCardNumber { get; set; }

        [JsonProperty("VendorCode")]
        public string VendorCode { get; set; }

        [JsonProperty("Profile")]
        public string Profile { get; set; }

        [JsonProperty("AurangabadProfile")]
        public string AurangabadProfile { get; set; }

        [JsonProperty("MumbaiProfile")]
        public string MumbaiProfile { get; set; }

        [JsonProperty("PuneProfile")]
        public string PuneProfile { get; set; }

        [JsonProperty("Reason")]
        public string Reason { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("ApproverEmailId")]
        public string ApproverEmailId { get; set; }

        [JsonProperty("ApproverEmployeeCode")]
        public int ApproverEmployeeCode { get; set; }

        [JsonProperty("TypeOfCard")]
        public string TypeOfCard { get; set; }

        [JsonProperty("ValidityStartDate")]
        public DateTime ValidityStartDate { get; set; }

        [JsonProperty("ValidityEndDate")]
        public DateTime ValidityEndDate { get; set; }

        public List<IDFCArea> AreaDetails { get; set; }
        
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }
        
        [JsonProperty("Chargable")]
        public string Chargable { get; set; }


        [JsonProperty("AttachmentFiles")]
        public AttachmentFilesResults AttachmentFiles { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }
    }

    public partial class IDCFDataAreaSubArea
    {
        [JsonProperty("LocationId")]
        public string LocationId { get; set; }

        [JsonProperty("Area")]
        public string Area { get; set; }

        [JsonProperty("SubAreas")]
        public List<string> SubAreas { get; set; }

        [JsonProperty("FormId")]
        public string FormId { get; set; }

        [JsonProperty("RowId")]
        public string RowId { get; set; }

    }

    public class IDFCArea
    {
        public int AreaId { get; set; }
        public string Area { get; set; }
        public List<IDFCSubArea> SubAreas { get; set; }
    }

    public class IDFCSubArea
    {
        public int SubAreaId { get; set; }
        public string SubArea { get; set; }

        [JsonProperty("Modified")]
        public DateTime Modified { get; set; }
        public string UserName { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }

        [JsonProperty("ApproverStatus")]
        public string ApproverStatus { get; set; }
    }
}
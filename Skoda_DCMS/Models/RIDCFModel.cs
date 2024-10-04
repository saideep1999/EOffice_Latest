using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class RIDCFModel
    {
        [JsonProperty("d")]
        public RIDCFResults ridcfflist { get; set; }

        [JsonProperty("a")]
        public RIDCFAreaSubAreaResults ridcffAreaSubArealist { get; set; }

    }

    public partial class RIDCFResults
    {
        [JsonProperty("results")]
        public List<RIDCFData> ridcfData { get; set; }


    }

    public partial class RIDCFAreaSubAreaResults
    {

        [JsonProperty("results")]
        public List<RIDCFDataAreaSubArea> ridcfFDataAreaSubArea { get; set; }
    }

    public partial class RIDCFData
    {

        [JsonProperty("ID")]
        public long Id { get; set; }

        [JsonProperty("SelfOnBehalf")]
        public string SelfOnBehalf { get; set; }

        [JsonProperty("SelfMobile")]
        public long? SelfMobile { get; set; }

        [JsonProperty("SelfTelephone")]
        public long? SelfTelephone { get; set; }

        [JsonProperty("SelfEmailID")]
        public string SelfEmailID { get; set; }

        [JsonProperty("OnBehalfFirstName")]
        public string OnBehalfFirstName { get; set; }

        [JsonProperty("OnBehalfSurname")]
        public string OnBehalfSurname { get; set; }

        [JsonProperty("OnBehalfDepartment")]
        public string OnBehalfDepartment { get; set; }

        [JsonProperty("OnBehalfCostCenter")]
        public string OnBehalfCostCenter { get; set; }

        [JsonProperty("OnBehalfMobile")]
        public long? OnBehalfMobile { get; set; }

        [JsonProperty("OnBehalfEmployeeIDNo")]
        public long? OnBehalfEmployeeIDNo { get; set; }

        [JsonProperty("OnBehalfTelephone")]
        public long? OnBehalfTelephone { get; set; }

        [JsonProperty("OnBehalfEmailID")]
        public string OnBehalfEmailID { get; set; }

        [JsonProperty("Date")]
        public DateTime Date { get; set; }

        [JsonProperty("ReasonforReissue")]
        public string ReasonforReissue { get; set; }

        [JsonProperty("EmployeeType")]
        public string EmployeeType { get; set; }

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

        [JsonProperty("Name")]
        public string Name { get; set; }

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

        [JsonProperty("IDCardNumber")]
        public string IDCardNumber { get; set; }

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
        public List<RIDFCArea> AreaDetails { get; set; }
    }

    public partial class RIDCFDataAreaSubArea
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

    public class RIDFCArea
    {
        public int AreaId { get; set; }
        public string Area { get; set; }
        public List<RIDFCSubArea> SubAreas { get; set; }
    }

    public class RIDFCSubArea
    {
        public int SubAreaId { get; set; }
        public string SubArea { get; set; }

        public DateTime Modified { get; set; }
        public string UserName { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }

        [JsonProperty("ApproverStatus")]
        public string ApproverStatus { get; set; }
    }

}
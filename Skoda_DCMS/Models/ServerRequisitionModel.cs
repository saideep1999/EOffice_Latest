using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ServerRequisitionModel
    {
        [JsonProperty("d")]
        public SRFResults srfflist { get; set; }
    }

    public partial class SRFResults
    {
        [JsonProperty("results")]
        public List<SRFData> srfData { get; set; }

    }
    public partial class SRFData
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("EmployeeCode")]
        public string EmployeeCode { get; set; }

        [JsonProperty("CostCenterNo")]
        public string CostCenterNo { get; set; }

        [JsonProperty("UserID")]
        public string UserID { get; set; }

        [JsonProperty("TypeofEmployee")]
        public string TypeofEmployee { get; set; }

        [JsonProperty("CompanyName")]
        public string CompanyName { get; set; }

        [JsonProperty("Designation")]
        public string Designation { get; set; }

        [JsonProperty("ContactNo")]
        public string ContactNo { get; set; }

        [JsonProperty("EmployeeEmailId")]
        public string EmployeeEmailId { get; set; }

        [JsonProperty("SelfTelephone")]
        public long? SelfTelephone { get; set; }
       
        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }

        [JsonProperty("OnBehalfEmployeeNumber")]
        public long? OnBehalfEmployeeNumber { get; set; }

        [JsonProperty("OnBehlafDepartment")]
        public string OnBehlafDepartment { get; set; }

        [JsonProperty("OnBehalfCostCenterNo")]
        public string OnBehalfCostCenterNo { get; set; }

        [JsonProperty("OnBehalfUserID")]
        public string OnBehalfUserID { get; set; }

        [JsonProperty("OnBehalfdesignation")]
        public string OnBehalfdesignation { get; set; }

        [JsonProperty("OnBehalfMobile")]
        public long? OnBehalfMobile { get; set; }       

        [JsonProperty("OnBehalfTelephone")]
        public long? OnBehalfTelephone { get; set; }

        //[JsonProperty("OnBehalfEmailID")]
        //public string OnBehalfEmailID { get; set; }
        [JsonProperty("OtherEmployeeEmailId")]
        public string OtherEmployeeEmailId { get; set; }

        [JsonProperty("OnBehalfCostCenterNumber")]
        public string OnBehalfCostCenterNumber { get; set; }

        [JsonProperty("ServerCreationType")]
        public string ServerCreationType { get; set; }

        [JsonProperty("ServerOwnerName")]
        public string ServerOwnerName { get; set; }

        [JsonProperty("AdminAccount")]
        public string AdminAccount { get; set; }

        [JsonProperty("ServerRole")]
        public string ServerRole { get; set; }

        [JsonProperty("ServerEnvironment")]
        public string ServerEnvironment { get; set; }

        [JsonProperty("ServerHardware")]
        public string ServerHardware { get; set; }

        [JsonProperty("RAM")]
        public string RAM { get; set; }

        [JsonProperty("NoofCPU")]
        public string NoofCPU { get; set; }

        [JsonProperty("NoofNetworkPorts")]
        public string NoofNetworkPorts { get; set; }

        [JsonProperty("StorageSize")]
        public string StorageSize { get; set; }

        [JsonProperty("TwoRoom")]
        public string TwoRoom { get; set; }

        [JsonProperty("NonTwoRoom")]
        public string NonTwoRoom { get; set; }

        [JsonProperty("OperatingSystem")]
        public string OperatingSystem { get; set; }        

        [JsonProperty("OSName")]
        public string OSName { get; set; }

        [JsonProperty("OSEdition")]
        public string OSEdition { get; set; }

        [JsonProperty("Architecture")]
        public string Architecture { get; set; }

        [JsonProperty("DBName")]
        public string DBName { get; set; }

        [JsonProperty("DBEdition")]
        public string DBEdition { get; set; }

        [JsonProperty("ServerCriticality")]
        public string ServerCriticality { get; set; }

        [JsonProperty("ServerType")]
        public string ServerType { get; set; }

        [JsonProperty("Temporaryfrom")]
        public DateTime Temporaryfrom { get; set; }

        [JsonProperty("TemporaryTo")]
        public DateTime TemporaryTo { get; set; }

        [JsonProperty("BackupRequired")]
        public string BackupRequired { get; set; }

        [JsonProperty("ReasonForServerRequisition")]
        public string ReasonForServerRequisition { get; set; }


        [JsonProperty("OnBehalfTypeofEmployee")]
        public string OnBehalfTypeofEmployee { get; set; }

        [JsonProperty("OnBehalfCompanyName")]
        public string OnBehalfCompanyName { get; set; }


        //Approval List     

        [JsonProperty("ApproverEmailId")]
        public string ApproverEmailId { get; set; }

        [JsonProperty("ApproverEmployeeCode")]
        public int ApproverEmployeeCode { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("OnBehalfLocation")]
        public string OnBehalfLocation { get; set; }

        [JsonProperty("DataBaseName")]
        public string DataBaseName { get; set; }

        [JsonProperty("DBId")]
        public long? DBId { get; set; }

        [JsonProperty("OSystemName")]
        public string OSystemName { get; set; }

        [JsonProperty("OSId")]
        public long? OSId { get; set; }

        [JsonProperty("OSLinuxId")]
        public long? OSLinuxId { get; set; }

        [JsonProperty("OSLinuxName")]
        public string OSLinuxName { get; set; }

        [JsonProperty("WeekNo")]
        public string WeekNo { get; set; }

        [JsonProperty("Day")]
        public string Day { get; set; }

        [JsonProperty("TimeFrame")]
        public string TimeFrame { get; set; }

        [JsonProperty("ServerLocation")]
        public string ServerLocation { get; set; }

        [JsonProperty("HostName")]
        public string HostName { get; set; }

        [JsonProperty("IPAddress")]
        public string IPAddress { get; set; }

        [JsonProperty("ServerCpu")]
        public string ServerCpu { get; set; }
       
        [JsonProperty("ServerMemory")]
        public string ServerMemory { get; set; }

        [JsonProperty("ServerDisk")]
        public string ServerDisk { get; set; }

        [JsonProperty("ServerLan")]
        public string ServerLan { get; set; }

        [JsonProperty("ServerOwn")]
        public string ServerOwn { get; set; }

        [JsonProperty("CurrentCpu")]
        public string CurrentCpu { get; set; }

        [JsonProperty("IncrementCpu")]
        public string IncrementCpu { get; set; }

        [JsonProperty("TotalCpu")]
        public string TotalCpu { get; set; }

        [JsonProperty("CurrentMemory")]
        public string CurrentMemory { get; set; }

        [JsonProperty("IncrementMemory")]
        public string IncrementMemory { get; set; }

        [JsonProperty("TotalMemory")]
        public string TotalMemory { get; set; }
       
        [JsonProperty("CurrentDisk")]
        public string CurrentDisk { get; set; }

        [JsonProperty("IncrementDisk")]
        public string IncrementDisk { get; set; }

        [JsonProperty("TotalDisk")]
        public string TotalDisk { get; set; }

        [JsonProperty("CurrentLan")]
        public string CurrentLan { get; set; }

        [JsonProperty("IncrementLan")]
        public string IncrementLan { get; set; }

        [JsonProperty("TotalLan")]
        public string TotalLan { get; set; }

        [JsonProperty("CurrentOwner")]
        public string CurrentOwner { get; set; }

        [JsonProperty("NewOwner")]
        public string NewOwner { get; set; }
    }
}
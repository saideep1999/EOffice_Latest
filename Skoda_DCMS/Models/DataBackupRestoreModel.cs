using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
   
        public partial class DataBackupRestoreModel
        {
            [JsonProperty("d")]
            public DataBackupRestoreResults List { get; set; }
        }

        public partial class DataBackupRestoreResults
        {
            [JsonProperty("results")]
            public List<DataBackupRestore> DataBackupRestoreList { get; set; }
        }

        public partial class DataBackupRestore
        {
        //Employee Details
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
            [JsonProperty("BusinessNeed")]
            public string BusinessNeed { get; set; }
            [JsonProperty("RequirementFor")]
            public string RequirementFor { get; set; }
            [JsonProperty("RequestFor")]
            public string RequestFor { get; set; }
        //File/Folder Backup Details
            [JsonProperty("FolderPath")]
            public string FolderPath { get; set; }
            [JsonProperty("FolderSize")]
            public double FolderSize { get; set; }
            [JsonProperty("RetentionPeriod")]
            public DateTime RetentionPeriod { get; set; }

            [JsonProperty("BackupServerName")]
            public string BackupServerName { get; set; }
            [JsonProperty("BackupIpAddress")]
            public string BackupIpAddress { get; set; }
          
          //Application Backup Details
            [JsonProperty("RestoreAt")]
            public string RestoreAt { get; set; }
            [JsonProperty("RestoreServerName")]
            public string RestoreServerName { get; set; }
            [JsonProperty("RestoreIpAddress")]
            public string RestoreIpAddress { get; set; }
            [JsonProperty("RestoreDate")]
            public Nullable<System.DateTime> RestoreDate { get; set; }
            [JsonProperty("RestoreFromDate")]
            public Nullable<System.DateTime> RestoreFromDate { get; set; }
            [JsonProperty("RestoreToDate")]
            public Nullable<System.DateTime> RestoreToDate { get; set; }
            [JsonProperty("AlternateFolderPath")]
            public string AlternateFolderPath { get; set; }

            [JsonProperty("BackupType")]
            public string BackupType { get; set; }
            [JsonProperty("RequestSubmissionFor")]
            public string RequestSubmissionFor { get; set; }
    }
}
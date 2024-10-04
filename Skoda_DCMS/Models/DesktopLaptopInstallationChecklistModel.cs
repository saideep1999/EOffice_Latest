using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class DLICModel
    {
        [JsonProperty("d")]
        public ListResult data { get; set; }
    }

    public class ListResult
    {
        [JsonProperty("results")]
        public List<DesktopLaptopInstallationChecklistModel> list { get; set; }

        [JsonProperty("__next")]
        public string nextDataURL { get; set; }
    }
    public class DesktopLaptopInstallationChecklistModel : ApplicantDataModel
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
        public bool? IsVirtualSmartCard { get; set; }
        public bool? IsAgreeAppInstallation { get; set; }
        public string OthersText { get; set; }

        [JsonProperty("FormID")]
        public FormLookup Form_ID { get; set; }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class CabBookingRequestModel
    {
        [JsonProperty("d")]
        public CBRFResults cbrfflist { get; set; }
       
    }

    public partial class CBRFResults
    {
        [JsonProperty("results")]
        public List<CBRFData> cbrfData { get; set; }

    }
    public partial class CBRFData
    {
        public CBRFData Clone()
        {
            return (CBRFData)base.MemberwiseClone();
        }
        
        [JsonProperty("ID")]
        public int Id { get; set; }
        public int CBUId { get; set; }

        [JsonProperty("RequestSubmissionFor")]
        public string RequestSubmissionFor { get; set; }

        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; }

        [JsonProperty("EmployeeEmailId")]
        public string EmployeeEmailId { get; set; }

        [JsonProperty("OtherEmployeeName")]
        public string OtherEmployeeName { get; set; }

        [JsonProperty("OtherEmployeeEmailId")]
        public string OtherEmployeeEmailId { get; set; }

        [JsonProperty("SelfFirstName")]
        public string SelfFirstName { get; set; }

        [JsonProperty("SelfSurname")]
        public string SelfSurname { get; set; }

        [JsonProperty("SelfEmployeeIDNo")]
        public long SelfEmployeeIDNo { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("CostCenterNumber")]
        public string CostCenterNumber { get; set; }

        [JsonProperty("Designation")]
        public string Designation { get; set; }

        [JsonProperty("SelfMobile")]
        public long SelfMobile { get; set; }

        [JsonProperty("SelfTelephone")]
        public long SelfTelephone { get; set; }

        [JsonProperty("SelfEmailID")]
        public string SelfEmailID { get; set; }

        [JsonProperty("OnBehlafFirstName")]
        public string OnBehlafFirstName { get; set; }

        [JsonProperty("OnBehalfSurname")]
        public string OnBehalfSurname { get; set; }

        [JsonProperty("OnBehlafDepartment")]
        public string OnBehlafDepartment { get; set; }

        [JsonProperty("OnBehalfMobile")]
        public long OnBehalfMobile { get; set; }

        [JsonProperty("OnBehalfEmployeeIDNo")]
        public long OnBehalfEmployeeIDNo { get; set; }

        [JsonProperty("OnBehalfTelephone")]
        public long OnBehalfTelephone { get; set; }

        [JsonProperty("OnBehalfEmailID")]
        public string OnBehalfEmailID { get; set; }

        [JsonProperty("OnBehalfCostCenterNumber")]
        public string OnBehalfCostCenterNumber { get; set; }

        [JsonProperty("OnBehalfDesignation")]
        public string OnBehalfDesignation { get; set; }
              
        [JsonProperty("ShoppingCartNo")]
        public string ShoppingCartNo { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ContactNumber")]
        public string ContactNumber { get; set; }

        [JsonProperty("CarRequiredFromDate")]
        public DateTime CarRequiredFromDate { get; set; }

        [JsonProperty("CarRequiredToDate")]
        public DateTime CarRequiredToDate { get; set; }

        [JsonProperty("CarRequiredFromTime")]
        public string CarRequiredFromTime { get; set; }

        [JsonProperty("CarRequiredToTime")]
        public string CarRequiredToTime { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("UserContactNumber")]
        public long UserContactNumber { get; set; }

        [JsonProperty("ReportingPlaceWithAddress")]
        public string ReportingPlaceWithAddress { get; set; }

        [JsonProperty("ReportingTime")]
        public DateTime ReportingTime { get; set; }

        [JsonProperty("ReasonforBooking")]
        public string ReasonforBooking { get; set; }

        [JsonProperty("Destination")]
        public string Destination { get; set; }

        [JsonProperty("TypeofCar")]
        public string TypeofCar { get; set; }

        [JsonProperty("NumberofUsers")]
        public string NumberofUsers { get; set; }

        [JsonProperty("AirportPickUpDrop")]
        public string AirportPickUpDrop { get; set; }

        [JsonProperty("FlightNo")]
        public string FlightNo { get; set; }

        [JsonProperty("FlightTime")]
        public DateTime FlightTime { get; set; }

        [JsonProperty("StartingKM")]
        public string StartingKM { get; set; }

        [JsonProperty("ClosingKM")]
        public string ClosingKM { get; set; }

        [JsonProperty("StartingTime")]
        public string StartingTime { get; set; }

        [JsonProperty("EndingTime")]
        public string EndingTime { get; set; }

        [JsonProperty("StartingDate")]
        public DateTime StartingDate { get; set; }

        [JsonProperty("ClosingDate")]
        public DateTime ClosingDate { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("ApproverEmailId")]
        public string ApproverEmailId { get; set; }

        [JsonProperty("ApproverEmployeeCode")]
        public int ApproverEmployeeCode { get; set; }

        [JsonProperty("CarRequiredFromTimeHours")]
        public string CarRequiredFromTimeHours { get; set; }

        [JsonProperty("CarRequiredFromTimeAMPM")]
        public string CarRequiredFromTimeAMPM { get; set; }

        [JsonProperty("CarRequiredToTimeHours")]
        public string CarRequiredToTimeHours { get; set; }

        [JsonProperty("CarRequiredToTimeAMPM")]
        public string CarRequiredToTimeAMPM { get; set; }

        [JsonProperty("ReportingTimeHours")]
        public string ReportingTimeHours { get; set; }

        [JsonProperty("ReportingTimeAMPM")]
        public string ReportingTimeAMPM { get; set; }

        [JsonProperty("FlightTimeHours")]
        public string FlightTimeHours { get; set; }

        [JsonProperty("FlightTimeAMPM")]
        public string FlightTimeAMPM { get; set; }

        //Type of car Dropdown
        [JsonProperty("CarID")]
        public int CarID { get; set; }

        [JsonProperty("CarName")]
        public string CarName { get; set; }

        [JsonProperty("TypeofCarOther")]
        public string TypeofCarOther { get; set; }


        //Vehicle Number Dropdown
        [JsonProperty("VehicleID")]
        public int VehicleID { get; set; }

        [JsonProperty("VehicleNumber")]
        public string VehicleNumber { get; set; }

        [JsonProperty("VehicleNumberOther")]
        public string VehicleNumberOther { get; set; }


        [JsonProperty("AttachmentFiles")]
        public AttachmentResults attachmentlist { get; set; }

        [JsonProperty("Location")]
        public string Location { get; set; }

        [JsonProperty("OnBehalfLocation")]
        public string OnBehalfLocation { get; set; }

        [JsonProperty("IsActive")]
        public long IsActive { get; set; }

    }

    public partial class AttachmentResults
    {
        [JsonProperty("results")]
        public List<AttachmentData> attachmentData { get; set; }
    }

    public partial class AttachmentData
    {
        [JsonProperty("FileName")]
        public string FileName { get; set; }

        [JsonProperty("ServerRelativeUrl")]
        public string ServerRelativeUrl { get; set; }
    }

    public partial class CabUsersModel
    {
        [JsonProperty("d")]
        public CabUsersResults List { get; set; }
    }
    public partial class CabUsersResults
    {
        [JsonProperty("results")]
        public List<CabUsersDto> CabUsersList { get; set; }
    }
    public class CabUsersDto
    {
        [JsonProperty("CabUsersId")]
        public FormLookup CabUsersId { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("UserContactNumber")]
        public long UserContactNumber { get; set; }

        [JsonProperty("Destination")]
        public string Destination { get; set; }

        [JsonProperty("ReportingTime")]
        public DateTime ReportingTime { get; set; }

        [JsonProperty("ReportingPlaceWithAddress")]
        public string ReportingPlaceWithAddress { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

    }


}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Skoda_DCMS.Models
{
    public partial class PPFModel
    {
        [JsonProperty("d")]
        public PPFResults ppflist { get; set; }
    }

    public partial class PPFResults
    {
        [JsonProperty("results")]
        public List<PPFData> ppfData { get; set; }
    }

    public partial class PPFData : ApplicantDataModel
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("FormID")]
        public FormLookup FormID { get; set; }

        [JsonProperty("VideoCameraDevice")]
        public string VideoCameraDevice { get; set; }

        [JsonProperty("CameraDevice")]
        public string CameraDevice { get; set; }

        [JsonProperty("MobileDevice")]
        public string MobileDevice { get; set; }

        [JsonProperty("TabletDevice")]
        public string TabletDevice { get; set; }

        //Video Camera
        [JsonProperty("VideoCameraMake")]
        public string VideoCameraMake { get; set; }

        [JsonProperty("VideoCameraModel")]
        public string VideoCameraModel { get; set; }

        [JsonProperty("VideoCameraSerialIMEINo")]
        public string VideoCameraSerialIMEINo { get; set; }

        [JsonProperty("VideoCameraSAVWIPLOwned")]
        public string VideoCameraSAVWIPLOwned { get; set; }

        [JsonProperty("VideoCameraCaptureVoiceSound")]
        public string VideoCameraCaptureVoiceSound { get; set; }

        [JsonProperty("VideoCameraCaptureVideo")]
        public string VideoCameraCaptureVideo { get; set; }

        [JsonProperty("VideoCameraCaptureImages")]
        public string VideoCameraCaptureImages { get; set; }

        [JsonProperty("VideoCameraBluetoothWireless")]
        public string VideoCameraBluetoothWireless { get; set; }

        [JsonProperty("VideoCameraOther")]
        public string VideoCameraOther { get; set; }

        //Camera
        [JsonProperty("CameraMake")]
        public string CameraMake { get; set; }

        [JsonProperty("CameraModel")]
        public string CameraModel { get; set; }

        [JsonProperty("CameraSerialIMEINo")]
        public string CameraSerialIMEINo { get; set; }

        [JsonProperty("CameraSAVWIPLOwned")]
        public string CameraSAVWIPLOwned { get; set; }

        [JsonProperty("CameraCaptureVoiceSound")]
        public string CameraCaptureVoiceSound { get; set; }

        [JsonProperty("CameraCapture")]
        public string CameraCapture { get; set; }

        [JsonProperty("CameraCaptureImages")]
        public string CameraCaptureImages { get; set; }

        [JsonProperty("CameraBluetoothWireless")]
        public string CameraBluetoothWireless { get; set; }

        [JsonProperty("CameraOther")]
        public string CameraOther { get; set; }

        //Mobile

        [JsonProperty("MobileMake")]
        public string MobileMake { get; set; }

        [JsonProperty("MobileModel")]
        public string MobileModel { get; set; }

        [JsonProperty("MobileSerialIMEINo")]
        public string MobileSerialIMEINo { get; set; }

        [JsonProperty("MobileSAVWIPLOwned")]
        public string MobileSAVWIPLOwned { get; set; }

        [JsonProperty("MobileCaptureVoiceSound")]
        public string MobileCaptureVoiceSound { get; set; }

        [JsonProperty("MobileCapture")]
        public string MobileCapture { get; set; }

        [JsonProperty("MobileCaptureImages")]
        public string MobileCaptureImages { get; set; }

        [JsonProperty("MobileBluetoothWireless")]
        public string MobileBluetoothWireless { get; set; }

        [JsonProperty("MobileOther")]
        public string MobileOther { get; set; }

        //Tablet
        [JsonProperty("TabletMake")]
        public string TabletMake { get; set; }

        [JsonProperty("TabletModel")]
        public string TabletModel { get; set; }

        [JsonProperty("TabletSerialIMEINo")]
        public string TabletSerialIMEINo { get; set; }

        [JsonProperty("TabletSAVWIPLOwned")]
        public string TabletSAVWIPLOwned { get; set; }

        [JsonProperty("TabletCaptureVoiceSound")]
        public string TabletCaptureVoiceSound { get; set; }

        [JsonProperty("TabletCapture")]
        public string TabletCapture { get; set; }

        [JsonProperty("TabletCaptureImages")]
        public string TabletCaptureImages { get; set; }

        [JsonProperty("TabletBluetoothWireless")]
        public string TabletBluetoothWireless { get; set; }

        [JsonProperty("TabletOther")]
        public string TabletOther { get; set; }



        [JsonProperty("Purpose")]
        public string Purpose { get; set; }

        [JsonProperty("PAFLocation")]
        public string PAFLocation { get; set; }

        [JsonProperty("Zone")]
        public string Zone { get; set; }

        [JsonProperty("ZoneHeadEmailId")]
        public string ZoneHeadEmailId { get; set; }

        [JsonProperty("CostCenterNumberByZone")]
        public long CostCenterNumberByZone { get; set; }

        [JsonProperty("IsActive")]
        public string IsActive { get; set; }
        

        [JsonProperty("PreSeriesCarOrPart")]
        public string PreSeriesCarOrPart { get; set; }

        [JsonProperty("ExceptionalPhoto")]
        public string ExceptionalPhoto { get; set; }

        [JsonProperty("ValidFrom")]
        public DateTime ValidFrom { get; set; }

        [JsonProperty("ValidTo")]
        public DateTime ValidTo { get; set; }

        [JsonProperty("ActivityStart")]
        public DateTime ActivityStart { get; set; }

        [JsonProperty("ActivityEnd")]
        public DateTime ActivityEnd { get; set; }

        [JsonProperty("ThirdPartyPhotographer")]
        public string ThirdPartyPhotographer { get; set; }
        [JsonProperty("Created_Date")]
        public DateTime Created_Date { get; set; }

    }

}
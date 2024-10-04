using Newtonsoft.Json;
using Syncfusion.Pdf.Lists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    //public class IPAFModel
    //{
    //    public int Id { get; set; }
    //}

    public class IPAFModel
    {
        [JsonProperty("d")]
        public IPAFResults IPAFResults { get; set; }
    }

    public class IPAFResults
    {
        [JsonProperty("results")]
        public List<IPAFData> IPAFData { get; set; }

    }

    public class IPAFData : ApplicantDataModel
    {
        public IPAFData Clone()
        {
            return (IPAFData)base.MemberwiseClone();
        }

        public int FormId { get; set; }
        public int FormIDId { get; set; }
        [JsonProperty("Id")]
        public int Id { get; set; }
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("BusinessJustification")]
        public string BusinessJustification { get; set; }

        [JsonProperty("Applicationname")]
        public string Applicationname { get; set; }

        [JsonProperty("Applicationurl")]
        public string Applicationurl { get; set; }

        [JsonProperty("Applicationaccess")]
        public string Applicationaccess { get; set; }

        [JsonProperty("Accessgroup")]
        public string Accessgroup { get; set; }
        [JsonProperty("RequestType")]
        public string RequestType { get; set; }

        [JsonProperty("RequestFromDate")]
        public DateTime? RequestFromDate { get; set; }
        [JsonProperty("RequestToDate")]
        public DateTime? RequestToDate { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int RowId { get; set; }
        public List<IPAFList> IPAFFormDataList { get; set; }
    }
    public class IPAFFormDataList
    {
        [JsonProperty("results")]
        public List<IPAFList> IPAFList { get; set; }

    }
    public class IPAFList
      {
       
        public string SrNo { get; set; }
        [JsonProperty("Id")]
        public int Id { get; set; }
        public int FormId { get; set; }
      
        [JsonProperty("BusinessNeed")]
        public string BusinessNeed { get; set; }

        [JsonProperty("BusinessJustification")]
        public string BusinessJustification { get; set; }

        [JsonProperty("Applicationname")]
        public string Applicationname { get; set; }

        [JsonProperty("Applicationurl")]
        public string Applicationurl { get; set; }

        [JsonProperty("Applicationaccess")]
        public string Applicationaccess { get; set; }

        [JsonProperty("Accessgroup")]
        public string Accessgroup { get; set; }
        [JsonProperty("RequestType")]
        public string RequestType { get; set; }

        [JsonProperty("RequestFromDate")]
        public DateTime? RequestFromDate { get; set; }
        [JsonProperty("RequestToDate")]
        public DateTime? RequestToDate { get; set; }
    }

    }
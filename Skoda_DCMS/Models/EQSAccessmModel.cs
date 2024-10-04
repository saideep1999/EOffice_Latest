using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class EQSAccessmModel
    {
        [JsonProperty("d")]
        public EQSAccessmModelResults EQSAccessmModelResults { get; set; }
    }

    public class EQSAccessmModelResults
    {
        [JsonProperty("results")]
        public List<EQSAccessmModelData> EQSAccessmModelData { get; set; }

    }

    public class EQSAccessmModelData : ApplicantDataModel
    {
        public EQSAccessmModelData Clone()
        {
            return (EQSAccessmModelData)base.MemberwiseClone();
        }
        public string Id { get; set; }
        public string FormIDId { get; set; }
        public string FormSrId { get; set; }
        //public string SrNo { get; set; }
        public string RequestType { get; set; }
        public string BusinessNeed { get; set; }
        //public string EmployeeName { get; set; }
        //public string EmployeeID { get; set; }
        //public string LogicCardID { get; set; }
        //public string StationName { get; set; }
        //public string Shop { get; set; }
        //public string AccessGroup { get; set; }
        public string BusinessJustification { get; set; }
        public List<EQSAccessTableData> EQSAccessTableData { get; set; }
        public string SrNo { get; set; }
  
        public string EmployeeID { get; set; }
        public string LogicCardID { get; set; }
        public string StationName { get; set; }
        public string Shop { get; set; }
        public string AccessGroup { get; set; }
    }

    //public class EQSAccessTableDataList
    //{
    //    [JsonProperty("results")]
    //    public EQSAccessTableData EQSAccessTableData { get; set; }

    //}
    public class EQSAccessTableData
    {
        public string SrNo { get; set; }
    
        public string EmployeeName { get; set; }
        public string EmployeeID { get; set; }
        public string LogicCardID { get; set; }
        public string StationName { get; set; }
        public string Shop { get; set; }
        public string AccessGroup { get; set; }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class ParallelApprovalMasterModel
    {
        [JsonProperty("d")]
        public ParallelApprovalNodeClass ParallelNode { get; set; }
    }
    public partial class ParallelApprovalNodeClass
    {
        [JsonProperty("results")]
        public List<ParallelApprovalDataModel> Data { get; set; }
    }
    public partial class ParallelApprovalDataModel
    {
        //[JsonProperty("ID")]
        //public int ID { get; set; }
        [JsonProperty("ApproverId")]
        public int ApproverId { get; set; }
        [JsonProperty("ApproverStatus")]
        public string ApproverStatus { get; set; }
        [JsonProperty("Comment")]
        public string Comment { get; set; }
        [JsonProperty("IsActive")]
        public int IsActive { get; set; }

        [JsonProperty("Department")]
        public string Department { get; set; }

        [JsonProperty("Section")]
        public string Section { get; set; }

        [JsonProperty("NextApproverId")]
        public int NextApproverId { get; set; }
        [JsonProperty("Modified")]
        public DateTime Modified { get; set; }
        public string UserName { get; set; }
        public int UserLevel { get; set; }
        [JsonProperty("FormId")]
        public FormLookup FormId { get; set; }
        [JsonProperty("Author")]
        public Author Author { get; set; }

        [JsonProperty("SubAreaId")]
        public int SubAreaId { get; set; }
    }
    public class ParallelApprovalMatrix
    {
        public string FName { get; set; }
        public string LName { get; set; }
        public int EmpNumber { get; set; }
        public string EmailId { get; set; }
        public int UserId { get; set; }
    }

}
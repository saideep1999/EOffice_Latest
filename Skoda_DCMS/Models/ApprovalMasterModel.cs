using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Skoda_DCMS.Converters;

namespace Skoda_DCMS.Models
{
    public class ApprovalMasterModel
    {
        [JsonProperty("d")]
        public NodeClass Node { get; set; }
    }
    public partial class NodeClass
    {
        [JsonProperty("results")]
        public List<ApprovalDataModel> Data { get; set; }
        public List<Tuple<int, string>> ApproverDesignationMapping { get; set; }
    }
    public partial class ApprovalDataModel
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("ApproverId")]
        public int? ApproverId { get; set; }

        [JsonProperty("AuthorityToEdit")]
        public int AuthorityToEdit { get; set; }

        [JsonProperty("ApproverStatus")]
        public string ApproverStatus { get; set; }
        [JsonProperty("Comment")]
        public string Comment { get; set; }
        [JsonProperty("Department")]
        public string Department { get; set; }
        [JsonProperty("IsActive")]
        public int IsActive { get; set; }
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

        public bool IsSubmitter { get; set; }
        public bool IsApprover { get; set; }

        [JsonProperty("RunWorkflow")]
        public string RunWorkflow { get; set; }

        [JsonProperty("Level")]
        public int Level { get; set; }
        public int InnerLevel { get; set; }

        [JsonProperty("Logic")]
        public string Logic { get; set; }
        public string Designation { get; set; }
        public DateTime TimeStamp { get; set; }

        [JsonProperty("AssistantForEmployeeUserId")]
        public int? AssistantForEmpUserId { get; set; }

        [JsonProperty("AssistantForEmployeeUserName")]
        public string AssistantForEmployeeUserName { get; set; }

        [JsonProperty("ExtraDetails")]
        public string ExtraDetails { get; set; }

        public string EmailId { get; set; }

        [JsonConverter(typeof(LowerCaseConverter))]
        [JsonProperty("ApproverUserName")]
        public string ApproverUserName { get; set; }

        [JsonProperty("RelationWith")]
        public int? RelationWith { get; set; } = 0;

        [JsonProperty("RelationId")]
        public int? RelationId { get; set; } = 0;

        [JsonProperty("RowId")]
        public int? RowId { get; set; }

        [JsonProperty("ApproverName")]
        public string ApproverName { get; set; }
    }
    public class ApprovalMatrix
    {
        public ApprovalMatrix Clone()
        {
            return (ApprovalMatrix)base.MemberwiseClone();
        }
        public string FName { get; set; }
        public string LName { get; set; }
        public long EmpNumber { get; set; }
        public string EmailId { get; set; }
        public int UserId { get; set; }
        public string Designation { get; set; }
        public int ApprovalLevel { get; set; }
        public string Logic { get; set; }

        public long DelegatedByEmpNum { get; set; }
        public bool IsIS { get; set; }
        public long AssistantForEmpNum { get; set; }
        //public long AssistantForEmpUserId { get; set; }

        public string AssistantForEmpUserName { get; set; }

        //public string AssistantForEmployeeUserName { get; set; }
        public string ExtraDetails { get; set; }

        [JsonConverter(typeof(LowerCaseConverter))]
        public string ApproverUserName { get; set; }
        public long RelationWith { get; set; }
        public long RelationId { get; set; }
        public long LogicId { get; set; }
        public long LogicWith { get; set; }

        public string ApproverName { get; set; }

        public string ApproverStatus { get; set; }
        public string ControllerName { get; set; }
        public long FormParentId { get; set; }
    }
}
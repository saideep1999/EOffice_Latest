using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class CreateApprovalsModels
    {
        public string FormId { get; set; }
        public List<CAFormDataList> CAFormData { get; set; }
    }
    public class CAFormDataList
    {
        public int SrNo { get; set; }
        public int Id { get; set; }
        public string CAId { get; set; }
        public string EmpId { get; set; }
        public string selName { get; set; }
        public string selRole { get; set; }
        public string selAppLevel { get; set; }
        public string selLogic { get; set; }
    }
}
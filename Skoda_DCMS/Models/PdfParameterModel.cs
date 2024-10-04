using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class PdfParameterModel
    {
        public dynamic RequesterData { get; set; }
        public IEnumerable<ApprovalDataModel> ApproverData { get; set; }
    }
}
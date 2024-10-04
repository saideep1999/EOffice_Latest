using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class Delegation
    {
        public int ID { get; set; }

        public string FromEmployeeID { get; set; }
        public string ToEmployeeID { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int IsActive { get; set; }

    }
}
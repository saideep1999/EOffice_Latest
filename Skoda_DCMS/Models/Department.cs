using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class Department
    {
        public int DivId { get; set; }
        public string DivName { get; set; }

    }
    public class SubDepartment
    {
        public int DeptId { get; set; }
        public int DivId { get; set; }
        public string DeptName { get; set; }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    //public class ResponseModel<T>
    //{
    //    public int Status { get; set; }
    //    public string Message { get; set; }
    //    public T Model { get; set; }
    //    public string Html { get; set; }
    //}
    public class ResponseModel<T>
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public T Model { get; set; }
        public string Html { get; set; }
        public string RouteUrl { get; set; }
        public string RowId { get; set; }
        public string Value { get; set; }
        public bool IsResubmit { get; set; }
    }
}
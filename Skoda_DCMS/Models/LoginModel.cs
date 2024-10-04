using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    //public class LoginModel
    //{
    //    public string UserName { get; set; }
    //    public string Password { get; set; }
    //    //public bool RememberMe { get; set; }
    //}
    public class LoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public string FormName { get; set; }
        //public bool RememberMe { get; set; }
    }
    public class EmailApproverStatusModel
    {
        public string Status { get; set; }
        public int AppRowId { get; set; }
        public string Response { get; set; }
        public string UserName { get; set; }
        public string UserFullname { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public string FormName { get; set; }
        //public bool RememberMe { get; set; }
    }
}
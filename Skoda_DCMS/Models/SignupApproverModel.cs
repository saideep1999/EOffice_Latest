using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class SignupApproverModel
    {
        public long Id { get; set; }   
        public long EmployeeNumber { get; set; }   
        public string FirstName { get; set; }   
        public string MiddleName { get; set; }   
        public string LastName { get; set; }   
        public string EmailID { get; set; }   
        public string PhoneNumber { get; set; }   
        public long CostCenter { get; set; }   
        public string Department { get; set; }   
        public string SubDepartment { get; set; }   
        public string ManagerEmployeeNumber { get; set; }   
        public string IsActive { get; set; }   
        public string UserName { get; set; }   
        public string Password { get; set; }   
        public DateTime? LastUpdateDateTime { get; set; }   
    }
    public class SignupApproverList
    {
        public long Id { get; set; }
        public long SrNo { get; set; }
        public string Name { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber { get; set; }
        public string Department { get; set; }
        public string SubDepartment { get; set; }
    }
    public class GetUserDetailsModel
    {
        public long Id { get; set; }
        public long SrNo { get; set; }
        public int EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string EmailID { get; set; }
        public string CostCenter { get; set; }
        public string ManagerEmployeeNumber { get; set; }


    }
    public class ResponseData
    {
        public long Status { get; set; }
        public string Message { get; set; }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.Models
{
    public class UserModel
    {
        [JsonProperty("d")]
        public UserData userData { get; set; }
    }

    public partial class UserData
    {
        [JsonProperty("Id")]
        public int UserId { get; set; }

        [JsonProperty("LoginName")]
        public string LoginName { get; set; }
        [JsonProperty("Title")]
        public string UserName { get; set; }
        public string FinalMailApproverUserName { get; set; }
        public bool IsFinalMailTriggeredManually { get; set; }
        [JsonProperty("Email")]
        public string Email { get; set; }
        [JsonProperty("IsSiteAdmin")]
        public bool IsSiteAdmin { get; set; }
        public int EmpNumber { get; set; }

        public int ManagerEmployeeNumber { get; set; }
        public int CostCenter { get; set; }
        public string Department { get; set; }

        public string SubDepartment { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public ExceptionType ExceptionType { get; set; }
        public bool IsLoginSuccessful { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string EmployeeName { get; set; }

        public bool IsSubmitter { get; set; }
        public bool IsApprover { get; set; }

        public long ApprovalLevel { get; set; }

        public bool IsParallelApprover { get; set; }
        public bool IsOnBehalf { get; set; }
        public bool IsCurrentApprover { get; set; }
        public bool IsNextApprover { get; set; }
        public bool IsLastApprover { get; set; }
        public string ID { get; set; }
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(this.EmployeeName))
                {
                    return $"{FirstName} {LastName}";
                }
                else
                    return this.EmployeeName;
            }
        }

        //public long ID { get; set; }
        //public string FullName { get; set; }
        public string CompanyName { get; set; }

        public string ObjectSid { get; set; }

        public string UserAccessId { get; set; }
        public string DomainName { get; set; }

        public bool IsMissingData { get; set; }

        public static implicit operator UserData(string v)
        {
            throw new NotImplementedException();
        }
    }

    public class SiteUserModel
    {
        [JsonProperty("d")]
        public SiteUserList userList { get; set; }
    }

    public class SiteUserList
    {
        [JsonProperty("results")]
        public List<UserData> userData { get; set; }
    }

    public class UserInsertStatusModel
    {
        public string ErrorMessage { get; set; }
        public string Message { get; set; }

        public int Status { get; set; }
    }

    public class MobileModel
    {
        public int Approvercount { get; set; }

        public int Rejectcount { get; set; }
        public int Pendingcount { get; set; }
        public int Enquirecount { get; set; }
        public int Cancelcount { get; set; }
        public int Totalcount { get; set; }
    }

    public class Tasklist
    {
        public string FormID { get; set; }
        public string FormName { get; set; }
        public string RequestedBy { get; set; }
        public string ReceivedDate { get; set; }
        public int SrNo { get; set; }
    }


}
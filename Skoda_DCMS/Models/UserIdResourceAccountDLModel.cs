using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Models
{
    public class UserIdResourceAccountDLModel
    {
        [JsonProperty("d")]
        public UserIdResourceAccountDLResults List { get; set; }
    }
    public partial class UserIdResourceAccountDLResults
    {
        [JsonProperty("results")]
        public List<UserIdResourceAccountDLData> UserIdResourceAccountDLList { get; set; }
    }

    public class UserIdResourceAccountDLData
    {
        [JsonProperty("AddRemoveUserRequestID")]
        public long AddRemoveUserRequestID { get; set; }
        [JsonProperty("ResourceAccountReqID")]
        public long ResourceAccountReqID { get; set; }
        [JsonProperty("EmployeeUserId")]
        public string EmployeeUserId { get; set; }
        [JsonProperty("ActionType")]
        public string ActionType { get; set; }
    }
}
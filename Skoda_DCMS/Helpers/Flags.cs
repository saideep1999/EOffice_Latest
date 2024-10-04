using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Skoda_DCMS.Helpers
{
    public class Flags
    {
        public enum ExceptionType
        {
            None = 0,
            InvalidUsernameOrPassword = 1,
            UserDoesNotExist = 2,
            ServerIssue = 3
        }

        public enum FormDashboard
        {
            NewlyAddedForms = 0,
            FreqUsedForms = 1
        }

        public enum FormStates
        {
            None= 0,
            Submit = 1,
            PartialApproval = 2,
            FinalApproval = 3,
            Reject = 4,
            Cancel = 5,
            Enquire = 6,
            ReSubmit = 7,
        }
    }
}
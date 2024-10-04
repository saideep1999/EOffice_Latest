using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Skoda_DCMS.Helpers.Flags;

namespace Skoda_DCMS.Models.CommonModels
{
    public class EmailDataModel
    {
        public EmailDataModel()
        {

        }
        public EmailDataModel(string subject, string body, IEnumerable<UserData> recipients, bool isBodyHtml = true)
        {
            this.Subject = subject;
            this.Body = body;
            this.Recipients = recipients;
            this.IsBodyHtml = isBodyHtml;
        }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }
        public IEnumerable<UserData> Recipients { get; set; }

        public IEnumerable<UserData> ParallelRecipients { get; set; }
        public UserData Sender { get; set; }
        public UserData OnBehalfSender { get; set; }
        public string FormId { get; set; }
        public FormStates Action { get; set; }
        public string UniqueFormName { get; set; }
        public string RequestId
        {
            get
            {
                return $"{UniqueFormName}{FormId}";
            }
        }
        public string ApproverResponse { get; set; }
        public string Comment { get; set; }
        public UserData CurrentUser { get; set; }
        public string FormName { get; set; }
        public bool TriggerFinalMailWithoutApprovers { get; set; }
        public List<ITServiceDeskContactModel> ServiceDeskList { get; set; }
        public string Location { get; set; }
        public List<string> ToIds { get; set; }
        public List<string> CCIds { get; set; }
        public List<KeyValuePair<string, Object>> ExtraFormData { get; set; }

        public string PAFLocation { get; set; }

        public List<string> IDList { get; set; }

    }

}
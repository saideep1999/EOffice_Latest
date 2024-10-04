using Skoda_DCMS.App_Start;
using Skoda_DCMS.Helpers;
using Skoda_DCMS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Skoda_DCMS.Controllers
{
    public class EmailServiceController : BaseController
    {
        CommonBAL CommonBAL;
        //public async Task SendEmailAsync(List<string> userlist, string subject, string body)
        //{
        //    try
        //    {
        //        //string smtpHost = WebConfigurationManager.AppSettings["smtpHost"];
        //        //string smtpEmail = WebConfigurationManager.AppSettings["smtpEmailId"];
        //        //string password = WebConfigurationManager.AppSettings["smtpPassword"];

        //        string smtpHost = "smtp.gmail.com";
        //        string smtpEmail = "devacc909909@gmail.com";
        //        //string password = "12180917";
        //        string password = "Password*123";
        //        MailMessage mail = new MailMessage();

        //        mail.Subject = subject;
        //        mail.Body = body;
        //        mail.IsBodyHtml = true;
        //        MailAddress mailAddress = new MailAddress(smtpEmail);
        //        mail.From = mailAddress;
        //        foreach (var item in userlist)
        //        {
        //            //  mail.To.Add(item.EmailId);
        //            mail.To.Add(item);
        //        }
        //        var smtpCliient = new SmtpClient(smtpHost);
        //        smtpCliient.Port = 587;
        //        smtpCliient.DeliveryMethod = SmtpDeliveryMethod.Network;
        //        smtpCliient.UseDefaultCredentials = false;
        //        smtpCliient.EnableSsl = true;
        //        smtpCliient.Credentials = new NetworkCredential(smtpEmail, password);
        //        await Task.Run(() => smtpCliient.SendMailAsync(mail));
        //        //await smtpCliient.SendMailAsync(mail);
        //        //isSend = true;
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        [HttpPost]
        public ActionResult FormStatus(int dataRowId, int formID)
        {
            CommonBAL = new CommonBAL();
            var result = CommonBAL.FormStatus(dataRowId, formID);
            return Json(result);
        }

        public async Task PrepareAndSendFinalEmailToHelpDesk(int rowId, int FormID, string FormName, string submitterEmpEmailId, UserData user)
        {
            string itServiceDeskToEmails = null;
            string itServiceDeskCcEmails = null;
            string itServiceDeskBCcEmails = null;
            submitterEmpEmailId = "devacc909909@gmail.com";
            CommonBAL objCommonBAL = new CommonBAL();
            try
            {
                string emailBodyITHelpDesk = await objCommonBAL.GetEmailBodyForITHelpDesk(rowId, FormID, FormName, user);
                string employeeLocation = string.Empty;
                int locationId = 0;
                if (FormID == 19) //Shared Folder Form
                {
                    //Shared Folder form should go to respective help desk based on File Server Name
                    //employeeLocation = GetEmployeeFileServerLocationFromRequest(masterRequest.RequestID);
                    employeeLocation = "";
                }
                else
                {
                    // employeeLocation = await objCommonBAL.GetEmployeeLocationFromRequest(rowId, FormID, FormName);
                    employeeLocation = "";
                }

                //var loc = form["ddEmpLocation"];
                //var approverIdList = await objCommonBAL.GetApprovalEmailDetails(rowId, FormID);

                //locationId = new LookupDAL().GetITServiceDeskLocationId(employeeLocation);
                //locationId = 1;


                //List<ITServiceDeskContactModel> approverIdList = new List<ITServiceDeskContactModel>();
              //  if (approverIdList != null && approverIdList.Count > 0)
              //  {
                    //List<ITServiceDesk> iTServiceDeskToContacts = approverIdList.Where(p => p.EMail != null).ToList();
                    //itServiceDeskToEmails = string.Join("; ", iTServiceDeskToContacts.Select(p => p.EMail));
                  //  itServiceDeskToEmails = "devacc909909@gmail.com";
                    //List<ITServiceDesk> iTServiceDeskCcContacts = approverIdList.Where(p => p.EMail != null).ToList();
                    //itServiceDeskCcEmails = string.Join("; ", iTServiceDeskCcContacts.Select(p => p.EMail));
                    //itServiceDeskCcEmails = itServiceDeskCcEmails + ";" + string.Join("; ", submitterEmpEmailId);
                  //  itServiceDeskCcEmails = "devacc909909@gmail.com";
                    //IT ASSET Admin team email contact for IT Clearance, IT Asset & Smart Phone requisition only
                //    if (FormID == 9 || FormID == 15 || FormID == 21)
                //    {
                //        string strITAssetAdminContact = Convert.ToString(ConfigurationManager.AppSettings["ITAssetAdminContact"]);
                //        itServiceDeskCcEmails = itServiceDeskCcEmails + ";" + string.Join("; ", strITAssetAdminContact);
                //    }

                //    itServiceDeskBCcEmails = "devacc909909@gmail.com";
                //}

        
                string itServiceDeskEmailSubject = Convert.ToString(ConfigurationManager.AppSettings["ITServiceDeskEmailSubject"]);
                var commonUtility = new CommonUtility();
                commonUtility.SendMail(
                    itServiceDeskToEmails,
                    itServiceDeskEmailSubject + FormID + "-" + FormName,
                    emailBodyITHelpDesk,
                    itServiceDeskCcEmails,
                    itServiceDeskBCcEmails);

                Console.WriteLine("************* Email is sent to IT Service Desk successfully.***************");

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                Console.WriteLine(ex.Message, ex);
            }
        }

    }
}
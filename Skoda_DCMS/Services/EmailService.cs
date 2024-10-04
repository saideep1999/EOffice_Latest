using Skoda_DCMS.App_Start;
using Skoda_DCMS.Models;
using Skoda_DCMS.Models.CommonModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Skoda_DCMS.Helpers
{
    public class EmailService
    {
        CommonBAL CommonBAL;

        public async Task SendMail(EmailDataModel emailData)
        {
            try
            {
                string smtpHost = WebConfigurationManager.AppSettings["smtpHost"];
                string smtpEmail = WebConfigurationManager.AppSettings["smtpEmailId"];
                string password = WebConfigurationManager.AppSettings["smtpPassword"];
                int port = Convert.ToInt32(WebConfigurationManager.AppSettings["smtpPort"]);
                bool enableSsl = Convert.ToBoolean(WebConfigurationManager.AppSettings["EnableSsl"]);

                var imgId = GenerateImageID();
                var emailBody = new EmailBodyBuilder();
                var emails = await emailBody.CreateEmails(emailData);

                foreach (var email in emails)
                {
                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(smtpEmail);
                    //if(email.Recipients != null)
                    //{
                    //    email.Recipients.ToList().ForEach(x => mailMessage.To.Add(new MailAddress(x.Email)));
                    //}
                    if (!email.Recipients.Any(x => x == null) && email.Recipients.Count() > 0)
                    {
                        email.Recipients.ToList().ForEach(x => mailMessage.To.Add(new MailAddress(x.Email)));

                        if (email.CCIds != null && email.CCIds.Count > 0)
                            email.CCIds.ForEach(x => mailMessage.CC.Add(new MailAddress(x)));

                        if (email.ToIds != null && email.ToIds.Count > 0)
                            email.ToIds.ForEach(x => mailMessage.To.Add(new MailAddress(x)));
                        //mailMessage.To.Add("saideep.s@mobinexttech.com");
                        //if (email.Body.GetType() == typeof(AlternateView))
                        //{
                        //    mailMessage.AlternateViews.Add((AlternateView)email.Body);
                        //}
                        //else
                        //{
                        //    mailMessage.Body = (string)email.Body;
                        //}
                        mailMessage.Subject = email.Subject;
                        mailMessage.AlternateViews.Add(GetAlternateView(email.Body, imgId));

                        //Add attachment to Email             
                        //mailMessage.Attachments.Add(new Attachment(attachment));
                        //Set the format of the mail message body as HTML
                        mailMessage.IsBodyHtml = true;
                        var smtpCliient = new SmtpClient(smtpHost);
                        smtpCliient.Port = port;
                        smtpCliient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpCliient.UseDefaultCredentials = false;
                        //It should be false in web.Config when deploying on Skoda UAT.
                        smtpCliient.EnableSsl = enableSsl;
                        //It should be false in web.Config when deploying on Skoda UAT.
                        smtpCliient.Credentials = new NetworkCredential(smtpEmail, password);
                        smtpCliient.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }

        public async Task SendEmailAsync(List<UserData> userlist, string subject, string body)
        {
            try
            {
                //string smtpHost = WebConfigurationManager.AppSettings["smtpHost"];
                //string smtpEmail = WebConfigurationManager.AppSettings["smtpEmailId"];
                //string password = WebConfigurationManager.AppSettings["smtpPassword"];

                string smtpHost = "smtp.gmail.com";
                string smtpEmail = "devacc909909@gmail.com";
                //string password = "12180917";
                string password = "Password*123";
                MailMessage mail = new MailMessage();

                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;
                MailAddress mailAddress = new MailAddress(smtpEmail);
                mail.From = mailAddress;
                foreach (var item in userlist)
                {
                    //  mail.To.Add(item.EmailId);
                    mail.To.Add(item.Email);
                }
                var smtpCliient = new SmtpClient(smtpHost);
                smtpCliient.Port = 587;
                smtpCliient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpCliient.UseDefaultCredentials = false;
                smtpCliient.EnableSsl = true;
                smtpCliient.Credentials = new System.Net.NetworkCredential(smtpEmail, password);
                await Task.Run(() => smtpCliient.SendMailAsync(mail));
                //await smtpCliient.SendMailAsync(mail);
                //isSend = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }

        private LinkedResource GenerateImageID()
        {
            try
            {
                string mailLogoPath = ConfigurationManager.AppSettings["mailLogoPath"];
                var path = System.Web.Hosting.HostingEnvironment.MapPath(mailLogoPath);
                LinkedResource img = new LinkedResource(path, MediaTypeNames.Image.Jpeg);
                img.ContentId = "LogoImage";
                return img;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return null;
        }

        private AlternateView GetAlternateView(string body, LinkedResource img)
        {
            AlternateView AV = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
            //if (body.Contains("<img src=cid:LogoImage"))
            AV.LinkedResources.Add(img);
            return AV;
        }

        //[HttpPost]
        //public ActionResult FormStatus(int dataRowId, int formID)
        //{
        //    CommonBAL = new CommonBAL();
        //    var result = CommonBAL.FormStatus(dataRowId, formID);
        //    return JsonResult(result);
        //}      


        //public async Task PrepareAndSendFinalEmailToHelpDesk(int rowId, int FormID, string FormName, string submitterEmpEmailId)
        //{
        //    string itServiceDeskToEmails = null;
        //    string itServiceDeskCcEmails = null;
        //    string itServiceDeskBCcEmails = null;
        //    submitterEmpEmailId = "devacc909909@gmail.com";
        //    CommonBAL objCommonBAL = new CommonBAL();
        //    try
        //    {
        //        string emailBodyITHelpDesk = await objCommonBAL.GetEmailBodyForITHelpDesk(rowId, FormID, FormName);
        //        string employeeLocation = string.Empty;
        //        int locationId = 0;
        //        //if (FormID == 12) //Shared Folder Form
        //        //{
        //        //    //Shared Folder form should go to respective help desk based on File Server Name
        //        //    //employeeLocation = GetEmployeeFileServerLocationFromRequest(masterRequest.RequestID);
        //        //    employeeLocation = "";
        //        //}
        //        //else
        //        //{
        //        //    //employeeLocation = GetEmployeeLocationFromRequest(formRequestDto.FormID, formRequestDto.FormName, masterRequest.RequestID);
        //        //    employeeLocation = "";
        //        //}

        //        //locationId = new LookupDAL().GetITServiceDeskLocationId(employeeLocation);
        //        //locationId = 1;
        //        ////List<ITServiceDeskContact> iTServiceDeskContacts = new LookupDAL().GetITServiceDeskContacts(locationId);
        //        //List<ITServiceDeskContact> iTServiceDeskContacts = new List<ITServiceDeskContact>();
        //        //if (iTServiceDeskContacts != null && iTServiceDeskContacts.Count > 0)
        //        //{
        //        //    List<ITServiceDeskContact> iTServiceDeskToContacts = iTServiceDeskContacts.Where(p => p.IsManager == false).ToList();
        //        //    itServiceDeskToEmails = string.Join("; ", iTServiceDeskToContacts.Select(p => p.Email));

        //        //    List<ITServiceDeskContact> iTServiceDeskCcContacts = iTServiceDeskContacts.Where(p => p.IsManager == true).ToList();
        //        //    itServiceDeskCcEmails = string.Join("; ", iTServiceDeskCcContacts.Select(p => p.Email));
        //        //    itServiceDeskCcEmails = itServiceDeskCcEmails + ";" + string.Join("; ", submitterEmpEmailId);

        //        //    //IT ASSET Admin team email contact for IT Clearance, IT Asset & Smart Phone requisition only
        //        //    if (formRequestDto.FormID == 9 || formRequestDto.FormID == 5 || formRequestDto.FormID == 10)
        //        //    {
        //        //        string strITAssetAdminContact = Convert.ToString(ConfigurationManager.AppSettings["ITAssetAdminContact"]);
        //        //        itServiceDeskCcEmails = itServiceDeskCcEmails + ";" + string.Join("; ", strITAssetAdminContact);
        //        //    }

        //        //    itServiceDeskBCcEmails = "shaunak.v@mobinexttech.com";
        //        //}

        //        itServiceDeskToEmails = "devacc909909@gmail.com";
        //        string itServiceDeskEmailSubject = Convert.ToString(ConfigurationManager.AppSettings["ITServiceDeskEmailSubject"]);
        //        CommonUtility.SendMail(
        //            itServiceDeskToEmails,
        //            itServiceDeskEmailSubject + FormID,
        //            emailBodyITHelpDesk,
        //            itServiceDeskCcEmails,
        //            itServiceDeskBCcEmails);

        //        Console.WriteLine("************* Email is sent to IT Service Desk successfully.***************");

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message, ex);
        //    }
        //}

        public async Task SendMailForISLS(EmailDataModel emailData, string GPNo)
        {
            try
            {
                string smtpHost = WebConfigurationManager.AppSettings["smtpHost"];
                string smtpEmail = WebConfigurationManager.AppSettings["smtpEmailId"];
                string password = WebConfigurationManager.AppSettings["smtpPassword"];
                int port = Convert.ToInt32(WebConfigurationManager.AppSettings["smtpPort"]);
                bool enableSsl = Convert.ToBoolean(WebConfigurationManager.AppSettings["EnableSsl"]);

                var imgId = GenerateImageID();
                var emailBody = new EmailBodyBuilder();
                var emails = await emailBody.CreateEmailForISLS(emailData, GPNo);

                foreach (var email in emails)
                {
                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(smtpEmail);
                    email.Recipients.ToList().ForEach(x => mailMessage.To.Add(new MailAddress(x.Email)));

                    if (email.CCIds != null && email.CCIds.Count > 0)
                        email.CCIds.ForEach(x => mailMessage.CC.Add(new MailAddress(x)));

                    if (email.ToIds != null && email.ToIds.Count > 0)
                        email.ToIds.ForEach(x => mailMessage.To.Add(new MailAddress(x)));

                    mailMessage.Subject = email.Subject;
                    //if (email.Body.GetType() == typeof(AlternateView))
                    //{
                    //    mailMessage.AlternateViews.Add((AlternateView)email.Body);
                    //}
                    //else
                    //{
                    //    mailMessage.Body = (string)email.Body;
                    //}                    
                    mailMessage.AlternateViews.Add(GetAlternateView(email.Body, imgId));

                    //Add attachment to Email             
                    //mailMessage.Attachments.Add(new Attachment(attachment));
                    //Set the format of the mail message body as HTML
                    mailMessage.IsBodyHtml = true;
                    var smtpCliient = new SmtpClient(smtpHost);
                    smtpCliient.Port = port;
                    smtpCliient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpCliient.UseDefaultCredentials = false;
                    //It should be false in web.Config when deploying on Skoda UAT.
                    smtpCliient.EnableSsl = enableSsl;
                    //It should be false in web.Config when deploying on Skoda UAT.
                    smtpCliient.Credentials = new NetworkCredential(smtpEmail, password);
                    smtpCliient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }


    }
}
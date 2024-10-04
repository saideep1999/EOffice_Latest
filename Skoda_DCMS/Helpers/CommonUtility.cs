using Skoda_DCMS.App_Start;
using Syncfusion.Compression.Zip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;

namespace Skoda_DCMS.Helpers
{
    public class CommonUtility
    {

        public void SendMail(string to, string subject, string body, string cc = "", string bcc = "")
        {
            try
            {
                string smtpHost = WebConfigurationManager.AppSettings["smtpHost"];
                string smtpEmail = WebConfigurationManager.AppSettings["smtpEmailId"];
                string password = WebConfigurationManager.AppSettings["smtpPassword"];

                //Instantiate a new instance of MailMessage
                MailMessage mailMessage = new MailMessage();

                string crossoverEmailID = ConfigurationManager.AppSettings["CrossoverEmailID"];

                //Set the recepient address of the mail message
                mailMessage.From = new MailAddress(smtpEmail);

                if (to != null && to != string.Empty)
                {
                    foreach (string addr in to.Split(';'))
                    {
                        if ((addr != null) && (addr != string.Empty))
                        {
                            mailMessage.To.Add(new MailAddress(addr));
                        }
                    }
                }

                //Check if the bcc value is null or an empty string
                if ((bcc != null) && (bcc != string.Empty))
                {
                    // Set the Bcc address of the mail message
                    foreach (string addr in bcc.Split(';'))
                    {
                        if ((addr != null) && (addr != string.Empty))
                        {
                            mailMessage.Bcc.Add(new MailAddress(addr));
                        }
                    }
                }

                //Check if the cc value is null or an empty value
                if ((cc != null) && (cc != string.Empty))
                {
                    // Set the CC address of the mail message
                    foreach (string addr in cc.Split(';'))
                    {
                        if ((addr != null) && (addr != string.Empty))
                        {
                            mailMessage.CC.Add(new MailAddress(addr));
                        }

                    }
                }

                //Set the subject of the mail message
                mailMessage.Subject = subject;

                // Set the body of the mail message
                mailMessage.Body = body;

                //Add attachment to Email             
                //mailMessage.Attachments.Add(new Attachment(attachment));

                //Set the format of the mail message body as HTML
                mailMessage.IsBodyHtml = true;

                var smtpCliient = new SmtpClient(smtpHost);
                smtpCliient.Port = 587;
                smtpCliient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpCliient.UseDefaultCredentials = false;
                smtpCliient.EnableSsl = true;
                smtpCliient.Credentials = new NetworkCredential(smtpEmail, password);
                smtpCliient.Send(mailMessage);


            }
            catch (Exception ex)
            {
                //throw ex;
                Log.Error(ex.Message, ex);
            }
            Console.WriteLine(Constants.SEND_EMAIL);

        }

    }
}
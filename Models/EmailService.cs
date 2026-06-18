using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
 using System.IO;
using System.Net.Mail;
using System.Text;

namespace slnRhonline.Models
{
    public class EmailService
    {
        private string _smtpHost;
        private int _smtpPort;
        private string _fromAddress;
        private string _fromDisplayName;

        public EmailService(string smtpHost, int smtpPort, string fromAddress, string fromDisplayName = null)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _fromAddress = fromAddress;
            _fromDisplayName = fromDisplayName ?? fromAddress;
        }

        public string SendEmail(string toAddress, string subject, string body,  string ccAddress = null)
        {
            try
            {
                MailMessage email = new MailMessage
                {
                    From = new MailAddress(_fromAddress, _fromDisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    Priority = MailPriority.Normal
                };

                email.To.Add(toAddress);

                //if (!string.IsNullOrEmpty(bccAddress))
                //{
                //    email.Bcc.Add(bccAddress);
                //}

                if (!string.IsNullOrEmpty(ccAddress))
                {
                    email.CC.Add(ccAddress);
                }

                // Configurar notificaciones de entrega
                email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.Delay;

                // Enviar el correo
                using (SmtpClient cliente = new SmtpClient("10.200.5.23", 25))
                {
                    cliente.UseDefaultCredentials = false;
                    cliente.EnableSsl = false;
                    cliente.Send(email);
                }

                return "EXITO";
            }
            catch (Exception ex)
            {
                return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }
        }

        public string SendEmailWithImage(string toAddress, string subject, string body, byte[] image, string imageName, bool isHtml = true, string bccAddress = null, string ccAddress = null)
        {
            try
            {
                MailMessage email = new MailMessage
                {
                    From = new MailAddress(_fromAddress, _fromDisplayName),
                    Subject = subject,
                    IsBodyHtml = isHtml,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    Priority = MailPriority.Normal
                };

                email.To.Add(toAddress);

                if (!string.IsNullOrEmpty(bccAddress))
                {
                    email.Bcc.Add(bccAddress);
                }

                if (!string.IsNullOrEmpty(ccAddress))
                {
                    email.CC.Add(ccAddress);
                }

                // Configurar el cuerpo del mensaje con la imagen
                AlternateView bodyView = AlternateView.CreateAlternateViewFromString(body, null, System.Net.Mime.MediaTypeNames.Text.Html);
                LinkedResource imageResource = new LinkedResource(new MemoryStream(image), System.Net.Mime.MediaTypeNames.Image.Jpeg)
                {
                    ContentId = imageName
                };
                bodyView.LinkedResources.Add(imageResource);
                email.AlternateViews.Add(bodyView);

                // Configurar notificaciones de entrega
                email.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.Delay;

                // Enviar el correo
                using (SmtpClient cliente = new SmtpClient("10.200.5.23", 25))
                {
                    cliente.UseDefaultCredentials = false;
                    cliente.EnableSsl = false;
                    cliente.Send(email);
                }

                return "EXITO";
            }
            catch (Exception ex)
            {
                return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }
        }
    }
}
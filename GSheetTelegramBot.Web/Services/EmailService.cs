using System.Net;
using System.Net.Mail;
using GSheetTelegramBot.Web.Helpers;
using GSheetTelegramBot.Web.Interfaces;

namespace GSheetTelegramBot.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfiguration _emailConfig;

        public EmailService(EmailConfiguration emailConfig)
        {
            _emailConfig = emailConfig;
        }

        public async Task SendEmailAsync(string? to, string subject, string htmlContent)
        {
            var fromAddress = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName);
            var toAddress = new MailAddress(to);

            using (var smtp = new SmtpClient
                   {
                       Host = _emailConfig.SmtpServer,
                       Port = _emailConfig.SmtpPort,
                       EnableSsl = true,
                       DeliveryMethod = SmtpDeliveryMethod.Network,
                       UseDefaultCredentials = false,
                       Credentials = new NetworkCredential(_emailConfig.SmtpUsername, _emailConfig.SmtpPassword)
                   })
            using (var message = new MailMessage(fromAddress, toAddress)
                   {
                       Subject = subject,
                       Body = htmlContent,
                       IsBodyHtml = true
                   })
            {
                try
                {
                    await smtp.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw; 
                }
             
            }
        }
    }
}

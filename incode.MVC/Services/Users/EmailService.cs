using System.Net;
using System.Net.Mail;

namespace incode.MVC.Services.Users
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string content)
        {
            var settings = _configuration.GetSection("EmailSettings");

            var client = new SmtpClient(settings["MailServer"], int.Parse(settings["MailPort"]))
            {
Credentials = new NetworkCredential(settings["SenderEmail"], settings["Password"]), //570錯誤檢查 直接打密碼
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(settings["SenderEmail"], settings["SenderName"]),
                Subject = subject,
                Body = content,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
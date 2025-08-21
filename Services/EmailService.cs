using CuaHangQuanAo.Entities;
using CuaHangQuanAo.Models;
using System.Net.Mail;

namespace CuaHangQuanAo.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            var smtpUser = _configuration["EmailSettings:Username"];
            var smtpPassword = _configuration["EmailSettings:Password"];

            var message = new MailMessage(fromEmail!, to, subject, body);
            message.IsBodyHtml = true;

            using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword);
                smtpClient.EnableSsl = true;
                try
                {
                    await smtpClient.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    throw new InvalidOperationException("Error sending email", ex);
                }
            }
        }
    }
}
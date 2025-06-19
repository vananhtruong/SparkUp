
using Microsoft.Extensions.Options;
using SparkUp.MVC.Models;
using System.Net;
using System.Net.Mail;

namespace SparkUp.MVC.Service
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSenderDto _emailSenderDto;

        public EmailSender(IOptions<EmailSenderDto> emailSenderDto)
        {
            _emailSenderDto = emailSenderDto.Value;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_emailSenderDto.Email, _emailSenderDto.Password),
                EnableSsl = true,
                Timeout = 20000 // 20 seconds
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSenderDto.Email),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            return client.SendMailAsync(mailMessage);
        }
    }
}

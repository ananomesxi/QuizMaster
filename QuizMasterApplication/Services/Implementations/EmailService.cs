using Microsoft.Extensions.Configuration;
using QuizMasterApplication.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync( string email, string subject, string message, CancellationToken cancellationToken = default)
        {
            var host = _configuration["Email:Host"] ?? throw new InvalidOperationException("Email host is not configured.");

            var senderEmail = _configuration["Email:SenderEmail"] ?? throw new InvalidOperationException("Sender email is not configured.");

            var senderPassword = _configuration["Email:SenderPassword"] ?? throw new InvalidOperationException("Sender password is not configured.");

            if (!int.TryParse(_configuration["Email:Port"], out var port))
            {
                throw new InvalidOperationException("Email port is not configured correctly.");
            }

            using var smtpClient = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage( senderEmail, email, subject, message);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
        }
    }
}
    
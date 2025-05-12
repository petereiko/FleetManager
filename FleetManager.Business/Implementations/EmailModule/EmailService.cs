using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Interfaces;
using FleetManager.Business;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.Interfaces.EmailModule;

namespace FleetManager.Business.Implementations.EmailModule
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly FleetManagerDbContext _context;

        public EmailService(IConfiguration configuration, FleetManagerDbContext context, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> LogEmail(EmailLogDto model)
        {
            bool isLogged = false;
            try
            {
                var log = new EmailLog
                {
                    Recepient = model.Email,
                    Subject = model.Subject,
                    Message = model.Message,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    IsDeleted = false,
                    IsSent = false,
                    RetryCount = 0,
                    CreatedBy = "System"
                };
                _context.EmailLogs.Add(log);
                isLogged = await _context.SaveChangesAsync() > 0;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return isLogged;
        }

        public bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return false;
        }


        public bool SendEmail(string email, string subject, string message)
        {
            bool isSent = false;
            if (string.IsNullOrEmpty(email)) return false;

            var smtpSettings = _configuration.GetSection("EmailSettings");

            var smtpClient = new SmtpClient(smtpSettings["SmtpServer"])
            {
                Port = int.Parse(smtpSettings["SmtpPort"]),
                Credentials = new NetworkCredential(smtpSettings["SmtpUser"], smtpSettings["SmtpPass"]),
                EnableSsl = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["SmtpUser"], smtpSettings["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email.Trim());

            try
            {
                smtpClient.Send(mailMessage);
                isSent = true;
            }
            catch (SmtpException smtpEx)
            {
                // Log SMTP-specific exceptions
                _logger.LogError(smtpEx.Message, smtpEx);
            }
            catch (Exception ex)
            {
                // Log general exceptions
                _logger.LogError(ex.Message, ex);
                throw new Exception("An unexpected error occurred while sending the email. Please try again later.");
            }
            return isSent;
        }


        public void SendEmailToAllSubscribers(string subject, string message, List<string> emails)
        {
            foreach (var email in emails)
            {
                SendEmail(email, subject, message);
            }
        }

        public bool SendEmailWithAttachment(string email, string subject, string message, Attachment attachment)
        {
            bool isSent = false;
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Invalid email address");
            }

            var smtpSettings = _configuration.GetSection("EmailSettings");

            var smtpClient = new SmtpClient(smtpSettings["SmtpServer"])
            {
                Port = int.Parse(smtpSettings["SmtpPort"]),
                Credentials = new NetworkCredential(smtpSettings["SmtpUser"], smtpSettings["SmtpPass"]),
                EnableSsl = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);
            mailMessage.Attachments.Add(attachment);

            try
            {
                smtpClient.Send(mailMessage);
                isSent = true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx.Message, smtpEx);
                throw new Exception("There was an issue sending the email. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw new Exception("An unexpected error occurred while sending the email. Please try again later.");
            }
            return isSent;
        }
    }
}

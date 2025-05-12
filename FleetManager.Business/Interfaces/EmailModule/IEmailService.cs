using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces;

namespace FleetManager.Business.Interfaces
{
    public interface IEmailService
    {
        bool IsValidEmail(string email);
        bool SendEmail(string email, string subject, string message);
        bool SendEmailWithAttachment(string email, string subject, string message, Attachment attachment);
        void SendEmailToAllSubscribers(string subject, string message, List<string> emails);
        Task<bool> LogEmail(EmailLogDto model);
    }
}

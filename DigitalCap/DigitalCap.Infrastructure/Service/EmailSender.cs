using DigitalCap.Core.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string message, string fromEmail)
        {
            return Task.CompletedTask;
        }
    }
}

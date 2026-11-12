using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string message, string fromEmail);
    }
}

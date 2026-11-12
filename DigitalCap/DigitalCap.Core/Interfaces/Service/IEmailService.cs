using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IEmailService
    {
        Task<ServiceResult<bool>> SendEmailAsync(string from, string fromPw, string to, string subject, string body);
        Task<ServiceResult<bool>> SendEmailAsync(string from, string fromPw, IEnumerable<string> to, string subject, string body, string htmlMessage);
        Task<ServiceResult<bool>> SendEmailAsync(IEnumerable<string> to, string subject, string body, string htmlMessage);
        Task<ServiceResult<bool>> SendEmailAsync(string from, string fromPw, IEnumerable<string> to, string subject, string body);
        Task<ServiceResult<bool>> SendEmailAsync(IEnumerable<string> to, string subject, string body);
        Task<ServiceResult<bool>> SendEmailAsync(string email, string subject, string body);
        Task<ServiceResult<bool>> SendEmailAsync(string email, string subject, string body, string htmlMessage);
        Task<ServiceResult<bool>> SendEmailAsyncWithCC(string to, string cc, string subject, string body, string htmlMessage);

        Task<ServiceResult<bool>> SendNewUserEmailAsync(string toEmail, string userName, string password, string senderEmail);
   
    }
}




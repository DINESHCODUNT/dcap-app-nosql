using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Infrastructure.Service;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailRepository;
        private readonly ITemplateReader _templateReader;
        private readonly IEmailSender _emailSender;
        public EmailService(IEmailRepository emailRepository, ITemplateReader templateReader, IEmailSender emailSender)
        {
            _emailRepository = emailRepository;
            _templateReader = templateReader;
            _emailSender = emailSender;
            
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(string from, string fromPw, string to, string subject, string body)
        {
            await _emailRepository.SendEmailAsync(from, fromPw, to, subject, body);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(string from, string fromPw, IEnumerable<string> to, string subject, string body, string htmlMessage)
        {
            await _emailRepository.SendEmailAsync(from, fromPw, to, subject, body, htmlMessage);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(IEnumerable<string> to, string subject, string body, string htmlMessage)
        {
            await _emailRepository.SendEmailAsync(to, subject, body, htmlMessage);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsyncWithCC(string to, string cc, string subject, string body, string htmlMessage)
        {
            await _emailRepository.SendEmailAsyncWithCC(to, cc, subject, body, htmlMessage);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(string email, string subject, string body)
        {
            await _emailRepository.SendEmailAsync(email, subject, body);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(string email, string subject, string body, string htmlMessage)
        {
            await _emailRepository.SendEmailAsync(email, subject, body, htmlMessage);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(string from, string fromPw, IEnumerable<string> to, string subject, string body)
        {
            await _emailRepository.SendEmailAsync(from, fromPw, to, subject, body);
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(IEnumerable<string> to, string subject, string body)
        {
            await _emailRepository.SendEmailAsync(to, subject, body);
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendNewUserEmailAsync(string toEmail,string userName,string password,string fromEmail)
        {
            var body = await _templateReader.ReadTemplateAsync("NewUserEmail",
            new { UserName = userName, Password = password });

            await _emailSender.SendEmailAsync(
                toEmail,
                "Your Account Has Been Created",
                body,
                fromEmail
            );

            return ServiceResult<bool>.Success(true);
        }

    }
}



    



using DigitalCap.Core.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class EmailRepository : IEmailRepository
    {
        private readonly int _serverPort = 587;
        private readonly string _serverAddress;
        private readonly string _defaultEmailFrom;
        private readonly string _defaultEmailPw;
        private readonly bool _devTest = false;
        public EmailRepository(IConfiguration config)
        {
            if (config["DevTest"] != null)
            {
                _serverAddress = "localhost";
                _serverPort = 25;
                _devTest = true;
            }
            else
            {
                _serverAddress = config["EmailServerAddress"];
            }
            _defaultEmailFrom = config["EmailDefaultFrom"];
            _defaultEmailPw = config["EmailDefaultPwd"];
        }
        public async Task SendEmailAsync(string from, string fromPw, string to, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(to);
                    message.From = new MailAddress(from);
                    message.Subject = subject;
                    message.Body = body;

                    await SendAsync(message, from, fromPw);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task SendEmailAsync(string from, string fromPw, IEnumerable<string> to, string subject, string body, string htmlMessage)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(from);
                    to.ToList().ForEach(x =>
                    {
                        message.To.Add(x);
                    });
                    message.Subject = subject;
                    message.Body = body;
                    ContentType mimeType = new ContentType("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(htmlMessage, mimeType);
                    message.AlternateViews.Add(alternate);

                    await SendAsync(message, from, fromPw);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body, string htmlMessage)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_defaultEmailFrom);
                    to.ToList().ForEach(x =>
                    {
                        message.To.Add(x);
                    });
                    message.Subject = subject;
                    message.Body = body;
                    ContentType mimeType = new ContentType("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(htmlMessage, mimeType);
                    message.AlternateViews.Add(alternate);

                    await SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SendEmailAsyncWithCC(string to, string cc, string subject, string body, string htmlMessage)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_defaultEmailFrom);
                    message.To.Add(to);
                    message.CC.Add(cc);
                    message.Subject = subject;
                    message.Body = body;
                    ContentType mimeType = new ContentType("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(htmlMessage, mimeType);
                    message.AlternateViews.Add(alternate);

                    await SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SendEmailAsync(string from, string fromPw, IEnumerable<string> to, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(from);
                    to.ToList().ForEach(x =>
                    {
                        message.To.Add(x);
                    });
                    message.Subject = subject;
                    message.Body = body;

                    await SendAsync(message, from, fromPw);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_defaultEmailFrom);
                    to.ToList().ForEach(x =>
                    {
                        message.To.Add(x);
                    });
                    message.Subject = subject;
                    message.Body = body;

                    await SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_defaultEmailFrom);
                    message.To.Add(email);
                    message.Body = body;
                    message.Subject = subject;

                    await SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task SendEmailAsync(string email, string subject, string body, string htmlMessage)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_defaultEmailFrom);
                    message.To.Add(email);
                    message.Body = body;
                    ContentType mimeType = new ContentType("text/html");
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(htmlMessage, mimeType);
                    message.AlternateViews.Add(alternate);
                    message.Subject = subject;

                    await SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task SendAsync(MailMessage message, string from = null, string fromPassword = null)
        {
            using (var client = new SmtpClient(_serverAddress, _serverPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(from ?? _defaultEmailFrom,
                                                           fromPassword ?? _defaultEmailPw);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                if (!_devTest)
                {
                    client.EnableSsl = true;
                    client.TargetName = $"STARTTLS/{_serverAddress}";
                }

#if DEBUG
                client.SendCompleted += (sender, e) =>
                {
                    var exception = e.Error;
                };
#endif

                await client.SendMailAsync(message);
            }
        }
    }
}





   






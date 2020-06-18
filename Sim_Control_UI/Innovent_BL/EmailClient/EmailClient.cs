using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Innovent_BL.EmailClient
{
    public interface IEmailSender
    {
        void SendEmail(Message message);
    }

    public class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;
        private readonly IOptions<EmailConfigOptions> _emailConfig;

        public EmailSender(IOptions<EmailConfigOptions> emailConfig, ILogger<EmailSender> logger)
        {
            _logger = logger;
            if (emailConfig.Value.SmtpServer == "")
            {
                _logger.LogInformation("Email settings are not configured correctly");
                return;
            }
            _emailConfig = emailConfig;
        }

        public void SendEmail(Message message)
        {
            if (_emailConfig == null)
            {
                _logger.LogInformation("Email settings are not configured correctly");
                return;
            }

            _logger.LogInformation("\t\tSending report...");
            var emailMessage = CreateEmailMessage(message);
            Send(emailMessage);
        }

        private MimeMessage CreateEmailMessage(Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailConfig.Value.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Text) { Text = message.Content };

            return emailMessage;
        }

        private void Send(MimeMessage mailMessage)
        {
            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(_emailConfig.Value.SmtpServer, _emailConfig.Value.Port, true);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(_emailConfig.Value.UserName, _emailConfig.Value.Password);

                    client.Send(mailMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    throw;
                }
                finally
                {
                    client.Disconnect(true);
                    client.Dispose();
                }
            }
        }
    }

    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public Message(IEnumerable<string> to, string subject, string content)
        {
            To = new List<MailboxAddress>();

            To.AddRange(to.Select(x => new MailboxAddress(x)));
            Subject = subject;
            Content = content;
        }
    }
}

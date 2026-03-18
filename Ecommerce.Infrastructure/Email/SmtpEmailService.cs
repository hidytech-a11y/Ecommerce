using System.Net;
using System.Net.Mail;
using Ecommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.Parse(_config["Email:SmtpPort"]);
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];

        var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var message = new MailMessage(
            username,
            to,
            subject,
            body);

        await client.SendMailAsync(message);
    }
}
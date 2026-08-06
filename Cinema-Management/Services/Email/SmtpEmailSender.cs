using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Cinema_Management.Services.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set Email:Smtp:Host and Email:Smtp:FromEmail.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                _options.FromEmail,
                string.IsNullOrWhiteSpace(_options.FromName)
                    ? _options.FromEmail
                    : _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(
                _options.UserName,
                _options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}

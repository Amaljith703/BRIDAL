using MailKit.Net.Smtp;
using MailKit.Security;
using MakeupArt.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MakeupArt.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public class EmailService(
    IOptions<SmtpOptions> smtpOptions,
    IHostEnvironment environment,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _smtp = smtpOptions.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email queued for {Recipient}: {Subject}", to, subject);

        // Always keep a local copy so development and support flows are testable without SMTP.
        try
        {
            var directory = Path.Combine(environment.ContentRootPath, "logs", "emails");
            Directory.CreateDirectory(directory);
            var file = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Safe(to)}.html");
            await File.WriteAllTextAsync(file, $"<!-- To: {to} | Subject: {subject} -->\n{htmlBody}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not write development email to disk.");
        }

        if (!_smtp.Enabled) return;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_smtp.Username))
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP delivery failed for {Recipient}.", to);
        }
    }

    private static string Safe(string value) => string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is '@' or '.' or '-' ? c : '_'));
}

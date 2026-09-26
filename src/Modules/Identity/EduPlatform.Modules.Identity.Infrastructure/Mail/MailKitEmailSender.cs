using EduPlatform.Modules.Identity.Application;
using EduPlatform.Modules.Identity.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EduPlatform.Modules.Identity.Infrastructure.Mail;

/// <summary>
/// Sends mail over SMTP. Locally that is Mailpit, which accepts everything and delivers
/// nothing, so no message can escape to a real address during development.
/// </summary>
/// <remarks>
/// A failure to send is logged but not rethrown. Registration and password reset have already
/// been committed by the time this runs; turning a mail-server hiccup into a 500 would tell the
/// user their registration failed when in fact the account exists.
/// </remarks>
internal sealed partial class MailKitEmailSender(
    IOptions<SmtpOptions> smtpOptions,
    IOptions<IdentitySettings> identitySettings,
    ILogger<MailKitEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var settings = identitySettings.Value;
        var smtp = smtpOptions.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(
                smtp.Host,
                smtp.Port,
                smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto,
                cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(smtp.Username))
            {
                await client
                    .AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken)
                    .ConfigureAwait(false);
            }

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

            Sent(logger, toAddress, subject);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SendFailed(logger, toAddress, subject, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent '{Subject}' to {Recipient}.")]
    private static partial void Sent(ILogger logger, string recipient, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not send '{Subject}' to {Recipient}.")]
    private static partial void SendFailed(ILogger logger, string recipient, string subject, Exception exception);
}

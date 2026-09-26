namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>Sends transactional e-mail. In development every message is captured by Mailpit.</summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

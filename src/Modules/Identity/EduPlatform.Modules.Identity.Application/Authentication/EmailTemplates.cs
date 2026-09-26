using System.Globalization;
using System.Net;

namespace EduPlatform.Modules.Identity.Application.Authentication;

/// <summary>
/// The transactional e-mails this module sends. Plain inline HTML rather than a template engine:
/// there are two messages, and a dependency would cost more than it saves.
/// </summary>
internal static class EmailTemplates
{
    public static string ConfirmEmail(string firstName, string clientBaseUrl, string token) =>
        Wrap(
            $"Здравейте, {WebUtility.HtmlEncode(firstName)}!",
            "Благодарим ви за регистрацията в EduPlatform. Остава само да потвърдите адреса си.",
            "Потвърждаване на адреса",
            $"{clientBaseUrl.TrimEnd('/')}/auth/confirm-email?token={WebUtility.UrlEncode(token)}",
            "Ако не сте се регистрирали, просто пренебрегнете това съобщение.");

    public static string ResetPassword(string firstName, string clientBaseUrl, string token, TimeSpan validFor) =>
        Wrap(
            $"Здравейте, {WebUtility.HtmlEncode(firstName)}!",
            "Получихме заявка за нова парола за вашия акаунт.",
            "Задаване на нова парола",
            $"{clientBaseUrl.TrimEnd('/')}/auth/reset-password?token={WebUtility.UrlEncode(token)}",
            string.Create(
                CultureInfo.InvariantCulture,
                $"Връзката е валидна {validFor.TotalHours:0} часа. Ако не сте заявявали смяна, не предприемайте нищо — паролата ви остава непроменена."));

    private static string Wrap(string greeting, string intro, string buttonText, string url, string footer) =>
        $"""
        <!doctype html>
        <html lang="bg">
          <body style="margin:0;padding:24px;background:#f4f6f8;font-family:Segoe UI,Roboto,Arial,sans-serif;color:#1a1a1a;">
            <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:12px;padding:32px;">
              <h1 style="margin:0 0 16px;font-size:20px;">{greeting}</h1>
              <p style="margin:0 0 24px;line-height:1.6;">{intro}</p>
              <p style="margin:0 0 28px;">
                <a href="{url}" style="display:inline-block;background:#1f4e79;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:600;">{buttonText}</a>
              </p>
              <p style="margin:0 0 8px;font-size:13px;color:#5f6b7a;line-height:1.6;">
                Ако бутонът не работи, копирайте този адрес в браузъра си:<br>
                <span style="word-break:break-all;">{url}</span>
              </p>
              <hr style="border:none;border-top:1px solid #e5e9ef;margin:24px 0;">
              <p style="margin:0;font-size:13px;color:#5f6b7a;line-height:1.6;">{footer}</p>
            </div>
          </body>
        </html>
        """;
}

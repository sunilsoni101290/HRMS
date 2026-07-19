namespace Application.Interfaces
{
    // Thin cross-cutting abstraction over sending an email. Backed today by
    // System.Net.Mail.SmtpClient (see Application.Services.EmailSender), but
    // callers should only ever depend on this interface - never construct an
    // SmtpClient themselves - so the transport can be swapped later without
    // touching any calling workflow (Leave Application, etc.).
    //
    // Contract: SendAsync NEVER throws. Any failure (missing/invalid SMTP
    // config, network error, bad address, whatever) is caught internally,
    // logged, and reported back as a `false` return value. This sandbox has
    // no real SMTP server available, so email is always a best-effort,
    // fire-and-forget secondary channel - the in-app Notification is the
    // channel that must actually work end-to-end.
    public interface IEmailSender
    {
        Task<bool> SendAsync(string toEmail, string subject, string body, bool isBodyHtml = false);
    }
}

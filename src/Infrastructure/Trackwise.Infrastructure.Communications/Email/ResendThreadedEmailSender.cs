using Trackwise.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Trackwise.Infrastructure.Communications.Email;

internal sealed class ResendThreadedEmailSender(
    IResend resend,
    IOptions<ResendOptions> options,
    ILogger<ResendThreadedEmailSender> logger) : IThreadedEmailSender
{
    public string ReplyDomain => options.Value.SenderDomain;

    public async Task<ThreadedEmailResult> SendAsync(ThreadedEmail email, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(email.To);
        ArgumentException.ThrowIfNullOrEmpty(email.Subject);
        ArgumentException.ThrowIfNullOrEmpty(email.HtmlBody);

        try
        {
            var message = new EmailMessage
            {
                From = $"{options.Value.SenderName} <{options.Value.SenderEmail}>",
                Subject = email.Subject,
                HtmlBody = email.HtmlBody
            };

            message.To.Add(email.To);

            // Resend's implicit string -> EmailAddressList conversion throws on null, so a one-off
            // email (no reply address) must leave ReplyTo unset rather than assign null.
            if (!string.IsNullOrEmpty(email.ReplyTo))
            {
                message.ReplyTo = email.ReplyTo;
            }

            if (!string.IsNullOrEmpty(email.MessageId))
            {
                message.Headers["Message-ID"] = email.MessageId;
            }

            if (!string.IsNullOrEmpty(email.InReplyToMessageId))
            {
                message.Headers["In-Reply-To"] = email.InReplyToMessageId;
            }

            if (!string.IsNullOrEmpty(email.References))
            {
                message.Headers["References"] = email.References;
            }

            var response = await resend.EmailSendAsync(message, ct);
            logger.LogInformation(
                "Threaded email has been sent to {Mail}, subject: '{Subject}', provider id: {ProviderId}",
                email.To, email.Subject, response.Content);
            return new ThreadedEmailResult(true, response.Content.ToString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Could not send threaded email to {Mail}, subject: '{Subject}'. \nThrown exception: {Exception}",
                email.To, email.Subject, ex.ToString());
            return new ThreadedEmailResult(false, null);
        }
    }
}

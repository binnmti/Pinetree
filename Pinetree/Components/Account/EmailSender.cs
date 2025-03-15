using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Pinetree.Data;

namespace Pinetree.Components.Account;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly EmailClient _emailClient;
    private readonly ILogger<EmailSender> _logger;
    private readonly string _senderEmail;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        var connectionString = configuration["AzureCommunicationServicesConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Azure Communication Services connection string is not configured.");
        }
        _emailClient = new EmailClient(connectionString);
        _logger = logger;
        _senderEmail = configuration["SenderEmail"] ?? "DoNotReply@yourdomain.com";
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var content = new EmailContent("Pinetree - Confirm your email")
        {
            Html = $"Dear {user.UserName}<br>" +
            "<br>" +
            $"Thank you for signing up for Pinetree.<br>" +
            "To complete your registration, please verify your email address by clicking the link below:<br>" +
            "<br>" +
            $"<a href='{confirmationLink}'>Verification Link</a>.<br>" +
            "<br>" +
            $"If you did not sign up, please disregard this email..<br>" +
            "<br>" +
            $"Sincerely,<br>" +
            $"Pinetree<br>"
        };
        await SendAsync(email, content);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var content = new EmailContent("Pinetree - Reset your password")
        {
            Html = $"Please reset your password by <a href='{resetLink}'>clicking here</a>."
        };
        await SendAsync(email, content);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var content = new EmailContent("Pinetree - Reset your password")
        {
            Html = $"Please reset your password using the following code: {resetCode}."
        };
        await SendAsync(email, content);
    }

    public async Task SendAsync(string email, EmailContent content)
    {
        var emailMessage = new EmailMessage(_senderEmail, email, content);
        try
        {
            var response = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", response);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailAddress}", email);
        }
    }
}

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
        var content = new EmailContent("Confirm your email")
        {
            Html = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."
        };
        await SendAsync(email, content);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var content = new EmailContent("Reset your password")
        {
            Html = $"Please reset your password by <a href='{resetLink}'>clicking here</a>."
        };
        await SendAsync(email, content);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var content = new EmailContent("Reset your password")
        {
            Html = $"Please reset your password using the following code: {resetCode}"
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

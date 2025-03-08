using Azure;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Pinetree.Data;

namespace Pinetree.Components.Account;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly EmailClient _emailClient;

    public EmailSender(IConfiguration configuration)
    {
        var connectionString = configuration["AzureCommunicationServicesConnectionString"];
        _emailClient = new EmailClient(connectionString);
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
        var emailMessage = new EmailMessage("DoNotReply@be0f5132-0b8e-4f6c-945a-a88cb022fcbc.azurecomm.net", email, content);
        try
        {
            var response = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
            Console.WriteLine($"Email sent successfully. MessageId: {response}");
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }
}

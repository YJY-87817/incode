namespace incode2.Services.Users
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string content);
    }
}
namespace Infrastructure.Services;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string displayName, string otp, CancellationToken cancellationToken = default);
}

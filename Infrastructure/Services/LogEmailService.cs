using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

internal sealed class LogEmailService : IEmailService
{
    private readonly ILogger<LogEmailService> _logger;

    public LogEmailService(ILogger<LogEmailService> logger) => _logger = logger;

    public Task SendOtpAsync(string toEmail, string displayName, string otp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV] OTP for {DisplayName} ({Email}): {Code} — valid for 10 minutes.",
            displayName, toEmail, otp);
        return Task.CompletedTask;
    }
}

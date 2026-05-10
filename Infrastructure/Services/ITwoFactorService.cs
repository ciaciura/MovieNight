namespace Infrastructure.Services;

public interface ITwoFactorService
{
    /// <summary>Generates a base32-encoded 160-bit TOTP secret.</summary>
    string GenerateTotpSecret();

    /// <summary>Returns an otpauth:// URI for QR code scanning in authenticator apps.</summary>
    string GetTotpSetupUri(string issuer, string accountName, string base32Secret);

    /// <summary>Validates a 6-digit TOTP code against the stored secret (±1 time window).</summary>
    bool VerifyTotp(string base32Secret, string code);

    /// <summary>Generates a cryptographically random 6-digit OTP and its SHA-256 hash.</summary>
    (string Code, string CodeHash) GenerateEmailOtp();

    /// <summary>Constant-time comparison of a submitted code against a stored SHA-256 hash.</summary>
    bool VerifyEmailOtpHash(string code, string storedHash);
}

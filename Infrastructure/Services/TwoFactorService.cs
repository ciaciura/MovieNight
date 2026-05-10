using System.Security.Cryptography;
using OtpNet;

namespace Infrastructure.Services;

internal sealed class TwoFactorService : ITwoFactorService
{
    public string GenerateTotpSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20); // 160-bit per RFC 4226
        return Base32Encoding.ToString(key);
    }

    public string GetTotpSetupUri(string issuer, string accountName, string base32Secret)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccount = Uri.EscapeDataString(accountName);
        var unpadded = base32Secret.TrimEnd('=');
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={unpadded}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyTotp(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var key = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(key);
            return totp.VerifyTotp(DateTime.UtcNow, code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    public (string Code, string CodeHash) GenerateEmailOtp()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return (code, ComputeHash(code));
    }

    public bool VerifyEmailOtpHash(string code, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var hash = ComputeHash(code.Trim());
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(hash),
            System.Text.Encoding.UTF8.GetBytes(storedHash));
    }

    private static string ComputeHash(string code)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}

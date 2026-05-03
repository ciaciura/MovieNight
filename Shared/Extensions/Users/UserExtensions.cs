namespace Shared.Extensions.Users;

public static class UserExtensions
{
    public static string NormalizeDisplayName(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? string.Empty
            : displayName.Trim().ToUpperInvariant();
    }

    public static string CanonicalizeDisplayName(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? string.Empty
            : displayName.Trim();
    }
}

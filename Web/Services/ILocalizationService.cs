using System.Globalization;

namespace MovieNight.Web.Services;

/// <summary>
/// Service for managing application localization and culture settings.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the localized string for the given resource name.
    /// </summary>
    string GetString(string resourceName);
    
    /// <summary>
    /// Gets the localized string for the given resource name with formatted arguments.
    /// </summary>
    string GetString(string resourceName, params object[] arguments);
    
    /// <summary>
    /// Gets the current culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }
    
    /// <summary>
    /// Sets the current culture and saves to browser storage.
    /// </summary>
    Task SetCultureAsync(string cultureName);
    
    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
}

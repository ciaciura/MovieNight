using System.Globalization;
using Microsoft.Extensions.Localization;

namespace MovieNight.Web.Services;

/// <summary>
/// Service for managing application localization and culture settings.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer _localizer;
    private CultureInfo _currentCulture;
    private readonly IReadOnlyList<CultureInfo> _supportedCultures;

    public LocalizationService(IStringLocalizerFactory localizerFactory)
    {
        _localizer = localizerFactory.Create(typeof(LocalizationResources));
        
        _supportedCultures = new List<CultureInfo>
        {
            new("en-US"),
            new("pl-PL")
        }.AsReadOnly();
        
        _currentCulture = CultureInfo.CurrentCulture;
    }

    public string GetString(string resourceName)
    {
        var localizedString = _localizer[resourceName];
        return localizedString.ResourceNotFound ? resourceName : localizedString.Value;
    }

    public string GetString(string resourceName, params object[] arguments)
    {
        var localizedString = _localizer[resourceName];
        string format = localizedString.ResourceNotFound ? resourceName : localizedString.Value;
        return string.Format(format, arguments);
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set => _currentCulture = value;
    }

    public async Task SetCultureAsync(string cultureName)
    {
        var culture = _supportedCultures.FirstOrDefault(c => c.Name == cultureName) 
            ?? _supportedCultures[0];
        
        CurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public IReadOnlyList<CultureInfo> SupportedCultures => _supportedCultures;
}

/// <summary>
/// Marker class for localization resources.
/// </summary>
public class LocalizationResources
{
}

# Localization Framework Setup

A complete localization (i18n) framework has been added to MovieNight Web project with support for multiple languages: English and Polish.

## Files Created

### Services
- **[Services/ILocalizationService.cs](Services/ILocalizationService.cs)** - Interface for localization
- **[Services/LocalizationService.cs](Services/LocalizationService.cs)** - Implementation with culture management
- **[Components/CultureSelector.razor](Components/CultureSelector.razor)** - Dropdown component to change language

### Resource Files (Translations)
Located in `Resources/` folder:
- `LocalizationResources.resx` - English (default)
- `LocalizationResources.pl-PL.resx` - Polish

## Configuration

The framework is configured in `Program.cs`:
- Localization services registered as scoped dependency
- Request localization middleware configured
- Supported cultures defined
- Cookie-based culture persistence

## Usage

### In Components

```razor
@inject ILocalizationService Localization

<h1>@Localization.GetString("App.Title")</h1>
<p>@Localization.GetString("Auth.Welcome")</p>
<button>@Localization.GetString("Common.Save")</button>
```

### With Formatted Arguments

```razor
@Localization.GetString("Validation.PasswordTooShort")
```

### Add Culture Selector to Navigation

Add the `CultureSelector` component to your `NavMenu.razor` or header:

```razor
<CultureSelector />
```

## Adding New Strings

1. Open `Resources/LocalizationResources.resx` (English version)
2. Add a new key-value pair, e.g., `Features.Movies.Title` = `"Movies"`
3. For each translation file (`en-US`, `pl-PL`), add the same key with the translated value
4. Use in code: `Localization.GetString("Features.Movies.Title")`

## Supported Cultures

| Code | Language | Culture |
|------|----------|---------|
| en-US | English | United States |
| pl-PL | Polish | Poland |

## Persistence

The selected culture is stored in a cookie named "Culture" and will be remembered across sessions.

## Current Resources

The framework includes pre-translated strings for:
- **App** - Application titles
- **Auth** - Authentication screens
- **Common** - Common actions (Save, Delete, Confirm, etc.)
- **Validation** - Validation messages

## Adding More Languages

To add a new language:

1. Create a new `.resx` file: `LocalizationResources.{CultureCode}.resx`
2. Add translations for all keys in the default `.resx` file
3. Update `LocalizationService.cs` to include the new culture in `_supportedCultures` list
4. Update `Program.cs` to add the culture to `supportedCultures` array

Example (adding Polish):
```csharp
new CultureInfo("pl-PL")
```

## Best Practices

1. **Key Naming**: Use hierarchical names like `Feature.Component.String`
2. **No Hardcoding**: Always use `GetString()` for user-facing text
3. **Consistent Keys**: Use the same key across all resource files
4. **Context in Keys**: Include context in key names, e.g., `Auth.LoginButton` vs `Common.LoginButton`
5. **Fallback**: If a key isn't found, the key name itself is returned

## API Integration

For API responses, create localized error messages in shared response contracts and localize them on the frontend using this service.

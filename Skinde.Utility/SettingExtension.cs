using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Skinde.Utility;

public static class SettingExtension
{
    public static bool? _displaySettingSource;

    public static string Setting(this IHostApplicationBuilder builder, string key = "", bool isHideValue = false)
    {
        return Setting(builder.Configuration, key, isHideValue);
    }

    public static string Setting(this IConfigurationManager configuration, string key = "", bool isHideValue = false)
    {
        return Setting((IConfiguration)configuration, key, isHideValue);
    }

    public static string Setting(this IConfiguration configuration, string key = "", bool isHideValue = false)
    {
        try
        {
            _displaySettingSource ??= True(configuration["DisplaySettingSource"]);

            var settingValue = configuration[key];
            var displaySettingValue = $"Setting: {key} = {(settingValue == null ? "(null)" : (settingValue == String.Empty ? "(empty)" : (isHideValue ? "(hidden)" : settingValue)))}";
            if (_displaySettingSource ?? false)
                displaySettingValue += $" ({FindSettingSource(configuration, key)?.Replace("ConfigurationProvider","")})";

            Log.Information(displaySettingValue);
            return settingValue ?? String.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error getting setting: {key} - {ex.Message}");
            return String.Empty;
        }
    }

    private static string? FindSettingSource(IConfiguration configuration, string key)
    {
        if (configuration is not IConfigurationRoot configurationRoot) return "(no-root)";
        foreach (var provider in configurationRoot.Providers.Reverse())
        {
            if (provider.TryGet(key, out _))
            {
                return provider.ToString(); // Stop after finding the first source
            }
        }
        return "(unknown)";
    }

    private static bool True(string? value)
    {
        return value != null && (value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }
}
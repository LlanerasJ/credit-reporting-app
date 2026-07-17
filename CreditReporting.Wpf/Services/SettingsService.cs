using System.IO;
using System.Text.Json;

namespace CreditReporting.Wpf.Services;

/// <summary>User-adjustable application settings, persisted per Windows user.</summary>
public class AppSettings
{
    public bool AutoTimeoutEnabled { get; set; } = true;
    public int AutoTimeoutMinutes { get; set; } = 15;
    public bool RememberUsernameEnabled { get; set; } = false;
    public string? RememberedUsername { get; set; }
    public bool Metro2ExportEnabled { get; set; } = true;
    public bool Metro2ImportEnabled { get; set; } = true;
    public bool ReportingEnabled { get; set; } = true;
}

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON under %APPDATA%.</summary>
public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CreditReporting", "settings.json");

    public AppSettings Current { get; private set; } = Load();

    /// <summary>Raised after settings are saved so live features can pick up changes.</summary>
    public event EventHandler? SettingsChanged;

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        Current = settings;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Stores the username to prefill next sign-in, or clears it when the setting is off.</summary>
    public void RememberUsername(string username)
    {
        AppSettings settings = Current;
        settings.RememberedUsername = settings.RememberUsernameEnabled ? username : null;
        Save(settings);
    }

    private static AppSettings Load()
    {
        AppSettings settings = new();
        try
        {
            if (File.Exists(SettingsPath))
                settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Unreadable or corrupt settings file: fall back to defaults.
        }

        if (settings.AutoTimeoutMinutes < 1)
            settings.AutoTimeoutMinutes = new AppSettings().AutoTimeoutMinutes;
        return settings;
    }
}

using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

/// <summary>Values handed over by the view, since PasswordBox.Password is not bindable.</summary>
public record PasswordChangeValues(string Current, string New, string Confirm);

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly ApiService _api;

    public IReadOnlyList<int> TimeoutOptions { get; } = [1, 5, 10, 15, 30, 60];

    [ObservableProperty] private bool _autoTimeoutEnabled;
    [ObservableProperty] private int _autoTimeoutMinutes;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _passwordStatusMessage = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _passwordChangeSucceeded;
    [ObservableProperty] private bool _metro2ExportEnabled;
    [ObservableProperty] private bool _metro2ImportEnabled;

    public SettingsViewModel(SettingsService settings, ApiService api)
    {
        _settings = settings;
        _api = api;
        // Assign the backing fields so loading the saved values does not immediately re-save.
        _autoTimeoutEnabled = settings.Current.AutoTimeoutEnabled;
        _autoTimeoutMinutes = settings.Current.AutoTimeoutMinutes;
        _metro2ExportEnabled = settings.Current.Metro2ExportEnabled;
        _metro2ImportEnabled = settings.Current.Metro2ImportEnabled;
    }

    // Settings are applied and persisted the moment they change.
    partial void OnAutoTimeoutEnabledChanged(bool value) => SaveSettings(value
        ? $"Saved. You will be signed out after {AutoTimeoutMinutes} minute(s) of inactivity."
        : "Saved. Auto timeout is off.");
    partial void OnAutoTimeoutMinutesChanged(int value) =>
        SaveSettings($"Saved. Auto timeout set to {value} minute(s).");
    partial void OnMetro2ExportEnabledChanged(bool value) =>
        SaveSettings(value ? "Saved. Metro 2 export enabled." : "Saved. Metro 2 export disabled.");
    partial void OnMetro2ImportEnabledChanged(bool value) =>
        SaveSettings(value ? "Saved. Metro 2 import enabled." : "Saved. Metro 2 import disabled.");

    private void SaveSettings(string successMessage)
    {
        try
        {
            _settings.Save(new AppSettings
            {
                AutoTimeoutEnabled = AutoTimeoutEnabled,
                AutoTimeoutMinutes = AutoTimeoutMinutes,
                Metro2ExportEnabled = Metro2ExportEnabled,
                Metro2ImportEnabled = Metro2ImportEnabled
            });
            StatusMessage = successMessage;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync(PasswordChangeValues values)
    {
        PasswordChangeSucceeded = false;

        if (string.IsNullOrEmpty(values.Current) || string.IsNullOrEmpty(values.New) || string.IsNullOrEmpty(values.Confirm))
        {
            PasswordStatusMessage = "All three password fields are required.";
            return;
        }
        if (values.New != values.Confirm)
        {
            PasswordStatusMessage = "New password and confirmation do not match.";
            return;
        }
        if (values.New.Length < 8)
        {
            PasswordStatusMessage = "New password must be at least 8 characters.";
            return;
        }

        IsBusy = true;
        try
        {
            await _api.ChangePasswordAsync(values.Current, values.New);
            PasswordStatusMessage = "Password changed.";
            PasswordChangeSucceeded = true;
        }
        catch (ApiException ex)
        {
            PasswordStatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

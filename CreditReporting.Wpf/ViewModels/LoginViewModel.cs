using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SettingsService _settings;
    private readonly Action _onLoginSucceeded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "analyst";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    public LoginViewModel(ApiService api, SettingsService settings, Action onLoginSucceeded)
    {
        _api = api;
        _settings = settings;
        _onLoginSucceeded = onLoginSucceeded;

        if (settings.Current.RememberUsernameEnabled &&
            !string.IsNullOrWhiteSpace(settings.Current.RememberedUsername))
        {
            _username = settings.Current.RememberedUsername;
        }
    }

    /// <summary>True when the username was prefilled, so the view can focus the password box instead.</summary>
    public bool HasRememberedUsername =>
        _settings.Current.RememberUsernameEnabled &&
        !string.IsNullOrWhiteSpace(_settings.Current.RememberedUsername);

    private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Username);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string? password)
    {
        ErrorMessage = "";
        IsBusy = true;
        try
        {
            string username = Username.Trim();
            await _api.LoginAsync(username, password ?? "");
            TryRememberUsername(username);
            _onLoginSucceeded();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void TryRememberUsername(string username)
    {
        try
        {
            _settings.RememberUsername(username);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A sign-in that succeeded must not fail because settings could not be written.
        }
    }
}

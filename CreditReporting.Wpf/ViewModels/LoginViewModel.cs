using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Action _onLoginSucceeded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "analyst";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    public LoginViewModel(ApiService api, Action onLoginSucceeded)
    {
        _api = api;
        _onLoginSucceeded = onLoginSucceeded;
    }

    private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Username);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string? password)
    {
        ErrorMessage = "";
        IsBusy = true;
        try
        {
            await _api.LoginAsync(Username.Trim(), password ?? "");
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
}

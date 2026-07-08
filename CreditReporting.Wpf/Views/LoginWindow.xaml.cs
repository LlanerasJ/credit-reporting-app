using System.Windows;
using System.Windows.Input;
using CreditReporting.Wpf.Services;
using CreditReporting.Wpf.ViewModels;

namespace CreditReporting.Wpf.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(ApiService api)
    {
        InitializeComponent();
        _viewModel = new LoginViewModel(api, () =>
        {
            new MainWindow(api).Show();
            Close();
        });
        DataContext = _viewModel;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    // PasswordBox.Password is not bindable, so the view hands it to the command here.
    private void Login_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }

    private void TryLogin()
    {
        if (_viewModel.LoginCommand.CanExecute(PasswordBox.Password))
            _viewModel.LoginCommand.Execute(PasswordBox.Password);
    }
}

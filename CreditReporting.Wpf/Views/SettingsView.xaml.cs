using System.Windows;
using System.Windows.Controls;
using CreditReporting.Wpf.ViewModels;

namespace CreditReporting.Wpf.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    // PasswordBox.Password is not bindable, so the view hands the values to the command here.
    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var values = new PasswordChangeValues(
            CurrentPasswordBox.Password, NewPasswordBox.Password, ConfirmPasswordBox.Password);
        if (!vm.ChangePasswordCommand.CanExecute(values)) return;

        await vm.ChangePasswordCommand.ExecuteAsync(values);
        if (vm.PasswordChangeSucceeded)
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }
}

using System.Windows;
using CreditReporting.Wpf.Services;
using CreditReporting.Wpf.Views;

namespace CreditReporting.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var api = new ApiService();
        new LoginWindow(api).Show();
    }
}

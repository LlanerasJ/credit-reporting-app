using System.Windows;
using CreditReporting.Wpf.Services;
using CreditReporting.Wpf.ViewModels;

namespace CreditReporting.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(ApiService api)
    {
        InitializeComponent();
        DataContext = new MainViewModel(api);
    }
}

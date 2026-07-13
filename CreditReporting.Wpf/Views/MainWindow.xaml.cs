using CreditReporting.Wpf.Services;
using CreditReporting.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CreditReporting.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly ApiService _api;
    private readonly SettingsService _settings;
    private readonly DispatcherTimer _inactivityTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private DateTime _lastActivityUtc = DateTime.UtcNow;

    public MainWindow(ApiService api, SettingsService settings)
    {
        InitializeComponent();
        _api = api;
        _settings = settings;
        DataContext = new MainViewModel(api, settings);

        PreviewKeyDown += (_, _) => _lastActivityUtc = DateTime.UtcNow;
        PreviewMouseDown += (_, _) => _lastActivityUtc = DateTime.UtcNow;
        PreviewMouseMove += (_, _) => _lastActivityUtc = DateTime.UtcNow;
        PreviewMouseWheel += (_, _) => _lastActivityUtc = DateTime.UtcNow;

        _inactivityTimer.Tick += (_, _) => CheckInactivity();
        _settings.SettingsChanged += OnSettingsChanged;
        ApplyTimeoutSetting();
    }

    protected override void OnClosed(EventArgs e)
    {
        _inactivityTimer.Stop();
        _settings.SettingsChanged -= OnSettingsChanged;
        (DataContext as MainViewModel)?.Dispose();
        base.OnClosed(e);
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => ApplyTimeoutSetting();

    private void ApplyTimeoutSetting()
    {
        if (_settings.Current.AutoTimeoutEnabled)
            _inactivityTimer.Start();
        else
            _inactivityTimer.Stop();
    }

    private void CheckInactivity()
    {
        var timeout = TimeSpan.FromMinutes(_settings.Current.AutoTimeoutMinutes);
        if (DateTime.UtcNow - _lastActivityUtc < timeout) return;

        _inactivityTimer.Stop();
        _api.Logout();
        new LoginWindow(_api, _settings,
            $"You were signed out after {_settings.Current.AutoTimeoutMinutes} minute(s) of inactivity.").Show();
        Close();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsView.Visibility = Visibility.Visible;
        SettingsView.Focus();
    }

    // Handle hiding the SettingsView when switching away from the Settings tab
    private void SettingsView_LostFocus(object sender, RoutedEventArgs e)
    {
        SettingsView.Visibility = Visibility.Collapsed;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsView.Visibility = Visibility.Collapsed;
    }
}

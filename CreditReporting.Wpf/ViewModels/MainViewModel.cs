using CommunityToolkit.Mvvm.ComponentModel;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settings;

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _metro2ExportVisible;
    [ObservableProperty] private bool _metro2ImportVisible;
    [ObservableProperty] private bool _reportingVisible;

    public string CurrentUser { get; }
    public CustomerSearchViewModel Search { get; }
    public CreditReportViewModel Report { get; }
    public ReportingViewModel Reporting { get; }
    public Metro2ExportViewModel Export { get; }
    public Metro2ImportViewModel Import { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(ApiService api, SettingsService settings)
    {
        _settings = settings;
        CurrentUser = $"{api.Username} ({api.Role})";
        Report = new CreditReportViewModel(api);
        Search = new CustomerSearchViewModel(api, OpenReportAsync);
        Reporting = new ReportingViewModel(api);
        Export = new Metro2ExportViewModel(settings, api);
        Import = new Metro2ImportViewModel(api);
        Settings = new SettingsViewModel(settings, api);

        _metro2ExportVisible = settings.Current.Metro2ExportEnabled;
        _metro2ImportVisible = settings.Current.Metro2ImportEnabled;
        _reportingVisible = settings.Current.ReportingEnabled;
        settings.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        Metro2ExportVisible = _settings.Current.Metro2ExportEnabled;
        Metro2ImportVisible = _settings.Current.Metro2ImportEnabled;
        ReportingVisible = _settings.Current.ReportingEnabled;
    }

    // The settings service outlives this viewmodel, so drop the subscription on close.
    public void Dispose()
    {
        _settings.SettingsChanged -= OnSettingsChanged;
        Settings.Dispose();
    }

    private async Task OpenReportAsync(int customerId)
    {
        SelectedTabIndex = 1; // Credit Report tab
        await Report.LoadAsync(customerId);
    }
}

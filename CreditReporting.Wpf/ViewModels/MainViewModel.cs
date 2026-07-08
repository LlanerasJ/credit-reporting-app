using CommunityToolkit.Mvvm.ComponentModel;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;

    public string CurrentUser { get; }
    public CustomerSearchViewModel Search { get; }
    public CreditReportViewModel Report { get; }
    public Metro2ExportViewModel Export { get; }
    public Metro2ImportViewModel Import { get; }

    public MainViewModel(ApiService api)
    {
        CurrentUser = $"{api.Username} ({api.Role})";
        Report = new CreditReportViewModel(api);
        Search = new CustomerSearchViewModel(api, OpenReportAsync);
        Export = new Metro2ExportViewModel(api);
        Import = new Metro2ImportViewModel(api);
    }

    private async Task OpenReportAsync(int customerId)
    {
        SelectedTabIndex = 1; // Credit Report tab
        await Report.LoadAsync(customerId);
    }
}
